using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LeafSweeper;

/// <summary>Result of one cleared level, kept in the recent history.</summary>
public sealed class LevelResult
{
    public int Level { get; set; }
    public int Swipes { get; set; }
    public int Gusts { get; set; }
    public int Seconds { get; set; }
    public string BugType { get; set; } = "";
    public string ClearedAt { get; set; } = "";
}

/// <summary>
/// Persistent player progress and lifetime statistics, stored as JSON in
/// user://save.json (app-private on Android; no permissions needed).
/// Loads are corrupt-safe: any failure yields a fresh save. Saves are
/// atomic: written to a temp file first, then renamed over the real one.
/// </summary>
public sealed class SaveData
{
    private const string Path = "user://save.json";
    private const string TempPath = "user://save.json.tmp";
    private const int HistoryLimit = 50;

    public int CurrentLevel { get; set; } = 1;
    public int LevelsCleared { get; set; }
    public int TotalSwipes { get; set; }
    public int TotalGusts { get; set; }
    public int TotalSeconds { get; set; }
    public Dictionary<string, int> BugFindCounts { get; } = new();
    public List<LevelResult> History { get; } = new();

    private static Variant Get(Godot.Collections.Dictionary d, string key, Variant fallback) =>
        d.TryGetValue(key, out Variant v) ? v : fallback;

    public static SaveData Load()
    {
        var data = new SaveData();
        if (!FileAccess.FileExists(Path))
            return data;
        using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        if (f == null)
            return data;
        try
        {
            var parsed = Json.ParseString(f.GetAsText());
            if (parsed.VariantType != Variant.Type.Dictionary)
                return data;
            var root = parsed.AsGodotDictionary();

            data.CurrentLevel = Get(root, "currentLevel", 1).AsInt32();
            data.LevelsCleared = Get(root, "levelsCleared", 0).AsInt32();
            data.TotalSwipes = Get(root, "totalSwipes", 0).AsInt32();
            data.TotalGusts = Get(root, "totalGusts", 0).AsInt32();
            data.TotalSeconds = Get(root, "totalSeconds", 0).AsInt32();

            if (Get(root, "bugFindCounts", default).AsGodotDictionary() is { } finds)
                foreach (var kv in finds)
                    data.BugFindCounts[kv.Key.AsString()] = kv.Value.AsInt32();

            if (Get(root, "history", default).AsGodotArray() is { } history)
                foreach (var entry in history)
                {
                    var d = entry.AsGodotDictionary();
                    data.History.Add(new LevelResult
                    {
                        Level = Get(d, "level", 0).AsInt32(),
                        Swipes = Get(d, "swipes", 0).AsInt32(),
                        Gusts = Get(d, "gusts", 0).AsInt32(),
                        Seconds = Get(d, "seconds", 0).AsInt32(),
                        BugType = Get(d, "bugType", "").AsString(),
                        ClearedAt = Get(d, "clearedAt", "").AsString(),
                    });
                }

            if (data.CurrentLevel < 1)
                data.CurrentLevel = 1;
            if (data.LevelsCleared < 0)
                data.LevelsCleared = 0;
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"LeafSweeper: save file unreadable, starting fresh ({e.Message})");
            return new SaveData();
        }

        return data;
    }

    public void Save()
    {
        var finds = new Godot.Collections.Dictionary();
        foreach (var kv in BugFindCounts)
            finds[kv.Key] = (Variant)kv.Value;

        var history = new Godot.Collections.Array();
        foreach (var r in History)
        {
            history.Add(new Godot.Collections.Dictionary
            {
                ["level"] = r.Level,
                ["swipes"] = r.Swipes,
                ["gusts"] = r.Gusts,
                ["seconds"] = r.Seconds,
                ["bugType"] = r.BugType,
                ["clearedAt"] = r.ClearedAt,
            });
        }

        var root = new Godot.Collections.Dictionary
        {
            ["version"] = 1,
            ["currentLevel"] = CurrentLevel,
            ["levelsCleared"] = LevelsCleared,
            ["totalSwipes"] = TotalSwipes,
            ["totalGusts"] = TotalGusts,
            ["totalSeconds"] = TotalSeconds,
            ["bugFindCounts"] = finds,
            ["history"] = history,
        };

        var err = FileAccess.Open(TempPath, FileAccess.ModeFlags.Write);
        if (err == null)
        {
            GD.PushError($"LeafSweeper: cannot write save file ({FileAccess.GetOpenError()})");
            return;
        }
        err.StoreString(Json.Stringify(root, "  "));
        err.Flush();
        err.Dispose();

        // Atomic swap: only rename over the real file once the temp copy is complete.
        using var dir = DirAccess.Open("user://");
        dir?.Rename(TempPath, Path);
    }

    /// <summary>Records a cleared level, updates aggregates, and saves.</summary>
    public void RecordClear(int level, int swipes, int seconds, string bugType, int gusts)
    {
        LevelsCleared++;
        TotalSwipes += swipes;
        TotalGusts += gusts;
        TotalSeconds += seconds;
        BugFindCounts.TryGetValue(bugType, out var count);
        BugFindCounts[bugType] = count + 1;

        History.Add(new LevelResult
        {
            Level = level,
            Swipes = swipes,
            Gusts = gusts,
            Seconds = seconds,
            BugType = bugType,
            ClearedAt = Time.GetDatetimeStringFromSystem(),
        });
        while (History.Count > HistoryLimit)
            History.RemoveAt(0);

        CurrentLevel = level + 1;
        Save();
    }

    public int BestSwipes() => History.Count == 0 ? 0 : History.Min(r => r.Swipes);

    public void Reset()
    {
        CurrentLevel = 1;
        LevelsCleared = 0;
        TotalSwipes = 0;
        TotalGusts = 0;
        TotalSeconds = 0;
        BugFindCounts.Clear();
        History.Clear();
        Save();
    }
}
