using Godot;
using System.Collections.Generic;

namespace LeafSweeper;

/// <summary>
/// The frozen-bug rescue: blizzard rounds wrap the bug in a translucent
/// ice chunk. The ice counts as cover for the occlusion rule, and once
/// the debris around it is cleared, tapping the ice cracks it — three
/// taps with three visible fracture stages, the last shattering the
/// block in a celebratory shard burst that sets the bug free. Main
/// routes the taps; this node owns the visual state machine and the
/// shards. Like the churn props it never moves gameplay nodes.
/// </summary>
public partial class IceBlock : Node2D
{
    // Crack area around the bug: a tap within this radius hits the ice.
    public const float Radius = 90f;

    // The chunk's visible half-extent (ice_chunk.svg draws its shape in
    // ~64 of the 100-unit texture, scaled by BugWrapScale 1.6): debris
    // counts as covering the ice only when its pixels reach inside this
    // ring. Widen it past the eye's ice and leaves lying BESIDE the
    // chunk flash as blockers the player can't see a reason for.
    public const float BlockerRadius = 55f;

    // Feel tunables: the chunk wraps the bug with margin; each crack
    // shakes the block briefly; shards fling radially and fade fast.
    private const float BugWrapScale = 1.6f;
    private const int HitsToShatter = 3;
    private const float ShakeSeconds = 0.28f;
    private const float ShakeAmount = 6f;    // px at the shake's peak
    private const int ShardCount = 10;
    private const float ShardSeconds = 0.65f;
    private const float ShardSpeed = 340f;   // px/s radial launch
    private const float LockPulseSeconds = 0.55f;

    private static readonly Color LockPulseColor = new(1f, 0.45f, 0.4f);

    private class Shard
    {
        public Sprite2D Sprite = null!;
        public Vector2 Velocity;
        public float Spin;
        public float Age;
    }

    private static readonly Texture2D ChunkTex =
        GD.Load<Texture2D>("res://assets/textures/ice_chunk.svg");
    private static readonly Texture2D Cracks1Tex =
        GD.Load<Texture2D>("res://assets/textures/ice_crack_1.svg");
    private static readonly Texture2D Cracks2Tex =
        GD.Load<Texture2D>("res://assets/textures/ice_crack_2.svg");

    private readonly Sprite2D _chunk = new() { Texture = ChunkTex };
    private readonly Sprite2D _cracks1 = new() { Texture = Cracks1Tex, Visible = false };
    private readonly Sprite2D _cracks2 = new() { Texture = Cracks2Tex, Visible = false };

    private readonly List<Shard> _shards = new();
    private float _shakeAge = -1f; // -1 = no shake in flight
    private float _lockAge = -1f;  // -1 = no locked pulse in flight
    private Vector2 _home;

    public IceBlock()
    {
        // Above the bug (0), level with the lower litter (1): the pile
        // visibly buries the ice, and clearing it exposes the chunk.
        ZIndex = 1;
        Visible = false;
        _chunk.Scale = Vector2.One * BugWrapScale;
        _cracks1.Scale = Vector2.One * BugWrapScale;
        _cracks2.Scale = Vector2.One * BugWrapScale;
        AddChild(_chunk);
        AddChild(_cracks1);
        AddChild(_cracks2);
    }

    /// <summary>Crack taps landed so far (0 before the first, 3 = shattered).</summary>
    public int Hits { get; private set; }

    /// <summary>True while the ice wraps the bug (pre-shatter).</summary>
    public bool Active { get; private set; }

    /// <summary>True once the third tap shattered the block.</summary>
    public bool Shattered { get; private set; }

    /// <summary>True while the tap point is over the ice.</summary>
    public bool ContainsPoint(Vector2 world) =>
        Position.DistanceTo(world) <= Radius;

    /// <summary>Wraps the bug: visible, fractures cleared, fresh shatter state.</summary>
    public void Place(Vector2 bugSpot)
    {
        Position = bugSpot;
        _home = bugSpot;
        Active = true;
        Shattered = false;
        Hits = 0;
        Visible = true;
        _cracks1.Visible = false;
        _cracks2.Visible = false;
        _chunk.Visible = true;
        _chunk.Position = Vector2.Zero;
        _chunk.RotationDegrees = 0f;
        _shakeAge = -1f;
    }

    /// <summary>Hides the block (non-blizzard rounds / round teardown).</summary>
    public void Reset()
    {
        Active = false;
        Visible = false;
    }

    /// <summary>
    /// Red "locked" flare on the chunk: the tap landed on cleared ice but
    /// the hammer power-up isn't armed, so the refusal must read as a
    /// reason instead of silence. The shake (Position/Rotation) and this
    /// pulse (Modulate) own disjoint sprite properties.
    /// </summary>
    public void PulseLocked()
    {
        if (!Active)
            return;
        _lockAge = 0f;
    }

    /// <summary>
    /// One crack: advances the visible fracture stage with a shake; the
    /// third tap shatters the block into a shard burst. The bug stays
    /// covered until the shatter — the rescue should feel earned.
    /// </summary>
    public void Crack()
    {
        if (!Active)
            return;
        Hits++;
        _shakeAge = 0f;
        if (Hits == 1)
        {
            _cracks1.Visible = true;
            return;
        }
        if (Hits == 2)
        {
            _cracks1.Visible = false;
            _cracks2.Visible = true;
            return;
        }
        // Third tap: shatter. The block itself vanishes; the shards are
        // children, so the node stays visible until they finish flying.
        Active = false;
        Shattered = true;
        _chunk.Visible = false;
        _cracks1.Visible = false;
        _cracks2.Visible = false;
        // Shard churn uses Godot's global RNG on purpose: the burst is
        // purely cosmetic, and the seeded generator stays reserved for
        // reproducible round layouts.
        for (int i = 0; i < ShardCount; i++)
        {
            float ang = i * Mathf.Tau / ShardCount + GD.Randf() * 0.5f;
            Shard s = new()
            {
                Sprite = new Sprite2D
                {
                    Texture = ChunkTex,
                    Scale = Vector2.One * (float)GD.RandRange(0.12, 0.22),
                    RotationDegrees = (float)GD.RandRange(0f, 360f),
                    ZIndex = 3,
                },
                Velocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))
                    * ShardSpeed * (float)GD.RandRange(0.7, 1.3),
                Spin = (float)GD.RandRange(-240f, 240f),
                Age = 0f,
            };
            AddChild(s.Sprite);
            _shards.Add(s);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Locked pulse: the chunk flares red fast and cools back to ice.
        if (_lockAge >= 0f && _chunk.Visible)
        {
            _lockAge += dt;
            float p = _lockAge / LockPulseSeconds;
            if (p >= 1f)
            {
                _lockAge = -1f;
                _chunk.Modulate = Colors.White;
            }
            else
            {
                float flare = p < 0.25f ? p / 0.25f : 1f - (p - 0.25f) / 0.75f;
                _chunk.Modulate = Colors.White.Lerp(LockPulseColor, flare);
            }
        }

        // Shake: a fast decaying wobble on the chunk after each crack.
        if (_shakeAge >= 0f && _chunk.Visible)
        {
            _shakeAge += dt;
            float p = _shakeAge / ShakeSeconds;
            if (p >= 1f)
            {
                _shakeAge = -1f;
                _chunk.Position = Vector2.Zero;
                _chunk.RotationDegrees = 0f;
            }
            else
            {
                float decay = 1f - p;
                _chunk.Position = new Vector2(
                    Mathf.Sin(_shakeAge * 62f), Mathf.Cos(_shakeAge * 71f))
                    * ShakeAmount * decay;
                _chunk.RotationDegrees = Mathf.Sin(_shakeAge * 55f) * 4f * decay;
            }
        }

        // Shard burst: fly, spin, fade, free — then the node hides itself.
        for (int i = _shards.Count - 1; i >= 0; i--)
        {
            Shard s = _shards[i];
            s.Age += dt;
            if (s.Age >= ShardSeconds)
            {
                s.Sprite.QueueFree();
                _shards.RemoveAt(i);
                continue;
            }
            s.Sprite.Position += s.Velocity * dt;
            s.Sprite.RotationDegrees += s.Spin * dt;
            float fade = 1f - Mathf.Clamp((s.Age - ShardSeconds * 0.5f)
                / (ShardSeconds * 0.5f), 0f, 1f);
            s.Sprite.Modulate = new Color(1f, 1f, 1f, fade);
        }
        if (Shattered && _shards.Count == 0 && Visible)
            Visible = false;
    }
}