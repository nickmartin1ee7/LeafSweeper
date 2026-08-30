using Godot;

namespace LeafSweeper;

/// <summary>
/// The "Storm Round" warning sign: shown during the end-of-round wind of
/// the round BEFORE a storm level. The label renders alone into a
/// SubViewport, and warn_sparks.gdshader repaints it as living
/// electricity — a neon rim hugging every glyph, lightning bolts arcing
/// around and through the lettering, sparks and flicker — set into a
/// roiling storm cloud the same shader paints behind the lettering.
/// Fades in with the crackle, fades out when the next round begins. Own
/// canvas layer above the HUD — explicit ladder: world 0 → storm 1 →
/// menu 2 → hud 3 → warn 4 → bug book 90.
/// </summary>
public partial class StormWarn : CanvasLayer
{
    private const string ShaderPath = "res://assets/shaders/warn_sparks.gdshader";

    // A quick dramatic entrance — the sign snaps on like failing neon.
    private const float FadeSeconds = 0.45f;

    // Once the storm round begins the sign rides the round's opening: a
    // two-second hold over the settle so the mood lands, then a slow
    // one-second dissolve that yields the screen to gameplay.
    private const float LingerSeconds = 2f;
    private const float LingerFadeSeconds = 1f;

    // Sign geometry as fractions of the viewport: centered horizontally,
    // sitting in the upper quarter so it never covers the win card that
    // owns the screen center during the end-round. Wider than the text
    // needs so the cloud puffs keep padding left/right instead of running
    // flush off the panel edges.
    private const float WidthFraction = 0.54f;

    // The text keeps the size it had at the old, narrower panel: extra
    // panel width is cloud padding around it, not lettering room.
    private const float TextWidthFraction = 0.83f;
    private const float HeightFraction = 0.155f;
    private const float TopAnchor = 0.12f;

    private Control _root = null!;
    private Panel _panel = null!;
    private ColorRect _fx = null!;
    private SubViewport _vp = null!;
    private Label _label = null!;
    private Tween _fade = null!;
    private Tween _linger = null!;

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
        // No card: the shader paints a storm cloud as the sign's
        // background, so the panel is purely a layout rect.
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
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
        // Start from the height-scaled size, then shrink by measurement
        // until the rendered text fits its slice of the panel width with
        // margin — real font metrics beat any constant divisor ("Storm
        // Round" was still clipping its trailing "d" at w/6.0). The text
        // budget stays a fixed fraction of the panel, so widening the
        // panel buys the cloud padding rather than bigger letters.
        _label.LabelSettings.FontSize = Mathf.Max(28, (int)(h * 0.56f));
        while (_label.LabelSettings.FontSize > 28 && _label.GetMinimumSize().X > w * TextWidthFraction)
        {
            _label.LabelSettings.FontSize = Mathf.Max(28, _label.LabelSettings.FontSize - 3);
        }
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

    /// <summary>
    /// Holds the sign over the storm round's opening for
    /// <see cref="LingerSeconds"/>, then dissolves it out over
    /// <see cref="LingerFadeSeconds"/>. StartLevel calls this instead of
    /// <see cref="HideWarning"/> when the round being started is the storm
    /// round the sign warned about.
    /// </summary>
    public void LingerThenFade()
    {
        if (!Visible)
            return;
        KillFades();
        _root.Modulate = new Color(1f, 1f, 1f, 1f);
        _linger = CreateTween();
        _linger.TweenInterval(LingerSeconds);
        _linger.TweenProperty(_root, "modulate:a", 0f, LingerFadeSeconds);
        _linger.TweenCallback(Callable.From(FinishHide));
    }

    private void FadeTo(float target)
    {
        KillFades();
        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", target, FadeSeconds);
        if (target == 0f)
            _fade.TweenCallback(Callable.From(FinishHide));
    }

    private void KillFades()
    {
        if (_fade != null && _fade.IsValid())
            _fade.Kill();
        if (_linger != null && _linger.IsValid())
            _linger.Kill();
    }

    private void FinishHide()
    {
        Visible = false;
        // Sign gone → stop paying for the label viewport.
        _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }
}
