using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// One bug species: the silhouette and feel (relative size, tap radius,
/// flavor lines keyed by <see cref="Id"/>). Color variants live in
/// <see cref="Variants"/> — those are what actually spawns and what the
/// bug book counts.
/// </summary>
public sealed class BugType
{
    public string Id { get; }
    public string DisplayName { get; }
    public string TexturePath { get; }
    public float RelativeSize { get; }
    public float TapRadius { get; }

    /// <summary>
    /// The species' color variants in stable catalog order. The first entry
    /// is the base look whose id equals the species id, so older saves
    /// (keyed by species id) keep counting against it.
    /// </summary>
    public BugVariant[] Variants { get; internal set; } = [];

    /// <summary>
    /// Radius of the bug's visible body that debris must clear before it
    /// counts as uncovered. Deliberately much tighter than TapRadius, which
    /// exists only to give fingers a forgiving target.
    /// </summary>
    public float OcclusionRadius { get; }

    public BugType(string id, string displayName, string texturePath,
        float relativeSize, float tapRadius)
    {
        Id = id;
        DisplayName = displayName;
        TexturePath = texturePath;
        RelativeSize = relativeSize;
        TapRadius = tapRadius;
        // 45% of the tap radius, clamped to 18–36px, keeps the occlusion
        // area hugging the critter's drawn body across the size range.
        OcclusionRadius = Mathf.Clamp(tapRadius * 0.45f, 18f, 36f);
    }
}

/// <summary>
/// One findable color/pattern variant of a species: a texture, a book-entry
/// display name ("Yellow Ladybug") and the save key players collect under.
/// </summary>
public sealed class BugVariant
{
    public string Id { get; }
    public string DisplayName { get; }
    public string TexturePath { get; }
    public BugType Species { get; }

    public BugVariant(string id, string displayName, string texturePath, BugType species)
    {
        Id = id;
        DisplayName = displayName;
        TexturePath = texturePath;
        Species = species;
    }
}

/// <summary>Static catalog of all findable critters and their variants.</summary>
public static class BugTypes
{
    // One line per species: id, display name, relative size, tap radius,
    // then the color variants as (texture suffix, display color) pairs.
    // The base texture (<id>.svg) is always the first variant and keeps the
    // bare species id as its save key for save-file compatibility.
    private static readonly BugType Ladybug = Sp("ladybug", "Ladybug", 0.85f, 66f,
        ("orange", "Orange"), ("pink", "Pink"), ("yellow", "Yellow"));
    private static readonly BugType Butterfly = Sp("butterfly", "Butterfly", 1.15f, 88f,
        ("blue", "Blue"), ("white", "White"), ("yellow", "Yellow"));
    private static readonly BugType Centipede = Sp("centipede", "Centipede", 1.05f, 82f,
        ("dark", "Dark"), ("orange", "Orange"), ("yellow", "Yellow"));
    private static readonly BugType Moth = Sp("moth", "Moth", 1.05f, 82f,
        ("green", "Green"), ("pink", "Pink"), ("white", "White"));
    private static readonly BugType Grasshopper = Sp("grasshopper", "Grasshopper", 1.0f, 78f,
        ("brown", "Brown"), ("pink", "Pink"), ("yellow", "Yellow"));
    private static readonly BugType Dragonfly = Sp("dragonfly", "Dragonfly", 1.1f, 80f,
        ("blue", "Blue"), ("green", "Green"), ("red", "Red"));
    private static readonly BugType Beetle = Sp("beetle", "Beetle", 0.9f, 70f,
        ("blue", "Blue"), ("copper", "Copper"), ("green", "Green"));
    private static readonly BugType Snail = Sp("snail", "Snail", 0.95f, 74f,
        ("banded", "Banded"), ("golden", "Golden"), ("green", "Green"));
    private static readonly BugType Firefly = Sp("firefly", "Firefly", 0.8f, 60f,
        ("blue", "Blue"), ("green", "Green"), ("orange", "Orange"));
    private static readonly BugType Bumblebee = Sp("bumblebee", "Bumblebee", 0.85f, 64f,
        ("black", "Black"), ("grey", "Grey"), ("orange", "Orange"));
    private static readonly BugType Caterpillar = Sp("caterpillar", "Caterpillar", 1.0f, 76f,
        ("black", "Black"), ("orange", "Orange"), ("yellow", "Yellow"));
    private static readonly BugType Mantis = Sp("mantis", "Mantis", 1.1f, 80f,
        ("brown", "Brown"), ("pink", "Pink"), ("yellow", "Yellow"));
    private static readonly BugType StickInsect = Sp("stick_insect", "Stick Insect", 1.15f, 78f,
        ("darkbrown", "Dark Brown"), ("green", "Green"), ("grey", "Grey"));
    private static readonly BugType Weevil = Sp("weevil", "Weevil", 0.8f, 62f,
        ("green", "Green"), ("grey", "Grey"), ("red", "Red"));
    private static readonly BugType Pillbug = Sp("pillbug", "Pill Bug", 0.7f, 56f,
        ("blue", "Blue"), ("dark", "Dark"), ("orange", "Orange"));
    private static readonly BugType Ant = Sp("ant", "Ant", 0.6f, 48f,
        ("brown", "Brown"), ("gold", "Gold"), ("red", "Red"));
    private static readonly BugType Fly = Sp("fly", "Fly", 0.65f, 52f,
        ("black", "Black"), ("blue", "Blue"), ("orange", "Orange"));
    private static readonly BugType Aphid = Sp("aphid", "Aphid", 0.55f, 44f,
        ("brown", "Brown"), ("pink", "Pink"), ("yellow", "Yellow"));
    private static readonly BugType Barklice = Sp("barklice", "Barklouse", 0.6f, 50f,
        ("dark", "Dark"), ("grey", "Grey"), ("rust", "Rust"));
    private static readonly BugType Cicada = Sp("cicada", "Cicada", 1.0f, 76f,
        ("dark", "Dark"), ("green", "Green"), ("orange", "Orange"));
    private static readonly BugType ClickBeetle = Sp("click_beetle", "Click Beetle", 0.85f, 66f,
        ("dark", "Dark"), ("grey", "Grey"), ("red", "Red"));
    private static readonly BugType Damselfly = Sp("damselfly", "Damselfly", 1.1f, 82f,
        ("green", "Green"), ("purple", "Purple"), ("red", "Red"));
    private static readonly BugType Earwig = Sp("earwig", "Earwig", 0.8f, 64f,
        ("dark", "Dark"), ("pale", "Pale"), ("red", "Red"));
    private static readonly BugType Earthworm = Sp("earthworm", "Earthworm", 1.15f, 86f,
        ("pale", "Pale"), ("red", "Red"), ("green", "Green"));
    private static readonly BugType Froghopper = Sp("froghopper", "Froghopper", 0.65f, 56f,
        ("brown", "Brown"), ("grey", "Grey"), ("yellow", "Yellow"));
    private static readonly BugType Glowworm = Sp("glowworm", "Glowworm", 0.7f, 58f,
        ("orange", "Orange"), ("blue", "Blue"), ("pink", "Pink"));
    private static readonly BugType JewelBeetle = Sp("jewel_beetle", "Jewel Beetle", 0.9f, 70f,
        ("blue", "Blue"), ("copper", "Copper"), ("purple", "Purple"));
    private static readonly BugType Lacewing = Sp("lacewing", "Lacewing", 0.95f, 72f,
        ("brown", "Brown"), ("grey", "Grey"), ("gold", "Gold"));
    private static readonly BugType Lanternfly = Sp("lanternfly", "Lanternfly", 0.85f, 68f,
        ("grey", "Grey"), ("red", "Red"), ("dark", "Dark"));
    private static readonly BugType Leafhopper = Sp("leafhopper", "Leafhopper", 0.7f, 58f,
        ("red", "Red"), ("blue", "Blue"), ("yellow", "Yellow"));
    private static readonly BugType Mayfly = Sp("mayfly", "Mayfly", 0.75f, 60f,
        ("grey", "Grey"), ("olive", "Olive"), ("cream", "Cream"));
    private static readonly BugType RhinocerosBeetle = Sp("rhinoceros_beetle", "Rhinoceros Beetle", 1.0f, 78f,
        ("black", "Black"), ("red", "Red"), ("green", "Green"));
    private static readonly BugType ShieldBug = Sp("shield_bug", "Shield Bug", 0.8f, 66f,
        ("brown", "Brown"), ("red", "Red"), ("blue", "Blue"));
    private static readonly BugType Silverfish = Sp("silverfish", "Silverfish", 0.75f, 60f,
        ("bronze", "Bronze"), ("slate", "Slate"), ("gold", "Gold"));
    private static readonly BugType Slug = Sp("slug", "Slug", 1.1f, 84f,
        ("yellow", "Yellow"), ("grey", "Grey"), ("black", "Black"));
    private static readonly BugType StagBeetle = Sp("stag_beetle", "Stag Beetle", 0.95f, 74f,
        ("black", "Black"), ("red", "Red"), ("mahogany", "Mahogany"));
    private static readonly BugType TigerBeetle = Sp("tiger_beetle", "Tiger Beetle", 0.8f, 66f,
        ("blue", "Blue"), ("bronze", "Bronze"), ("purple", "Purple"));
    private static readonly BugType TortoiseBeetle = Sp("tortoise_beetle", "Tortoise Beetle", 0.65f, 56f,
        ("green", "Green"), ("red", "Red"), ("silver", "Silver"));
    private static readonly BugType WaterStrider = Sp("water_strider", "Water Strider", 1.0f, 76f,
        ("dark", "Dark"), ("grey", "Grey"), ("rust", "Rust"));

    public static readonly BugType[] All =
    {
        Ladybug, Butterfly, Centipede, Moth, Grasshopper, Dragonfly, Beetle, Snail,
        Firefly, Bumblebee, Caterpillar, Mantis, StickInsect, Weevil, Pillbug, Ant,
        Fly, Aphid, Barklice, Cicada, ClickBeetle, Damselfly, Earwig, Earthworm,
        Froghopper, Glowworm, JewelBeetle, Lacewing, Lanternfly, Leafhopper, Mayfly,
        RhinocerosBeetle, ShieldBug, Silverfish, Slug, StagBeetle, TigerBeetle,
        TortoiseBeetle, WaterStrider,
    };

    /// <summary>Every findable variant across all species, in catalog order.</summary>
    public static BugVariant[] AllVariants { get; } = BuildAllVariants();

    private static readonly Dictionary<string, BugVariant> ByIdMap = BuildByIdMap();

    private static readonly RandomNumberGenerator Rng = new();

    public static BugType Random() => All[Rng.RandiRange(0, All.Length - 1)];

    /// <summary>Uniform species roll, then a uniform variant roll inside it.</summary>
    public static BugVariant RandomVariant()
    {
        var species = Random();
        return species.Variants[Rng.RandiRange(0, species.Variants.Length - 1)];
    }

    public static BugType ById(string id)
    {
        foreach (var t in All)
            if (t.Id == id)
                return t;
        return Ladybug;
    }

    /// <summary>
    /// Save key → variant. Unknown ids (old saves, future content changes)
    /// fall back to the ladybug's base variant.
    /// </summary>
    public static BugVariant VariantById(string id) =>
        ByIdMap.GetValueOrDefault(id, AllVariants[0]);

    /// <summary>
    /// Builds a species and its four variants; the base look (no suffix)
    /// shares the species id so pre-variant saves keep counting.
    /// </summary>
    private static BugType Sp(string id, string name, float size, float tap,
        params (string Suffix, string Color)[] variants)
    {
        var species = new BugType(id, name, $"res://assets/textures/bugs/{id}.svg", size, tap);
        var list = new List<BugVariant> { new(id, name, species.TexturePath, species) };
        foreach (var (suffix, color) in variants)
            list.Add(new BugVariant($"{id}_{suffix}", $"{color} {name}",
                $"res://assets/textures/bugs/{id}_{suffix}.svg", species));
        species.Variants = list.ToArray();
        return species;
    }

    private static BugVariant[] BuildAllVariants()
    {
        var list = new List<BugVariant>();
        foreach (var t in All)
            list.AddRange(t.Variants);
        return list.ToArray();
    }

    private static Dictionary<string, BugVariant> BuildByIdMap()
    {
        var map = new Dictionary<string, BugVariant>();
        foreach (var v in AllVariants)
            map[v.Id] = v;
        return map;
    }
}
