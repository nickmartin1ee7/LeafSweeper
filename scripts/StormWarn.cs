using Godot;

namespace LeafSweeper;

/// <summary>
/// The "Storm Round" warning sign: shown during the end-of-round wind of
/// the round BEFORE a storm level. The label renders alone into a
/// SubViewport, and warn_sparks.gdshader repaints it as living
/// electricity — a neon rim hugging every glyph, lightning bolts arcing
/// around and through the lettering, sparks and flicker — over a soft,
/// borderless dark card (no boxy fence). Fades in with the crackle,
/// fades out when the next round begins. Own canvas layer above the HUD
/// — explicit ladder: world 0 → storm 1 → menu 2 → hud 3 → warn 4 →
/// bug book 90.
/// </summary>
public partial class StormWarn : CanvasLayer
{
    private const string ShaderPath = "res://assets/shaders/warn_sparks.gdshader";

    // A quick dramatic entrance — the sign snaps on like failing neon.
    private const float FadeSeconds = 0.45f;

    // Sign geometry as fractions of the viewport: centered horizontally,
    // sitting in the upper quarter so it never covers the win card that
    // owns the screen center during the end-round. Taller than before so
    // the bolts have room to arc above and below the lettering.
    private const float WidthFraction = 0.46f;
    private const float HeightFraction = 0.155f;
    private const float TopAnchor = 0.12f;

    private Control _root = null!;
    private Panel _panel = null!;
    private ColorRect _fx = null!;
    private SubViewport _vp = null!;
    private Label _label = null!;
    private Tween _fade = null!;

    public StormWarn()
    {
        Layer = 4;
        Visible = false;
    }

    public override void _Ready()
    {
        // Everything hangs off one root so fading is a single Modulate.
        _root = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        // Anchors AND offsets — SetAnchorsPreset alone preserves the old
        // zero rect (the storm overlay's invisible-storm bug).
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _panel = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            // Soft borderless card: dark enough for the neon to pop,
            // with a wide blurred shadow so it never reads as a box.
            BgColor = new Color(0.03f, 0.05f, 0.10f, 0.58f),
            CornerRadiusBottomLeft = 24,
            CornerRadiusBottomRight = 24,
            CornerRadiusTopLeft = 24,
            CornerRadiusTopRight = 24,
            ShadowSize = 16,
            ShadowColor = new Color(0f, 0f, 0f, 0.45f),
        });
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorTop = TopAnchor;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorBottom = TopAnchor;
        _root.AddChild(_panel);

        // The label lives inside its own viewport; its texture is the
        // glyph mask the shader wraps its bolts around.
        _vp = new SubViewport
        {
            Disable3D = true,
            TransparentBg = true,
            // Only updated while the sign is on screen — costs nothing
            // when hidden.
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
        };
        _panel.AddChild(_vp);

        _label = Hud.MakeLabel(64, true, new Color(0.90f, 0.96f, 1.0f));
        _label.Text = "Storm Round";
        // Storm-blue outline instead of the HUD's earthy brown.
        _label.LabelSettings.OutlineColor = new Color(0.02f, 0.09f, 0.18f, 0.95f);
        _label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _vp.AddChild(_label);

        _fx = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        _fx.Material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };
        _fx.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel.AddChild(_fx);

        LayoutPanel();
    }

    // Offsets are pixels, so the fractional sign geometry is resolved
    // against the live viewport (re-run on show — the sign is transient
    // and follows window resizes lazily). The viewport size IS the fx
    // rect size, so shader UVs and mask UVs line up pixel for pixel.
    private void LayoutPanel()
    {
        Vector2 view = GetViewport().GetVisibleRect().Size;
        float w = view.X * WidthFraction;
        float h = view.Y * HeightFraction;
        _panel.OffsetLeft = -w / 2f;
        _panel.OffsetRight = w / 2f;
        _panel.OffsetTop = -h / 2f;
        _panel.OffsetBottom = h / 2f;
        _vp.Size = new Vector2I((int)w, (int)h);
        // Width is the binding constraint in portrait: "Storm Round" spans
        // ~5.8 em in the default font, so cap by w/6.0 (small margin over
        // the measured 5.8) or the glyphs clip out of the SubViewport mask.
        _label.LabelSettings.FontSize = Mathf.Max(28, (int)Mathf.Min(h * 0.56f, w / 6.0f));
        if (_fx.Material is ShaderMaterial mat)
        {
            mat.SetShaderParameter("text_mask", _vp.GetTexture());
            mat.SetShaderParameter("texel", new Vector2(1f / w, 1f / h));
            mat.SetShaderParameter("aspect", w / h);
        }
    }

    /// <summary>Crackles the warning sign on for the coming storm round.</summary>
    public void ShowWarning()
    {
        if (Visible && _root.Modulate.A >= 1f)
            return;
        LayoutPanel();
        _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        Visible = true;
        FadeTo(1f);
    }

    /// <summary>Fades the sign out once the storm round has begun.</summary>
    public void HideWarning()
    {
        if (!Visible)
            return;
        FadeTo(0f);
    }

    private void FadeTo(float target)
    {
        if (_fade != null && _fade.IsValid())
            _fade.Kill();
        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", target, FadeSeconds);
        if (target == 0f)
            _fade.TweenCallback(Callable.From(() =>
            {
                Visible = false;
                // Sign gone → stop paying for the label viewport.
                _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            }));
    }
}
