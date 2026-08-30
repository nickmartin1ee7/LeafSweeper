using System.Collections.Generic;
using Godot;

namespace LeafSweeper;

/// <summary>
/// Per-species fun facts for the win card. Every species has its own pool of
/// 6+ real, verified facts about the actual animal, so repeat finds keep
/// teaching something new instead of repeating praise.
/// </summary>
public static class BugFlavor
{
    private static readonly RandomNumberGenerator Rng = new();

    private static readonly Dictionary<string, string[]> Facts = new()
    {
        ["ladybug"] = new[]
        {
            "More than 6,000 ladybug species live all over the world.",
            "A ladybug's red-and-black pattern warns birds that it tastes bad.",
            "When scared, a ladybug leaks bitter yellow drops from its leg joints.",
            "One ladybug can eat several thousand aphids in its lifetime.",
            "Ladybugs huddle together in huge groups to sleep through the winter.",
            "Some ladybugs can fly up to 120 kilometres (75 miles) in a single trip.",
            "Newly hatched ladybug larvae often begin life by eating the other eggs in their nest.",
        },
        ["butterfly"] = new[]
        {
            "Butterflies taste with their feet — taste sensors grow right on them.",
            "Butterfly wings are covered in tiny scales that dust off like glitter when touched.",
            "Adult butterflies live from one week to nearly a year, depending on the species.",
            "Butterflies glue every egg to a leaf with a special fast-hardening glue.",
            "In the coldest lands, some butterflies need more than a year to finish growing up.",
            "Some monarch butterflies migrate about 3,000 km (1,800 miles) south to winter homes.",
            "The oldest known butterfly fossils are more than 50 million years old.",
        },
        ["centipede"] = new[]
        {
            "No centipede has exactly 100 legs — they have 15 to 191 pairs, always an odd number.",
            "A centipede's \"fangs\" are actually modified legs that inject venom.",
            "The biggest centipedes can grow as long as 30 centimetres (12 inches).",
            "Many centipedes have no eyes at all, and most can only tell light from dark.",
            "Centipedes have one pair of legs on each body segment — millipedes have two.",
            "Centipedes dry out fast, so they hide by day and hunt at night.",
            "A centipede that loses legs can grow them back when it molts.",
        },
        ["moth"] = new[]
        {
            "There are about 160,000 known moth species — 90% of all butterflies and moths.",
            "Grown-up moths never chew clothes — only moth caterpillars eat fabric fibers.",
            "The giant atlas moth can have a wingspan up to 30 centimetres (12 inches) wide.",
            "One silkworm cocoon holds a single silk strand about 915 metres long.",
            "Male moths can smell a female's scent trail from more than a kilometre away.",
            "Giant silk moths like the atlas moth are born with no working mouth — they never eat.",
            "Moths spiral at lamps, probably because they steer by a fixed angle to the moon.",
        },
        ["grasshopper"] = new[]
        {
            "A big grasshopper can leap about 1 metre — 20 times its own body length.",
            "Grasshoppers have five eyes: two big compound ones plus three tiny simple ones.",
            "Grasshoppers hear with two eardrum-like patches on their belly, not their head.",
            "Grasshoppers were hopping around Earth about 250 million years ago.",
            "Male grasshoppers \"sing\" by rubbing pegs on their hind legs against their wings.",
            "Some grasshoppers turn into locusts and swarm in huge, hungry crowds.",
            "A grasshopper's leg works like a spring: it locks, loads, then lets go all at once.",
        },
        ["dragonfly"] = new[]
        {
            "Each adult dragonfly eye is built from nearly 24,000 tiny lenses.",
            "Dragonflies are among the fastest-flying insects on Earth.",
            "Dragonfly nymphs breathe through gills in their rear ends and jet away by squirting water.",
            "A dragonfly nymph can live underwater for up to five years before growing wings.",
            "Dragonfly cousins 325 million years ago had wingspans up to 75 cm (30 inches).",
            "Some dragonflies migrate across oceans on their long journeys.",
            "A dragonfly can hover, dart backwards, and stop dead in mid-air.",
        },
        ["beetle"] = new[]
        {
            "Beetles are Earth's biggest animal group — 400,000 known species.",
            "A beetle's front wings are hardened into armor-like cases called elytra.",
            "A dung beetle can pull 1,141 times its own body weight.",
            "Bombardier beetles fire a popping chemical spray heated to nearly 100°C.",
            "Fireflies are beetles — they make their own glow-light to find each other.",
            "Click beetles snap a springy hinge to leap high into the air with a click.",
            "Beetles range from smaller than a sand grain to longer than a hand.",
        },
        ["snail"] = new[]
        {
            "A snail's ribbon tongue is covered with thousands of microscopic, file-like teeth.",
            "Every garden snail is both male and female — any two snails can be parents.",
            "Snails glide on mucus with sliding suction, so they can climb walls and ceilings.",
            "A snail's eyes sit on the tips of its two longer tentacles.",
            "In dry or cold times, a snail seals its shell opening shut with dried mucus.",
            "Garden snails \"fence\" with tiny chalky darts before they mate.",
            "A snail's spiral shell grows along with it for its whole life.",
        },
        ["firefly"] = new[]
        {
            "Fireflies are beetles, not flies — there are about 2,000 firefly species.",
            "Firefly light is \"cold light\" — almost all its energy comes out as light, not heat.",
            "Every firefly glows as a larva, using its light as a warning to predators.",
            "In some places, thousands of fireflies flash on and off together in perfect unison.",
            "Frogs that gobble lots of fireflies can end up glowing a little themselves.",
            "Firefly larvae hunt snails and slugs on the ground.",
        },
        ["bumblebee"] = new[]
        {
            "Bumblebees shiver to warm their flight muscles before they can take off.",
            "Bumblebees grab flowers and buzz to shake the pollen loose — \"buzz pollination\".",
            "A bumblebee colony usually holds only 50–400 bees, tiny next to a honeybee hive.",
            "Bumblebees have no ears, but they can feel vibrations through their bodies.",
            "Bumblebees can sense the faint electric fields that flowers give off.",
            "Bumblebees can learn new skills, like pulling strings, just by watching other bees.",
            "A bumblebee colony lasts one season; the young queen hibernates underground alone.",
        },
        ["caterpillar"] = new[]
        {
            "A caterpillar is the larva — the baby stage — of a butterfly or moth.",
            "A tobacco hornworm caterpillar can grow 10,000 times heavier in under 20 days.",
            "Most caterpillars have 12 tiny simple eyes, six on each side of the head.",
            "Most caterpillars shed their entire skin four or five times as they grow.",
            "The walnut sphinx caterpillar whistles to scare hungry birds away.",
            "Some caterpillars escape danger by dropping off branches on a silk line.",
            "Some caterpillars feed ants sweet rewards and get protection in return.",
        },
        ["mantis"] = new[]
        {
            "A praying mantis can turn its head nearly 180 degrees to look over its shoulder.",
            "A mantis has just one ear, in the middle of its body, and can hear bats coming.",
            "Mantises have 3-D (stereo) vision, which helps them judge how far away prey is.",
            "A mantis's compound eye can be built from up to 10,000 tiny lenses.",
            "A female mantis lays 10 to 400 eggs inside a foamy case that hardens to protect them.",
            "Some mantises look just like flowers, luring in insects that come to visit.",
        },
        ["stick_insect"] = new[]
        {
            "The longest insect on Earth is a stick insect — females can reach about 64 cm (25 in).",
            "Some stick insect eggs have a tasty handle that ants carry into their nests.",
            "Many female stick insects can lay eggs that hatch with no male around.",
            "Some stick insects can slowly change color to match their surroundings.",
            "Stick insects sway side to side, like twigs blowing in the breeze, to stay hidden.",
            "A stick insect in danger can shed a leg to escape.",
            "The heaviest stick insects — big females — can weigh up to 65 grams (2.3 oz).",
        },
        ["weevil"] = new[]
        {
            "Weevils belong to the largest animal family on Earth — tens of thousands of species.",
            "A mother weevil drills holes with her long snout to lay her eggs inside plants.",
            "Most weevils have elbowed antennae that bend in the middle and end in a little club.",
            "Weevils range from about 1 millimeter long to 35 millimeters long.",
            "When disturbed, many weevils play dead by lying completely still.",
            "Bark beetles, the famous tree-tunneling beetles, are actually a type of weevil.",
            "Weevil larvae are C-shaped grubs that have no legs at all.",
        },
        ["pillbug"] = new[]
        {
            "Pill bugs are crustaceans — closer cousins of crabs and lobsters than of insects.",
            "A pill bug can roll its whole body into a perfect ball to stay safe.",
            "Pill bugs breathe with gill-like organs that must stay damp to work.",
            "Pill bugs molt in two halves — the back half sheds first, then the front days later.",
            "A mother pill bug carries her eggs in a water-filled pouch under her body.",
            "A pill bug uses two tiny tail tubes to bring water up to its mouth.",
            "A female pill bug can raise up to 40 young at a time, with up to three broods a season.",
        },
        ["ant"] = new[]
        {
            "Scientists estimate about 20 quadrillion ants live on Earth.",
            "Ants have no lungs — air passes through tiny holes in their body called spiracles.",
            "A trap-jaw ant snaps its jaws shut in about 130 microseconds — up to 230 km/h.",
            "The bullet ant's sting is rated the most painful of any insect.",
            "Some ants teach nestmates the way to food by leading them in tandem runs.",
            "Weaver ants sew leaf nests together using silk made by their larvae.",
            "A queen ant can live up to 30 years; workers live just 1 to 3 years.",
        },
        ["fly"] = new[]
        {
            "House flies taste with their feet — they find sugar just by walking on it.",
            "A fly sees movement about seven times faster than we do, making swats look like slow motion.",
            "Flies have only one pair of wings; the second pair turned into tiny balancing organs.",
            "Sticky pads on their feet let flies walk up smooth walls and hang upside down.",
            "One female house fly can lay up to 500 eggs in her lifetime.",
            "An adult house fly usually lives for only two to four weeks.",
            "Flies can't chew — they spit on food to soften it, then sponge it up as a liquid.",
        },
        ["aphid"] = new[]
        {
            "Some aphid babies are born already pregnant — it's called telescoping generations.",
            "Aphids can shoot droplets of quick-hardening wax from tubes on their backs.",
            "Ants protect aphids from predators and drink the sugary honeydew the aphids release.",
            "An aphid drinks more sugary sap than it can use and leaks the leftover as honeydew.",
            "Around 5,000 aphid species have been described by scientists.",
            "Winged aphids can ride the wind as high as 600 meters in the air.",
            "Aphids have been around for about 280 million years — before the dinosaurs.",
        },
        ["barklice"] = new[]
        {
            "Barklice eat algae, lichens, fungi, and tiny scraps of dead leaves.",
            "Some barklice spin silk and can wrap whole tree trunks in silky webs.",
            "Indoor cousins called booklice nibble on grains, wallpaper glue, and book bindings.",
            "True lice are barklice's close cousins — they evolved from inside this family tree.",
            "Many barklice females can have babies without ever meeting a male.",
            "Booklice are tiny — usually only 1 to 2 millimeters long.",
            "Barklice are harmless scavengers — they never bite people.",
        },
        ["cicada"] = new[]
        {
            "Some cicadas live underground for 17 years before finally emerging as adults.",
            "Male cicadas can sing up to 120 decibels — among the loudest sounds any insect makes.",
            "Only male cicadas sing, vibrating drum-like organs called tymbals on their belly.",
            "A male cicada's hollow belly works as a built-in sound box that makes its call louder.",
            "Rain rolls right off cicada wings, washing them clean as it goes.",
            "Microscopic spikes on cicada wings kill bacteria by tearing their cell membranes apart.",
            "Cicadas jet their pee at up to 3 metres per second — the fastest squirt ever measured.",
        },
        ["click_beetle"] = new[]
        {
            "Click beetles are named for the loud clicking noise they make when caught.",
            "To flip upright, a click beetle hooks a spine into a notch, lets it snap, and is hurled up.",
            "Click beetle babies, called wireworms, can live underground for two to six years.",
            "Wireworms sniff out plant roots by following the carbon dioxide trails roots give off.",
            "Some tropical click beetles give off light bright enough to read by.",
            "Farmers once trapped click beetles with sweet baits because adults love sugary liquids.",
            "Most click beetles are brown or black and under 18 millimeters long.",
        },
        ["damselfly"] = new[]
        {
            "Most damselflies rest with their wings folded together above their body.",
            "A damselfly's big eyes are always widely separated, unlike a dragonfly's.",
            "Damselfly babies live in water and breathe through three leaflike tail gills.",
            "A damselfly nymph catches prey by shooting out its long, hinged lower lip like a mask.",
            "Adult damselflies hunt and eat mosquitoes, flies, and other small insects.",
            "Mating damselflies link their bodies into a circle called the wheel, shaped like a heart.",
            "Helicopter damselflies pluck spiders right out of their webs to eat.",
        },
        ["earwig"] = new[]
        {
            "Earwig pincers tell the sexes apart: males' pincers curve, females' stay straight.",
            "Earwigs use their pincers to catch prey, defend themselves, and fold up their wings.",
            "Mother earwigs guard their eggs and keep cleaning them to protect them from fungus.",
            "Baby earwigs stay with their mother and eat food she spits up for them.",
            "Most earwigs have wings, but they are hardly ever seen flying.",
            "Earwigs hide in tight, damp cracks during the day and come out at night.",
            "The largest known earwig, from Saint Helena, reached 78 mm and died out in 2014.",
        },
        ["earthworm"] = new[]
        {
            "Earthworms have no lungs — they breathe through their damp skin.",
            "Earthworms have five pairs of aortic arches that act as hearts.",
            "The longest earthworms, living on the Mekong River's banks, can stretch up to 3 meters.",
            "One Australian earthworm, the blue squirter, can squirt fluid up to 30 centimeters high.",
            "Earthworms are born with every segment they will ever have, growing by fattening each one.",
            "Earthworms have no eyes, but their skin is dotted with cells that sense light.",
        },
        ["froghopper"] = new[]
        {
            "Froghoppers can leap 70 cm straight up — about 100 times their own body length.",
            "They blast off at 4,000 m/s² — more than 400 times the pull of gravity.",
            "Their young, called spittlebugs, hide in frothy \"cuckoo spit\" bubbles.",
            "Spittlebug nymphs sip watery sap flowing up from plant roots.",
            "Some spittlebug young skip the bubbles and live inside little chalky tubes instead.",
            "A female froghopper can lay up to 400 eggs, and her eggs can survive the winter.",
            "Froghoppers have tiny zinc-hardened spine tips on their legs for grip at takeoff.",
        },
        ["glowworm"] = new[]
        {
            "A \"glowworm\" is usually a baby firefly — or a firefly female that can't fly.",
            "All firefly babies glow — a warning signal that tells predators they taste bad.",
            "Glowworm light is \"cold light\" — it gives off no infrared or ultraviolet.",
            "Glowworm larvae are hunters that prey on snails and slugs.",
            "Some adult glowworms have no mouth and live only to mate and lay eggs.",
            "New Zealand glowworms are fungus gnat larvae that glow to lure insects into silk snares.",
            "In glowworm caves, thousands of larvae light up the ceiling like a night sky.",
        },
        ["jewel_beetle"] = new[]
        {
            "A jewel beetle's metallic shine comes from microscopic shell structures, not pigment.",
            "One golden jewel beetle larva lived 47 years inside a wooden staircase before emerging.",
            "Some jewel beetles can smell pine smoke from far away and see infrared to find fires.",
            "The invasive emerald ash borer is a jewel beetle whose larvae can kill ash trees.",
            "Male Australian jewel beetles sometimes court brown beer bottles, mistaking them for females.",
            "Jewel beetles range from 3 mm to 8 cm long.",
            "Jewel beetle larvae tunnel through roots, logs, stems, and even leaves of plants.",
        },
        ["lacewing"] = new[]
        {
            "Green lacewing babies are nicknamed \"aphid lions\" for their huge appetite for aphids.",
            "A lacewing larva can eat about 100 aphids in a single week.",
            "A mother lacewing hangs each egg on a slim silk stalk, like a tiny flagpole.",
            "Many green lacewings have strikingly golden eyes.",
            "Lacewings have ears at their wing bases; hearing a bat's squeak, they fold and drop.",
            "Lacewings \"sing\" to each other with body vibrations — lookalike species have different songs.",
            "Farmers raise and release millions of green lacewings to gobble up crop pests.",
        },
        ["lanternfly"] = new[]
        {
            "Despite its name, the lanternfly is not a fly — it's a kind of planthopper.",
            "Lanternflies give off no light — the \"lantern\" name came from an old myth about glowing heads.",
            "Spotted lanternfly nymphs start black with white spots, then turn red with white spots.",
            "Its gray front wings hide bright red hind wings underneath.",
            "It feeds on the sap of more than 170 different plants, including grapevines.",
            "Each lanternfly egg mass packs 30–50 eggs under a gray, mud-like coating.",
            "The spotted lanternfly reached the United States in 2014 and spread as an invasive pest.",
        },
        ["leafhopper"] = new[]
        {
            "Leafhoppers are the only insects that make \"brochosomes\" — tiny spheres they coat themselves in.",
            "Sharpshooter leafhoppers can eject up to 300 times their body weight in liquid each day.",
            "They fling each droplet so cleverly that it flies off faster than the flick that hit it.",
            "Some leafhoppers build silk nests under leaves to hide from predators.",
            "Some leafhoppers occasionally poke human skin and sip blood — scientists aren't sure why.",
            "Leafhoppers can spread plant diseases as they feed on sap.",
        },
        ["mayfly"] = new[]
        {
            "Mayflies are the only insects that molt one more time after growing working wings.",
            "Adult mayflies cannot eat — their mouthparts are useless and their bellies are filled with air.",
            "One mayfly species, Dolania americana, has adult females that live less than five minutes.",
            "A female mayfly lays between 400 and 3,000 eggs at a time.",
            "Young mayflies live underwater for months or even years, molting up to 50 times.",
            "Whole mayfly populations hatch together, dancing in enormous swarms for a day or two.",
            "Europe's largest mayfly, the Tisza mayfly, can reach 12 cm from head to tail-tip.",
        },
        ["rhinoceros_beetle"] = new[]
        {
            "The Hercules beetle can reach 17 cm long with its horn — one of the longest beetles.",
            "Rhinoceros beetles are harmless to people — they cannot bite or sting.",
            "Only the males grow horns, which they use to wrestle other males and to dig.",
            "A male rhinoceros beetle's horn size shows how well he ate while growing up.",
            "When disturbed, some rhinoceros beetles hiss by rubbing their abdomen against their wings.",
            "Hercules beetles change color from olive-green to black as the air gets more humid.",
            "Hercules beetle grubs can grow to 11 cm long and weigh more than 100 grams.",
        },
        ["shield_bug"] = new[]
        {
            "Stink bugs are named for the stinky liquid they release to keep enemies away.",
            "There are about 5,000 species of stink bug in the world.",
            "Stink bugs have broad, oval bodies shaped like tiny shields.",
            "A female stink bug can lay 100 or more eggs that look like tiny barrels.",
            "Some mother stink bugs stay to guard their eggs and newly hatched young.",
            "Not all stink bugs eat plants — some hunt and eat other insects, like beetle larvae.",
            "In cool climates, adult stink bugs hibernate through the winter.",
        },
        ["silverfish"] = new[]
        {
            "Silverfish get their name from their silvery color and fish-like wiggle when running.",
            "A silverfish can live 2 to 8 years, which is very old for such a small insect.",
            "Silverfish molt up to 66 times in their lives — sometimes 30 times in just one year.",
            "With water to drink, a silverfish can go a whole year without eating any food.",
            "A silverfish can regrow a lost antenna or tail bristle in about four weeks.",
            "Silverfish can eat paper — their gut makes an enzyme that digests cellulose.",
            "Before mating, two silverfish perform a little dance that can last half an hour.",
        },
        ["slug"] = new[]
        {
            "Every slug is a hermaphrodite — each one has both male and female body parts.",
            "Most slugs carry a tiny shell hidden inside their body that stores calcium.",
            "A slug breathes through a small hole on the right side of its back.",
            "Slug slime has tiny fibers that let slugs climb up walls and leaves without slipping.",
            "Some slugs make slime so sticky it can trap a predator trying to grab them.",
            "In the wild, most slugs live only about one year.",
            "Some slugs are carnivores that hunt and eat other slugs and earthworms.",
        },
        ["stag_beetle"] = new[]
        {
            "Male stag beetles have huge jaws shaped like deer antlers, for wrestling other males.",
            "The stag beetle is the largest beetle in Europe.",
            "The biggest male stag beetles grow up to 7.5 centimeters (3 inches) long.",
            "Stag beetle grubs live in rotting wood for 3 to 7 years before turning into adults.",
            "Adult stag beetles live only a few weeks.",
            "Adult stag beetles drink nectar, tree sap, and juice from fallen fruit instead of chewing.",
            "Stag beetle grubs can squeak to each other by rubbing little combs on their legs.",
        },
        ["tiger_beetle"] = new[]
        {
            "The fastest tiger beetle runs 9 km/h (5.6 mph) — about 125 body lengths every second.",
            "Tiger beetles run so fast their eyes can't keep up, so they sprint, stop, and look around.",
            "Tiger beetle larvae hide in burrows up to a meter deep and snatch prey that wanders close.",
            "Hooks on a tiger beetle larva's back anchor it in its burrow so prey can't pull it out.",
            "Tiger beetles have big bulging eyes and long, curved jaws for grabbing prey.",
            "While running, tiger beetles hold their antennae stiffly in front to feel for obstacles.",
            "Some tiger beetles make ultrasound clicks that trick bats into thinking they taste bad.",
        },
        ["tortoise_beetle"] = new[]
        {
            "Tortoise beetles can hide their head and legs under the wide edges of their shell.",
            "Many tortoise beetles are shiny and metallic, like little jewels.",
            "Some tortoise beetles change color as water shifts inside their see-through shell.",
            "The golden tortoise beetle turns from shiny gold to red with black spots when touched.",
            "The golden tortoise beetle is nicknamed the \"goldbug\" and is only 5–7 millimeters long.",
            "Tortoise beetle larvae carry a shield made of their own poop and old skins on a fork.",
        },
        ["water_strider"] = new[]
        {
            "Water striders can walk on water thanks to surface tension and long, water-repellent legs.",
            "A water strider is covered with more than a thousand waterproof hairs per square millimeter.",
            "Water striders can glide across water at 1 meter per second or faster.",
            "Water striders feel ripples from trapped insects with their front legs, then dash over to eat.",
            "The largest water strider has legs that can each stretch more than 10 centimeters.",
            "A few ocean-going water striders are the only insects that live out on the open sea.",
            "If a wave dunks a water strider, its hairs trap air bubbles that float it back to the surface.",
        },
    };

    private static readonly string[] Fallback =
    {
        "A happy little critter, found at last.",
        "Another cozy corner of the forest, tidied.",
        "It wiggles with gratitude.",
    };

    /// <summary>One random fun fact about this bug's real-life species.</summary>
    public static string Pick(BugType bug)
    {
        if (!Facts.TryGetValue(bug.Id, out var pool))
            pool = Fallback;
        return pool[Rng.RandiRange(0, pool.Length - 1)];
    }
}
