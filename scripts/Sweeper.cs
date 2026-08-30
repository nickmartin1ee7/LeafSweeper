using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Turns drag input into sweep flings. Debris near the pointer (or near the
/// segment between the last and current pointer position, so fast swipes
/// don't tunnel through) is flung along the pointer's velocity. A single
/// swipe (touch-down to lift) clears at most <see cref="MaxDebrisPerSwipe"/>
/// pieces of debris; the double-tap <see cref="Burst"/> shares that cap.
/// </summary>
public sealed class Sweeper
{
    private const float SweepRadius = 55f;

    /// <summary>Radius of the double-tap radial burst, in world units.</summary>
    public const float BurstRadius = 130f;

    /// <summary>
    /// Speed the burst flings with, before weight damping — roughly a brisk
    /// finger swipe, so burst debris leaves the area the same way.
    /// </summary>
    private const float BurstFlingSpeed = 1800f;

    /// <summary>Max debris one swipe gesture may clear.</summary>
    public const int MaxDebrisPerSwipe = 12;

    private readonly Func<IEnumerable<Debris>> _debris;
    private readonly RandomNumberGenerator _rng;
    private readonly Action _onSwipeCompleted;

    private bool _dragging;
    private Vector2 _lastPos;
    private ulong _lastTicks;
    private int _clearedThisSwipe;

    public Sweeper(Func<IEnumerable<Debris>> debris,
        RandomNumberGenerator rng, Action onSwipeCompleted)
    {
        _debris = debris;
        _rng = rng;
        _onSwipeCompleted = onSwipeCompleted;
    }

    /// <summary>Handles a screen-drag event in world coordinates.</summary>
    public void Drag(Vector2 worldPos, ulong ticks)
    {
        if (!_dragging)
            return;

        float dt = (ticks - _lastTicks) / 1_000_000_000f;
        Vector2 delta = worldPos - _lastPos;
        if (dt <= 0.001f)
        {
            _lastPos = worldPos;
            _lastTicks = ticks;
            return;
        }

        Vector2 velocity = delta / dt;
        Vector2 from = _lastPos;

        // A swipe is only strong enough to clear MaxDebrisPerSwipe items;
        // once spent, further dragging moves the finger but flings nothing.
        foreach (var d in _debris())
        {
            if (_clearedThisSwipe >= MaxDebrisPerSwipe)
                break;
            if (!GodotObject.IsInstanceValid(d) || d.Swept)
                continue;
            if (SegmentCircleHit(from, worldPos, d.Position, SweepRadius + 30f))
            {
                d.Fling(velocity, _rng);
                _clearedThisSwipe++;
            }
        }

        _lastPos = worldPos;
        _lastTicks = ticks;
    }

    public void Begin(Vector2 worldPos, ulong ticks)
    {
        if (_dragging)
            return; // a second simultaneous touch must not reset the in-flight gesture
        _dragging = true;
        _lastPos = worldPos;
        _lastTicks = ticks;
        _clearedThisSwipe = 0;
    }

    public void End()
    {
        if (!_dragging)
            return;
        _dragging = false;
        // A touch-down to lift only counts as a swipe when it actually
        // swept debris — bare taps (and fruitless drags) stay free.
        if (_clearedThisSwipe > 0)
            _onSwipeCompleted();
    }

    public void Cancel() => _dragging = false;

    /// <summary>Whether the current (or just-finished) gesture swept anything.</summary>
    public bool SweptThisGesture => _clearedThisSwipe > 0;

    /// <summary>
    /// Double-tap burst: a swipe without the drag. Flings the debris
    /// nearest <paramref name="center"/> radially outward, still capped at
    /// <see cref="MaxDebrisPerSwipe"/> pieces, and reports through
    /// <see cref="_onSwipeCompleted"/> so it counts like any other swipe.
    /// Returns how many pieces were flung.
    /// </summary>
    public int Burst(Vector2 center)
    {
        var hits = new List<(Debris Debris, float Dist)>();
        foreach (var d in _debris())
        {
            if (!GodotObject.IsInstanceValid(d) || d.Swept)
                continue;
            float dist = d.Position.DistanceTo(center);
            if (dist <= BurstRadius)
                hits.Add((d, dist));
        }
        if (hits.Count == 0)
            return 0;
        hits.Sort((a, b) => a.Dist.CompareTo(b.Dist));

        int cleared = 0;
        foreach (var (d, dist) in hits)
        {
            if (cleared >= MaxDebrisPerSwipe)
                break;
            Vector2 dir = dist > 1f ? (d.Position - center) / dist : Vector2.Right;
            d.Fling(dir * BurstFlingSpeed, _rng);
            cleared++;
        }
        if (cleared > 0)
            _onSwipeCompleted();
        return cleared;
    }

    private static bool SegmentCircleHit(Vector2 a, Vector2 b, Vector2 center, float radius)
    {
        Vector2 ab = b - a;
        float len2 = ab.LengthSquared();
        float t = len2 < 0.001f
            ? 0f
            : Mathf.Clamp((center - a).Dot(ab) / len2, 0f, 1f);
        return (a + ab * t).DistanceTo(center) <= radius;
    }
}
