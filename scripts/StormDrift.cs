using Godot;
using System.Collections.Generic;

namespace LeafSweeper;

/// <summary>
/// One storm drift gust: a raft of decorative litter that sweeps across the
/// screen from offscreen left to offscreen right. Every piece orbits a
/// personal center that rides the wind, and the lane radius breathes, so
/// the traces read as loose spirals tumbling downwind. Pure storm chaos:
/// the raft never lands, blocks nothing, and frees itself once the whole
/// raft has crossed — the gameplay floor is never touched.
/// </summary>
public partial class StormDrift : Node2D
{
    // Crossing pace: the raft clears the viewport in ~2.4s — not too slow,
    // moderately fast, and screen-relative so it reads the same on phones
    // and desktops.
    private const float CrossSeconds = 2.4f;

    // Spiral feel: each piece loops its personal center about this many
    // times during the crossing, and the lane radius breathes so the loops
    // tighten and loosen along the way — spiral traces, not rigid circles.
    private const float OrbitLapsMin = 1.4f;  // loops per crossing (min)
    private const float OrbitLapsMax = 2.2f;  // loops per crossing (max)
    private const float OrbitRadiusMin = 24f; // px lane radius (min)
    private const float OrbitRadiusMax = 62f; // px lane radius (max)
    private const float BreathAmp = 0.35f;    // lane breathing, fraction of radius
    private const float BreathFreqMin = 0.5f; // breath cycles per second (min)
    private const float BreathFreqMax = 1.0f; // breath cycles per second (max)
    private const float SpinMax = 4f;         // rad/s self-spin while tumbling

    // Storm lean: the raft sags downward as it crosses, matching the
    // downwind lean of the storm rain.
    private const float DownDriftMin = 30f;   // px of total descent (min)
    private const float DownDriftExtra = 60f; // px of extra descent jitter

    // Offscreen margins: pieces start and end fully outside the viewport.
    // Budgeted for the worst piece: along-track stagger (60 at spawn, 40 at
    // exit) + widest lane (OrbitRadiusMax × 1.35 breath ≈ 84) + largest
    // spinning sprite (100px SVG × 1.4 scale → half-diagonal ≈ 99) ≈ 243px,
    // so 250 covers both ends without the odd pop-in/pop-out.
    private const float Margin = 250f;
    private const float StaggerMax = 60f;

    private readonly List<Sprite2D> _sprites = new();
    private readonly List<Vector2> _starts = new();
    private readonly List<float> _phases = new();
    private readonly List<float> _radii = new();
    private readonly List<float> _laps = new();
    private readonly List<float> _breathFreqs = new();
    private readonly List<float> _breathOffsets = new();
    private readonly List<float> _spins = new();
    private readonly List<float> _drops = new();

    private float _age;
    private float _span; // px the raft travels: viewport width + both margins

    /// <summary>Pieces in this raft — exposed for the autoplay self-test.</summary>
    public int PieceCount => _sprites.Count;

    /// <summary>World position of the first piece — exposed for the autoplay self-test.</summary>
    public Vector2 ProbePosition => _sprites.Count > 0 ? _sprites[0].Position : Vector2.Zero;

    public StormDrift(Rect2 area, IReadOnlyList<string> texturePaths,
        RandomNumberGenerator rng, int count)
    {
        _span = area.Size.X + Margin * 2f;
        for (int i = 0; i < count; i++)
        {
            var sprite = new Sprite2D
            {
                Texture = GD.Load<Texture2D>(texturePaths[rng.RandiRange(0, texturePaths.Count - 1)]),
                Position = new Vector2(
                    -Margin + rng.RandfRange(-40f, StaggerMax),
                    rng.RandfRange(80f, area.Size.Y - 60f)),
                RotationDegrees = rng.RandfRange(0f, 360f),
                Scale = Vector2.One * rng.RandfRange(0.9f, 1.4f),
            };
            // Per-instance variety so identical textures don't look stamped.
            sprite.SelfModulate = new Color(1, 1, 1, 1).Lerp(
                new Color(0.92f, 0.92f, 0.88f, 1), rng.Randf());
            AddChild(sprite);
            _sprites.Add(sprite);
            _starts.Add(sprite.Position);
            _phases.Add(rng.RandfRange(0f, Mathf.Tau));
            _radii.Add(rng.RandfRange(OrbitRadiusMin, OrbitRadiusMax));
            _laps.Add(rng.RandfRange(OrbitLapsMin, OrbitLapsMax));
            _breathFreqs.Add(rng.RandfRange(BreathFreqMin, BreathFreqMax));
            _breathOffsets.Add(rng.RandfRange(0f, Mathf.Tau));
            _spins.Add(rng.RandfRange(-SpinMax, SpinMax));
            _drops.Add(DownDriftMin + rng.RandfRange(0f, DownDriftExtra));
        }
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float t = _age / CrossSeconds;
        if (t >= 1f)
        {
            QueueFree(); // the whole raft is offscreen by now
            return;
        }
        // Everything is a function of t: the crossing speed and the loop
        // rate stay exact even if a frame hiccups.
        for (int i = 0; i < _sprites.Count; i++)
        {
            Vector2 center = _starts[i] + new Vector2(_span * t, _drops[i] * t);
            float breath = 1f + Mathf.Sin(_age * Mathf.Tau * _breathFreqs[i]
                + _breathOffsets[i]) * BreathAmp;
            _sprites[i].Position = center
                + Vector2.Right.Rotated(_phases[i] + _laps[i] * Mathf.Tau * t)
                    * _radii[i] * breath;
            _sprites[i].Rotation += _spins[i] * (float)delta;
        }
    }
}
