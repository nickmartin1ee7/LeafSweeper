using System;
using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Per-level play tracking (elapsed time, swipe gestures, gust powers used)
/// plus the friendly between-round comments shown on the win overlay.
/// </summary>
public sealed class LevelStats
{
    private static readonly string[] CozyRemarks =
    {
        "Cozy is cozy.",
        "No rush — the bug waited happily.",
        "A lovely tidy-up.",
        "The forest floor thanks you.",
        "Another critter found!",
    };

    public int Level { get; private set; }
    public int Swipes { get; private set; }
    public int Gusts { get; private set; }
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }

    public void Start(int level)
    {
        Level = level;
        Swipes = 0;
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

    public void CountSwipe() => Swipes++;

    /// <summary>Counts one gust power use for the current round.</summary>
    public void CountGust() => Gusts++;

    public static string FormatTime(float seconds)
    {
        int s = Math.Max(0, (int)seconds);
        return $"{s / 60}:{s % 60:D2}";
    }

    /// <summary>
    /// Builds the win-overlay comment: praise for efficient finds, cozy
    /// reassurance for slow ones, and a lifetime-best nod when relevant.
    /// </summary>
    public string Comment(SaveData save, BugType bug)
    {
        var comments = new List<string>();

        int best = save.BestSwipes();
        if (Swipes <= 5)
            comments.Add($"Just {Swipes} swipes to find the {bug.DisplayName.ToLower()}!");
        else if (Swipes <= 12)
            comments.Add($"Only {Swipes} swipes — sharp eyes!");
        else if (Swipes <= 25)
            comments.Add($"{Swipes} swipes to find the {bug.DisplayName.ToLower()}.");

        comments.Add($"Found in {FormatTime(Elapsed)}");

        if (best > 0 && Swipes <= best)
            comments.Add("That's your tidiest find yet!");
        else if (save.LevelsCleared > 3 && Swipes > best * 2)
            comments.Add($"Your best is {best} swipes — today was a leisurely stroll.");

        comments.Add(CozyRemarks[(Level + Swipes) % CozyRemarks.Length]);

        return string.Join("\n", comments);
    }
}
