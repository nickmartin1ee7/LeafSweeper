namespace LeafSweeper;

/// <summary>
/// Very gentle, casual-tuned difficulty curve. Saturates by level 200 so
/// late levels are only moderately harder than level 1. All values are
/// deterministic functions of the level number so any level can be rebuilt.
/// </summary>
public static class RoundConfig
{
    public const int SaturateLevel = 200;

    // Fraction of the floor's area that should be occupied by debris sprites.
    // ~0.00054 ≈ 6× pixel coverage (floor completely hidden) at 1080×2340;
    // saturates around 9× for a thick late-game litter.
    public const float CoverageStart = 0.00054f;
    public const float CoverageEnd = 0.00084f;

    public const float StartBugScale = 1.0f;
    public const float MinBugScale = 0.75f;

    // Camouflage stays at zero until CamoStartLevel, then eases to MaxCamo.
    public const int CamoStartLevel = 60;
    public const float MaxCamo = 0.25f;

    // Gust coins hidden below the debris each round. Normal rounds keep
    // the coin scarce — one gust, so spending it is a real decision. Storm
    // rounds re-bury swept ground constantly, so the player gets a richer
    // supply (StormGustCoins) to fight the flood.
    public const int NormalGustCoins = 1;
    public const int StormGustCoins = 3;

    // ---- Seasons: the difficulty progression ----
    // The year is the new difficulty ladder: Spring is today's game, each
    // following season layers a new weather mechanic on top, and a
    // completed year loops back to Spring with permanent, stacking
    // bonuses. Pure functions of the level like every curve.

    /// <summary>The four seasons a level's weather and palette come from.</summary>
    public enum Season { Spring, Summer, Fall, Winter }

    // Levels per season. The very first Spring is levels 1-99 (the game
    // starts at level 1); every later season is exactly SeasonLength long.
    public const int SeasonLength = 100;

    // One full year — level 400 is the first Spring of Year 2.
    public const int LoopLength = SeasonLength * 4;

    // Permanent bonuses per completed year, stacking every loop: one sweep
    // clears more debris, and every round buries another gust coin.
    public const int LoopSweepPowerBonus = 2;
    public const int LoopGustCoinBonus = 1;

    // Sweep power base matches Sweeper.MaxDebrisPerSweep, which levels
    // apply at round start via Sweeper.SetSweepPower.
    public const int BaseSweepPower = 12;

    /// <summary>The season this level plays in: 1-99 Spring, 100-199 Summer,
    /// 200-299 Fall, 300-399 Winter, 400+ the year loops back to Spring.</summary>
    public static Season SeasonForLevel(int level) =>
        (Season)(level / SeasonLength % 4);

    /// <summary>0-based year number: level 400 starts year 1, so the loop
    /// bonuses are active from level 400 on.</summary>
    public static int LoopIndex(int level) => level / LoopLength;

    /// <summary>Debris pieces one sweep may clear on this level: 12 base,
    /// +2 per completed year.</summary>
    public static int SweepPowerForLevel(int level) =>
        BaseSweepPower + LoopSweepPowerBonus * LoopIndex(level);

    /// <summary>Display name for a season — stable against enum reordering.</summary>
    public static string SeasonName(Season season) => season switch
    {
        Season.Spring => "Spring",
        Season.Summer => "Summer",
        Season.Fall => "Fall",
        _ => "Winter",
    };

    /// <summary>Gust coins hidden below the debris on this level's round,
    /// richer on storm rounds and +1 per completed year.</summary>
    public static int GustCoinsForLevel(int level) =>
        (IsStormLevel(level) ? StormGustCoins : NormalGustCoins)
        + LoopGustCoinBonus * LoopIndex(level);

    /// <summary>0..1 ramp that saturates at <see cref="SaturateLevel"/>.</summary>
    public static float Progress(int level)
    {
        if (level >= SaturateLevel)
            return 1f;
        // Square-root-ish early growth that keeps levels 1-20 nearly identical.
        float t = (level - 1f) / (SaturateLevel - 1f);
        return t * t * (3f - 2f * t); // smoothstep: gentle start and end
    }

    public static float Coverage(int level)
    {
        float coverage = CoverageStart + (CoverageEnd - CoverageStart) * Progress(level);
        // After-storm relief: the level right after a storm sweeps a
        // lighter floor — a breather in the difficulty rhythm (pure
        // function of the level; level 1's tiny extra relief is harmless).
        if (level % StormEveryLevels == 1)
            coverage *= AfterStormReliefFactor;
        return coverage;
    }

    public static float BugScale(int level) =>
        StartBugScale - (StartBugScale - MinBugScale) * Progress(level);

    /// <summary>0..1 how strongly the bug blends toward leaf colors.</summary>
    public static float Camouflage(int level)
    {
        if (level < CamoStartLevel)
            return 0f;
        float t = (level - (float)CamoStartLevel) / (SaturateLevel - CamoStartLevel);
        return MaxCamo * t;
    }

    // Storm rounds: from StormFirstLevel on, every StormEveryLevels-th level
    // turns the weather and swept ground forgets itself — every cleared
    // patch is re-littered a few seconds after it was cleared. Pure
    // function of the level like every curve.
    public const int StormEveryLevels = 10;
    public const int StormFirstLevel = 10;

    /// <summary>True on storm levels: every 10th level from level 10 on.</summary>
    public static bool IsStormLevel(int level) =>
        level >= StormFirstLevel && level % StormEveryLevels == 0;

    // Season pacing: the season's debut level (100/200/300) is a
    // celebration round — new vibe, banner, no new mechanic. "One new
    // mechanic per cluster": the season's event (tornado / water streams /
    // blizzard extras) first fires on the second storm of the season
    // (110/210/310), and every level right after a storm gets a small
    // coverage relief so spikes alternate with breathers.
    public const int SeasonDebutGraceStorms = 1;
    public const float AfterStormReliefFactor = 0.9f;

    // Summer/fall season events re-churn a settled storm round on this
    // fixed cadence.
    public const float TornadoInterval = 20f;
    public const float StreamInterval = 20f;

    /// <summary>
    /// True when the season event may fire on this level: storm levels past
    /// the season's debut grace (the storm ordinal within the season must
    /// reach <see cref="SeasonDebutGraceStorms"/>). Debut storms stay
    /// celebration rounds.
    /// </summary>
    public static bool SeasonEventAllowed(int level) =>
        IsStormLevel(level) && level % SeasonLength / StormEveryLevels >= SeasonDebutGraceStorms;
}
