using Godot;

namespace LeafSweeper;

/// <summary>
/// The blizzard rescue key: a small mallet hidden below the debris on
/// winter storm rounds, uncovered like the bug and the gust coins.
/// Collecting one doesn't end the round — it shines golden, winds up a
/// rising CLOCKWISE loop (the mirror of the gust coin's
/// counter-clockwise spiral), then dashes to the top middle of the
/// screen where it floats in place as a power-up. While it hovers
/// there, taps on the ice chunk crack the block (three taps shatter it
/// and pick up the bug). Main lifts it onto the HUD layer for the
/// flight so it passes above the weather; the armed state is banked
/// the moment the float starts.
/// </summary>
public partial class Hammer : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";
    // The wind-up mirrors the gust coin's, but spins the other way —
    // the hammer swings INTO the swing of a strike rather than riding a
    // draft. It sweeps LoopTurns over LoopSeconds while the radius
    // tightens by LoopShrink, which lifts the hammer above its pick-up
    // spot; the dash that follows is the payoff that parks it at the
    // power-up slot.
    private const float LoopTurns = 1.1f;
    private const float LoopSeconds = 0.62f;
    private const float DashSeconds = 0.2f;
    // Loop size rides the hammer→slot distance but stays local, like
    // the coin's — a giant loop would sweep half the screen.
    private const float LoopRadiusRatio = 0.35f;
    private const float LoopRadiusMin = 140f;
    private const float LoopRadiusMax = 260f;
    private const float LoopShrink = 0.55f;
    // Flight-path clamp (same rationale as the coin's): inset so the
    // widest disk stays fully visible while the loop hugs screen edges.
    private const float PathScreenMargin = 80f;
    // The hammer face fills ~55% of its 100×100 grid, so 0.32 × size
    // hugs the visible icon: debris only over the empty margins of the
    // texture doesn't cover.
    private const float OcclusionRatio = 0.32f;
    // Float feel: the parked power-up bobs and sways so it reads alive
    // — a frozen indicator would look like a rendering bug.
    private const float FloatBobSeconds = 1.7f;
    private const float FloatBobHeight = 10f;
    private const float FloatSwayDegrees = 5f;
    // The power-up parks at roughly twice the size it was found: at the
    // top middle it's a held power-up banner, not floor clutter, and the
    // swell into it (a Back ease after the dash) is the "armed" beat.
    private const float FloatScale = 1.6f;

    private Sprite2D _sprite = null!;

    [Signal] public delegate void FloatStartedEventHandler();

    public bool Collected { get; private set; }

    /// <summary>True once the hammer reached the slot and floats there.</summary>
    public bool Floating { get; private set; }

    /// <summary>World-space radius where taps register: half the drawn icon.</summary>
    public float TapRadius => _sprite.Texture.GetSize().X * _sprite.Scale.X * 0.5f;

    /// <summary>
    /// World-space radius of the hammer's visible face that debris must
    /// clear before it counts as uncovered — tighter than TapRadius.
    /// </summary>
    public float OcclusionRadius { get; private set; }

    private Vector2 _floatAnchor;
    private float _floatAge;
    // The collect tween is kept so a mid-flight interruption (viewport
    // resize) can kill it and park the hammer at the new slot instantly.
    private Tween? _flightTween;
    private float _baseScale = 1f;

    public void Setup(float size, RandomNumberGenerator rng)
    {
        _sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/icons/hammer.svg"),
            Material = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) },
        };
        // 0 keeps the glow fully off during normal play.
        ((ShaderMaterial)_sprite.Material).SetShaderParameter("intensity", 0.0f);
        float texSize = _sprite.Texture.GetSize().X;
        float s = size / Mathf.Max(texSize, 1f);
        _sprite.Scale = new Vector2(s, s);
        OcclusionRadius = size * OcclusionRatio;
        AddChild(_sprite);

        // Like the bug and the coins: below every debris layer (Z 1 and
        // 2) until collected.
        ZIndex = 0;
        RotationDegrees = rng.RandfRange(-30f, 30f);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= TapRadius;

    /// <summary>
    /// Golden discovery moment: the outline shines in while the hammer
    /// swells, then it winds up a rising CLOCKWISE loop above its
    /// pick-up spot and dashes to the top-middle power-up slot,
    /// staying fully visible the whole way (the path is clamped to the
    /// visible screen like the coin's). It emits
    /// <see cref="FloatStarted"/> the instant it reaches the slot, then
    /// floats there — it is a held power-up, not an absorbed one.
    /// </summary>
    public void Collect(Vector2 floatTarget)
    {
        Collected = true;
        // Pop above every debris layer and UI — the hammer is the
        // center of attention until it parks.
        ZIndex = 100;
        var mat = (ShaderMaterial)_sprite.Material;
        _baseScale = Scale.X;
        float baseScale = _baseScale;

        var tween = _flightTween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One * baseScale * 1.45f, 0.25f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenMethod(
            Callable.From<float>(v => mat.SetShaderParameter("intensity", v)), 0.0f, 1.0f, 0.25f);
        tween.TweenInterval(0.3f);

        // The wind-up: a rising CLOCKWISE loop pivoted just above the
        // pick-up spot. Increasing the angle sweeps the hammer
        // clockwise on screen (y-down flips the visual direction) —
        // the coin's spiral runs the other way.
        Vector2 away = Position - floatTarget;
        float startDist = Mathf.Max(away.Length(), 1f);
        float loopRadius = Mathf.Clamp(startDist * LoopRadiusRatio, LoopRadiusMin, LoopRadiusMax);
        Vector2 loopCenter = Position - Vector2.Down * loopRadius;
        float startAngle = (Position - loopCenter).Angle();
        Rect2 pathBounds = GetViewportRect().Grow(-PathScreenMargin);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            float angle = startAngle + t * LoopTurns * Mathf.Tau;
            Vector2 pos = loopCenter
                + Vector2.Right.Rotated(angle) * (loopRadius * (1f - LoopShrink * t));
            Position = new Vector2(
                Mathf.Clamp(pos.X, pathBounds.Position.X, pathBounds.End.X),
                Mathf.Clamp(pos.Y, pathBounds.Position.Y, pathBounds.End.Y));
        }), 0.0f, 1.0f, LoopSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.Parallel().TweenProperty(this, "rotation", Rotation + LoopTurns * Mathf.Tau, LoopSeconds);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * baseScale * 1.15f, LoopSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        // The bam: a short accelerating dash that parks the hammer at
        // the slot at its flight scale, then it swells into its float
        // size — the "armed" beat.
        tween.TweenProperty(this, "position", floatTarget, DashSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        // ...then it swells into its float size — the "armed" beat.
        tween.TweenCallback(Callable.From(() =>
        {
            Rotation = 0f;
            _floatAnchor = Position;
            _floatAge = 0f;
            Floating = true;
            EmitSignal(SignalName.FloatStarted);
        }));
        tween.TweenProperty(this, "scale", Vector2.One * baseScale * FloatScale, 0.22f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Re-parks a floating hammer after a viewport resize (the slot is
    /// screen-relative, so the anchor must ride the new size).
    /// </summary>
    public void SnapFloat(Vector2 anchor)
    {
        if (!Floating)
            return;
        _floatAnchor = anchor;
        Position = anchor;
    }

    /// <summary>
    /// Aborts a mid-flight collect and parks the hammer at the slot
    /// immediately (viewport resize during the flight): the tween path
    /// was computed against the old screen size, so without this the
    /// hammer would land on a stale anchor and float there for the rest
    /// of the round. Emits <see cref="FloatStarted"/> so the armed state
    /// banks exactly as the uninterrupted flight would have.
    /// </summary>
    public void SkipToFloat(Vector2 anchor)
    {
        if (!Collected || Floating)
            return;
        _flightTween?.Kill();
        _flightTween = null;
        Rotation = 0f;
        Scale = Vector2.One * _baseScale * FloatScale;
        _floatAnchor = anchor;
        Position = anchor;
        _floatAge = 0f;
        Floating = true;
        EmitSignal(SignalName.FloatStarted);
    }

    public override void _Process(double delta)
    {
        if (!Floating)
            return;
        _floatAge += (float)delta;
        // Bob and sway around the slot: a held tool idles like it's
        // ready to swing.
        Position = _floatAnchor
            + Vector2.Down * (Mathf.Sin(_floatAge * Mathf.Tau / FloatBobSeconds) * FloatBobHeight);
        RotationDegrees = Mathf.Sin(_floatAge * Mathf.Tau / (FloatBobSeconds * 1.4f)) * FloatSwayDegrees;
    }
}
