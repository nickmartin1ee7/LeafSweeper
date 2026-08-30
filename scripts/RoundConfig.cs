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

    /// <summary>Gust coins hidden below the debris on this level's round.</summary>
    public static int GustCoinsForLevel(int level) =>
    	IsStormLevel(level) ? StormGustCoins : NormalGustCoins;

    /// <summary>0..1 ramp that saturates at <see cref="SaturateLevel"/>.</summary>
    public static float Progress(int level)
    {
        if (level >= SaturateLevel)
            return 1f;
        // Square-root-ish early growth that keeps levels 1-20 nearly identical.
        float t = (level - 1f) / (SaturateLevel - 1f);
        return t * t * (3f - 2f * t); // smoothstep: gentle start and end
    }

    public static float Coverage(int level) =>
        CoverageStart + (CoverageEnd - CoverageStart) * Progress(level);

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
}
