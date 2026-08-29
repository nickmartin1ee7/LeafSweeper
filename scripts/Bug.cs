using Godot;

namespace LeafSweeper;

/// <summary>
/// The hidden critter. Sits still under/above the debris layer, never flees.
/// Supports a camouflage tint for late levels and a small win celebration.
/// </summary>
public partial class Bug : Node2D
{
    private Sprite2D _sprite = null!;
    private BugType _type = null!;

    public BugType Type => _type;

    public void Setup(BugType type, float scale, float camouflage)
    {
        _type = type;
        // A bug node is reused across levels; drop the old sprite first or
        // they stack up on top of each other.
        foreach (Node child in GetChildren())
            child.QueueFree();
        _sprite = new Sprite2D { Texture = GD.Load<Texture2D>(type.TexturePath) };
        AddChild(_sprite);

        Scale = new Vector2(scale, scale);
        // Camouflage blends the bug toward dusty leaf colors; 0 = fully normal.
        Color leaf = new(0.62f, 0.60f, 0.38f);
        _sprite.Modulate = Colors.White.Lerp(leaf, camouflage);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= _type.TapRadius * Scale.X;

    /// <summary>Small happy pulse when found. Petal sparkles are Main's job.</summary>
    public void Celebrate()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Scale * 1.25f, 0.18f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", Scale, 0.25f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}
