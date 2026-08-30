# LeafSweeper — Agentic Development Guide

This project was built end-to-end in an agentic development session: an AI
coding agent (GitHub Copilot CLI) implementing against an approved plan,
with the human steering direction, playtesting in the Godot editor, and
gating every hand-off. This document captures the workflow, patterns and
practices so a contributor can reproduce the loop — or so a future agent
session can pick the project up cold.

> **Scope note:** [`AGENTS.md`](../AGENTS.md) is the source of truth for the
> operational rules — validation commands, cleanup, the worktree slice loop,
> the PR requirement and the TODO-tracked workflow. This document explains
> *why* those rules exist and records the troubleshooting patterns behind
> them; if any command line here disagrees with AGENTS.md, follow AGENTS.md.

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
5. **Commit atomically — and only when the human asks.** An agent session
   does not commit, push or open a PR on its own initiative: it hands the
   slice off green and uncommitted on its branch, and the human decides
   when to land it. When a commit *is* wanted: one logical change per
   commit — a fix, a feature, or a doc alignment, never a mixed bag.
   Commit messages lead with the change ("Cover the whole floor with
   debris…") and explain the *why* in the body. Generated metadata
   (`.uid`, `.import` files) gets its own commit, separate from behavior
   changes. Landing always goes through a GitHub PR: push the slice branch,
   open the PR, and once it is merged, remove the worktree and delete the
   branch — merged branches are never left behind.
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
| 4 | `LEAF_AUTOPLAY=1 godot --headless --quit-after 2000` | gameplay logic + persistence |
| 5 | windowed run + screenshot hook (visual slices) | rendered pixels: layout, layering, fonts |
| 6 | render generated SVGs + visual checklist (art slices) | silhouette errors: detached/overlapping appendages, legs through the body |

`LEAF_AUTOPLAY` is an env-gated self-test in `scripts/Main.cs`: it resets
the save (deterministic), plays a level end-to-end (ticks, sweeps, win),
then reloads the save and asserts the full round-trip. It exits `0` on
pass, `1` on failure, so it can gate commits or CI. Lessons baked in:

- **Self-tests must be deterministic.** The original autoplay assumed a
  fresh save and failed spuriously once real save data existed; it now
  resets first.
- **Env-gated hooks beat test frameworks** for this scale — no extra
  dependencies, runs with the same binary the player runs.
- **A fresh worktree needs `--import` before level 4.** Without a prior
  import pass nothing boots and autoplay **silently exits 0 with no
  `AUTOPLAY` output** — always grep for `AUTOPLAY`, never trust the exit
  code alone.
- **Animation-gated assertions.** Outcomes that land at the end of a tween
  (a coin arriving at the dock button before its +1 banks) are tested the
  way players experience them: `RunHeadlessAutoplay` is `async void` and
  `await ToSignal(coin, GustCoin.SignalName.CollectionFlightFinished)`
  before asserting. `--quit-after 2000` must outlast the awaited animation
  (~1.6 s ≈ 100 frames).
- **Pixel-accurate logic needs independent ground truth.** When a hot
  path uses a cached or approximated structure (`Debris.Covers` scans a
  cached 4px alpha mask), verify it in the autoplay against a brute-force
  recompute that shares no code with it — `CoversByTextureAlpha` samples
  the texture's alpha channel directly — for a positive case (a blocker
  parked on the bug) *and* a negative one (a far-away point). A fast path
  and its verifier must never share the bug being tested for: a shared
  coordinate-mapping mistake would otherwise self-confirm as green.
- **Import artifacts dirty the worktree.** Each `--import` run rewrites
  `LeafSweeper.csproj` (dropping a redundant line) and leaves
  `LeafSweeper.csproj.old` — `git checkout -- LeafSweeper.csproj` and
  delete the `.old` before committing. A new script's generated `.cs.uid`
  is committed together with the script.
- **Whitespace drift on the main checkout.** Tab-reindent edits keep
  reappearing in the main checkout (editor auto-format on save). Before
  committing there, confirm `git diff --ignore-all-space --stat` is empty
  and discard them — the repo standard is 4-space indent.
- **C# API surface ≠ GDScript.** `GD.RandfRange` doesn't exist in Godot C#
  (it's `GD.RandRange`, doubles, or a `RandomNumberGenerator`) — the level 1
  build catches these, so never skip it.

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

**Level 6 exists because generators emit plausible-but-broken art.** Every
rung above was green while the bug catalog shipped with wings attached to
the wrong body part, legs stroked *across* the body fill, and fanned wings
floating free of the trunk — valid SVGs, clean build, autoplay passing.
Generated art is judged by looking at it, in a loop:

1. Render each asset at 400 px over the ground color:
   `rsvg-convert -w 400 -h 400 -b "#6a5c43" assets/textures/bugs/<bug>.svg -o out.png`
2. View it against the checklist in [`art-style.md`](art-style.md)
   (appendages attach and reach the body, nothing crosses the fill,
   silhouette reads at a glance).
3. Fix the generator function, regenerate, re-render. Repeat until every
   asset passes; record per-asset verdicts so the loop terminates.

When eyeballing disagrees with itself, **measure**: extract the generated
coordinates and compare them against the body geometry. The dragonfly's
detached wings looked fine in isolation but failed arithmetic — a rotated
wing's inner tip must land within the body's stroke (center ± rx·cosθ).
Two SVG rules cover most failures: appendages tucked under a body must be
drawn *before* the body shapes, and anchored appendages must have their
attachment point computed, not eyeballed.

**Parallel subagents for independent asset fixes.** When a review yields
N independent failures (one bug = one generator function = one region of
`gen_art.mjs`), spawn one agent per item instead of serially editing:
each gets narrow constraints (edit only your function, regenerate, render
and self-verify, no git commands) and works in its own context. Keep the
agents alive after their first result — sending a follow-up with concrete
corrective math beats relaunching, because they retain what they already
tried. Two hygiene rules: sweep the worktree for stray preview PNGs
agents leave behind before committing, and re-verify the union yourself —
subagent self-reports are a filter, not a verdict.

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
| Ground, bug, hidden gust coins | 0 | bug and coins stay below *every* debris piece until tapped |
| DebrisBottom / DebrisTop | 1 / 2 | both render over the bug and coins by design |
| Gust streaks, petal sparkles | 3 | effects ride above all debris |
| Celebrated bug | 100 | discovery pop — still below the HUD CanvasLayer |
| Collected gust coin | on the HUD layer | its flight must pass *above* the dock (see below) |

**CanvasLayer beats `ZIndex`.** A `Node2D` can never render above the HUD
dock by raising `ZIndex` — the dock lives on a `CanvasLayer`, a separate
canvas that always draws over the default one (this is why the coin's fade
"under the dock" was wrong). When a world-space thing must fly over UI —
the gust coin spiralling into the dock button — reparent it onto the HUD
layer and convert to screen space with `GetCanvasTransform() * worldPos`.

**Tunables as named constants.** Sweep radius, fling factors, friction and
coverage are `const`s in small files with comments explaining the feel
they produce, so a playtest finding maps to one named knob:

| Playtest finding | Knob |
|------------------|------|
| "Floor is visible / bug is findable too fast" | `RoundConfig.CoverageStart/End` |
| "Bug shows stacked sprites" | node lifecycle in `Bug.Setup` |
| "Sweeps clear too much / too little" | `Sweeper.SweepRadius`, `Sweeper.MaxDebrisPerSweep`, `Debris.FlingFactor`, `Debris.Friction`, `Debris.FadeDelayScale` |
| "Burst clears too much / too little, or double-taps misfire" | `Sweeper.BurstRadius`, `Sweeper.BurstFlingSpeed`, `Main.DoubleTapWindowMs`, `Main.DoubleTapSlop`, `Main.TapTravelSlop` |
| "Must sweep a huge empty radius before a bug/coin is tappable" | occlusion radii — `BugType.OcclusionRadius` (45% of tap, clamped 18–36px), `GustCoin.OcclusionRatio`; mask fidelity — `Debris.MaskCellSize`, `Debris.AlphaThreshold` |
| "Taps inflate the sweep counter" | `Sweeper.End()` — `onSweepCompleted` fires only when `_clearedThisSweep > 0`, so taps and fruitless drags never count |
| "Menu looks wrong at odd aspects" | fit-on-resize (`Main.FitGround` + `Main.OnViewportResized`) |
| "A Control (dock/HUD) is invisible despite being added" | `SetAnchorsPreset` sets anchors but **not offsets** — a zero-height rect pinned to the screen edge; set anchors *and* offsets explicitly (`Hud.BuildDock`) |
| "Sweeps act through the dock/HUD" | GUI input dies there: dock uses `MouseFilter.Stop`, sweeping is `_UnhandledInput` |
| "Gust power feels stingy / generous" | `SaveData.StartingGustPower`, `RoundConfig.NormalGustCoins` / `RoundConfig.StormGustCoins` (`GustCoinsForLevel`) |
| "Coin flight / arrival feel off" | `GustCoin.LoopSeconds`, `GustCoin.LoopTurns`, `GustCoin.DashSeconds`, `GustCoin.PathScreenMargin`, `GustCoin.WindIconRatio`, the pulse in `Hud.PulseGustPower` |
| "Badge/popup scales from a corner" | Controls scale around `PivotOffset` — set it to the center before tweening `scale` (`Hud.PulseGustPower`) |

**Slices happen in worktrees.** Each slice is implemented in a dedicated
git worktree beside the main checkout, on a short-lived branch — never in
the main checkout itself, which routinely carries the human's uncommitted
in-progress edits. Validate and commit in the worktree, push the branch,
and open a GitHub PR — `main` advances only through PRs, never through a
local `git merge`. Once the PR is merged, clean up without being asked:
`git worktree remove` the slice directory and `git branch -d` the merged
branch. Merged branches are never left behind.

An agent session whose working directory *is* the main checkout must treat
that as a trap, not an invitation: the folder-backed session makes the
main checkout the default cwd, and editing files there dirties it exactly
as if you'd worked on the human's desk. Create the worktree *before* the
first edit. If edits already landed in the main checkout, recover without
losing work — this happened for real (the occlusion-hitbox slice was
written straight into the main checkout) and the rescue took three steps:

```sh
git diff -- <changed files> > ~/persistent/slice.patch   # 1. capture
git worktree add ../LeafSweeper-<slice> -b <slice>       # 2. isolate
git apply ~/persistent/slice.patch                       #    (in worktree)
git checkout -- <changed files>                          # 3. restore main
```

Keep the patch in a persistent path (`/tmp` is volatile between agent
tool calls), verify the worktree's `git status` shows the slice before
restoring the main checkout, and never commit during the rescue unless
told to. At hand-off the main checkout must look untouched: `git status`
shows nothing but the human's own pre-existing edits.

**Docs live with code.** Behavior changes update `README.md` and
`docs/*` in the same session — numbers in prose (counts, radii) drift
fast, so doc alignment is part of the slice, not a cleanup phase.

## Environment constraints discovered

- **Running Godot dirties the tree.** Every `godot-mono --import` (or any
  headless run) rewrites `LeafSweeper.csproj` — dropping an Android
  TargetFramework line — and rewrites `scripts/SaveData.cs`/`Sweeper.cs`
  with whitespace-only churn (spaces→tabs). After each Godot run:
  `git checkout -- LeafSweeper.csproj; rm -f LeafSweeper.csproj.old`, and
  before committing, confirm the churn is content-free (`git diff -w` is
  empty) and discard it with `git checkout -- <file>` — never hand off
  whitespace-only dirty files. The binary on this machine is `godot-mono`
  (NixOS), not plain `godot`.
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
- **This is a NixOS machine — resolve local tools once per session.** The
  Godot binary is a Nix store wrapper (glob
  `/nix/store/*godot-mono-wrapper*/bin/godot-mono`), not a stable PATH
  entry. `git-remote-https` crashes with `version 'CURL_OPENSSL_4' not
  found`, so pushes inject the authenticated token into the URL instead:
  `git -c credential.helper= -c http.sslVerify=false push
  https://<user>:$(gh auth token)@github.com/<owner>/<repo>.git <branch>`.
  `/tmp` is volatile between agent tool calls — keep scratch artifacts in
  persistent paths.
- **Long-running installs are human-run; exports can be headless.** Android
  SDK/template installation is done from the editor by the human, but the
  APK export itself runs headlessly with the command in the README — the
  42 MB signed demo APK builds in a few minutes once gradle is warm.

## Quick reference

The command cheatsheet (validate ladder, post-run cleanup, worktree slice
loop with push/PR steps, whitespace check) lives once, in
[`AGENTS.md`](../AGENTS.md) — a single copy so it cannot drift from the
rules agents actually follow.

See also: [`docs/architecture.md`](architecture.md) for the code map,
[`docs/testing.md`](testing.md) for the device checklist,
[`docs/game-design.md`](game-design.md) for the design values all of this
serves.
