using Godot;

namespace LeafSweeper;

/// <summary>
/// The hidden critter. Sits still below every debris layer.
/// Supports a camouflage tint for late levels and a golden win celebration:
/// a shining outline fades in while the bug swells up, holds a beat, then
/// settles back down before the round ends.
/// </summary>
public partial class Bug : Node2D
{
    private const string OutlineShaderPath = "res://assets/shaders/gold_outline.gdshader";
    private const string PrismaticShaderPath = "res://assets/shaders/prismatic.gdshader";

    private Sprite2D _sprite = null!;
    private BugVariant _variant = null!;

    public BugVariant Variant => _variant;

    /// <summary>The species behind the found variant (flavor, comments).</summary>
    public BugType Type => _variant.Species;

    /// <summary>True when this round's critter rolled the rare prismatic look.</summary>
    public bool IsPrismatic { get; private set; }

    [Signal] public delegate void CelebrationFinishedEventHandler();

    public void Setup(BugVariant variant, float scale, float camouflage,
        bool prismatic = false)
    {
        _variant = variant;
        IsPrismatic = prismatic;
        // A bug node is reused across levels; drop the old sprite first or
        // they stack up on top of each other.
        foreach (Node child in GetChildren())
            child.QueueFree();
        _sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>(variant.TexturePath),
            Material = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) },
        };
        // 0 keeps the glow fully off during normal play.
        ((ShaderMaterial)_sprite.Material).SetShaderParameter("intensity", 0.0f);
        AddChild(_sprite);

        // Z 0 keeps the bug below every debris layer (Z 1 and 2); Celebrate()
        // raises it above everything for the round finale.
        ZIndex = 0;
        Scale = new Vector2(scale, scale);
        // Camouflage blends the bug toward dusty leaf colors; 0 = fully normal.
        // Prismatic critters skip it entirely — they want to be seen (once
        // uncovered) in full rainbow.
        Color leaf = new(0.62f, 0.60f, 0.38f);
        _sprite.Modulate = Colors.White.Lerp(leaf, prismatic ? 0f : camouflage);
        if (prismatic)
            ((ShaderMaterial)_sprite.Material).Shader =
                GD.Load<Shader>(PrismaticShaderPath);
    }

    public bool ContainsPoint(Vector2 worldPoint) =>
        Position.DistanceTo(worldPoint) <= TapRadius;

    /// <summary>World-space radius where taps register: type radius × node scale.</summary>
    public float TapRadius => _variant.Species.TapRadius * Scale.X;

    /// <summary>
    /// World-space radius of the bug's visible body: type occlusion radius ×
    /// node scale. Debris must clear this (not the tap area) to uncover it.
    /// </summary>
    public float OcclusionRadius => _variant.Species.OcclusionRadius * Scale.X;

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

