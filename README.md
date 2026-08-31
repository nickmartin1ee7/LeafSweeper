# LeafSweeper

<img width="537" height="1003" alt="image" src="https://github.com/user-attachments/assets/89bf9134-a3db-4fc9-a90c-830a428b829b" />
<img width="537" height="1003" alt="image" src="https://github.com/user-attachments/assets/9b7c085a-21c6-4406-bf30-38ab9fd5e7b2" />

A cozy, pressure-free 2D mobile puzzle game about tidying up nature's mess.
Sweep leaves, petals, sticks and stones off a patch of forest floor until you
reveal the little creature hiding underneath — then tap it to win the round.
No timers, no fail state; the bug never runs away. Just you, the leaves, and
a patient ladybug.

Built with **Godot 4.7 (C# / .NET 8)** for Android phones (FHD+ portrait,
1080×2340 design resolution; the ground extends to fit any aspect).

## How to play

1. **Play** from the title menu (continues at your current level).
2. **Drag** your finger across the screen to sweep debris away — each sweep
   clears at most 12 pieces, so tidy up a little at a time. Heavy things
   (rocks, sticks, moss) need more effort than light petals. Only gestures
   that actually sweep debris count toward the sweep counter — bare taps
   are free.
3. Spot the hidden bug and **tap** it — a petal celebration and a friendly
   comment about your solve ("Just 18 sweeps to find the bug!") appear.
4. Tap **Next** to keep going. Difficulty rises *very* gently: by level 200
   the litter thickens from ~1365 to ~2123 pieces, the bug is slightly smaller
   and may blend in a little — that's all.

Thirty-nine bug species hide in the leaves — ladybug, butterfly, centipede,
moth, grasshopper, dragonfly, beetle, snail, firefly, bumblebee, caterpillar,
mantis, stick insect, weevil, pill bug, ant, fly, aphid, barklouse, cicada,
click beetle, damselfly, earwig, earthworm, froghopper, glowworm, jewel
beetle, lacewing, lanternfly, leafhopper, mayfly, rhinoceros beetle, shield
bug, silverfish, slug, stag beetle, tiger beetle, tortoise beetle and water
strider — each with four natural color variants (156 collectible looks,
named like "Yellow Ladybug"), so you learn to spot shapes rather than
colors. And 1 round in 100 hides a **prismatic** bug wearing a shifting
rainbow sheen: find it and a yellow-sun lens flare rides the bug up to its
seat behind the win card before the grandiose near-white card appears,
while the circling litter turns to a swirl of gold and white.

## Features

- **Endless rounds** with a smooth, casual-tuned difficulty curve
  (`scripts/RoundConfig.cs` — one tunable function).
- **Bug Collection Book** — the gold book coin at the dock's bottom-left
  opens a full-screen, single-page book: it opens on the cover, turns
  itself to your game stats, and dog-eared page corners (fold + shadow +
  arrow, top-right forward / bottom-right back) or a horizontal swipe on
  the page (an even spine flip either way) page through the whole
  collection. Bugs you haven't met yet are drifting black mist silhouettes
  marked "??? (x0)"; found ones show their art and count ("Firefly (x3)").
  Tap anywhere off the page to close it.
- **Rare prismatic bugs** — a 1% chance per round (test with
  `LEAF_PRISMATIC=1`): rainbow-sheened critter, sun-flare discovery, and a
  grandiose win card — lifted to a lighter near-white with a gold rim —
  plus a shiny "Prismatic" banner that rides out the round's end.
- **Storm levels** — every 10th level (from level 10; test with
  `LEAF_STORM=1`) the sky darkens under wind-carried rain, drifting fog and
  lightning, and each patch you sweep is re-littered a few seconds later —
  memory becomes the difficulty. Gusts don't help hoarders either: debris
  cleared by a gust is re-littered just like swept ground. And the storm
  escalates: every 4–6 seconds a gust dumps a fresh cluster of 6–12 brand-new
  pieces onto random spots, piling the litter up to 3× the round's starting
  floor before the flood relents (swept patches keep re-littering after
  that). Two storm-only rhythms layer on more chaos, each on its own
  10–20s timer: a spiral gust spins a small clockwise swirl through the
  litter (never wider than a fifth of the screen), and loose debris rafts
  drift across the screen in spiral-y loops — pure atmosphere; the rafts
  never land. The round before a storm round warns you with a sparking
  "Storm Round" sign.
- **Local save data** (`user://save.json`, app-private on Android — no
  permissions): current level, levels cleared, lifetime sweeps & play time,
  per-bug-variant find counts, prismatic finds, and the last 50 cleared
  levels. Saved atomically after every clear; a corrupt or missing file
  just starts a fresh save.
- **Between-round comments** picked from templates by how you played,
  referencing your history ("Your best is 12 sweeps!") — plus a real fun
  fact about the species you just found, picked from its own 6+ fact pool.
- **Main menu** with lifetime progress and your favorite critter.
- **Update notification** — once at boot the title screen checks GitHub
  releases and shows a tappable "🌐 Update Available (vX.Y.Z)" line when a
  newer release exists; tapping it opens the releases page so you can grab
  the new version. Any failure (offline, rate limit) just stays silent.
- **Ambient rustles** — every 2–4s a stray draft shivers a localized cluster
  of about 4–7 pieces of the litter in place (three times as often during
  storms). Purely cosmetic: the wobble lives on the pieces' sprites, so
  coverage, sweeping and the win gyre are never affected.
- **Procedurally generated art** — every texture in `assets/textures/` is
  reproducible from `tools/gen_art.mjs` (see `docs/art-style.md`).

## Building

Requirements: .NET SDK 8+ (project targets `Godot.NET.Sdk/4.7.1`), Godot 4.7.1 mono.

```sh
dotnet build                    # compile C#
godot --headless --import       # import assets/scenes
godot --headless --quit-after 180   # boot the game headless (smoke test)
LEAF_AUTOPLAY=1 godot --headless --quit-after 4700   # self-test: plays a level (with a forced prismatic bug and storm weather), verifies catalog/book/save round-trip, exit code 0 on pass
```

For desktop testing, just open the project in the Godot editor and press
Play — mouse input is emulated as touch.

### Android export

The preset `Android` is configured for a signed release build via gradle
(`use_gradle_build=true`, output `build/LeafSweeper.apk`, package
`com.gitgoodsoftware.leafsweeper`). Prerequisites: Godot 4.7.1 mono export
templates, the Android build template (`android/build` in the project),
an Android SDK + JDK configured in Editor Settings → Export → Android,
and a release keystore.

Build the shareable demo APK with one command:

```sh
./build-demo-apk.sh
```

One-time setup: copy `keystore.env.example` to `keystore.env` and fill in
the release keystore password (the file is gitignored, never committed).
The script finds Godot, exports `build/LeafSweeper.apk` and cleans up
the csproj churn the exporter leaves behind. Then share or install it:

```sh
adb install -r build/LeafSweeper.apk
```

See `docs/testing.md` for the device-testing checklist used during
development.

## Documentation

- [`AGENTS.md`](AGENTS.md) — instructions for AI coding agents contributing
  to this repo (workflow, validation, environment quirks).
- [`docs/game-design.md`](docs/game-design.md) — the design document.
- [`docs/architecture.md`](docs/architecture.md) — scripts, scene tree, data flow.
- [`docs/art-style.md`](docs/art-style.md) — art direction, palette and how to
  regenerate assets (with the original reference sheet:
  [`docs/art-reference.jpg`](docs/art-reference.jpg)).
- [`docs/testing.md`](docs/testing.md) — validation workflow and device checklist.
- [`docs/agentic-development.md`](docs/agentic-development.md) — how this
  project is built with an AI coding agent: workflow, patterns, practices.

## Project layout

```
assets/textures/    generated SVG art (debris, bugs, ground)
assets/textures/bugs/  bug catalog art
scenes/Main.tscn    minimal root scene; everything is built in code
scripts/            all game code (C#)
tools/gen_art.mjs   procedural texture generator (node tools/gen_art.mjs)
docs/               design, architecture, art and testing docs
```

## License / scope

Personal MVP. Out of scope for now: sound effects, cloud sync.
