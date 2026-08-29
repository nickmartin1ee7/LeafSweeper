using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Turns drag input into sweep flings. Debris near the pointer (or near the
/// segment between the last and current pointer position, so fast swipes
/// don't tunnel through) is flung along the pointer's velocity.
/// </summary>
public sealed class Sweeper
{
    private const float SweepRadius = 95f;

    private readonly Func<IEnumerable<Debris>> _debris;
    private readonly RandomNumberGenerator _rng;
    private readonly Action _onSwipeCompleted;

    private bool _dragging;
    private Vector2 _lastPos;
    private ulong _lastTicks;

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

        foreach (var d in _debris())
        {
            if (!GodotObject.IsInstanceValid(d) || d.Swept)
                continue;
            if (SegmentCircleHit(from, worldPos, d.Position, SweepRadius + 30f))
                d.Fling(velocity, _rng);
        }

        _lastPos = worldPos;
        _lastTicks = ticks;
    }

    public void Begin(Vector2 worldPos, ulong ticks)
    {
        _dragging = true;
        _lastPos = worldPos;
        _lastTicks = ticks;
    }

    public void End()
    {
        if (!_dragging)
            return;
        _dragging = false;
        _onSwipeCompleted();
    }

    public void Cancel() => _dragging = false;

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
