using System;
using Godot;

namespace LeafSweeper;

/// <summary>
/// In-game HUD: a permanent bottom dock (level/swipes labels + wind/restart
/// controls) plus the win overlay with the between-round stats comment and
/// Next/Menu buttons. The dock's fixed height also defines the playable
/// area — the floor never extends underneath it.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>
    /// Dock height in design pixels. Main subtracts this from the viewport
    /// to get the playable rect, so debris, the bug and the ground clamp all
    /// stay mutually exclusive with the dock.
    /// </summary>
    public const float DockHeight = 300f;

    private Control _dock = null!;
    private Label _levelLabel = null!;
    private Label _swipeLabel = null!;
    private Button _windButton = null!;
    private Button _restartButton = null!;

    private Control _winOverlay = null!;
    private Control _bugSlot = null!;
    private Label _winTitle = null!;
    private Label _winComment = null!;
    private Label _winStats = null!;
    private Button _nextButton = null!;
    private Button _menuButton = null!;

    private Control _restartDialog = null!;

    public override void _Ready()
    {
        _dock = BuildDock();
        _dock.Visible = false;
        AddChild(_dock);

        _winOverlay = BuildWinOverlay();
        _winOverlay.Visible = false;
        AddChild(_winOverlay);

        _restartDialog = BuildRestartDialog();
        _restartDialog.Visible = false;
        AddChild(_restartDialog);
    }

    private Control BuildDock()
    {
        _levelLabel = MakeLabel(52, true, new Color("3f5228"), outlineSize: 0);
        _swipeLabel = MakeLabel(52, true, new Color("6b5233"), outlineSize: 0);
        _swipeLabel.HorizontalAlignment = HorizontalAlignment.Right;

        _windButton = MakeIconButton("res://assets/icons/wind.svg", new Color("6f9a44"));
        _windButton.TooltipText = "Gust: blow away some debris";
        _windButton.Pressed += () => WindPressed?.Invoke();

        _restartButton = MakeIconButton("res://assets/icons/restart.svg", new Color("a08a68"));
        _restartButton.TooltipText = "Restart this level";
        _restartButton.Pressed += ShowRestartDialog;

        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color("f7f0e1"),
            BorderWidthTop = 6,
            BorderColor = new Color("c9a06a"),
        };
        panel.AddThemeStyleboxOverride("panel", style);
        // Swallows touches so sweeping can never act through the dock.
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var row = new MarginContainer();
        row.AddThemeConstantOverride("margin_left", 36);
        row.AddThemeConstantOverride("margin_right", 36);

        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 28);
        _levelLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _levelLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        _swipeLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _swipeLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        box.AddChild(_levelLabel);
        box.AddChild(_swipeLabel);
        box.AddChild(_windButton);
        box.AddChild(_restartButton);
        row.AddChild(box);
        panel.AddChild(row);

        panel.CustomMinimumSize = new Vector2(0, DockHeight);
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        return panel;
    }

    private Control BuildWinOverlay()
    {
        var dim = new ColorRect { Color = new Color(0.12f, 0.09f, 0.05f, 0.55f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color("f7f0e1"),
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            ContentMarginLeft = 48,
            ContentMarginRight = 48,
            ContentMarginTop = 40,
            ContentMarginBottom = 40,
            BorderWidthBottom = 6,
            BorderWidthTop = 6,
            BorderWidthLeft = 6,
            BorderWidthRight = 6,
            BorderColor = new Color("c9a06a"),
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var box = new VBoxContainer { CustomMinimumSize = new Vector2(640, 0) };
        box.AddThemeConstantOverride("separation", 18);

        _winTitle = MakeLabel(64, true, new Color("3f5228"));
        _winTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _winTitle.Text = "Bug found!";

        // The celebrated bug is seated here, between the title and the stats.
        _bugSlot = new Control { CustomMinimumSize = new Vector2(0, 240) };

        _winComment = MakeLabel(38, false, new Color("4a3a26"));
        _winComment.HorizontalAlignment = HorizontalAlignment.Center;

        _winStats = MakeLabel(34, true, new Color("6b5233"));
        _winStats.HorizontalAlignment = HorizontalAlignment.Center;

        _nextButton = MakeButton("Next level", new Color("6f9a44"));
        _nextButton.Pressed += () => NextPressed?.Invoke();

        _menuButton = MakeButton("Main menu", new Color("a08a68"));
        _menuButton.Pressed += () => MenuPressed?.Invoke();

        box.AddChild(_winTitle);
        box.AddChild(_bugSlot);
        box.AddChild(_winComment);
        box.AddChild(_winStats);
        box.AddChild(Spacer(8));
        box.AddChild(_nextButton);
        box.AddChild(Spacer(2));
        box.AddChild(_menuButton);
        panel.AddChild(box);
        center.AddChild(panel);
        dim.AddChild(center);

        return dim;
    }

    public event Action? NextPressed;
    public event Action? MenuPressed;
    public event Action? WindPressed;
    public event Action? RestartConfirmed;

    public void ShowLevel(int level) => _levelLabel.Text = $"Level {level}";

    public void ShowSwipes(int swipes) => _swipeLabel.Text = $"{swipes} swipes";

    public void ShowWin(string comment, string statsLine, Node2D? bug = null)
    {
        if (bug != null)
            SeatBug(bug);
        _winComment.Text = comment;
        _winStats.Text = statsLine;
        _winOverlay.Visible = true;
        _winOverlay.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_winOverlay, "modulate:a", 1f, 0.35f);
    }

    /// <summary>
    /// Moves the celebrated bug into the win card slot between the title and
    /// the stats, easing it from wherever it landed to the slot center. The
    /// tween re-reads the slot's global rect every step, so resizes while the
    /// card fades in can't strand the bug off-center.
    /// </summary>
    private void SeatBug(Node2D bug)
    {
        bug.Reparent(_bugSlot);
        Vector2 from = bug.GlobalPosition;
        var tween = bug.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                Vector2 target = _bugSlot.GetGlobalRect().GetCenter();
                bug.GlobalPosition = from.Lerp(target, t);
            }), 0.0f, 1.0f, 0.3f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public void HideWin() => _winOverlay.Visible = false;

    /// <summary>The dock lives on the play screen; the menu hides it.</summary>
    public void SetDockVisible(bool visible) => _dock.Visible = visible;

    public void ShowRestartDialog()
    {
        _restartDialog.Visible = true;
        _restartDialog.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_restartDialog, "modulate:a", 1f, 0.2f);
    }

    public void HideRestartDialog() => _restartDialog.Visible = false;

    private Control BuildRestartDialog()
    {
        var dim = new ColorRect { Color = new Color(0.12f, 0.09f, 0.05f, 0.55f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color("f7f0e1"),
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            ContentMarginLeft = 48,
            ContentMarginRight = 48,
            ContentMarginTop = 40,
            ContentMarginBottom = 40,
            BorderWidthBottom = 6,
            BorderWidthTop = 6,
            BorderWidthLeft = 6,
            BorderWidthRight = 6,
            BorderColor = new Color("c9a06a"),
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var box = new VBoxContainer { CustomMinimumSize = new Vector2(640, 0) };
        box.AddThemeConstantOverride("separation", 16);

        var title = MakeLabel(54, true, new Color("3f5228"));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Text = "Restart this level?";

        var hint = MakeLabel(32, false, new Color("4a3a26"));
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.Text = "The debris will be re-scattered and your\nswipe count for this round resets.";

        var restartButton = MakeButton("Restart", new Color("6f9a44"));
        restartButton.Pressed += () =>
        {
            HideRestartDialog();
            RestartConfirmed?.Invoke();
        };

        var cancelButton = MakeButton("Cancel", new Color("a08a68"));
        cancelButton.Pressed += HideRestartDialog;

        box.AddChild(title);
        box.AddChild(hint);
        box.AddChild(Spacer(6));
        box.AddChild(restartButton);
        box.AddChild(cancelButton);
        panel.AddChild(box);
        center.AddChild(panel);
        dim.AddChild(center);

        return dim;
    }

    // ------------------------------------------------------------ helpers --

    private static Control Spacer(float height) =>
        new() { CustomMinimumSize = new Vector2(0, height) };

    /// <summary>Square icon button with a cozy panel behind the artwork.</summary>
    private Button MakeIconButton(string iconPath, Color accent)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.75f),
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            BorderWidthBottom = 6,
            BorderWidthTop = 6,
            BorderWidthLeft = 6,
            BorderWidthRight = 6,
            BorderColor = accent,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
        };
        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = accent;
        var disabled = (StyleBoxFlat)normal.Duplicate();
        disabled.BgColor = new Color(1f, 1f, 1f, 0.3f);

        var button = new Button
        {
            CustomMinimumSize = new Vector2(252, 252),
            Icon = GD.Load<Texture2D>(iconPath),
            IconAlignment = HorizontalAlignment.Center,
            ExpandIcon = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", normal);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", disabled);
        return button;
    }

    internal static Label MakeLabel(int size, bool bold, Color? color = null,
        int outlineSize = -1)
    {
        return new Label
        {
            LabelSettings = new LabelSettings
            {
                FontSize = size,
                FontColor = color ?? new Color("fff8ec"),
                OutlineSize = outlineSize >= 0 ? outlineSize : Math.Max(6, size / 5),
                OutlineColor = new Color(0.22f, 0.16f, 0.09f, 0.9f),
            },
        };
    }

    internal static Button MakeButton(string text, Color bg)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            ContentMarginLeft = 40,
            ContentMarginRight = 40,
            ContentMarginTop = 18,
            ContentMarginBottom = 18,
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = bg.Lightened(0.12f);
        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = bg.Darkened(0.12f);

        var button = new Button { Text = text };
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", new Color("fff8ec"));
        button.AddThemeColorOverride("font_hover_color", new Color("ffffff"));
        button.AddThemeFontSizeOverride("font_size", 42);
        return button;
    }
}
