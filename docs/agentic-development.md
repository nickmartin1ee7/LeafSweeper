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
   code. Each slice is implemented in its own git worktree on a short-lived
   branch (see the worktree slice loop under *Patterns and practices*), so
   the main checkout — which often carries in-progress human edits — is
   never disturbed mid-slice.
4. **Validate headlessly after every slice.** See the validation ladder
   below. Nothing is committed without a green build plus the cheapest
   meaningful runtime check.
5. **Commit atomically.** One logical change per commit — a fix, a feature,
   or a doc alignment, never a mixed bag. Commit messages lead with the
   change ("Cover the whole floor with debris…") and explain the *why* in
   the body. Generated metadata (`.uid`, `.import` files) gets its own
   commit, separate from behavior changes. When the slice is green, merge
   its branch into `main` locally, then always remove the worktree and
   delete the branch — merged branches are never left behind.
6. **Hand off to the human for playtesting.** The agent stops at "buildable
   and headlessly verified" and the human verifies feel and visuals in the
   editor or on device. Feedback comes back as concrete findings
   ("bug is visible without swiping", "quadruple the debris"), which spawn
   new todos and a new loop iteration.

```
plan (approved) ─► todos ─► implement in a worktree ─► headless validate ─► atomic commit
      ▲                                                                        │
      └───────────── human playtest feedback ◄─────────────────────────────────┘
```

## Headless validation ladder

Cheapest check first; escalate only when needed:

| Level | Command | Catches |
|-------|---------|---------|
| 1 | `dotnet build` | compile errors, API misuse |
| 2 | `godot --headless --import` | bad scenes/textures/import errors |
| 3 | `godot --headless --quit-after 180` | boot-time script crashes |
| 4 | `LEAF_AUTOPLAY=1 godot --headless --quit-after 300` | gameplay logic + persistence |
| 5 | windowed run + screenshot hook (visual slices) | rendered pixels: layout, layering, fonts |

`LEAF_AUTOPLAY` is an env-gated self-test in `scripts/Main.cs`: it resets
the save (deterministic), plays a level end-to-end (ticks, swipes, win),
then reloads the save and asserts the full round-trip. It exits `0` on
pass, `1` on failure, so it can gate commits or CI. Lessons baked in:

- **Self-tests must be deterministic.** The original autoplay assumed a
  fresh save and failed spuriously once real save data existed; it now
  resets first.
- **Env-gated hooks beat test frameworks** for this scale — no extra
  dependencies, runs with the same binary the player runs.

**Level 5 exists because headless tests are blind.** During the wood-dock
slice every rung above was green while the entire dock was invisible on
screen — logic and pixels are different failure surfaces. When a slice
touches layout, layering or rendering, capture real pixels before handing
off, using the same env-gated-hook pattern:

1. Temporarily add a hook in `Main._Ready`: on `LEAF_SHOT=<path>`, start a
   level, await ~30 `ProcessFrame`s, save
   `GetViewport().GetTexture().GetImage()` to the path, then quit.
2. `LEAF_SHOT=/tmp/shot.png godot --path .` — **windowed**, not headless:
   headless mode has no framebuffer to capture.
3. Inspect the PNG, iterate, then **remove the hook** before committing.

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

**Layering encodes visibility.** Z-order is a gameplay statement, not a
styling detail. Keep it an explicit ladder (declared in `Main.BuildTree`),
never tree-order luck — `ZIndex` beats tree order anyway:

| Layer | Z | Note |
|-------|---|------|
| Ground, bug | 0 | bug stays below *every* debris piece until tapped |
| DebrisBottom / DebrisTop | 1 / 2 | both render over the bug by design |
| Gust streaks, petal sparkles | 3 | effects ride above all debris |
| Celebrated bug | 100 | discovery pop — still below the HUD CanvasLayer |

**Tunables as named constants.** Sweep radius, fling factors, friction and
coverage are `const`s in small files with comments explaining the feel
they produce, so a playtest finding maps to one named knob:

| Playtest finding | Knob |
|------------------|------|
| "Floor is visible / bug is findable too fast" | `RoundConfig.CoverageStart/End` |
| "Bug shows stacked sprites" | node lifecycle in `Bug.Setup` |
| "Sweeps clear too much / too little" | `Sweeper.SweepRadius`, `Sweeper.MaxDebrisPerSwipe`, `Debris.FlingFactor`, `Debris.Friction`, `Debris.FadeDelayScale` |
| "Menu looks wrong at odd aspects" | fit-on-resize (`Main.FitGround` + `Main.OnViewportResized`) |
| "A Control (dock/HUD) is invisible despite being added" | `SetAnchorsPreset` sets anchors but **not offsets** — a zero-height rect pinned to the screen edge; set anchors *and* offsets explicitly (`Hud.BuildDock`) |
| "Sweeps act through the dock/HUD" | GUI input dies there: dock uses `MouseFilter.Stop`, sweeping is `_UnhandledInput` |

**Slices happen in worktrees.** Each slice is implemented in a dedicated
git worktree beside the main checkout, on a short-lived branch — never in
the main checkout itself, which routinely carries the human's uncommitted
in-progress edits. Validate and commit in the worktree, merge the branch
into `main` locally, then clean up without being asked: `git worktree
remove` the slice directory and `git branch -d` the merged branch.
Merged branches are never left behind.

**Docs live with code.** Behavior changes update `README.md` and
`docs/*` in the same session — numbers in prose (counts, radii) drift
fast, so doc alignment is part of the slice, not a cleanup phase.

## Environment constraints discovered

- **.NET target pinned to net8.0.** The contributor's local SDK is 8.0.x
  (`NETSDK1045` on net9.0). Keep `LeafSweeper.csproj` at net8.0 unless the
  whole toolchain moves together. (net9.0 was tried once and reverted for
  exactly this reason.)
- **Android APKs need gradle builds + a keystore via env vars.** The
  prebuilt export template does not package the game's .NET assemblies;
  the APK installs but dies at boot (`.NET: Assemblies not found`).
  `gradle_build/use_gradle_build=true` fixes that. Godot 4.7 reads the
  release keystore from the preset `keystore/release` options **or**
  `GODOT_ANDROID_KEYSTORE_RELEASE_*` env vars — no editor-settings
  fallback — so secrets stay out of the repo. Export headlessly with
  `godot --headless --path . --export-release Android build/LeafSweeper.apk`
  (see README for the full command). The demo identity is package
  `com.gitgoodsoftware.leafsweeper`, signed by the
  `CN=GitGoodSoftware, O=GitGoodSoftware, C=US` release keystore.
- **Machine-specific paths are not in the repo.** Editor settings (Android
  SDK, JDK paths, keystores) live in the user's Godot editor settings or
  `~/.local/share/godot/keystores`; only portable configuration is committed.
- **Long-running installs are human-run; exports can be headless.** Android
  SDK/template installation is done from the editor by the human, but the
  APK export itself runs headlessly with the command in the README — the
  42 MB signed demo APK builds in a few minutes once gradle is warm.

## Quick reference

```sh
dotnet build                                   # 1. compile
godot --headless --import                      # 2. import
godot --headless --quit-after 180              # 3. boot smoke
LEAF_AUTOPLAY=1 godot --headless --quit-after 300   # 4. gameplay self-test

# Worktree slice loop (run from the main checkout)
git worktree add ../LeafSweeper-<slice> -b <slice>   # isolate the slice
# ...dotnet build / headless checks / atomic commits in the worktree...
git merge <slice>                                    # absorb into main
git worktree remove ../LeafSweeper-<slice>           # clean up (always)
git branch -d <slice>
```

See also: [`docs/architecture.md`](architecture.md) for the code map,
[`docs/testing.md`](testing.md) for the device checklist,
[`docs/game-design.md`](game-design.md) for the design values all of this
serves.
