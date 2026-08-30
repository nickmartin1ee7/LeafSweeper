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
            "It saw your sweep coming in slow motion. Impressive.",
            "The fastest critter on the floor, caught by kindness.",
        },
        ["aphid"] = new[]
        {
            "A gentle giant of the tiny world — for an aphid.",
            "It promised to keep its sap-sipping to the wild leaves.",
            "Small enough to sit on a freckle, found all the same.",
            "The whole herd will hear how you spotted this one.",
        },
        ["barklice"] = new[]
        {
            "A librarian of the bark, catalogued at last.",
            "It makes its own silky web-house. Very tidy of it.",
            "You found it living its best quiet life under the bark.",
            "This little bookworm — barkworm? — approves of your collection.",
        },
        ["cicada"] = new[]
        {
            "It hummed a whole summer's worth of thanks.",
            "Seventeen years underground for this standing ovation.",
            "The loudest thank-you in the forest, at full volume.",
            "Its song is your name now — you'll hear it all August.",
        },
        ["click_beetle"] = new[]
        {
            "It clicked its heels — both of them, mid-air.",
            "Flipped on its back, and aced the recovery.",
            "It performed the world's smallest high jump for you.",
            "Click, flip, pop: that's how a beetle says wow.",
        },
        ["damselfly"] = new[]
        {
            "A fairy of the pond, delicate as dew.",
            "It folded its glassy wings in a graceful bow.",
            "Dragonflies zoom — damselflies waltz. You got the dancer.",
            "It perched on your fingertip like it had always planned to.",
        },
        ["earwig"] = new[]
        {
            "Those pincers are for showing off. Mostly.",
            "It's a wonderful mother, and today a proud find.",
            "Fearsome-looking, secretly shy, absolutely delighted.",
            "It pinched the air in your honor. Gently, of course.",
        },
        ["earthworm"] = new[]
        {
            "The garden's chief engineer, on an inspection tour.",
            "It wiggled its appreciation from head to tail.",
            "Every tidy flower bed owes a debt to this one.",
            "It wrote a thank-you in the soil. It's tiny cursive.",
        },
        ["froghopper"] = new[]
        {
            "Champion jumper of the tiny leagues.",
            "It blows bubble nests — a tidy bug for a tidy player.",
            "It once leapt clear over a puddle. Twice.",
            "Spring-loaded, pocket-sized, thoroughly found.",
        },
        ["glowworm"] = new[]
        {
            "A nightlight with a very warm heart.",
            "It lit up the moment you arrived — no bulb needed.",
            "It glowed a little brighter for the occasion.",
            "Your sweeping guide, arriving after dark.",
        },
        ["jewel_beetle"] = new[]
        {
            "Cut from the forest's finest jewelry box.",
            "That shine needs no polish. Ever.",
            "It caught the sun and threw a tiny rainbow back.",
            "Worth more found than any crown jewel.",
        },
        ["lacewing"] = new[]
        {
            "Wings woven finer than any lace curtain.",
            "It guards the garden from aphids. A tiny hero.",
            "Its eggs wave hello on silken stalks.",
            "Delicate, diligent, and delightful to find.",
        },
        ["lanternfly"] = new[]
        {
            "It carries a lantern built right into its head.",
            "A little lantern flight through the leaf litter.",
            "It nodded, and its snout glowed twice.",
            "The prettiest planthopper the pile has ever hidden.",
        },
        ["leafhopper"] = new[]
        {
            "It hopped from leaf to leaf, then to your collection.",
            "Painted in colors no leaf could match.",
            "Its zigzag hops drew a happy little star.",
            "The leaf pile's liveliest Acrobat, caught mid-leap.",
        },
        ["mayfly"] = new[]
        {
            "One perfect day, spent celebrating with you.",
            "It danced the mayfly dance — a private show.",
            "Brief but brilliant, like a firework with wings.",
            "It made its one day count. You were the reason.",
        },
        ["rhinoceros_beetle"] = new[]
        {
            "The strongest heavyweight in the whole forest.",
            "It could lift 850 more of itself. It lifted you instead.",
            "That horn is for lifting logs — and spirits.",
            "A gentle giant wearing a knight's helmet.",
        },
        ["shield_bug"] = new[]
        {
            "Armored in bright colors, gentle underneath.",
            "Its shield says \"found\" now. Fitting.",
            "It guarded the leaf pile. You breached it kindly.",
            "A tiny knight who salutes your sweeping.",
        },
        ["silverfish"] = new[]
        {
            "It shimmered like moonlight on water.",
            "Three whiskery tails, endless wiggly gratitude.",
            "Older than the dinosaurs — and shy about it.",
            "It glinted once for the camera. Metaphorically.",
        },
        ["slug"] = new[]
        {
            "It brought its own sparkle to the party.",
            "No shell, no problem — it travels light.",
            "It left a silver ribbon in thanks.",
            "Slow, gleaming, and absolutely thrilled.",
        },
        ["stag_beetle"] = new[]
        {
            "Grand antlers for the grandest find.",
            "It bowed, antlers first, very formal.",
            "Those jaws could wrestle — but today, only hugs.",
            "The forest's own stag, caught in your clearing.",
        },
        ["tiger_beetle"] = new[]
        {
            "The sprinter of the sand, caught at last.",
            "It ran in circles when it heard the news.",
            "Striped, speedy, and stopped in its tracks by you.",
            "It stalks tiny prey — you stalked it better.",
        },
        ["tortoise_beetle"] = new[]
        {
            "A turtle shell with beetle perks.",
            "It changes color like a mood ring. Right now: joy.",
            "Home is where the dome is.",
            "Slow to start, dazzling to find.",
        },
        ["water_strider"] = new[]
        {
            "It skated across the puddle in celebration.",
            "Walking on water? It prefers \"puddle skating\".",
            "Four little dimples followed it everywhere.",
            "The pond's finest skater, doing laps in your honor.",
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
