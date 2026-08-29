using Godot;

namespace LeafSweeper;

/// <summary>
/// The hidden critter. Sits still under/above the debris layer, never flees.
/// Supports a camouflage tint for late levels and a golden win celebration:
/// a shining outline fades in while the bug swells up, holds a beat, then
/// settles back down before the round ends.
/// </summary>
public partial class Bug : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";

    private Sprite2D _sprite = null!;
    private BugType _type = null!;

    public BugType Type => _type;

    [Signal] public delegate void CelebrationFinishedEventHandler();

    public void Setup(BugType type, float scale, float camouflage)
    {
        _type = type;
        // A bug node is reused across levels; drop the old sprite first or
        // they stack up on top of each other.
        foreach (Node child in GetChildren())
            child.QueueFree();
        _sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>(type.TexturePath),
            Material = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) },
        };
        // 0 keeps the glow fully off during normal play.
        ((ShaderMaterial)_sprite.Material).SetShaderParameter("intensity", 0.0f);
        AddChild(_sprite);

        // Celebration raises ZIndex above the debris; reset for the new round.
        ZIndex = 0;
        Scale = new Vector2(scale, scale);
        // Camouflage blends the bug toward dusty leaf colors; 0 = fully normal.
        Color leaf = new(0.62f, 0.60f, 0.38f);
        _sprite.Modulate = Colors.White.Lerp(leaf, camouflage);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= _type.TapRadius * Scale.X;

    /// <summary>
    /// Golden discovery moment: the outline shines in while the bug grows,
    /// holds a beat, then flies to the screen center to await its place on
    /// the win card. Emits <see cref="CelebrationFinished"/> when done.
    /// </summary>
    public void Celebrate(Vector2 centerTarget)
    {
        // Pop above every debris layer — the bug is the center of attention.
        ZIndex = 100;
        var mat = (ShaderMaterial)_sprite.Material;

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Scale * 1.45f, 0.25f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenMethod(
            Callable.From<float>(v => mat.SetShaderParameter("intensity", v)), 0.0f, 1.0f, 0.25f);
        tween.TweenInterval(0.35f);
        tween.TweenProperty(this, "position", centerTarget, 0.55f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.CelebrationFinished)));
    }
}

