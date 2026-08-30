using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// The bug collection book: a full-screen overlay whose cover swings open,
/// then pages through the player's collection (5×4 entries per page) and a
/// lifetime stats page. Built entirely in code like every other UI here.
/// Content comes straight from <see cref="BugBookModel"/>, so autoplay can
/// assert the same numbers the player sees.
/// </summary>
public partial class BugBook : CanvasLayer
{
    private const int Cols = 5;
    private const int Rows = 4;
    private const int PerPage = Cols * Rows;
    private const string MistShaderPath = "res://assets/shaders/bug_mist.gdshader";

    private static readonly Color Ink = new("4a3a26");
    private static readonly Color InkSoft = new("6b5233");
    private static readonly Color DeepInk = new("3f5228");

    private Control _root = null!;
    private Control _bookBody = null!;
    private Control _coverLeft = null!;
    private Control _coverRight = null!;
    private Label _pageTitle = null!;
    private GridContainer _grid = null!;
    private VBoxContainer _statsPage = null!;
    private HBoxContainer _dots = null!;
    private Button _prevButton = null!;
    private Button _nextButton = null!;

    private BugBookModel _model = new(new SaveData());
    private int _page;           // 0..Pages-1; last page is the stats page
    private bool _open;
    private bool _turning;

    /// <summary>Book page count: collection pages plus the trailing stats page.</summary>
    public int Pages => Mathf.CeilToInt(_model.Entries.Count / (float)PerPage) + 1;

    public bool IsOpen => _open;

    public event Action? Closed;

    public override void _Ready()
    {
        // One above the HUD's layer so the open book covers the dock too.
        Layer = 90;
        BuildTree();
        _root.Visible = false;
    }

    public void Open(SaveData save)
    {
        _model = new BugBookModel(save);
        _page = 0;
        RebuildContent();
        _open = true;
        _turning = false;

        _root.Visible = true;
        _root.Modulate = new Color(1, 1, 1, 0);
        _coverLeft.Visible = true;
        _coverRight.Visible = true;
        _coverLeft.Scale = Vector2.One;
        _coverRight.Scale = Vector2.One;

        // Entrance beat: dim rises, the book rises from below, then the two
        // cover halves swing flat around the spine and the pages take over.
        var tween = CreateTween();
        tween.TweenProperty(_root, "modulate:a", 1f, 0.2f);
        tween.Parallel().TweenProperty(_bookBody, "position", Vector2.Zero, 0.45f)
            .From(new Vector2(0f, 420f))
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_coverLeft, "scale:x", 0.02f, 0.42f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(_coverRight, "scale:x", 0.02f, 0.42f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            _coverLeft.Visible = false;
            _coverRight.Visible = false;
        }));
    }

    public void Close()
    {
        if (!_open)
            return;
        _open = false;
        _turning = false;
        _root.Visible = false;
        Closed?.Invoke();
    }

    private void BuildTree()
    {
        // Full-rect dim that swallows every tap; tapping it closes the book.
        _root = new ColorRect
        {
            Color = new Color(0.12f, 0.09f, 0.05f, 0.62f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.GuiInput += e =>
        {
            bool pressed = e is InputEventScreenTouch { Pressed: true }
                or InputEventMouseButton { Pressed: true };
            if (pressed)
                Close();
        };
        AddChild(_root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);

        _bookBody = new Control
        {
            CustomMinimumSize = new Vector2(1020, 700),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        center.AddChild(_bookBody);

        // The open book: two cream pages with a thin spine gap between them.
        _bookBody.AddChild(MakePage(new Vector2(0, 0), content: false));
        var rightPage = MakePage(new Vector2(514, 0), content: true);
        _bookBody.AddChild(rightPage);

        // Cover: two dark halves that pivot on the spine and swing flat.
        // Left half pivots at its right edge, right half at its left edge.
        _coverLeft = MakeCoverHalf();
        _coverLeft.Position = new Vector2(-6, -6);
        _coverLeft.PivotOffset = new Vector2(520, 0);
        _coverRight = MakeCoverHalf();
        _coverRight.Position = new Vector2(512, -6);
        _coverRight.PivotOffset = Vector2.Zero;
        _bookBody.AddChild(_coverLeft);
        _bookBody.AddChild(_coverRight);
    }

    private PanelContainer MakePage(Vector2 pos, bool content)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color("f7f0e1"),
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            BorderColor = new Color("c9a06a"),
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 10,
        };
        var page = new PanelContainer
        {
            Position = pos,
            CustomMinimumSize = new Vector2(508, 700),
            MouseFilter = content
                ? Control.MouseFilterEnum.Stop
                : Control.MouseFilterEnum.Ignore,
        };
        page.AddThemeStyleboxOverride("panel", style);
        if (content)
            BuildRightPage(page);
        else
            BuildLeftPage(page);
        return page;
    }

    private static PanelContainer MakeCoverHalf()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color("7a4a22"),
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            BorderColor = new Color("54331a"),
            BorderWidthLeft = 5,
            BorderWidthRight = 5,
            BorderWidthTop = 5,
            BorderWidthBottom = 5,
        };
        var half = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 708),
            Size = new Vector2(520, 708),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        half.AddThemeStyleboxOverride("panel", style);
        return half;
    }

    private void BuildLeftPage(PanelContainer page)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        page.AddChild(box);

        box.AddChild(Spacer(30));

        var title = Hud.MakeLabel(46, true, DeepInk, 0);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Text = "Bug Book";
        box.AddChild(title);

        var subtitle = Hud.MakeLabel(21, false, InkSoft, 0);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Text = "Every critter the leaves are hiding";
        box.AddChild(subtitle);

        box.AddChild(Spacer(10));

        var statsScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _statsPage = new VBoxContainer();
        _statsPage.AddThemeConstantOverride("separation", 10);
        statsScroll.AddChild(_statsPage);
        box.AddChild(statsScroll);

        box.AddChild(Spacer(8));

        _dots = new HBoxContainer();
        _dots.Alignment = BoxContainer.AlignmentMode.Center;
        _dots.AddThemeConstantOverride("separation", 8);
        box.AddChild(_dots);
        box.AddChild(Spacer(12));
    }

    private void BuildRightPage(PanelContainer page)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        page.AddChild(box);

        _pageTitle = Hud.MakeLabel(28, true, Ink, 0);
        _pageTitle.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(_pageTitle);

        _grid = new GridContainer { Columns = Cols };
        _grid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _grid.AddThemeConstantOverride("h_separation", 2);
        _grid.AddThemeConstantOverride("v_separation", 2);
        box.AddChild(_grid);

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 10);
        footer.AddChild(Flex());
        _prevButton = MakeArrow("◀", () => TurnPage(-1));
        _nextButton = MakeArrow("▶", () => TurnPage(1));
        footer.AddChild(_prevButton);
        footer.AddChild(_nextButton);
        footer.AddChild(Flex());
        box.AddChild(footer);
    }

    private Button MakeArrow(string glyph, Action onClick)
    {
        var button = new Button { Text = glyph };
        button.AddThemeFontSizeOverride("font_size", 34);
        button.AddThemeColorOverride("font_color", Ink);
        button.Pressed += () => onClick();
        return button;
    }

    private void TurnPage(int direction)
    {
        if (!_open || _turning)
            return;
        int next = _page + direction;
        if (next < 0 || next >= Pages)
            return;
        _turning = true;

        // A cream sheet sweeps across the right page like the page lifting
        // and folding toward the spine; content swaps at the halfway beat.
        var flip = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color("efe5cf"),
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            BorderColor = new Color("c9a06a"),
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
        };
        flip.AddThemeStyleboxOverride("panel", style);
        flip.Position = new Vector2(514, 0);
        flip.CustomMinimumSize = new Vector2(508, 700);
        flip.Size = new Vector2(508, 700);
        flip.PivotOffset = Vector2.Zero; // spine edge: swings left like a page
        flip.MouseFilter = Control.MouseFilterEnum.Ignore;
        _bookBody.AddChild(flip);

        var tween = CreateTween();
        tween.TweenProperty(flip, "scale:x", 0.04f, 0.3f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            _page = next;
            RebuildContent();
        }));
        tween.TweenProperty(flip, "modulate:a", 0f, 0.08f);
        tween.TweenCallback(Callable.From(() =>
        {
            flip.QueueFree();
            _turning = false;
        }));
    }

    private void RebuildContent()
    {
        bool statsPage = _page == Pages - 1;
        _pageTitle.Text = statsPage
            ? "Game Stats"
            : $"Collection  ·  {_page + 1}/{Pages - 1}";
        _prevButton.Disabled = _page == 0;
        _nextButton.Disabled = statsPage;

        RebuildStats();
        RebuildDots();

        // Reused nodes must clear old children before adding new ones.
        foreach (Node child in _grid.GetChildren())
            child.QueueFree();

        int first = _page * PerPage;
        for (int i = 0; i < PerPage; i++)
        {
            int index = first + i;
            if (index < _model.Entries.Count)
                _grid.AddChild(MakeEntryCell(_model.Entries[index]));
            else
                _grid.AddChild(new Control { CustomMinimumSize = new Vector2(94, 118) });
        }
    }

    private void RebuildStats()
    {
        foreach (Node child in _statsPage.GetChildren())
            child.QueueFree();

        if (_page < Pages - 1)
            return; // stats only render on the last page

        AddStat("Bugs found", _model.TotalBugs.ToString());
        AddStat("Variants discovered", $"{_model.FoundVariants} / {_model.Entries.Count}");
        AddStat("Species discovered", $"{_model.FoundSpecies} / {BugTypes.All.Length}");
        AddStat("Best round",
            _model.BestRound > 0 ? $"{_model.BestRound} sweeps" : "—");
        AddStat("Total sweeps", _model.TotalSweeps.ToString());
        AddStat("Gusts blown", _model.TotalGusts.ToString());
        AddStat("Time in the leaves", LevelStats.FormatTime(_model.TotalSeconds));
        AddStat("Prismatic finds", _model.PrismaticFinds.ToString());
        AddStat("Favorite critter", _model.Favorite);
    }

    private void AddStat(string caption, string value)
    {
        var captionLabel = Hud.MakeLabel(17, false, InkSoft, 0);
        captionLabel.Text = caption;
        var valueLabel = Hud.MakeLabel(30, true, Ink, 0);
        valueLabel.Text = value;
        valueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statsPage.AddChild(captionLabel);
        _statsPage.AddChild(valueLabel);
    }

    private void RebuildDots()
    {
        foreach (Node child in _dots.GetChildren())
            child.QueueFree();
        for (int i = 0; i < Pages; i++)
        {
            var dot = new ColorRect { CustomMinimumSize = new Vector2(12, 12) };
            dot.Color = i == _page ? DeepInk : new Color("cbbfa4");
            _dots.AddChild(dot);
        }
    }

    private Control MakeEntryCell(BugBookModel.Entry entry)
    {
        var cell = new VBoxContainer { CustomMinimumSize = new Vector2(94, 118) };
        cell.AddThemeConstantOverride("separation", 2);

        var sprite = new TextureRect
        {
            Texture = GD.Load<Texture2D>(entry.Variant.TexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(92, 86),
        };
        if (!entry.Found)
        {
            // Unknown critter: black silhouette wrapped in drifting mist.
            sprite.Material = new ShaderMaterial
            {
                Shader = GD.Load<Shader>(MistShaderPath),
            };
        }
        cell.AddChild(sprite);

        var label = Hud.MakeLabel(13, false, entry.Found ? Ink : InkSoft, 0);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.Text = entry.Label;
        cell.AddChild(label);
        return cell;
    }

    private static Control Flex() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

    private static Control Spacer(int height) => new() { CustomMinimumSize = new Vector2(0, height) };
}
