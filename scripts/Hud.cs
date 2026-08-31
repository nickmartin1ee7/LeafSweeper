using System;
using Godot;

namespace LeafSweeper;

/// <summary>
/// In-game HUD: a wood dock along the bottom (bug book + gust/restart coin
/// buttons), the level label at the top-left, the sweeps counter label at the
/// top-right, and the win overlay with the between-round stats comment and
/// Next/Menu buttons. The dock's fixed height also defines the playable
/// area — nothing spawns underneath it, though swept debris may drift over.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>
    /// Dock height in design pixels. Main subtracts this from the viewport
    /// to get the playable rect where debris and the bug may spawn.
    /// </summary>
    public const float DockHeight = 300f;

    private Control _dock = null!;
    private Label _levelLabel = null!;
    private Label _sweepLabel = null!;
    private Button _bookButton = null!;
    private Button _windButton = null!;
    private Button _restartButton = null!;
    private Panel _gustBadge = null!;
    private Label _gustBadgeLabel = null!;
    private StyleBoxFlat _gustBadgeStyle = null!;

    private Control _winOverlay = null!;
    private Control _bugSlot = null!;
    private Label _winTitle = null!;
    private Label _winComment = null!;
    private Label _winStats = null!;
    private HBoxContainer _statsRow = null!;
    private Button _nextButton = null!;
    private Button _menuButton = null!;
    private PanelContainer _winPanel = null!;
    private StyleBoxFlat _winStyle = null!;
    private StyleBoxFlat _winGrandStyle = null!;
    private PrismaticGlow? _prismaticGlow;
    private Tween? _prismaticTitleLoop;

    private Control _restartDialog = null!;

    /// <summary>One value + caption cell in the win card's stats row.</summary>
    public readonly record struct WinStat(string Value, string Caption, bool Highlight);

    public override void _Ready()
    {
        _levelLabel = BuildLevelLabel();
        _levelLabel.Visible = false;
        AddChild(_levelLabel);

        _sweepLabel = BuildSweepsLabel();
        _sweepLabel.Visible = false;
        AddChild(_sweepLabel);

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

    /// <summary>Level indicator at the top-left corner.</summary>
    private Label BuildLevelLabel()
    {
        var label = MakeLabel(60, true);
        label.HorizontalAlignment = HorizontalAlignment.Left;
        // Explicit anchors: full width, pinned to the top, text hugging the
        // left edge with a notch-safe margin. (Anchor presets leave offsets
        // untouched, which silently produced zero-height rects before.)
        label.AnchorLeft = 0f;
        label.AnchorTop = 0f;
        label.AnchorRight = 1f;
        label.AnchorBottom = 0f;
        label.OffsetLeft = 36f;
        label.OffsetRight = 0f;
        label.OffsetTop = 24f;
        label.OffsetBottom = 130f;
        return label;
    }

    /// <summary>
    /// Sweep counter as plain text pinned to the top-right corner, styled
    /// like the level label, out of the dock's way and clear of the level
    /// label on the left.
    /// </summary>
    private Label BuildSweepsLabel()
    {
        _sweepLabel = MakeLabel(60, true);
        _sweepLabel.HorizontalAlignment = HorizontalAlignment.Right;
        // Explicit anchors, mirroring the level label: full width, pinned to
        // the top, text hugging the right edge with a notch-safe margin.
        // (Anchor presets leave offsets untouched, which silently produced
        // zero-height rects before.)
        _sweepLabel.AnchorLeft = 0f;
        _sweepLabel.AnchorTop = 0f;
        _sweepLabel.AnchorRight = 1f;
        _sweepLabel.AnchorBottom = 0f;
        _sweepLabel.OffsetLeft = 0f;
        _sweepLabel.OffsetRight = -24f;
        _sweepLabel.OffsetTop = 24f;
        _sweepLabel.OffsetBottom = 130f;
        return _sweepLabel;
    }

    private Control BuildDock()
    {
        _bookButton = MakeCoinButton("res://assets/icons/book.svg");
        _bookButton.TooltipText = "Open the bug book";
        _bookButton.Pressed += () => BookPressed?.Invoke();

        _windButton = MakeCoinButton("res://assets/icons/wind.svg");
        _windButton.TooltipText = "Gust: blow away some debris";
        _windButton.Pressed += () => WindPressed?.Invoke();
        // The persistent gust power balance, shown as a small counter circle
        // pinned to the gust coin's top-right (like a coin stack badge).
        _gustBadge = BuildGustBadge();
        _windButton.AddChild(_gustBadge);

        _restartButton = MakeCoinButton("res://assets/icons/restart.svg");
        _restartButton.TooltipText = "Restart this level";
        _restartButton.Pressed += ShowRestartDialog;

        var dock = new Control();
        // Swallows touches so sweeping can never act through the dock.
        dock.MouseFilter = Control.MouseFilterEnum.Stop;
        // Explicit anchors and offsets: a full-width tray of DockHeight
        // pinned to the bottom edge of the screen.
        dock.AnchorLeft = 0f;
        dock.AnchorTop = 1f;
        dock.AnchorRight = 1f;
        dock.AnchorBottom = 1f;
        dock.OffsetLeft = 0f;
        dock.OffsetRight = 0f;
        dock.OffsetTop = -DockHeight;
        dock.OffsetBottom = 0f;
        dock.GrowHorizontal = Control.GrowDirection.Both;
        dock.GrowVertical = Control.GrowDirection.Begin;

        var wood = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/textures/wood.svg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wood.AnchorLeft = 0f;
        wood.AnchorTop = 0f;
        wood.AnchorRight = 1f;
        wood.AnchorBottom = 1f;
        dock.AddChild(wood);

        var row = new MarginContainer();
        row.AddThemeConstantOverride("margin_left", 48);
        row.AddThemeConstantOverride("margin_right", 48);
        row.AddThemeConstantOverride("margin_top", 24);
        row.AddThemeConstantOverride("margin_bottom", 24);
        row.AnchorLeft = 0f;
        row.AnchorTop = 0f;
        row.AnchorRight = 1f;
        row.AnchorBottom = 1f;

        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 28);
        // All three columns are equal expand-fill slots, so the gust coin
        // sits exactly on the dock's center no matter how wide the side
        // coins are.
        var bookSlot = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        bookSlot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bookSlot.AddChild(_bookButton);
        box.AddChild(bookSlot);
        var gustSlot = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        gustSlot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        gustSlot.AddChild(_windButton);
        box.AddChild(gustSlot);
        var rightSlot = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rightSlot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rightSlot.AddChild(_restartButton);
        box.AddChild(rightSlot);
        row.AddChild(box);
        dock.AddChild(row);

        return dock;
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
        _winStyle = panelStyle;
        _winGrandStyle = new StyleBoxFlat
        {
            // Prismatic finds lift the card toward light: a near-white
            // porcelain instead of the everyday cream, so the prismatic
            // glow and the gold rim read against a brighter panel.
            BgColor = new Color("fffdf2"),
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            ContentMarginLeft = 48,
            ContentMarginRight = 48,
            ContentMarginTop = 40,
            ContentMarginBottom = 40,
            BorderWidthBottom = 8,
            BorderWidthTop = 8,
            BorderWidthLeft = 8,
            BorderWidthRight = 8,
            BorderColor = new Color("d89a2a"),
        };
        _winPanel = panel;
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
        // Wrap long flavor lines instead of letting their minimum width
        // stretch the card past the screen edge.
        _winComment.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        _winStats = MakeLabel(34, true, new Color("6b5233"));
        _winStats.HorizontalAlignment = HorizontalAlignment.Center;
        _winStats.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        // Lifetime summary row: three value/caption cells filled in by ShowWin.
        _statsRow = new HBoxContainer();
        _statsRow.AddThemeConstantOverride("separation", 12);

        _nextButton = MakeButton("Next level", new Color("6f9a44"));
        _nextButton.Pressed += () => NextPressed?.Invoke();

        _menuButton = MakeButton("Main menu", new Color("a08a68"));
        _menuButton.Pressed += () => MenuPressed?.Invoke();

        box.AddChild(_winTitle);
        box.AddChild(_bugSlot);
        box.AddChild(_winComment);
        box.AddChild(_winStats);
        box.AddChild(_statsRow);
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
    public event Action? BookPressed;
    public event Action? WindPressed;
    public event Action? RestartConfirmed;

    /// <summary>Level tag with its season, e.g. "Level 120 · Summer".</summary>
    public void ShowLevel(int level, RoundConfig.Season season) =>
        _levelLabel.Text = $"Level {level} · {RoundConfig.SeasonName(season)}";

    /// <summary>The current level tag text, for the autoplay probe.</summary>
    public string LevelText => _levelLabel.Text;

    public void ShowSweeps(int sweeps) => _sweepLabel.Text = $"{sweeps} Sweeps";

    /// <summary>
    /// Updates the gust power counter on the dock and greys the gust button
    /// out once the balance runs dry.
    /// </summary>
    public void ShowGustPower(int power)
    {
        _gustBadgeLabel.Text = $"×{power}";
        _windButton.Disabled = power <= 0;
    }

    /// <summary>Gust button center in screen coordinates, the coin flight's target.</summary>
    public Vector2 WindButtonCenter => _windButton.GetGlobalRect().GetCenter();

    /// <summary>
    /// Dramatic arrival beat as a gust coin's power lands: the badge pops
    /// with a golden flash while the counter ticks up, a gold ring and
    /// sparks burst out of the button and the button itself glitters.
    /// </summary>
    public void PulseGustPower()
    {
        _gustBadge.PivotOffset = _gustBadge.Size / 2f;
        var pop = CreateTween();
        pop.TweenProperty(_gustBadge, "scale", Vector2.One * 1.55f, 0.15f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        pop.TweenProperty(_gustBadge, "scale", Vector2.One, 0.35f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        var goldFlash = new Color(1f, 0.88f, 0.4f);
        var flash = CreateTween();
        flash.TweenProperty(_gustBadgeStyle, "bg_color", goldFlash, 0.15f);
        flash.Parallel().TweenProperty(_gustBadgeStyle, "border_color", goldFlash.Darkened(0.3f), 0.15f);
        flash.TweenProperty(_gustBadgeStyle, "bg_color", new Color("8a5c17"), 0.4f);
        flash.Parallel().TweenProperty(_gustBadgeStyle, "border_color", new Color("5f3f0e"), 0.4f);

        GoldBurst(WindButtonCenter);

        var glitter = CreateTween();
        glitter.TweenProperty(_windButton, "modulate", new Color(1.45f, 1.25f, 0.75f), 0.12f);
        glitter.TweenProperty(_windButton, "modulate", Colors.White, 0.4f);
    }

    /// <summary>Expanding gold ring plus flying sparks bursting from the button.</summary>
    private void GoldBurst(Vector2 center)
    {
        var ring = new Line2D
        {
            Width = 7f,
            DefaultColor = new Color(1f, 0.85f, 0.3f, 0.95f),
            Position = center,
            ZIndex = 60,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        const int segments = 28;
        var points = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++)
            points[i] = Vector2.Right.Rotated(i * Mathf.Tau / segments) * 46f;
        ring.Points = points;
        AddChild(ring);

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(ring, "scale", Vector2.One * 3.4f, 0.5f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ring, "modulate:a", 0f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(ring.QueueFree));

        var sparkTex = GD.Load<Texture2D>("res://assets/icons/coin.svg");
        for (int i = 0; i < 6; i++)
        {
            var spark = new Sprite2D
            {
                Texture = sparkTex,
                Position = center,
                Scale = Vector2.One * 0.18f,
                ZIndex = 60,
            };
            AddChild(spark);
            Vector2 dir = Vector2.Right.Rotated(Mathf.Tau * (i + GD.Randf() * 0.6f) / 6f);
            var sparkTween = CreateTween().SetParallel();
            sparkTween.TweenProperty(spark, "position", center + dir * (float)GD.RandRange(120.0, 210.0), 0.45f)
                .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
            sparkTween.TweenProperty(spark, "scale", Vector2.One * 0.02f, 0.45f)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            sparkTween.TweenProperty(spark, "modulate:a", 0f, 0.45f);
            sparkTween.Chain().TweenCallback(Callable.From(spark.QueueFree));
        }
    }

    public void ShowWin(string comment, string statsLine, WinStat[] stats,
        Node2D? bug = null, bool grandiose = false)
    {
        if (bug != null)
            SeatBug(bug);
        _winComment.Text = comment;
        _winStats.Text = statsLine;
        FillStatsRow(stats);
        ApplyGrandiose(grandiose, bug);
        _winOverlay.Visible = true;
        _winOverlay.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_winOverlay, "modulate:a", 1f, 0.35f);
    }

    /// <summary>
    /// Autoplay probe: the prismatic rays/sparkles layer only exists while
    /// the win card is dressed in its grandiose (lighter, prismatic) style,
    /// so this proves the card actually took the find's grand form.
    /// </summary>
    public bool WinGrandActive => _prismaticGlow != null;

    /// <summary>
    /// Dresses (or undresses) the win card for a prismatic find: radiant
    /// gold panel, rotating rays with looping sparkles behind the content,
    /// and a shimmering title.
    /// </summary>
    private void ApplyGrandiose(bool grandiose, Node2D? bug)
    {
        bool isPrismatic = grandiose && bug != null;
        _winPanel.AddThemeStyleboxOverride("panel",
            isPrismatic ? _winGrandStyle : _winStyle);

        if (isPrismatic && _prismaticGlow == null)
        {
            _prismaticGlow = new PrismaticGlow();
            _winPanel.AddChild(_prismaticGlow);
            _winPanel.MoveChild(_prismaticGlow, 0);
            _prismaticTitleLoop = CreateTween().SetLoops();
            _prismaticTitleLoop.TweenProperty(_winTitle, "modulate",
                new Color(1f, 0.85f, 0.4f), 0.5f);
            _prismaticTitleLoop.TweenProperty(_winTitle, "modulate",
                new Color(1f, 0.6f, 0.9f), 0.5f);
            _prismaticTitleLoop.TweenProperty(_winTitle, "modulate",
                new Color(0.6f, 0.9f, 1f), 0.5f);
            _prismaticTitleLoop.TweenProperty(_winTitle, "modulate",
                Colors.White, 0.5f);
        }
        else if (!isPrismatic && _prismaticGlow != null)
        {
            _prismaticTitleLoop?.Kill();
            _prismaticTitleLoop = null;
            _prismaticGlow.QueueFree();
            _prismaticGlow = null;
            _winTitle.Modulate = Colors.White;
        }
    }

    /// <summary>Rebuilds the lifetime stats row from the given cells.</summary>
    private void FillStatsRow(WinStat[] stats)
    {
        foreach (Node child in _statsRow.GetChildren())
            child.QueueFree();
        foreach (var stat in stats)
            _statsRow.AddChild(BuildStatCell(stat));
    }

    private static Control BuildStatCell(WinStat stat)
    {
        var cell = new VBoxContainer();
        cell.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var value = MakeLabel(46, true,
            stat.Highlight ? new Color("8a5c17") : new Color("3f5228"));
        value.Text = stat.Value;
        value.HorizontalAlignment = HorizontalAlignment.Center;

        var caption = MakeLabel(26, true, new Color("6b5233"));
        caption.Text = stat.Caption;
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        cell.AddChild(value);
        cell.AddChild(caption);
        return cell;
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

    /// <summary>Dock + level label live on the play screen; the menu hides them.</summary>
    public void SetDockVisible(bool visible)
    {
        _dock.Visible = visible;
        _levelLabel.Visible = visible;
        _sweepLabel.Visible = visible;
    }

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
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hint.Text = "The debris will be re-scattered and your\nsweep count for this round resets.";

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

    /// <summary>Small bronze counter circle pinned to the gust coin's top-right.</summary>
    private Panel BuildGustBadge()
    {
        var badge = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = new Color("8a5c17"),
            CornerRadiusBottomLeft = 48,
            CornerRadiusBottomRight = 48,
            CornerRadiusTopLeft = 48,
            CornerRadiusTopRight = 48,
            BorderWidthBottom = 5,
            BorderWidthTop = 5,
            BorderWidthLeft = 5,
            BorderWidthRight = 5,
            BorderColor = new Color("5f3f0e"),
        };
        badge.AddThemeStyleboxOverride("panel", style);
        _gustBadgeStyle = style;
        // Anchored inside the button's top-right corner; buttons don't clip
        // children, so the badge can overhang the coin's edge.
        badge.AnchorLeft = 1f;
        badge.AnchorTop = 0f;
        badge.AnchorRight = 1f;
        badge.AnchorBottom = 0f;
        badge.OffsetLeft = -104f;
        badge.OffsetTop = 10f;
        badge.OffsetRight = -12f;
        badge.OffsetBottom = 102f;

        _gustBadgeLabel = MakeLabel(44, true, new Color("fff8ec"), 8);
        _gustBadgeLabel.Text = "×0";
        _gustBadgeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _gustBadgeLabel.VerticalAlignment = VerticalAlignment.Center;
        _gustBadgeLabel.AnchorLeft = 0f;
        _gustBadgeLabel.AnchorTop = 0f;
        _gustBadgeLabel.AnchorRight = 1f;
        _gustBadgeLabel.AnchorBottom = 1f;
        _gustBadgeLabel.OffsetLeft = 0f;
        _gustBadgeLabel.OffsetTop = 0f;
        _gustBadgeLabel.OffsetRight = 0f;
        _gustBadgeLabel.OffsetBottom = 0f;
        badge.AddChild(_gustBadgeLabel);
        return badge;
    }

    /// <summary>Circular dark-gold coin button with embossed shading.</summary>
    private Button MakeCoinButton(string iconPath)
    {
        var coin = GD.Load<Texture2D>("res://assets/icons/coin.svg");
        var normal = new StyleBoxTexture
        {
            Texture = coin,
            ContentMarginLeft = 46,
            ContentMarginRight = 46,
            ContentMarginTop = 46,
            ContentMarginBottom = 46,
        };
        var hover = new StyleBoxTexture
        {
            Texture = coin,
            ModulateColor = new Color(1.15f, 1.1f, 0.95f),
            ContentMarginLeft = 46,
            ContentMarginRight = 46,
            ContentMarginTop = 46,
            ContentMarginBottom = 46,
        };
        var pressed = new StyleBoxTexture
        {
            Texture = coin,
            ModulateColor = new Color(0.78f, 0.72f, 0.58f),
            ContentMarginLeft = 46,
            ContentMarginRight = 46,
            ContentMarginTop = 46,
            ContentMarginBottom = 46,
        };

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
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        var disabled = (StyleBoxTexture)pressed.Duplicate();
        disabled.ModulateColor = new Color(0.55f, 0.52f, 0.45f);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
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

    internal static Button MakeButton(string text, Color bg, int fontSize = 42)
    {
        // Scale padding and corners with the font so larger buttons stay
        // visually proportional.
        float scale = fontSize / 42f;
        var normal = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusBottomLeft = (int)(20 * scale),
            CornerRadiusBottomRight = (int)(20 * scale),
            CornerRadiusTopLeft = (int)(20 * scale),
            CornerRadiusTopRight = (int)(20 * scale),
            ContentMarginLeft = 40 * scale,
            ContentMarginRight = 40 * scale,
            ContentMarginTop = 18 * scale,
            ContentMarginBottom = 18 * scale,
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
        button.AddThemeFontSizeOverride("font_size", fontSize);
        return button;
    }
}
