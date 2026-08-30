using Godot;

namespace LeafSweeper;

/// <summary>
/// A gold gust coin hidden below the debris, acting like the bug: it must be
/// uncovered before it can be tapped. Collecting one doesn't end the round —
/// the coin shines golden and swells, then spirals into the dock's gust
/// button. Main lifts it onto the HUD layer for the flight so it passes
/// above everything, dock included; banking the +1 gust power happens when
/// the coin arrives and makes the counter pulse.
/// </summary>
public partial class GustCoin : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";
    private const string WindIconPath = "res://assets/icons/wind.svg";
    private const float WindIconRatio = 0.62f;
    private const float SpiralTurns = 1.5f;
    private const float SpiralSeconds = 0.8f;
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
    /// then it spins away in a shrinking spiral toward the gust button,
    /// staying fully visible the whole way — the coin rides on the HUD layer,
    /// above the dock itself. Emits <see cref="CollectionFlightFinished"/>
    /// the instant it reaches the button, then melts into it.
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

        // Shrinking spiral: the radius decays from the coin's current
        // distance down to zero while it sweeps around the target.
        Vector2 away = Position - flightTarget;
        float startAngle = away.Angle();
        float startDist = Mathf.Max(away.Length(), 1f);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            float angle = startAngle + t * SpiralTurns * Mathf.Tau;
            Position = flightTarget + Vector2.Right.Rotated(angle) * startDist * (1f - t);
        }), 0.0f, 1.0f, SpiralSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * baseScale * 0.5f, SpiralSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(this, "rotation", Rotation + SpiralTurns * Mathf.Tau, SpiralSeconds);
        // Arrived: bank the power now (the counter pulses), and the coin
        // melts away into the button as it's absorbed.
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.CollectionFlightFinished)));
        tween.TweenProperty(this, "scale", Vector2.One * baseScale * 0.04f, 0.14f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
