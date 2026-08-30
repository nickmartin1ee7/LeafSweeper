# LeafSweeper

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

Seventeen bug types hide in the leaves: ladybug, butterfly, centipede, moth,
grasshopper, dragonfly, beetle, snail, firefly, bumblebee, caterpillar,
mantis, stick insect, weevil, pill bug, ant and fly — each with its own
size and silhouette, so you learn to spot shapes rather than colors.

## Features

- **Endless rounds** with a smooth, casual-tuned difficulty curve
  (`scripts/RoundConfig.cs` — one tunable function).
- **Local save data** (`user://save.json`, app-private on Android — no
  permissions): current level, levels cleared, lifetime sweeps & play time,
  per-bug-type find counts, and the last 50 cleared levels. Saved atomically
  after every clear; a corrupt or missing file just starts a fresh save.
- **Between-round comments** picked from templates by how you played,
  referencing your history ("Your best is 12 sweeps!").
- **Main menu** with lifetime progress and your favorite critter.
- **Ambient rustles** — every 2–4s a stray draft shivers a localized cluster
  of about 4–7 pieces of the litter in place. Purely cosmetic: the wobble
  lives on the pieces' sprites, so coverage, sweeping and the win gyre are
  never affected.
- **Procedurally generated art** — every texture in `assets/textures/` is
  reproducible from `tools/gen_art.mjs` (see `docs/art-style.md`).

## Building

Requirements: .NET SDK 8+ (project targets `Godot.NET.Sdk/4.7.1`), Godot 4.7.1 mono.

```sh
dotnet build                    # compile C#
godot --headless --import       # import assets/scenes
godot --headless --quit-after 180   # boot the game headless (smoke test)
LEAF_AUTOPLAY=1 godot --headless --quit-after 2000   # self-test: plays a level, verifies save round-trip, exit code 0 on pass
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
