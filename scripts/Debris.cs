using Godot;

namespace LeafSweeper;

public enum DebrisWeight
{
    Light,   // leaves, petals
    Medium,  // moss
    Heavy,   // sticks, rocks
}

/// <summary>
/// One piece of ground clutter. Swept debris gets a velocity + spin,
/// slides with exponential friction, fades out and frees itself.
/// Heavier pieces launch slower but glide farther and linger longer
/// before fading, so their slide reads as weight.
/// Lightweight custom movement: no physics engine.
/// </summary>
public partial class Debris : Node2D
{
    private static readonly float[] FlingFactor = { 0.65f, 0.5f, 0.35f };
    // Friction drops with weight: total slide distance ≈ v0/friction, so
    // sticks and rocks glide out of the swept patch while leaves flick
    // away and vanish almost where they land.
    private static readonly float[] Friction = { 3.4f, 2.3f, 1.5f };
    // Heavier debris lingers before fading so its longer slide is visible.
    private static readonly float[] FadeDelayScale = { 1.0f, 1.35f, 1.7f };

    private Sprite2D _sprite = null!;
    private Vector2 _velocity;
    private float _angularVel;
    private float _fadeDelay = 0.55f;
    private float _age;

    public bool Swept { get; private set; }
    public DebrisWeight Weight { get; private set; }

    /// <summary>
    /// The playable rect debris may occupy. Flung pieces clamp against its
    /// edge with a soft bounce — nothing slides underneath the HUD dock.
    /// </summary>
    public Rect2 Bounds { get; set; } = new(-100000f, -100000f, 200000f, 200000f);

    public void Setup(string texturePath, Vector2 pos, float rotDeg,
        float scale, DebrisWeight weight, RandomNumberGenerator rng)
    {
        Position = pos;
        RotationDegrees = rotDeg;
        Weight = weight;

        _sprite = new Sprite2D { Texture = GD.Load<Texture2D>(texturePath) };
        _sprite.Scale = new Vector2(scale, scale);
        AddChild(_sprite);

        // Per-instance variety so identical textures don't look stamped.
        _sprite.SelfModulate = new Color(1, 1, 1, 1).Lerp(
            new Color(0.92f, 0.92f, 0.88f, 1), rng.Randf());
    }

    public void Fling(Vector2 pointerVelocity, RandomNumberGenerator rng)
    {
        if (Swept)
            return;
        Swept = true;

        int w = (int)Weight;
        float speed = pointerVelocity.Length();
        Vector2 dir = speed > 1f ? pointerVelocity.Normalized() : Vector2.Right;
        // Weight dampens the fling; a little jitter keeps it organic.
        float fling = speed * FlingFactor[w] * rng.RandfRange(0.9f, 1.25f);
        dir = dir.Rotated(rng.RandfRange(-0.25f, 0.25f));

        _velocity = dir * fling;
        _angularVel = rng.RandfRange(-7f, 7f) * Mathf.Clamp(fling / 400f, 0.3f, 1.6f);
        _fadeDelay = rng.RandfRange(0.35f, 0.6f) * FadeDelayScale[w];
    }

    public override void _Process(double delta)
    {
        if (!Swept)
            return;

        float dt = (float)delta;
        Position += _velocity * dt;
        Rotation += _angularVel * dt;

        // Keep flung debris out of the dock: clamp and bounce softly.
        const float edge = 30f;
        Vector2 min = Bounds.Position + new Vector2(edge, edge);
        Vector2 max = Bounds.End - new Vector2(edge, edge);
        if (Position.X < min.X) { Position = new Vector2(min.X, Position.Y); _velocity.X = Mathf.Abs(_velocity.X) * 0.35f; }
        else if (Position.X > max.X) { Position = new Vector2(max.X, Position.Y); _velocity.X = -Mathf.Abs(_velocity.X) * 0.35f; }
        if (Position.Y < min.Y) { Position = new Vector2(Position.X, min.Y); _velocity.Y = Mathf.Abs(_velocity.Y) * 0.35f; }
        else if (Position.Y > max.Y) { Position = new Vector2(Position.X, max.Y); _velocity.Y = -Mathf.Abs(_velocity.Y) * 0.35f; }

        float dampen = Mathf.Exp(-Friction[(int)Weight] * dt);
        _velocity *= dampen;
        _angularVel *= dampen;

        _age += dt;
        if (_age > _fadeDelay)
        {
            Modulate = new Color(1, 1, 1, Mathf.Clamp(Modulate.A - dt * 1.6f, 0f, 1f));
            if (Modulate.A <= 0.01f)
                QueueFree();
        }
    }
}
