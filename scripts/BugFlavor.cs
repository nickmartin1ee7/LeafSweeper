using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Per-bug congratulatory lines for the win card. Every species has its own
/// small pool of unique variants, so repeat finds don't repeat the praise.
/// </summary>
public static class BugFlavor
{
    private static readonly RandomNumberGenerator Rng = new();

    private static readonly Dictionary<string, string[]> Lines = new()
    {
        ["ladybug"] = new[]
        {
            "Seven spots of pure luck, found by you!",
            "This lucky ladybug will tidy the garden's aphids.",
            "A ladybug lands — folklore says wishes come true.",
            "Spotted! The luckiest little beetle around.",
        },
        ["butterfly"] = new[]
        {
            "Wings like stained glass — what a find!",
            "This butterfly began life as a tiny caterpillar.",
            "Flowers will line up for a visit from this one.",
            "A gentle flutter to celebrate your tidy sweep.",
        },
        ["centipede"] = new[]
        {
            "All those legs, and it still needed you to find it.",
            "It lost count of its own legs long ago.",
            "A hundred tiny feet are doing a happy dance.",
            "Quick on its feet — and it has plenty of those.",
        },
        ["moth"] = new[]
        {
            "Drawn to the light of your tidy leaf pile.",
            "Fuzzy wings, soft heart, new favorite hiding spot.",
            "It says thank you in the quietest flutter.",
            "A night moth found by broad daylight — special!",
        },
        ["grasshopper"] = new[]
        {
            "Built to leap, but it stayed for the celebration.",
            "Its chirp today sounds suspiciously like \"well done\".",
            "Those mighty legs could jump the whole debris pile.",
            "You swept the one leaf it hadn't hopped off yet.",
        },
        ["dragonfly"] = new[]
        {
            "Ace pilot of the pond, caught mid-hover.",
            "Its crystal eyes saw you coming — and stayed anyway.",
            "Four wings, zero fluster, one graceful hello.",
            "It can fly backwards, but today it flew to you.",
        },
        ["beetle"] = new[]
        {
            "That shiny shell polishes up the whole forest floor.",
            "Pound for pound, stronger than any elephant.",
            "Armored, cheerful, and absolutely found.",
            "Under that tough shell beats a gentle heart.",
        },
        ["snail"] = new[]
        {
            "It carried its home all the way here. No rush at all.",
            "A spiral shell full of patience, found at last.",
            "It left a tiny glittering trail just for you.",
            "Slow and steady wins the tidiest sweep.",
        },
        ["firefly"] = new[]
        {
            "Its little lantern glows brighter when it's happy.",
            "Summer nights just found their mascot.",
            "A living fairy light, delivered by your sweeping.",
            "It blinks once: \"nice sweeping, friend.\"",
        },
        ["bumblebee"] = new[]
        {
            "A proud pollinator, buzzing its approval.",
            "It buzzed the whole time — that was applause.",
            "This bee has flowers to tell about you now.",
            "Round, fuzzy, and full of honeycomb gratitude.",
        },
        ["caterpillar"] = new[]
        {
            "It munched every leaf but the one you kept tidy.",
            "One day this little nibbler will be a butterfly.",
            "All those legs in a row — a hundred tiny thanks.",
            "It promises to save some leaves for next time.",
        },
        ["mantis"] = new[]
        {
            "It bowed its praying arms in quiet thanks.",
            "The only hunter that says grace before dinner.",
            "It turned its head all the way around — impressed.",
            "A tiny green nod of respect from the grass.",
        },
        ["stick_insect"] = new[]
        {
            "The best hider in the forest, finally found.",
            "It swore it was just a twig. Nice try.",
            "Master of disguise, no match for your sweeping.",
            "You found the one stick that could walk away.",
        },
        ["weevil"] = new[]
        {
            "That splendid snout sniffs out your talent.",
            "Long snout, short legs, enormous delight.",
            "It drew a tiny heart in the dust with its snout.",
            "Certifiably the most charming weevil around.",
        },
        ["pillbug"] = new[]
        {
            "It rolled into a happy little ball of joy.",
            "Roly-poly round and proud of it.",
            "Its armor curls shut — its grin can't.",
            "A tiny armadillo's answer to a tidy floor.",
        },
        ["ant"] = new[]
        {
            "It will tell the whole colony about you tonight.",
            "Strong for its size, tiny in your thanks.",
            "The colony's most dedicated scout, found at last.",
            "It marched off to spread the good news.",
        },
        ["fly"] = new[]
        {
            "Outmaneuvered at last — what swift sweeping!",
            "Those reflexes dodged everyone but you.",
            "It saw your swipe coming in slow motion. Impressive.",
            "The fastest critter on the floor, caught by kindness.",
        },
    };

    private static readonly string[] Fallback =
    {
        "A happy little critter, found at last.",
        "Another cozy corner of the forest, tidied.",
        "It wiggles with gratitude.",
    };

    /// <summary>One random variant of this bug's own celebration line.</summary>
    public static string Pick(BugType bug)
    {
        if (!Lines.TryGetValue(bug.Id, out var pool))
            pool = Fallback;
        return pool[Rng.RandiRange(0, pool.Length - 1)];
    }
}
