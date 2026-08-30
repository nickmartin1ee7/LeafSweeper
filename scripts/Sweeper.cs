using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Turns drag input into sweep flings. Debris near the pointer (or near the
/// segment between the last and current pointer position, so fast sweeps
/// don't tunnel through) is flung along the pointer's velocity. A single
/// sweep (touch-down to lift) clears at most <see cref="MaxDebrisPerSweep"/>
/// pieces of debris; the double-tap <see cref="Burst"/> shares that cap.
/// </summary>
public sealed class Sweeper
{
	private const float SweepRadius = 55f;

	/// <summary>Radius of the double-tap radial burst, in world units.</summary>
	public const float BurstRadius = 130f;

	/// <summary>
	/// Speed the burst flings with, before weight damping — roughly a brisk
	/// finger sweep, so burst debris leaves the area the same way.
	/// </summary>
	private const float BurstFlingSpeed = 1800f;

	/// <summary>Max debris one sweep gesture may clear.</summary>
	public const int MaxDebrisPerSweep = 12;

	private readonly Func<IEnumerable<Debris>> _debris;
	private readonly RandomNumberGenerator _rng;
	private readonly Action _onSweepCompleted;

	private bool _dragging;
	private Vector2 _lastPos;
	private ulong _lastTicks;
	private int _clearedThisSweep;

	public Sweeper(Func<IEnumerable<Debris>> debris,
		RandomNumberGenerator rng, Action onSweepCompleted)
	{
		_debris = debris;
		_rng = rng;
		_onSweepCompleted = onSweepCompleted;
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

		// A sweep is only strong enough to clear MaxDebrisPerSweep items;
		// once spent, further dragging moves the finger but flings nothing.
		foreach (var d in _debris())
		{
			if (_clearedThisSweep >= MaxDebrisPerSweep)
				break;
			if (!GodotObject.IsInstanceValid(d) || d.Swept)
				continue;
			if (SegmentCircleHit(from, worldPos, d.Position, SweepRadius + 30f))
			{
				d.Fling(velocity, _rng);
				_clearedThisSweep++;
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
		_clearedThisSweep = 0;
	}

	public void End()
	{
		if (!_dragging)
			return;
		_dragging = false;
		// A touch-down to lift only counts as a sweep when it actually
		// swept debris — bare taps (and fruitless drags) stay free.
		if (_clearedThisSweep > 0)
			_onSweepCompleted();
	}

	public void Cancel() => _dragging = false;

	/// <summary>Whether the current (or just-finished) gesture swept anything.</summary>
	public bool SweptThisGesture => _clearedThisSweep > 0;

	/// <summary>
	/// Double-tap burst: a sweep without the drag. Flings the debris
	/// nearest <paramref name="center"/> radially outward, still capped at
	/// <see cref="MaxDebrisPerSweep"/> pieces, and reports through
	/// <see cref="_onSweepCompleted"/> so it counts like any other sweep.
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
			if (cleared >= MaxDebrisPerSweep)
				break;
			Vector2 dir = dist > 1f ? (d.Position - center) / dist : Vector2.Right;
			d.Fling(dir * BurstFlingSpeed, _rng);
			cleared++;
		}
		if (cleared > 0)
			_onSweepCompleted();
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
