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
        Image img = sprite.Texture.GetImage();
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
