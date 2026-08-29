# LeafSweeper — Agentic Development Guide

This project was built end-to-end in an agentic development session: an AI
coding agent (GitHub Copilot CLI) implementing against an approved plan,
with the human steering direction, playtesting in the Godot editor, and
gating every hand-off. This document captures the workflow, patterns and
practices so a contributor can reproduce the loop — or so a future agent
session can pick the project up cold.

## The session loop

Each slice of work went through the same six steps:

1. **Plan first, code second.** A plan (goal, milestone breakdown, task
   list) is written and approved by the human *before* any code changes.
   The plan lives outside the repo (session workspace) and gets progress
   notes appended as the session evolves.
2. **Track work in a structured todo list.** Every planned unit of work is
   a todo with an id, gerund-form title, and a description detailed enough
   to execute without re-reading the plan. Statuses move
   `pending → in_progress → done`; dependencies are recorded so the "ready"
   set is always queryable. When scope emerges mid-session (e.g. a playtest
   finding), it becomes a new todo instead of loose work.
3. **Implement in small vertical slices.** One todo = one coherent change
   (a data layer, a controller, a menu). No drive-by refactors of unrelated
   code.
4. **Validate headlessly after every slice.** See the validation ladder
   below. Nothing is committed without a green build plus the cheapest
   meaningful runtime check.
5. **Commit atomically.** One logical change per commit — a fix, a feature,
   or a doc alignment, never a mixed bag. Commit messages lead with the
   change ("Cover the whole floor with debris…") and explain the *why* in
   the body. Generated metadata (`.uid`, `.import` files) gets its own
   commit, separate from behavior changes.
6. **Hand off to the human for playtesting.** The agent stops at "buildable
   and headlessly verified" and the human verifies feel and visuals in the
   editor or on device. Feedback comes back as concrete findings
   ("bug is visible without swiping", "quadruple the debris"), which spawn
   new todos and a new loop iteration.

```
plan (approved) ─► todos ─► implement ─► headless validate ─► atomic commit
      ▲                                                            │
      └────────── human playtest feedback ◄────────────────────────┘
```

## Headless validation ladder

Cheapest check first; escalate only when needed:

| Level | Command | Catches |
|-------|---------|---------|
| 1 | `dotnet build` | compile errors, API misuse |
| 2 | `godot --headless --import` | bad scenes/textures/import errors |
| 3 | `godot --headless --quit-after 180` | boot-time script crashes |
| 4 | `LEAF_AUTOPLAY=1 godot --headless --quit-after 300` | gameplay logic + persistence |

`LEAF_AUTOPLAY` is an env-gated self-test in `scripts/Main.cs`: it resets
the save (deterministic), plays a level end-to-end (ticks, swipes, win),
then reloads the save and asserts the full round-trip. It exits `0` on
pass, `1` on failure, so it can gate commits or CI. Lessons baked in:

- **Self-tests must be deterministic.** The original autoplay assumed a
  fresh save and failed spuriously once real save data existed; it now
  resets first.
- **Env-gated hooks beat test frameworks** for this scale — no extra
  dependencies, runs with the same binary the player runs.

## Patterns and practices

**Code-built scene tree.** `scenes/Main.tscn` is a one-node shell; every
UI and gameplay node is constructed in `Main.BuildTree()`. The scene stays
diffable and the agent never hand-edits binary/`tscn` state.

**Pure-function difficulty config.** All tuning lives in
`scripts/RoundConfig.cs` as deterministic functions of the level number
(`Coverage`, `BugScale`, `Camouflage`). Any level can be rebuilt exactly,
and playtest feedback becomes a one-constant edit.

**Area-driven placement, not sampling.** Debris spawns on a jittered grid
where count = floor area × coverage density. An earlier rejection-sampling
approach (random points + min-distance checks) was replaced because it
couldn't *guarantee* coverage; the grid guarantees no bare floor and no
visible bug, while the jitter hides the lattice.

**Node lifecycle hygiene.** Reused nodes must clear old children before
adding new ones. A real bug: `Bug.Setup` added a sprite each level, so
level 8 drew eight bugs stacked. Rule of thumb: `Setup()` = teardown +
build, never build-only.

**Layering encodes visibility.** The bug renders *under* both debris
layers; anything the player shouldn't see yet goes below everything the
player interacts with. Z-order is a gameplay statement, not a styling
detail.

**Tunables as named constants.** Sweep radius, fling factors, friction and
coverage are `const`s in small files with comments explaining the feel
they produce, so a playtest finding maps to one named knob:

| Playtest finding | Knob |
|------------------|------|
| "Floor is visible / bug is findable too fast" | `RoundConfig.CoverageStart/End` |
| "Bug shows stacked sprites" | node lifecycle in `Bug.Setup` |
| "Sweeps clear too much / too little" | `Sweeper.SweepRadius`, `Sweeper.MaxDebrisPerSwipe`, `Debris.FlingFactor`, `Debris.Friction`, `Debris.FadeDelayScale` |
| "Menu looks wrong at odd aspects" | fit-on-resize (`Main.FitGround` + `Main.OnViewportResized`) |

**Docs live with code.** Behavior changes update `README.md` and
`docs/*` in the same session — numbers in prose (counts, radii) drift
fast, so doc alignment is part of the slice, not a cleanup phase.

## Environment constraints discovered

- **.NET target pinned to net8.0.** The contributor's local SDK is 8.0.x
  (`NETSDK1045` on net9.0). Keep `LeafSweeper.csproj` at net8.0 unless the
  whole toolchain moves together.
- **C# Android exports need a gradle build.** The prebuilt export template
  does not package the game's .NET assemblies; the APK installs but dies
  at boot (`.NET: Assemblies not found`). `gradle_build/use_gradle_build`
  is set in `export_presets.cfg` for this reason.
- **Machine-specific paths are not in the repo.** Editor settings (Android
  SDK, keystore, JDK paths) live in the user's Godot editor settings;
  only portable configuration is committed.
- **Long-running installs/exports are human-run.** Android template
  installation and gradle exports are done from the editor by the human;
  the agent handles code, docs and headless validation.

## Quick reference

```sh
dotnet build                                   # 1. compile
godot --headless --import                      # 2. import
godot --headless --quit-after 180              # 3. boot smoke
LEAF_AUTOPLAY=1 godot --headless --quit-after 300   # 4. gameplay self-test
```

See also: [`docs/architecture.md`](architecture.md) for the code map,
[`docs/testing.md`](testing.md) for the device checklist,
[`docs/game-design.md`](game-design.md) for the design values all of this
serves.
