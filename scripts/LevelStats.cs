using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Per-level play tracking (elapsed time, sweep gestures, gust powers used)
/// plus the friendly between-round comments shown on the win overlay.
/// </summary>
public sealed class LevelStats
{
    private static readonly RandomNumberGenerator Rng = new();

    // Sweep-efficiency remarks, from quickest to coziest.
    private static readonly string[] SwiftRemarks =
    {
        "Quick as a whisker!",
        "Barely a rustle — lovely!",
        "A lightning tidy-up!",
    };

    private static readonly string[] KeenRemarks =
    {
        "Sharp eyes, gentle sweeping.",
        "Swept like the autumn wind.",
        "Straight to the point!",
    };

    private static readonly string[] SteadyRemarks =
    {
        "A patient, pleasant search.",
        "Every leaf turned with care.",
        "A nice, unhurried stroll.",
    };

    private static readonly string[] CozyRemarks =
    {
        "Cozy is cozy.",
        "No rush — the bug waited happily.",
        "The forest floor thanks you.",
    };

    private static readonly string[] BestRemarks =
    {
        "That's your tidiest find yet!",
        "A brand-new tidiest find!",
        "Your neatest sweep ever!",
    };

    private static readonly string[] StrollRemarks =
    {
        "today was a leisurely stroll.",
        "a lazy wander through the leaves.",
        "the bug enjoyed the extra company.",
    };

    public int Level { get; private set; }
    public int Sweeps { get; private set; }
    public int Gusts { get; private set; }
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }

    public void Start(int level)
    {
        Level = level;
        Sweeps = 0;
        Gusts = 0;
        Elapsed = 0f;
        Running = true;
    }

    public void Stop() => Running = false;

    public void Tick(double delta)
    {
        if (Running)
            Elapsed += (float)delta;
    }

    public void CountSweep() => Sweeps++;

    /// <summary>Counts one gust power use for the current round.</summary>
    public void CountGust() => Gusts++;

    public static string FormatTime(float seconds)
    {
        int s = Math.Max(0, (int)seconds);
        return $"{s / 60}:{s % 60:D2}";
    }

    /// <summary>
    /// Builds the win-overlay comment: a variant of the found bug's own
    /// celebration line, plus a sweep-efficiency remark with a lifetime-best
    /// nod when relevant. Round numbers live in the stats row instead, so
    /// the comment stays pure flavor.
    /// </summary>
    public string Comment(SaveData save, BugVariant bug)
    {
        var lines = new List<string> { BugFlavor.Pick(bug.Species) };

        int best = save.BestSweeps();
        if (best > 0 && Sweeps <= best)
            lines.Add(Pick(BestRemarks));
        else if (save.LevelsCleared > 3 && Sweeps > best * 2)
            lines.Add($"Your best is {best} sweeps — {Pick(StrollRemarks)}");
        else
            lines.Add(Pick(Sweeps <= 5 ? SwiftRemarks
                : Sweeps <= 12 ? KeenRemarks
                : Sweeps <= 25 ? SteadyRemarks
                : CozyRemarks));

        return string.Join("\n", lines);
    }

    private static string Pick(string[] pool) =>
        pool[Rng.RandiRange(0, pool.Length - 1)];
}
