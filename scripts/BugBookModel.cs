using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Pure book content: every variant in stable catalog order with its found
/// state, plus the lifetime stats page. Deliberately UI-free — the autoplay
/// probe and the BugBook overlay read the same model, so what the test
/// asserts is exactly what gets drawn.
/// </summary>
public sealed class BugBookModel
{
    public sealed class Entry
    {
        public BugVariant Variant { get; }
        public int Count { get; }
        public bool Found => Count > 0;

        /// <summary>"Yellow Ladybug" once found, "???" before the first find.</summary>
        public string DisplayName => Found ? Variant.DisplayName : "???";

        public string Label => $"{DisplayName} (x{Count})";

        public Entry(BugVariant variant, int count)
        {
            Variant = variant;
            Count = count;
        }
    }

    /// <summary>All 156 entries in stable catalog order.</summary>
    public List<Entry> Entries { get; }

    /// <summary>Variants found at least once.</summary>
    public int FoundVariants { get; }

    /// <summary>Species with at least one found variant.</summary>
    public int FoundSpecies { get; }

    /// <summary>Sum of every find.</summary>
    public int TotalBugs { get; }

    public int PrismaticFinds { get; }

    /// <summary>Best round in sweeps; 0 = no cleared round yet.</summary>
    public int BestRound { get; }

    public int TotalSweeps { get; }
    public int TotalGusts { get; }
    public int TotalSeconds { get; }

    /// <summary>The variant with the most finds; "—" before the first find.</summary>
    public string Favorite { get; }

    public BugBookModel(SaveData save)
    {
        Entries = new List<Entry>();
        var speciesFound = new HashSet<string>();
        Entry? favorite = null;
        int foundVariants = 0, total = 0;

        foreach (var variant in BugTypes.AllVariants)
        {
            save.BugFindCounts.TryGetValue(variant.Id, out int count);
            var entry = new Entry(variant, count);
            Entries.Add(entry);
            if (!entry.Found)
                continue;
            foundVariants++;
            total += count;
            speciesFound.Add(variant.Species.Id);
            if (favorite == null || count > favorite.Count)
                favorite = entry;
        }

        FoundVariants = foundVariants;
        FoundSpecies = speciesFound.Count;
        TotalBugs = total;
        PrismaticFinds = save.PrismaticFinds;
        BestRound = save.BestSweeps();
        TotalSweeps = save.TotalSweeps;
        TotalGusts = save.TotalGusts;
        TotalSeconds = save.TotalSeconds;
        Favorite = favorite?.DisplayName ?? "—";
    }
}
