using System;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Title screen: game name, cozy subtitle, Play (resume) / New game buttons
/// and a small line of lifetime progress from the save file.
/// </summary>
public partial class MainMenu : CanvasLayer
{
    private Label _progressLabel = null!;
    private Button _playButton = null!;
    private Button _newGameButton = null!;
    private LinkButton _updateLink = null!;

    public event Action? PlayPressed;
    public event Action? NewGamePressed;

    public override void _Ready()
    {
        var dim = new ColorRect { Color = new Color(0.12f, 0.09f, 0.05f, 0.42f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var box = new VBoxContainer { CustomMinimumSize = new Vector2(720, 0) };
        box.AddThemeConstantOverride("separation", 22);

        var title = Hud.MakeLabel(132, true, new Color("fff3d9"));
        title.Text = "LeafSweeper";
        title.HorizontalAlignment = HorizontalAlignment.Center;

        var subtitle = Hud.MakeLabel(48, false, new Color("f0e2c4"));
        subtitle.Text = "A cozy little forest sweeping bug hunt";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;

        _progressLabel = Hud.MakeLabel(42, true, new Color("f5e8cd"));
        _progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
        // Long lifetime + favorite-critter lines must wrap, never run off
        // the screen edge.
        _progressLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        _playButton = Hud.MakeButton("Play", new Color("6f9a44"), 56);
        _playButton.Pressed += () => PlayPressed?.Invoke();

        _newGameButton = Hud.MakeButton("New game", new Color("a08a68"), 56);
        _newGameButton.Pressed += () => NewGamePressed?.Invoke();

        _updateLink = MakeUpdateLink();
        _updateLink.Pressed += OnUpdateLinkPressed;

        var checker = new UpdateChecker { Name = "UpdateChecker" };
        checker.UpdateAvailable += ShowUpdate;

        box.AddChild(title);
        box.AddChild(subtitle);
        box.AddChild(Spacer());
        box.AddChild(_progressLabel);
        box.AddChild(Spacer());
        box.AddChild(_playButton);
        box.AddChild(Spacer());
        box.AddChild(_newGameButton);
        box.AddChild(Spacer());
        box.AddChild(_updateLink);

        center.AddChild(box);
        dim.AddChild(center);
        AddChild(dim);
        AddChild(checker);
    }

    /// <summary>
    /// The tappable "Update Available" line: styled like the menu's outlined
    /// labels but in a warmer gold so it reads as a link; hidden until the
    /// update checker confirms a newer release. LinkButton.Uri opens the
    /// releases page in the browser via OS.ShellOpen on press.
    /// </summary>
    private static LinkButton MakeUpdateLink()
    {
        var link = new LinkButton
        {
            Visible = false,
            Uri = UpdateChecker.ReleasesUrl,
            FocusMode = Control.FocusModeEnum.None,
            // LinkButton paints its text at the control's left edge (no
            // alignment property), so let the control shrink to the text
            // and let the container center it instead.
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        link.AddThemeFontSizeOverride("font_size", 40);
        link.AddThemeColorOverride("font_color", new Color("e8cf8e"));
        link.AddThemeColorOverride("font_hover_color", new Color("ffe9a8"));
        link.AddThemeColorOverride("font_pressed_color", new Color("ffe9a8"));
        link.AddThemeColorOverride("font_outline_color", new Color(0.22f, 0.16f, 0.09f, 0.9f));
        link.AddThemeConstantOverride("outline_size", 8);
        return link;
    }

    /// <summary>Shows the update line once a newer release is confirmed.</summary>
    private void ShowUpdate(string latestVersion)
    {
        _updateLink.Text = $"🌐 Update Available ({latestVersion})";
        _updateLink.Visible = true;
    }

    private void OnUpdateLinkPressed() =>
        GD.Print($"UPDATE link pressed: {UpdateChecker.ReleasesUrl}");

    private static Control Spacer() =>
        new() { CustomMinimumSize = new Vector2(0, 10) };

    /// <summary>Refreshes the progress line from the save file.</summary>
    public void Refresh(SaveData save)
    {
        _playButton.Text = save.LevelsCleared == 0
            ? "Play"
            : $"Play — Level {save.CurrentLevel}";
        _newGameButton.Visible = save.LevelsCleared > 0;

        string lifetime = save.LevelsCleared == 0
            ? string.Empty
            : $"{save.LevelsCleared} bug{(save.LevelsCleared == 1 ? "" : "s")} found · " +
              $"{LevelStats.FormatTime(save.TotalSeconds)} of sweeping";
        if (save.TotalGusts > 0)
            lifetime += $" · {save.TotalGusts} gust{(save.TotalGusts == 1 ? "" : "s")} blown";

        string favorite = FavoriteBug(save);
        _progressLabel.Text = favorite == null ? lifetime : $"{lifetime}\nFavorite critter: {favorite}";
    }

    private static string? FavoriteBug(SaveData save)
    {
        string? best = null;
        int bestCount = 0;
        foreach (var (id, count) in save.BugFindCounts)
        {
            if (count > bestCount)
            {
                bestCount = count;
                best = id;
            }
        }
        return best == null ? null : BugTypes.ById(best).DisplayName + $" ×{bestCount}";
    }
}
