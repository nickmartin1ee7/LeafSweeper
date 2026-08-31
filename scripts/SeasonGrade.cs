using Godot;

namespace LeafSweeper;

/// <summary>
/// Seasonal vibe grade: a full-rect, touch-transparent ColorRect running
/// assets/shaders/season.gdshader — the only screen-reading node in the
/// game, so every level of a season carries the season's general mood
/// (Spring fresh & clear, Summer warm golden, Fall amber, Winter cold and
/// pale) while the storm veil and every UI draw on top untouched. Sits at
/// the bottom of the UI ladder — explicit ladder: world 0 → season grade 1
/// → storm 2 → menu 3 → hud 4 → warn 5 → prismatic 6 → season banner 7 →
/// bug book 90. The grade fades in like weather via its `intensity`
/// uniform and hides itself entirely while off, so non-graded time costs
/// nothing.
/// </summary>
public partial class SeasonGrade : CanvasLayer
{
    private const string ShaderPath = "res://assets/shaders/season.gdshader";

    // Perf fallback: the screen-reading grade is the only back-buffer copy
    // in the game; if a device playtest shows its cost, flip this off and
    // the grade becomes a plain alpha-composite tint veil (storm-style, no
    // screen read) at VeilAlpha — visibly cruder, but nearly free.
    private const bool ScreenReadGrade = true;
    private const float VeilAlpha = 0.28f;

    // A slow fade so the season arrives like weather, not a light switch.
    private const float FadeSeconds = 1.4f;

    /// <summary>
    /// Per-season look: tint multiplier, saturation, brightness, plus the
    /// season's atmosphere cast (a screen-blended hue — see
    /// season.gdshader) and how strongly it applies.
    /// </summary>
    public readonly record struct Grade(Color Tint, float Saturation, float Brightness,
        Color Cast, float CastAmount);

    // The four looks. Each season must read at a glance (playtest: the
    // first tints were too subtle to notice), so every season pairs a
    // definite tint with a soft cast of its own hue — Spring a fresh green
    // lift, Summer a warm golden haze, Fall a deep amber low sun, Winter a
    // cold desaturated blue (difficult visibility stays the blizzard's
    // job, not the grade's).
    public static readonly Grade[] Grades =
    {
        new(new(0.94f, 1.05f, 0.94f), 1.10f, 1.02f,
            new(0.88f, 1.00f, 0.84f), 0.10f),  // Spring: fresh & green
        new(new(1.10f, 1.00f, 0.84f), 1.10f, 1.04f,
            new(1.00f, 0.82f, 0.52f), 0.15f),  // Summer: warm golden haze
        new(new(1.16f, 0.95f, 0.72f), 1.05f, 0.97f,
            new(0.95f, 0.60f, 0.28f), 0.16f),  // Fall: amber low sun
        new(new(0.84f, 0.92f, 1.10f), 0.62f, 0.98f,
            new(0.60f, 0.72f, 0.90f), 0.14f),  // Winter: cold, pale
    };

    private ColorRect _rect = null!;
    private ShaderMaterial _material = null!;
    private Tween _fade = null!;

    // Current look, mirrored from the uniforms so probes (and the veil
    // fallback) can read what the grade is showing.
    public Grade Current { get; private set; }

    /// <summary>Current grade intensity — 0 hidden, 1 full grade.</summary>
    public float Intensity { get; private set; }

    public SeasonGrade()
    {
        // Explicit canvas ladder (declared in Main.BuildTree): the grade
        // sits directly above the world, below the storm veil.
        Layer = 1;
        Visible = false;
    }

    public override void _Ready()
    {
        _rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        if (ScreenReadGrade)
        {
            _material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };
            _material.SetShaderParameter("intensity", 0f);
            _rect.Material = _material;
        }
        AddChild(_rect);
        // Anchors AND offsets: plain SetAnchorsPreset leaves the offsets
        // preserving the rect's old zero size — the full-screen rect stays
        // 0×0 and renders nothing (the storm overlay's trap).
        _rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>
    /// Cross-fades to a season's look. Same-season calls are free (the
    /// fade continues undisturbed); a new season swaps the uniforms
    /// immediately — during the fade the look snaps to the new season's,
    /// which reads as the weather turning, and both grades are gentle
    /// enough that the swap never pops.
    /// </summary>
    public void ShowSeason(RoundConfig.Season season)
    {
        Current = Grades[(int)season];
        ApplyUniforms(Current);
        if (!Visible)
        {
            Visible = true;
            Intensity = 0f;
            if (!ScreenReadGrade)
                _rect.Color = new Color(Current.Cast, 0f);
        }
        FadeTo(1f);
    }

    /// <summary>Fades the grade out and hides the layer once it's gone.</summary>
    public void HideGrade()
    {
        FadeTo(0f);
    }

    private void ApplyUniforms(Grade grade)
    {
        if (!ScreenReadGrade)
            return;
        _material.SetShaderParameter("tint",
            new Vector3(grade.Tint.R, grade.Tint.G, grade.Tint.B));
        _material.SetShaderParameter("saturation", grade.Saturation);
        _material.SetShaderParameter("brightness", grade.Brightness);
        _material.SetShaderParameter("cast",
            new Vector3(grade.Cast.R, grade.Cast.G, grade.Cast.B));
        _material.SetShaderParameter("cast_amount", grade.CastAmount);
    }

    private void FadeTo(float target)
    {
        if (_fade != null && _fade.IsValid())
            _fade.Kill();
        _fade = CreateTween();
        if (ScreenReadGrade)
        {
            _fade.TweenMethod(Callable.From<float>(v =>
            {
                Intensity = v;
                _material.SetShaderParameter("intensity", v);
            }), Intensity, target, FadeSeconds);
        }
        else
        {
            _fade.TweenMethod(Callable.From<float>(v =>
            {
                Intensity = v;
                _rect.Color = new Color(Current.Tint, v * VeilAlpha);
            }), Intensity, target, FadeSeconds);
        }
        if (target == 0f)
            _fade.TweenCallback(Callable.From(() => Visible = false));
    }
}
