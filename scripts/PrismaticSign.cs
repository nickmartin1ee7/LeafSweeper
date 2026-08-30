using Godot;

namespace LeafSweeper;

/// <summary>
/// The "Prismatic" banner: shown during the end-of-round of a round whose
/// critter rolled the rare prismatic look — the mirror image of the storm
/// crawling through the lettering, bright specular bands sweeping through
/// it like light over foil, star glints, a soft pastel aura. Fades in with
/// the win card, holds over the next round's opening, then fades out. Own
/// canvas layer above the storm warn — explicit ladder: world 0 → storm 1 →
/// menu 2 → hud 3 → warn 4 → prismatic 5 → bug book 90. Solo finds perch
/// high above the win card; when the storm sign shares the end-round the
/// banner yields and slots in just below the storm cloud.
/// </summary>
public partial class PrismaticSign : CanvasLayer
{
    private const string ShaderPath = "res://assets/shaders/prismatic_sign.gdshader";

    // A quick celebratory entrance alongside the win card's fade-in.
    private const float FadeSeconds = 0.45f;

    // Once the next round begins the banner rides its opening: a
    // two-second hold so the mood lands, then a slow four-second
    // dissolve that yields the screen to gameplay (same pacing as the
    // storm sign's linger).
    private const float LingerSeconds = 2f;
    private const float LingerFadeSeconds = 4f;

    // Banner geometry as fractions of the viewport: centered horizontally,
    // riding high in the upper quarter — well clear of the centered win
    // card below it, so the label never sits inside the "Bug found!" modal
    // even when the card is tall.
    private const float WidthFraction = 0.50f;

    // The text keeps the sizing logic of the storm sign: extra panel width
    // is aura padding around the lettering, not lettering room.
    private const float TextWidthFraction = 0.83f;
    private const float HeightFraction = 0.085f;
    private const float TopAnchor = 0.13f;

    // When the storm sign shares the end-round (a prismatic find right
    // before a storm round) the banner yields the upper quarter and slots
    // in just below the storm sign's cloud instead of beside it — the
    // cloud's puffs bleed past the sign's box bottom, so the gap can even
    // dip slightly negative and still read as a clean stack.
    private const float StormGapFraction = -0.003f;
    private const float StormFollowTopAnchor =
        StormWarn.BoxBottomFraction + StormGapFraction + HeightFraction / 2f;

    private Control _root = null!;
    private Panel _panel = null!;
    private ColorRect _fx = null!;
    private SubViewport _vp = null!;
    private Label _label = null!;
    private Tween _fade = null!;
    private Tween _linger = null!;

    // Set at ShowSign time: true when the storm sign is up for the same
    // end-round, dropping the banner just below the storm cloud.
    private bool _belowStorm;

    // Autoplay reads the tweened alpha to prove the fade is actually
    // visible — the banner's pixels are painted by the shader, which must
    // respect modulate or every fade silently becomes a hard cut.
    public float FadeAlpha => _root.Modulate.A;

    public PrismaticSign()
    {
        Layer = 5;
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
        // No card: the shader paints the pastel aura as the banner's
        // backdrop, so the panel is purely a layout rect.
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorRight = 0.5f;
        _root.AddChild(_panel);

        // The label lives inside its own viewport; its texture is the
        // glyph mask the shader wraps its shine around.
        _vp = new SubViewport
        {
            Disable3D = true,
            TransparentBg = true,
            // Only updated while the banner is on screen — costs nothing
            // when hidden.
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
        };
        _panel.AddChild(_vp);

        _label = Hud.MakeLabel(64, true, new Color(1f, 0.98f, 0.9f));
        _label.Text = "Prismatic";
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

    // Offsets are pixels, so the fractional banner geometry is resolved
    // against the live viewport (re-run on show — the banner is transient
    // and follows window resizes lazily). The viewport size IS the fx
    // rect size, so shader UVs and mask UVs line up pixel for pixel.
    private void LayoutPanel()
    {
        Vector2 view = GetViewport().GetVisibleRect().Size;
        float anchor = _belowStorm ? StormFollowTopAnchor : TopAnchor;
        _panel.AnchorTop = anchor;
        _panel.AnchorBottom = anchor;
        float w = view.X * WidthFraction;
        float h = view.Y * HeightFraction;
        _panel.OffsetLeft = -w / 2f;
        _panel.OffsetRight = w / 2f;
        _panel.OffsetTop = -h / 2f;
        _panel.OffsetBottom = h / 2f;
        _vp.Size = new Vector2I((int)w, (int)h);
        // Start from the height-scaled size, then shrink by measurement
        // until the rendered text fits its slice of the panel width with
        // margin — real font metrics beat any constant divisor.
        _label.LabelSettings.FontSize = Mathf.Max(28, (int)(h * 0.56f));
        while (_label.LabelSettings.FontSize > 28 && _label.GetMinimumSize().X > w * TextWidthFraction)
        {
            _label.LabelSettings.FontSize = Mathf.Max(28, _label.LabelSettings.FontSize - 3);
        }
        if (_fx.Material is ShaderMaterial mat)
        {
            mat.SetShaderParameter("text_mask", _vp.GetTexture());
            mat.SetShaderParameter("texel", new Vector2(1f / w, 1f / h));
        }
    }

    /// <summary>
    /// Rides the banner in over the prismatic round's end. When the storm
    /// sign is up for the same end-round (a prismatic find right before a
    /// storm round), pass <paramref name="belowStormSign"/> so the banner
    /// slots in just below the storm cloud instead of beside it.
    /// </summary>
    public void ShowSign(bool belowStormSign = false)
    {
        _belowStorm = belowStormSign;
        if (Visible && _root.Modulate.A >= 1f)
        {
            LayoutPanel();
            return;
        }
        LayoutPanel();
        _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        Visible = true;
        FadeTo(1f);
    }

    /// <summary>Fades the banner out at once (menu returns, non-follow-ups).</summary>
    public void HideSign()
    {
        if (!Visible)
            return;
        FadeTo(0f);
    }

    /// <summary>
    /// Holds the banner over the next round's opening for
    /// <see cref="LingerSeconds"/>, then dissolves it out over
    /// <see cref="LingerFadeSeconds"/>. StartLevel calls this instead of
    /// <see cref="HideSign"/> when the round being started follows a
    /// prismatic find.
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
        // Banner gone → stop paying for the label viewport.
        _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }
}
