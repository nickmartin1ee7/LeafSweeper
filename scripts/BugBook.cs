using System;
using Godot;

namespace LeafSweeper;

/// <summary>
/// The bug collection book: a full-screen overlay showing ONE oversized page
/// at a time so everything stays readable on a phone. It opens on the cover
/// and paging uses dog-eared page corners
/// (folded paper + drop shadow + arrow): top-right turns forward, bottom-right
/// turns back. Tapping anywhere outside the page closes the book, and the dim
/// swallows every tap while open so nothing underneath (dock buttons) reacts.
/// Content comes straight from <see cref="BugBookModel"/>, so autoplay can
/// assert the same numbers the player sees.
/// </summary>
public partial class BugBook : CanvasLayer
{
    private const string MistShaderPath = "res://assets/shaders/bug_mist.gdshader";

    // Dog-ear triangle leg, in page pixels — big enough for a thumb on mobile.
    private const float FoldSize = 104f;

    private static readonly Color Ink = new("4a3a26");
    private static readonly Color InkSoft = new("6b5233");
    private static readonly Color DeepInk = new("3f5228");
    private static readonly Color Paper = new("f7f0e1");
    private static readonly Color PaperBack = new("e7dcc3");
    private static readonly Color PageBorder = new("c9a06a");
    private static readonly Color Leather = new("7a4a22");
    private static readonly Color LeatherDark = new("54331a");
    private static readonly Color Gold = new("e0b34d");

    private Control _root = null!;
    private Control _bookBody = null!;
    private PanelContainer _pagePanel = null!;
    private Control _pageContent = null!;
    private Label _pageTitle = null!;
    private HBoxContainer _dots = null!;
    private Control _dogEarNext = null!;
    private Control _dogEarBack = null!;
    private StyleBoxFlat _paperStyle = null!;
    private StyleBoxFlat _coverStyle = null!;

    private BugBookModel _model = new(new SaveData());
    private int _page;             // 0 = cover, 1 = stats, 2+ = collection
    private int _collectionPages = 1;
    private int _cols = 3;
    private int _rows = 5;
    private int _perPage = 15;
    private Vector2 _pageSize = new(1010, 2000);
    private bool _open;
    private bool _turning;
    private Tween? _anim;

    /// <summary>Total pages: cover + stats + collection pages.</summary>
    public int Pages => 2 + _collectionPages;

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
        if (_open)
            return;
        _model = new BugBookModel(save);
        ComputePageSize();
        _page = 0;
        _turning = false;
        _open = true;

        _root.Visible = true;
        _root.Modulate = new Color(1, 1, 1, 0);
        _bookBody.Position = new Vector2(0f, 620f);
        RebuildPage();

        // Entrance beat: dim rises and the closed cover rises from below,
        // then holds on the cover until the player turns the page themselves.
        _anim?.Kill();
        _anim = CreateTween();
        _anim.TweenProperty(_root, "modulate:a", 1f, 0.2f);
        _anim.Parallel().TweenProperty(_bookBody, "position", Vector2.Zero, 0.45f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    public void Close()
    {
        if (!_open)
            return;
        _open = false;
        _turning = false;
        _anim?.Kill();
        _anim = CreateTween();
        _anim.TweenProperty(_root, "modulate:a", 0f, 0.22f);
        _anim.Parallel().TweenProperty(_bookBody, "position", new Vector2(0f, 620f), 0.32f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        _anim.TweenCallback(Callable.From(() =>
        {
            _root.Visible = false;
            Closed?.Invoke();
        }));
    }

    private void BuildTree()
    {
        // Full-rect dim that swallows every tap so the dock below stays dead
        // while the book is open; tapping anywhere off the page closes it.
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

        var center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);

        _bookBody = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        center.AddChild(_bookBody);

        _paperStyle = new StyleBoxFlat
        {
            BgColor = Paper,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            BorderColor = PageBorder,
            BorderWidthBottom = 5,
            BorderWidthTop = 5,
            BorderWidthLeft = 5,
            BorderWidthRight = 5,
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 24,
            ContentMarginBottom = 22,
        };
        _coverStyle = new StyleBoxFlat
        {
            BgColor = Leather,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            BorderColor = LeatherDark,
            BorderWidthBottom = 9,
            BorderWidthTop = 9,
            BorderWidthLeft = 9,
            BorderWidthRight = 9,
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 24,
            ContentMarginBottom = 22,
        };

        _pagePanel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        _pagePanel.AddThemeStyleboxOverride("panel", _paperStyle);
        _bookBody.AddChild(_pagePanel);

        var pageBox = new VBoxContainer();
        pageBox.AddThemeConstantOverride("separation", 10);
        _pagePanel.AddChild(pageBox);

        _pageTitle = Hud.MakeLabel(46, true, Ink, 0);
        _pageTitle.HorizontalAlignment = HorizontalAlignment.Center;
        pageBox.AddChild(_pageTitle);

        _pageContent = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        pageBox.AddChild(_pageContent);

        _dots = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _dots.AddThemeConstantOverride("separation", 10);
        pageBox.AddChild(_dots);

        _dogEarNext = MakeDogEar(forward: true);
        _dogEarBack = MakeDogEar(forward: false);
        _bookBody.AddChild(_dogEarNext);
        _bookBody.AddChild(_dogEarBack);
    }

    /// <summary>
    /// Sizes the single page to fill most of the viewport and picks a grid
    /// that suits its shape: 3×5 on the tall phone screen, 5×4 in landscape.
    /// </summary>
    private void ComputePageSize()
    {
        Vector2 view = GetViewport().GetVisibleRect().Size;
        if (view.Y >= view.X)
        {
            _pageSize = new Vector2(Mathf.Clamp(view.X * 0.94f, 640f, 1240f), view.Y * 0.86f);
            _cols = 3;
            _rows = 5;
        }
        else
        {
            _pageSize = new Vector2(view.X * 0.8f, Mathf.Clamp(view.Y * 0.86f, 560f, 1200f));
            _cols = 5;
            _rows = 4;
        }
        _perPage = _cols * _rows;
        _collectionPages = Mathf.CeilToInt(_model.Entries.Count / (float)_perPage);

        _bookBody.CustomMinimumSize = _pageSize;
        _pagePanel.CustomMinimumSize = _pageSize;
        _pagePanel.Size = _pageSize;
        _pagePanel.Position = Vector2.Zero;
        PositionDogEars();
    }

    private void PositionDogEars()
    {
        _dogEarNext.Position = new Vector2(_pageSize.X - FoldSize - 2, 2);
        _dogEarBack.Position = new Vector2(_pageSize.X - FoldSize - 2, _pageSize.Y - FoldSize - 2);
    }

    /// <summary>
    /// A dog-eared page corner: the corner's shade where the paper lifted, a
    /// drop shadow under the fold, the folded flap itself, and an arrow.
    /// </summary>
    private Control MakeDogEar(bool forward)
    {
        float f = FoldSize;

        var ear = new Control
        {
            Name = forward ? "DogEarNext" : "DogEarBack",
            CustomMinimumSize = new Vector2(f, f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        ear.GuiInput += e =>
        {
            bool pressed = e is InputEventScreenTouch { Pressed: true }
                or InputEventMouseButton { Pressed: true };
            if (pressed)
                TurnPage(forward ? 1 : -1);
        };

        // Flap triangle: forward ear folds the top-right corner down-left,
        // back ear folds the bottom-right corner up-left.
        Vector2[] flap = forward
            ? new[] { new Vector2(0, 0), new Vector2(f, f), new Vector2(0, f) }
            : new[] { new Vector2(0, f), new Vector2(f, 0), Vector2.Zero };

        // The lifted corner shades the page where the paper used to be.
        var cutShade = new Polygon2D
        {
            Polygon = forward
                ? new[] { new Vector2(0, 0), new Vector2(f, 0), new Vector2(f, f) }
                : new[] { new Vector2(0, f), new Vector2(f, 0), new Vector2(f, f) },
            Color = new Color(0, 0, 0, 0.08f),
        };
        ear.AddChild(cutShade);

        var shadow = new Polygon2D
        {
            Polygon = flap,
            Position = forward ? new Vector2(-7, 7) : new Vector2(-7, -7),
            Color = new Color(0, 0, 0, 0.25f),
        };
        ear.AddChild(shadow);

        var fold = new Polygon2D
        {
            Polygon = flap,
            Color = PaperBack,
        };
        ear.AddChild(fold);

        var arrow = Hud.MakeLabel(40, true, Ink, 0);
        arrow.Text = forward ? "▶" : "◀";
        // Center the arrow on the flap's centroid.
        Vector2 centroid = forward
            ? new Vector2(f / 3f, 2f * f / 3f)
            : new Vector2(f / 3f, f / 3f);
        arrow.Position = centroid - new Vector2(16, 26);
        ear.AddChild(arrow);

        return ear;
    }

    private void TurnPage(int direction)
    {
        if (!_open || _turning)
            return;
        int next = _page + direction;
        if (next < 0 || next >= Pages)
            return;
        _turning = true;

        // A cream sheet the size of the page folds toward the spine on the
        // left edge; the content swaps at the halfway beat.
        var flip = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = PaperBack,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            BorderColor = PageBorder,
            BorderWidthBottom = 5,
            BorderWidthTop = 5,
            BorderWidthLeft = 5,
            BorderWidthRight = 5,
        };
        flip.AddThemeStyleboxOverride("panel", style);
        flip.Position = Vector2.Zero;
        flip.CustomMinimumSize = _pageSize;
        flip.Size = _pageSize;
        flip.PivotOffset = new Vector2(0, _pageSize.Y / 2f); // spine edge
        _bookBody.AddChild(flip);

        var tween = CreateTween();
        tween.TweenProperty(flip, "scale:x", 0.04f, 0.32f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            _page = next;
            RebuildPage();
        }));
        tween.TweenProperty(flip, "modulate:a", 0f, 0.1f);
        tween.TweenCallback(Callable.From(() =>
        {
            flip.QueueFree();
            _turning = false;
        }));
    }

    private void RebuildPage()
    {
        bool cover = _page == 0;
        bool stats = _page == 1;
        _pagePanel.AddThemeStyleboxOverride("panel", cover ? _coverStyle : _paperStyle);
        _pageTitle.Visible = !cover;
        _pageTitle.Text = stats
            ? "Game Stats"
            : $"Collection  ·  {_page - 1}/{_collectionPages}";
        _dots.Visible = !cover;
        RebuildDots();
        _dogEarNext.Visible = _page < Pages - 1;
        _dogEarBack.Visible = _page > 0;

        // Reused nodes must clear old children before adding new ones —
        // and immediately, so stale content never flashes under the new.
        foreach (Node child in _pageContent.GetChildren())
        {
            _pageContent.RemoveChild(child);
            child.QueueFree();
        }

        if (cover)
            BuildCoverPage();
        else if (stats)
            BuildStatsPage();
        else
            BuildCollectionPage();
    }

    private void BuildCoverPage()
    {
        var centerBox = new CenterContainer();
        centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _pageContent.AddChild(centerBox);

        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 36);
        centerBox.AddChild(box);

        var emblem = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/icons/book.svg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(260, 260),
        };
        box.AddChild(emblem);

        var title = Hud.MakeLabel(88, true, Gold, 0);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Text = "Bug Book";
        box.AddChild(title);

        var subtitle = Hud.MakeLabel(36, false, new Color("ecdcb6"), 0);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Text = "Every critter the leaves are hiding";
        box.AddChild(subtitle);
    }

    private void BuildStatsPage()
    {
        var box = new VBoxContainer();
        box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        box.AddThemeConstantOverride("separation", 12);
        _pageContent.AddChild(box);

        AddStat(box, "Bugs found", _model.TotalBugs.ToString());
        AddStat(box, "Variants discovered", $"{_model.FoundVariants} / {_model.Entries.Count}");
        AddStat(box, "Species discovered", $"{_model.FoundSpecies} / {BugTypes.All.Length}");
        AddStat(box, "Best round",
            _model.BestRound > 0 ? $"{_model.BestRound} sweeps" : "—");
        AddStat(box, "Total sweeps", _model.TotalSweeps.ToString());
        AddStat(box, "Gusts blown", _model.TotalGusts.ToString());
        AddStat(box, "Time in the leaves", LevelStats.FormatTime(_model.TotalSeconds));
        AddStat(box, "Prismatic finds", _model.PrismaticFinds.ToString());
        AddStat(box, "Favorite critter", _model.Favorite);
    }

    private static void AddStat(VBoxContainer box, string caption, string value)
    {
        var row = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 16);

        var captionLabel = Hud.MakeLabel(32, false, InkSoft, 0);
        captionLabel.Text = caption;
        captionLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        captionLabel.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(captionLabel);

        var valueLabel = Hud.MakeLabel(40, true, Ink, 0);
        valueLabel.Text = value;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(valueLabel);

        box.AddChild(row);
    }

    private void BuildCollectionPage()
    {
        var grid = new GridContainer
        {
            Columns = _cols,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        grid.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        _pageContent.AddChild(grid);

        // Cell size from the space left under the title and dots.
        ComputeCellSize(out float cellW, out float cellH);

        int first = (_page - 2) * _perPage;
        for (int i = 0; i < _perPage; i++)
        {
            int index = first + i;
            if (index < _model.Entries.Count)
                grid.AddChild(MakeEntryCell(_model.Entries[index], cellW, cellH));
            else
                grid.AddChild(new Control { CustomMinimumSize = new Vector2(cellW, cellH) });
        }
    }

    private void ComputeCellSize(out float cellW, out float cellH)
    {
        var style = _paperStyle;
        float contentW = _pageSize.X - style.ContentMarginLeft - style.ContentMarginRight;
        float titleH = 62f; // page title row + separation
        float dotsH = 24f;
        float contentH = _pageSize.Y - style.ContentMarginTop - style.ContentMarginBottom
            - titleH - dotsH;
        cellW = (contentW - 8f * (_cols - 1)) / _cols;
        cellH = (contentH - 8f * (_rows - 1)) / _rows;
    }

    private void RebuildDots()
    {
        foreach (Node child in _dots.GetChildren())
        {
            _dots.RemoveChild(child);
            child.QueueFree();
        }
        for (int i = 0; i < Pages; i++)
        {
            var dot = new ColorRect { CustomMinimumSize = new Vector2(14, 14) };
            dot.Color = i == _page ? DeepInk : new Color("cbbfa4");
            _dots.AddChild(dot);
        }
    }

    private Control MakeEntryCell(BugBookModel.Entry entry, float cellW, float cellH)
    {
        var cell = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(cellW, cellH),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        cell.AddThemeConstantOverride("separation", 4);

        var sprite = new TextureRect
        {
            Texture = GD.Load<Texture2D>(entry.Variant.TexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(cellW, cellH * 0.72f),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
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

        var label = Hud.MakeLabel(32, false, entry.Found ? Ink : InkSoft, 0);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.Text = entry.Label;
        cell.AddChild(label);
        return cell;
    }
}
