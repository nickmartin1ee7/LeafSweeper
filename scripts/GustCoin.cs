using Godot;

namespace LeafSweeper;

/// <summary>
/// A gold gust coin hidden below the debris, acting like the bug: it must be
/// uncovered before it can be tapped. Collecting one doesn't end the round —
/// the coin shines golden and swells, winds up a rising counter-clockwise
/// loop like it caught the gust it's named for, then snaps down into the
/// dock's gust button. Main lifts it onto the HUD layer for the flight so it
/// passes above everything, dock included; banking the +1 gust power happens
/// when the coin arrives and makes the counter pulse.
/// </summary>
public partial class GustCoin : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";
    private const string WindIconPath = "res://assets/icons/wind.svg";
    private const float WindIconRatio = 0.62f;
    // The wind-up: a rising counter-clockwise loop that sells the gust —
    // the coin spins upward like a leaf caught in a draft instead of
    // draining straight into the dock. It sweeps LoopTurns over LoopSeconds
    // while the radius tightens by LoopShrink, which lifts the coin above
    // its pick-up spot; the dash that follows is the payoff that snaps it
    // onto the button.
    private const float LoopTurns = 1.1f;
    private const float LoopSeconds = 0.62f;
    private const float DashSeconds = 0.2f;
    // Loop size rides the coin→button distance (a coin found far from the
    // dock earns a bigger wind-up) but stays local — a giant loop would
    // sweep half the screen and read as chaos instead of a flourish.
    private const float LoopRadiusRatio = 0.35f;
    private const float LoopRadiusMin = 140f;
    private const float LoopRadiusMax = 260f;
    private const float LoopShrink = 0.55f;
    // Flight-path clamp: the wind-up loop pivots just above the coin, and
    // when a coin is uncovered near the screen's top or side edges the loop
    // arcs past them. Insetting the clamp by 80 px keeps the coin's widest
    // disk (~72 px at the 1.45× swell) fully visible while the loop hugs
    // the screen edge; the dash stays inside because both of its endpoints
    // already are.
    private const float PathScreenMargin = 80f;
    // The coin disk fills ~60% of its 100×100 grid, so 0.3 × size hugs the
    // visible disk: debris only over the empty texture margins doesn't cover.
    private const float OcclusionRatio = 0.3f;

    private Sprite2D _sprite = null!;

    [Signal] public delegate void CollectionFlightFinishedEventHandler();

    public bool Collected { get; private set; }

    /// <summary>World-space radius where taps register: half the drawn coin.</summary>
    public float TapRadius => _sprite.Texture.GetSize().X * _sprite.Scale.X * 0.5f;

    /// <summary>
    /// World-space radius of the coin's visible disk that debris must clear
    /// before it counts as uncovered — tighter than the forgiving TapRadius.
    /// </summary>
    public float OcclusionRadius { get; private set; }

    public void Setup(float size, RandomNumberGenerator rng)
    {
        _sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/icons/coin.svg"),
            Material = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) },
        };
        // 0 keeps the glow fully off during normal play.
        ((ShaderMaterial)_sprite.Material).SetShaderParameter("intensity", 0.0f);
        float texSize = _sprite.Texture.GetSize().X;
        float s = size / Mathf.Max(texSize, 1f);
        _sprite.Scale = new Vector2(s, s);
        OcclusionRadius = size * OcclusionRatio;
        AddChild(_sprite);

        // The gust mark on the coin face, echoing the dock's gust button
        // (both textures share the same 100×100 grid, so one ratio fits).
        var icon = new Sprite2D { Texture = GD.Load<Texture2D>(WindIconPath) };
        icon.Scale = Vector2.One * s * WindIconRatio;
        AddChild(icon);

        // Like the bug: below every debris layer (Z 1 and 2) until collected.
        ZIndex = 0;
        RotationDegrees = rng.RandfRange(-30f, 30f);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= TapRadius;

    /// <summary>
    /// Golden discovery moment: the outline shines in while the coin swells,
    /// then it winds up a rising counter-clockwise loop above its pick-up
    /// spot and snaps down onto the gust button, staying fully visible the
    /// whole way — the coin rides on the HUD layer, above the dock itself,
    /// and the loop path is clamped to the visible screen so it hugs the
    /// screen edges instead of leaving the view. Emits
    /// <see cref="CollectionFlightFinished"/> the instant it reaches the
    /// button, then melts into it.
    /// </summary>
    public void Collect(Vector2 flightTarget)
    {
        Collected = true;
        // Pop above every debris layer — the coin is the center of attention.
        ZIndex = 100;
        var mat = (ShaderMaterial)_sprite.Material;
        float baseScale = Scale.X;

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One * baseScale * 1.45f, 0.25f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenMethod(
            Callable.From<float>(v => mat.SetShaderParameter("intensity", v)), 0.0f, 1.0f, 0.25f);
        tween.TweenInterval(0.3f);

        // The wind-up: a rising counter-clockwise loop pivoted just above
        // the coin's pick-up spot. Decreasing the angle sweeps the coin
        // counter-clockwise on screen (y-down flips the visual direction),
        // and tightening the radius as it turns lifts it upward — each
        // traveled point is clamped into the visible screen (inset by
        // PathScreenMargin) so a coin uncovered near an edge hugs it.
        Vector2 away = Position - flightTarget;
        float startDist = Mathf.Max(away.Length(), 1f);
        float loopRadius = Mathf.Clamp(startDist * LoopRadiusRatio, LoopRadiusMin, LoopRadiusMax);
        Vector2 loopCenter = Position - Vector2.Down * loopRadius;
        float startAngle = (Position - loopCenter).Angle();
        Rect2 pathBounds = GetViewportRect().Grow(-PathScreenMargin);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            float angle = startAngle - t * LoopTurns * Mathf.Tau;
            Vector2 pos = loopCenter
                + Vector2.Right.Rotated(angle) * (loopRadius * (1f - LoopShrink * t));
            Position = new Vector2(
                Mathf.Clamp(pos.X, pathBounds.Position.X, pathBounds.End.X),
                Mathf.Clamp(pos.Y, pathBounds.Position.Y, pathBounds.End.Y));
        }), 0.0f, 1.0f, LoopSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.Parallel().TweenProperty(this, "rotation", Rotation - LoopTurns * Mathf.Tau, LoopSeconds);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * baseScale * 1.15f, LoopSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        // The bam: a short accelerating dash that snaps the coin onto the
        // button — the loop never touches the dock, the landing does.
        tween.TweenProperty(this, "position", flightTarget, DashSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * baseScale * 0.5f, DashSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        // Arrived: bank the power now (the counter pulses), and the coin
        // melts away into the button as it's absorbed.
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.CollectionFlightFinished)));
        tween.TweenProperty(this, "scale", Vector2.One * baseScale * 0.04f, 0.14f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
