using System;
using Godot;

namespace LeafSweeper;

/// <summary>
/// In-level HUD (level number, swipe counter) plus the win overlay with the
/// between-round stats comment and Next/Menu buttons.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _levelLabel = null!;
    private Label _swipeLabel = null!;

    private Control _winOverlay = null!;
    private Label _winTitle = null!;
    private Label _winComment = null!;
    private Label _winStats = null!;
    private Button _nextButton = null!;
    private Button _menuButton = null!;

    public override void _Ready()
    {
        AddChild(BuildTopBar());

        _winOverlay = BuildWinOverlay();
        _winOverlay.Visible = false;
        AddChild(_winOverlay);
    }

    private Control BuildTopBar()
    {
        _levelLabel = MakeLabel(42, true);
        _swipeLabel = MakeLabel(42, true);
        _swipeLabel.HorizontalAlignment = HorizontalAlignment.Right;

        var row = new MarginContainer();
        row.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        row.AddThemeConstantOverride("margin_left", 36);
        row.AddThemeConstantOverride("margin_right", 36);
        row.AddThemeConstantOverride("margin_top", 28);

        var box = new HBoxContainer();
        _levelLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _swipeLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        box.AddChild(_levelLabel);
        box.AddChild(_swipeLabel);
        row.AddChild(box);
        return row;
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

        _winComment = MakeLabel(38, false, new Color("4a3a26"));
        _winComment.HorizontalAlignment = HorizontalAlignment.Center;

        _winStats = MakeLabel(34, true, new Color("6b5233"));
        _winStats.HorizontalAlignment = HorizontalAlignment.Center;

        _nextButton = MakeButton("Next level", new Color("6f9a44"));
        _nextButton.Pressed += () => NextPressed?.Invoke();

        _menuButton = MakeButton("Main menu", new Color("a08a68"));
        _menuButton.Pressed += () => MenuPressed?.Invoke();

        box.AddChild(_winTitle);
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

    public void ShowLevel(int level) => _levelLabel.Text = $"Level {level}";

    public void ShowSwipes(int swipes) => _swipeLabel.Text = $"{swipes} swipes";

    public void ShowWin(string comment, string statsLine)
    {
        _winComment.Text = comment;
        _winStats.Text = statsLine;
        _winOverlay.Visible = true;
        _winOverlay.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_winOverlay, "modulate:a", 1f, 0.35f);
    }

    public void HideWin() => _winOverlay.Visible = false;

    public void SetInLevelVisible(bool visible)
    {
        _levelLabel.Visible = visible;
        _swipeLabel.Visible = visible;
    }

    // ------------------------------------------------------------ helpers --

    private static Control Spacer(float height) =>
        new() { CustomMinimumSize = new Vector2(0, height) };

    internal static Label MakeLabel(int size, bool bold, Color? color = null)
    {
        return new Label
        {
            LabelSettings = new LabelSettings
            {
                FontSize = size,
                FontColor = color ?? new Color("fff8ec"),
                OutlineSize = Math.Max(6, size / 5),
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
