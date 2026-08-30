using Godot;

namespace LeafSweeper;

/// <summary>
/// One entry in the bug catalog: texture, display name, relative size and
/// the tap radius used for hit detection (scaled by the bug's world scale).
/// </summary>
public sealed class BugType
{
    public string Id { get; }
    public string DisplayName { get; }
    public string TexturePath { get; }
    public float RelativeSize { get; }
    public float TapRadius { get; }

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

/// <summary>Static catalog of all findable critters.</summary>
public static class BugTypes
{
    public static readonly BugType Ladybug =
        new("ladybug", "Ladybug", "res://assets/textures/bugs/ladybug.svg", 0.85f, 66f);
    public static readonly BugType Butterfly =
        new("butterfly", "Butterfly", "res://assets/textures/bugs/butterfly.svg", 1.15f, 88f);
    public static readonly BugType Centipede =
        new("centipede", "Centipede", "res://assets/textures/bugs/centipede.svg", 1.05f, 82f);
    public static readonly BugType Moth =
        new("moth", "Moth", "res://assets/textures/bugs/moth.svg", 1.05f, 82f);
    public static readonly BugType Grasshopper =
        new("grasshopper", "Grasshopper", "res://assets/textures/bugs/grasshopper.svg", 1.0f, 78f);
    public static readonly BugType Dragonfly =
        new("dragonfly", "Dragonfly", "res://assets/textures/bugs/dragonfly.svg", 1.1f, 80f);
    public static readonly BugType Beetle =
        new("beetle", "Beetle", "res://assets/textures/bugs/beetle.svg", 0.9f, 70f);
    public static readonly BugType Snail =
        new("snail", "Snail", "res://assets/textures/bugs/snail.svg", 0.95f, 74f);
    public static readonly BugType Firefly =
        new("firefly", "Firefly", "res://assets/textures/bugs/firefly.svg", 0.8f, 60f);
    public static readonly BugType Bumblebee =
        new("bumblebee", "Bumblebee", "res://assets/textures/bugs/bumblebee.svg", 0.85f, 64f);
    public static readonly BugType Caterpillar =
        new("caterpillar", "Caterpillar", "res://assets/textures/bugs/caterpillar.svg", 1.0f, 76f);
    public static readonly BugType Mantis =
        new("mantis", "Mantis", "res://assets/textures/bugs/mantis.svg", 1.1f, 80f);
    public static readonly BugType StickInsect =
        new("stick_insect", "Stick Insect", "res://assets/textures/bugs/stick_insect.svg", 1.15f, 78f);
    public static readonly BugType Weevil =
        new("weevil", "Weevil", "res://assets/textures/bugs/weevil.svg", 0.8f, 62f);
    public static readonly BugType Pillbug =
        new("pillbug", "Pill Bug", "res://assets/textures/bugs/pillbug.svg", 0.7f, 56f);
    public static readonly BugType Ant =
        new("ant", "Ant", "res://assets/textures/bugs/ant.svg", 0.6f, 48f);
    public static readonly BugType Fly =
        new("fly", "Fly", "res://assets/textures/bugs/fly.svg", 0.65f, 52f);

    public static readonly BugType[] All =
    {
        Ladybug, Butterfly, Centipede, Moth, Grasshopper, Dragonfly, Beetle, Snail,
        Firefly, Bumblebee, Caterpillar, Mantis, StickInsect, Weevil, Pillbug, Ant,
        Fly,
    };

    private static readonly RandomNumberGenerator Rng = new();

    public static BugType Random() => All[Rng.RandiRange(0, All.Length - 1)];

    public static BugType ById(string id)
    {
        foreach (var t in All)
            if (t.Id == id)
                return t;
        return Ladybug;
    }
}
