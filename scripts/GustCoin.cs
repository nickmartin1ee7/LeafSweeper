using Godot;

namespace LeafSweeper;

/// <summary>
/// A gold gust coin hidden below the debris, acting like the bug: it must be
/// uncovered before it can be tapped. Collecting one doesn't end the round —
/// the coin shines golden and swells, then spirals into the dock's gust
/// button, banking +1 gust power for the player.
/// </summary>
public partial class GustCoin : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";
    private const float SpiralTurns = 1.5f;
    private const float SpiralSeconds = 0.8f;

    private Sprite2D _sprite = null!;

    [Signal] public delegate void CollectionFlightFinishedEventHandler();

    public bool Collected { get; private set; }

    /// <summary>World-space radius where taps register: half the drawn coin.</summary>
    public float TapRadius => _sprite.Texture.GetSize().X * _sprite.Scale.X * 0.5f;

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
        AddChild(_sprite);

        // Like the bug: below every debris layer (Z 1 and 2) until collected.
        ZIndex = 0;
        RotationDegrees = rng.RandfRange(-30f, 30f);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= TapRadius;

    /// <summary>
    /// Golden discovery moment: the outline shines in while the coin swells,
    /// then it spins away in a shrinking spiral toward the gust button and
    /// melts out just as it slips under the dock wood. Emits
    /// <see cref="CollectionFlightFinished"/> when done.
    /// </summary>
    public void Collect(Vector2 worldTarget)
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
        Vector2 away = Position - worldTarget;
        float startAngle = away.Angle();
        float startDist = Mathf.Max(away.Length(), 1f);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            float angle = startAngle + t * SpiralTurns * Mathf.Tau;
            Position = worldTarget + Vector2.Right.Rotated(angle) * startDist * (1f - t);
        }), 0.0f, 1.0f, SpiralSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * baseScale * 0.5f, SpiralSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(this, "rotation", Rotation + SpiralTurns * Mathf.Tau, SpiralSeconds);
        // The dock lives on a higher CanvasLayer, so fade out on approach.
        tween.Parallel().TweenProperty(this, "modulate:a", 0f, SpiralSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.CollectionFlightFinished)));
    }
}
