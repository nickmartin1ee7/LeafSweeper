using System;
using System.Text;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Compares the installed game version against the latest GitHub release and
/// raises UpdateAvailable when a newer release exists. Every failure path
/// (offline, HTTP error, rate limit, unexpected payload) is silent — the
/// notification is a nice-to-have that must never disturb play. Diagnostics
/// go to the log with an "UPDATE" prefix so headless runs and logcat can
/// grep for them.
/// </summary>
public partial class UpdateChecker : Node
{
    /// <summary>Where the "Update Available" line sends the player.</summary>
    public const string ReleasesUrl = "https://github.com/nickmartin1ee7/LeafSweeper/releases";

    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/nickmartin1ee7/LeafSweeper/releases/latest";

    // A dead network must not leave a request hanging forever; 10s is
    // generous on mobile data and keeps airplane-mode boots snappy.
    private const float RequestTimeoutSeconds = 10f;

    /// <summary>Raised with the newest release version, e.g. "v0.2.0".</summary>
    public event Action<string>? UpdateAvailable;

    /// <summary>Raised with the installed version when no newer release exists, e.g. "v0.0.1".</summary>
    public event Action<string>? UpToDate;

    /// <summary>Raised when the check could not complete (offline, blocked, bad payload).</summary>
    public event Action? CheckFailed;

    public override void _Ready()
    {
        try
        {
            string installed = InstalledVersion();

            // Test hook: skip the network and pretend the given tag is the
            // latest release (documented testing surface, like LEAF_STORM);
            // "fail" simulates a dead check so the failure state can be
            // exercised headlessly.
            string fake = OS.GetEnvironment("LEAF_FAKE_UPDATE");
            if (!string.IsNullOrEmpty(fake))
            {
                if (fake == "fail")
                    throw new InvalidOperationException(
                        "simulated via LEAF_FAKE_UPDATE=fail");
                ReportIfNewer(fake, installed);
                return;
            }

            var http = new HttpRequest { Name = "LatestReleaseRequest" };
            http.Timeout = RequestTimeoutSeconds;
            http.RequestCompleted += OnRequestCompleted;
            AddChild(http);

            // GitHub's API requires a User-Agent on every request.
            Error error = http.Request(LatestReleaseApiUrl,
                new[] { $"User-Agent: LeafSweeper/{installed}" });
            if (error != Error.Ok)
                ReportCheckFailed($"request could not start: {error}");
        }
        catch (Exception e)
        {
            ReportCheckFailed($"{e.GetType().Name}: {e.Message}");
        }
    }

    private void OnRequestCompleted(long result, long responseCode,
        string[] headers, byte[] body)
    {
        try
        {
            if (result != (long)HttpRequest.Result.Success)
            {
                ReportCheckFailed($"request result {result}");
                return;
            }
            if (responseCode != 200)
            {
                ReportCheckFailed($"HTTP {responseCode}");
                return;
            }

            string? tag = ExtractTagName(Encoding.UTF8.GetString(body));
            if (tag == null)
            {
                ReportCheckFailed("no tag_name in response");
                return;
            }
            ReportIfNewer(tag, InstalledVersion());
        }
        catch (Exception e)
        {
            ReportCheckFailed($"{e.GetType().Name}: {e.Message}");
        }
    }

    private void ReportIfNewer(string latestTag, string installed)
    {
        if (!TryParseVersion(latestTag, out Version latest))
        {
            ReportCheckFailed($"unreadable tag '{latestTag}'");
            return;
        }

        // A missing or unreadable installed version shouldn't hide updates
        // — only a readable, >= latest install suppresses the line.
        if (TryParseVersion(installed, out Version current) && latest <= current)
        {
            string installedDisplay = "v" + current;
            GD.Print($"UPDATE up to date: installed {current}, latest {latest}");
            UpToDate?.Invoke(installedDisplay);
            return;
        }

        string display = "v" + latest;
        GD.Print($"UPDATE available: {display} (installed {installed})");
        UpdateAvailable?.Invoke(display);
    }

    private static string? ExtractTagName(string json)
    {
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Dictionary)
            return null;
        var dict = parsed.AsGodotDictionary();
        return dict.ContainsKey("tag_name") ? dict["tag_name"].AsString() : null;
    }

    private static bool TryParseVersion(string text, out Version version)
    {
        // Release tags may be "v0.2.0" or "0.2.0-beta"; System.Version only
        // reads the numeric core, so trim the prefix and prerelease suffix.
        text = text?.Trim().Split('-')[0].TrimStart('v', 'V') ?? string.Empty;
        return System.Version.TryParse(text, out version!);
    }

    /// <summary>Single funnel for every dead-check path: log + notify.</summary>
    private void ReportCheckFailed(string reason)
    {
        GD.Print($"UPDATE check failed: {reason}");
        CheckFailed?.Invoke();
    }

    private static string InstalledVersion() =>
        ProjectSettings.GetSetting("application/config/version", true).AsString();
}
