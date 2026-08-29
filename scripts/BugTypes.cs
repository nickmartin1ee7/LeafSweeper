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

    public BugType(string id, string displayName, string texturePath,
        float relativeSize, float tapRadius)
    {
        Id = id;
        DisplayName = displayName;
        TexturePath = texturePath;
        RelativeSize = relativeSize;
        TapRadius = tapRadius;
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

    public static readonly BugType[] All =
    {
        Ladybug, Butterfly, Centipede, Moth, Grasshopper, Dragonfly, Beetle, Snail,
        Firefly, Bumblebee, Caterpillar,
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
