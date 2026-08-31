using Godot;
using System.Collections.Generic;

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
/// At round start pieces drop in with <see cref="SettleIn"/>; after a win
/// the survivors ride the end-of-round gyre via <see cref="StartEndRoundWind"/>.
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
    // Ambient rustle reach shrinks with weight: leaves flick visibly in a
    // stray draft, rocks barely shiver (see Rustle in Main.cs's scheduler).
    private static readonly float[] RustleAmp = { 5f, 3f, 1.6f };

    private Sprite2D _sprite = null!;
    private Vector2 _velocity;
    private float _angularVel;
    private float _fadeDelay = 0.55f;
    private float _age;

    // End-of-round wind feel: once the round is won the leftover pieces
    // get picked up by a slow clockwise gyre around the floor's center.
    // Speeds are rad/s; per-piece jitter makes inner and outer pieces
    // shear past each other instead of turning like a rigid carousel,
    // a breathing lane radius keeps the ring loose and organic, and a
    // gentle vertical bob reads as leaves rising and sinking in a draft.
    private const float WindSpeedBase = 1.15f;    // rad/s mean orbit speed
    private const float WindSpeedJitter = 0.5f;   // ± fraction of mean, per piece
    private const float WindEaseSeconds = 1.8f;   // smoothstep ramp from rest to full gyre
    private const float WindSpinBase = 1.2f;      // rad/s mean self-spin
    private const float WindBreathAmp = 0.12f;    // lane radius breathing, fraction of radius
    private const float WindBreathFreq = 0.9f;    // breathing cycles per second
    private const float WindBobAmp = 14f;         // px of vertical draft bob
    private const float WindBobFreq = 1.1f;       // bob cycles per second

    // Round-start settle feel: pieces drop in from above and land softly
    // on their spots, staggered along a diagonal so the litter arrives
    // like a curtain swept across the floor rather than a single dump.
    private const float SettleHeightMin = 260f;    // px above the final spot the fall starts from
    private const float SettleHeightExtra = 220f;  // extra start-height jitter
    private const float SettleSeconds = 0.85f;     // per-piece fall duration (±15% jitter)
    private const float SettleSweepSeconds = 1.4f; // diagonal stagger across the whole floor
    private const float SettleJitter = 0.25f;      // seconds of random stagger on top of the sweep
    private const float SettleSpinTurns = 2.2f;    // max full turns while tumbling down

    // Ambient rustle feel: a stray draft brushes the piece for a fraction
    // of a second and it settles back onto its spot. The wobble lives on
    // the child sprite only, so the node transform — everything gameplay
    // reads (coverage, sweeping, the wind gyre) — never moves.
    private const float RustleSeconds = 0.55f;     // shiver duration (±20% jitter)
    private const float RustleWobbleRate = 26f;    // rad/s of the shiver oscillation
    private const float RustleWobbleJitter = 0.25f; // ± fraction of the wobble rate
    private const float RustleTurn = 9f;           // max degrees of rotation wiggle

    // End-of-round wind state: when active the piece orbits/rotates without
    // fading. Initialized by StartEndRoundWind().
    private bool _windActive;
    private Vector2 _windCenter;
    private float _windAge;
    private float _windEase;
    private float _windPhase;
    private float _windRadius;
    private float _windAngularSpeed;
    private float _windSpin;
    private float _windBreathOffset;
    private float _windBreathFreq;
    private float _windBobAmp;
    private float _windBobFreq;
    private float _windBobOffset;

    // Round-start settle state: initialized by SettleIn().
    private bool _settling;
    private float _settleAge;
    private float _settleSeconds;
    private Vector2 _settleFrom;
    private Vector2 _settleTarget;
    private float _settleFromRot;
    private float _settleTargetRot;

    // Ambient rustle state: initialized by Rustle().
    private bool _rustling;
    private float _rustleAge;
    private float _rustleSeconds;
    private Vector2 _rustleDir;
    private float _rustleAmp;
    private float _rustleWobbleRate;
    private float _rustleTurn;

    // Alpha-mask resolution: one cache byte per 4px texture cell. Fine
    // enough to hug the drawn piece's shape, coarse enough that the mask
    // scan stays trivially cheap.
    private const int MaskCellSize = 4;
    // Texels dimmer than this count as transparent (anti-aliased edges).
    internal const float AlphaThreshold = 0.1f;

    // Process-wide cache: one AlphaMask per debris texture, built once on
    // first use and shared by every piece using that texture.
    private static readonly Dictionary<string, AlphaMask> MaskCache = new();

    public bool Swept { get; private set; }
    public DebrisWeight Weight { get; private set; }

    /// <summary>
    /// World-space radius of this piece's bounding circle — half its widest
    /// scaled extent. A cheap upper bound used for early rejection in
    /// <see cref="Covers"/>; the real footprint is the alpha mask.
    /// </summary>
    public float ExtentRadius
    {
        get
        {
            Vector2 size = _sprite.Texture.GetSize() * _sprite.Scale.X;
            return Mathf.Max(size.X, size.Y) * 0.5f;
        }
    }

    /// <summary>Sprite scale factor, exposed for the autoplay ground-truth check.</summary>
    public float SpriteScale => _sprite.Scale.X;

    /// <summary>The piece's texture, exposed for the autoplay ground-truth check.</summary>
    public Texture2D Texture => _sprite.Texture;

    /// <summary>True while the piece rides the end-of-round wind gyre.</summary>
    public bool IsRidingWind => _windActive;

    /// <summary>True while the piece is still falling into place at round start.</summary>
    public bool IsSettling => _settling;

    /// <summary>
    /// The floor spot a settling piece is falling toward. Sweeping or gusting
    /// the piece mid-air intercepts it before it lands, which leaves this
    /// ground clean — so the storm pool records this spot, never the
    /// transient mid-air position.
    /// </summary>
    public Vector2 SettleTarget => _settleTarget;

    /// <summary>True while the piece shivers from an ambient rustle.</summary>
    public bool IsRustling => _rustling;

    /// <summary>
    /// Picks the piece up into the end-of-round wind: it orbits clockwise
    /// around <paramref name="center"/> (its current spot becomes its lane
    /// radius), tumbles gently, and never fades — the litter keeps circling
    /// while the win card is up.
    /// </summary>
    public void StartEndRoundWind(Vector2 center, RandomNumberGenerator rng, float speedScale = 1f)
    {
        if (Swept || _windActive)
            return;
        if (_settling)
        {
            // A storm drop still tumbling when the round is won: land it
            // instantly on its destined spot so the gyre lane radius comes
            // from the floor, not from a mid-air position (and no frozen
            // partial fade-in alpha rides the wind).
            _settling = false;
            Position = _settleTarget;
            RotationDegrees = _settleTargetRot;
            Modulate = Colors.White;
        }
        _windActive = true;
        _windCenter = center;
        Vector2 offset = Position - center;
        _windRadius = Mathf.Max(offset.Length(), 40f);
        _windPhase = offset.Angle();
        _windAge = 0f;
        _windEase = 0f;
        // Per-piece speed jitter shears the gyre; the self-spin echoes the
        // orbit so the piece reads as tumbling along its lane. speedScale
        // dials the whole motion down (the menu gyre idles far slower
        // than the end-of-round one).
        _windAngularSpeed = WindSpeedBase * rng.RandfRange(1f - WindSpeedJitter, 1f + WindSpeedJitter) * speedScale;
        _windSpin = (rng.RandfRange(-1f, 1f) * WindSpinBase + _windAngularSpeed * 0.6f) * speedScale;
        _windBreathFreq = WindBreathFreq * rng.RandfRange(0.7f, 1.3f);
        _windBreathOffset = rng.RandfRange(0f, Mathf.Tau);
        _windBobAmp = WindBobAmp * rng.RandfRange(0.5f, 1.5f);
        _windBobFreq = WindBobFreq * rng.RandfRange(0.7f, 1.3f);
        _windBobOffset = rng.RandfRange(0f, Mathf.Tau);
    }

    /// <summary>Keeps the gyre centered when the viewport resizes mid-wind.</summary>
    public void SetWindCenter(Vector2 center) => _windCenter = center;

    /// <summary>Rescales the fall path when the viewport resizes mid-settle.</summary>
    public void ScaleSettle(Vector2 ratio)
    {
        if (!_settling)
            return;
        _settleFrom *= ratio;
        _settleTarget *= ratio;
    }

    /// <summary>
    /// Round-start entrance: lifts the piece above its already-assigned spot
    /// and drops it in with a tumble and a soft landing. <paramref name="delay"/>
    /// staggers pieces along the spawn order so the litter falls in like a
    /// curtain instead of all at once.
    /// </summary>
    public void SettleIn(RandomNumberGenerator rng, float delay)
    {
        _settling = true;
        _settleAge = -delay;
        _settleSeconds = SettleSeconds * rng.RandfRange(0.85f, 1.15f);
        _settleTarget = Position;
        _settleTargetRot = RotationDegrees;
        _settleFrom = Position + Vector2.Up * (SettleHeightMin + rng.RandfRange(0f, SettleHeightExtra));
        // Raw-degree interpolation (not LerpAngle) so the ±turns offset
        // unwinds as real full spins during the fall.
        _settleFromRot = _settleTargetRot + rng.RandfRange(-1f, 1f) * SettleSpinTurns * 360f;
        Position = _settleFrom;
        RotationDegrees = _settleFromRot;
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>
    /// A stray draft brushes past: the piece shivers in place for a
    /// fraction of a second. Purely cosmetic — the wobble lives on the
    /// child sprite, so the node transform behind the gameplay math
    /// (coverage, sweeping, the wind gyre) never moves. Ignored while the
    /// piece is mid-fling, falling into place or riding the gyre.
    /// <paramref name="delay"/> holds the shiver back (spiral gusts
    /// stagger pieces by their clockwise distance from the wave's start).
    /// </summary>
    public void Rustle(Vector2 dir, float falloff, RandomNumberGenerator rng, float delay = 0f)
    {
        if (Swept || _settling || _windActive)
            return;
        _rustling = true;
        _rustleAge = -delay;
        _rustleSeconds = RustleSeconds * rng.RandfRange(0.8f, 1.2f);
        _rustleDir = dir.Normalized();
        _rustleAmp = RustleAmp[(int)Weight] * falloff * rng.RandfRange(0.7f, 1.3f);
        _rustleWobbleRate = RustleWobbleRate
            * rng.RandfRange(1f - RustleWobbleJitter, 1f + RustleWobbleJitter);
        _rustleTurn = rng.RandfRange(-RustleTurn, RustleTurn);
    }

    private void UpdateRustle(float dt)
    {
        _rustleAge += dt;
        float t = Mathf.Clamp(_rustleAge / _rustleSeconds, 0f, 1f);
        // Quick flick, dying tail: the draft kicks the piece and it
        // eases back exactly onto its spot.
        float envelope = Mathf.Sin(Mathf.Pi * Mathf.Min(t * 2.5f, 1f)) * (1f - t);
        float wobble = Mathf.Sin(_rustleAge * _rustleWobbleRate);
        _sprite.Position = _rustleDir * _rustleAmp * wobble * envelope;
        _sprite.RotationDegrees = _rustleTurn * wobble * envelope;
        if (t >= 1f)
        {
            _rustling = false;
            _sprite.Position = Vector2.Zero;
            _sprite.RotationDegrees = 0f;
        }
    }

    private void UpdateEndRoundWind(float dt)
    {
        _windAge += dt;
        _windEase = Mathf.Min(_windEase + dt / WindEaseSeconds, 1f);
        float ease = Mathf.SmoothStep(0f, 1f, _windEase);

        // Clockwise on screen: y points down, so an increasing angle turns
        // the piece clockwise around the center.
        _windPhase += _windAngularSpeed * dt * ease;
        // The lane breathes so the ring keeps loosening and tightening.
        float breath = 1f + Mathf.Sin(_windAge * Mathf.Tau * _windBreathFreq + _windBreathOffset)
            * WindBreathAmp;
        // The whole gyre bobs a little, like the draft itself rises and sinks.
        Vector2 center = _windCenter + Vector2.Down
            * (Mathf.Sin(_windAge * Mathf.Tau * _windBobFreq + _windBobOffset) * _windBobAmp * ease);
        Position = center + Vector2.Right.Rotated(_windPhase) * _windRadius * breath;
        Rotation += _windSpin * dt * ease;
    }

    private void UpdateSettle(float dt)
    {
        _settleAge += dt;
        float t = Mathf.Clamp(_settleAge / _settleSeconds, 0f, 1f);
        // Quart-out: a fast entry that eases into a soft landing.
        float eased = 1f - Mathf.Pow(1f - t, 4f);
        Position = _settleFrom.Lerp(_settleTarget, eased);
        RotationDegrees = Mathf.Lerp(_settleFromRot, _settleTargetRot, eased);
        // Fade in over the first third of the fall so pieces don't pop.
        Modulate = new Color(1f, 1f, 1f, Mathf.Min(t * 3f, 1f));
        if (t >= 1f)
        {
            _settling = false;
            Modulate = Colors.White;
            Position = _settleTarget;
            RotationDegrees = _settleTargetRot;
        }
    }

    /// <summary>
    /// True when opaque pixels of this piece fall within <paramref name="radius"/>
    /// world units of <paramref name="worldPoint"/> — the pixel-accurate
    /// overlap test behind the covered-bug/coin rule, so debris floating in
    /// the texture's transparent margins no longer hides things.
    /// </summary>
    public bool Covers(Vector2 worldPoint, float radius)
    {
        // Early circular rejection: a piece whose bounding circle can't
        // reach the test circle never overlaps, whatever its shape.
        if (Position.DistanceTo(worldPoint) > ExtentRadius + radius)
            return false;

        AlphaMask mask = GetAlphaMask(_sprite);
        if (mask == null)
        {
            // Mask unavailable (unreadable texture): fall back to a
            // conservative circle test inside the bounding extent.
            return Position.DistanceTo(worldPoint) <= ExtentRadius * 0.7f + radius;
        }

        // Map the world point into unscaled texture-pixel space: ToLocal
        // undoes this node's position and rotation, dividing by the sprite
        // scale undoes the draw size, and shifting by half the texture size
        // converts from the centered sprite origin to texture pixels.
        Vector2 texPoint = ToLocal(worldPoint) / _sprite.Scale.X
            + new Vector2(mask.Width, mask.Height) * 0.5f;

        // Scan only the mask cells whose rectangle could touch the test
        // circle (radius converted to texture pixels by the sprite scale).
        float texRadius = radius / _sprite.Scale.X;
        int minX = Mathf.Max(0, Mathf.FloorToInt((texPoint.X - texRadius) / MaskCellSize));
        int maxX = Mathf.Min(mask.Cols - 1, Mathf.FloorToInt((texPoint.X + texRadius) / MaskCellSize));
        int minY = Mathf.Max(0, Mathf.FloorToInt((texPoint.Y - texRadius) / MaskCellSize));
        int maxY = Mathf.Min(mask.Rows - 1, Mathf.FloorToInt((texPoint.Y + texRadius) / MaskCellSize));

        float rSq = texRadius * texRadius;
        for (int cy = minY; cy <= maxY; cy++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                if (mask.Cells[cy * mask.Cols + cx] == 0)
                    continue;
                // Distance from the test point to the nearest point of the
                // opaque cell's rectangle; at most half a cell off the true
                // texel distance (≤ ~2.8px at 4px cells).
                float nx = Mathf.Clamp(texPoint.X, cx * MaskCellSize, (cx + 1) * MaskCellSize);
                float ny = Mathf.Clamp(texPoint.Y, cy * MaskCellSize, (cy + 1) * MaskCellSize);
                float dx = texPoint.X - nx;
                float dy = texPoint.Y - ny;
                if (dx * dx + dy * dy <= rSq)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Builds (or fetches from the process-wide cache) the alpha coverage
    /// mask of the sprite's texture: one byte per 4px cell, 1 when any texel
    /// in the cell is opaque enough to hide things underneath.
    /// </summary>
    private static AlphaMask GetAlphaMask(Sprite2D sprite)
    {
        string key = sprite.Texture.ResourcePath;
        if (MaskCache.TryGetValue(key, out AlphaMask cached))
            return cached;

        AlphaMask mask = null;
        // GetImage returns a fresh native Image per call; dispose it once
        // the mask is built so the wrapper can't outlive the run and read
        // as a leak at engine exit.
        using Image img = sprite.Texture.GetImage();
        if (img != null && (!img.IsCompressed() || img.Decompress() == Error.Ok))
        {
            int width = img.GetWidth();
            int height = img.GetHeight();
            int cols = Mathf.CeilToInt(width / (float)MaskCellSize);
            int rows = Mathf.CeilToInt(height / (float)MaskCellSize);
            var cells = new byte[cols * rows];
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    int x0 = cx * MaskCellSize;
                    int y0 = cy * MaskCellSize;
                    int x1 = Mathf.Min(x0 + MaskCellSize, width);
                    int y1 = Mathf.Min(y0 + MaskCellSize, height);
                    for (int y = y0; y < y1; y++)
                    {
                        bool opaque = false;
                        for (int x = x0; x < x1; x++)
                        {
                            if (img.GetPixel(x, y).A > AlphaThreshold)
                            {
                                opaque = true;
                                break;
                            }
                        }
                        if (opaque)
                        {
                            cells[cy * cols + cx] = 1;
                            break;
                        }
                    }
                }
            }
            mask = new AlphaMask(cells, cols, rows, width, height);
        }
        // Cache nulls too, so an unreadable texture doesn't rescan every frame.
        MaskCache[key] = mask;
        return mask;
    }

    /// <summary>Cached alpha coverage grid for one debris texture.</summary>
    private sealed class AlphaMask
    {
        public readonly byte[] Cells;
        public readonly int Cols;
        public readonly int Rows;
        public readonly int Width;
        public readonly int Height;

        public AlphaMask(byte[] cells, int cols, int rows, int width, int height)
        {
            Cells = cells;
            Cols = cols;
            Rows = rows;
            Width = width;
            Height = height;
        }
    }

    public void Setup(string texturePath, Vector2 pos, float rotDeg,
        float scale, DebrisWeight weight, RandomNumberGenerator rng)
    {
        Position = pos;
        RotationDegrees = rotDeg;
        Weight = weight;

        _sprite = new Sprite2D { Texture = GD.Load<Texture2D>(texturePath) };
        _sprite.Scale = new Vector2(scale, scale);
        // Shared celebration shader (Main-owned): gold_mix 0 is an exact
        // passthrough, so pieces only tint when a prismatic find is
        // celebrating — and every piece flips at once via the one material.
        if (CelebrationMaterial != null)
            _sprite.Material = CelebrationMaterial;
        AddChild(_sprite);

        // Per-instance variety so identical textures don't look stamped.
        _sprite.SelfModulate = new Color(1, 1, 1, 1).Lerp(
            new Color(0.92f, 0.92f, 0.88f, 1), rng.Randf());
    }

    /// <summary>
    /// Main's shared prismatic-celebration ShaderMaterial, handed to every
    /// piece before Setup so the whole litter can be tinted gold/white
    /// with a single uniform tween.
    /// </summary>
    public ShaderMaterial? CelebrationMaterial { get; set; }

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

        // A fling overrides any ambient rustle: snap the sprite back onto
        // its spot so the slide owns the whole piece.
        _rustling = false;
        _sprite.Position = Vector2.Zero;
        _sprite.RotationDegrees = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        // Wind and settle modes own the piece completely — a swept fling
        // cancels them implicitly by falling through to the slide below.
        if (_windActive && !Swept)
        {
            UpdateEndRoundWind(dt);
            return;
        }
        if (_settling && !Swept)
        {
            UpdateSettle(dt);
            return;
        }
        if (_rustling && !Swept)
        {
            UpdateRustle(dt);
            return;
        }
        if (!Swept)
            return;

        Position += _velocity * dt;
        Rotation += _angularVel * dt;

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
