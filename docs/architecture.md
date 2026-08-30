# LeafSweeper — Architecture

## Overview

Godot 4.7 C# (.NET 8), **code-first scene construction**: `scenes/Main.tscn`
is a single `Node2D` with `scripts/Main.cs` attached; every other node is
built and wired in C#. No physics engine — debris motion is a lightweight
hand-rolled model (velocity, spin, friction) that is predictable and cheap on
mobile.

## Scene tree (built at runtime by `Main.BuildTree`)

```
Main (Node2D, scripts/Main.cs)
├── Ground (Node2D)
│   └── Sprite2D              ground.svg, scaled to cover viewport
├── DebrisBottom (Node2D)     70% of debris — ZIndex 1, always over the bug
├── Bug (Node2D)              hidden until StartLevel — ZIndex 0 (below debris)
├── DebrisTop (Node2D)        30% of debris — ZIndex 2, always over the bug
├── Hud (CanvasLayer)         wood dock + top level label + win overlay
└── Menu (CanvasLayer)        title screen
```

## State machine

`Main` holds `GameState { Menu, Playing, Won }`:

- **Menu** — `MainMenu` visible, level cleared.
- **Playing** — input routed to the sweeper and the bug tap check.
- **Won** — celebration + `Hud.ShowWin(comment, statsLine)`; Next →
  `StartLevel(save.CurrentLevel)` (RecordClear already advanced it), Menu →
  title.

## Modules

| Script | Responsibility |
|--------|----------------|
| `scripts/Main.cs` | Controller: builds tree, state machine, level setup/teardown, input routing (a tap only wins when the bug is uncovered — `BugIsCovered` runs the pixel-accurate `Debris.Covers` test against the bug's occlusion area, far tighter than its tap area; a covered tap starts sweeping instead; uncovered gust coins are collected the same way via `DebrisOverlaps`/`SelectableCoinAt` against the coin's occlusion radius), win flow, debris spawn layout (inside the playable rect above the HUD dock), gust coin spawning (3/round, spread away from the bug and each other) and collection (`CollectCoin` lifts the coin onto the HUD layer so it flies above the dock; the +1 `gustPower` is banked when the coin arrives, then `Hud.PulseGustPower` fires), wind gust (clears ~25% of remaining debris with streak effects; spends one gust power and counts the use per round), restart handling, petal sparkle. Also hosts the `LEAF_AUTOPLAY=1` headless self-test, which verifies the coverage rule against alpha ground truth sampled straight from the blocker texture. |
| `scripts/Sweeper.cs` | Input-to-interaction: converts touch stream into flings. Uses a **segment-vs-circle sweep test** (sweep radius 55 + 30 margin) between successive touch positions so fast swipes can't tunnel over debris, and enforces a **per-swipe cap** of 12 cleared debris (`MaxDebrisPerSwipe`). Emits `onSwipeCompleted` only for finished touches that cleared at least one piece of debris, so bare taps never count as swipes; a second simultaneous touch is ignored while a gesture is in flight. |
| `scripts/Debris.cs` | One debris item: weight class (Light/Medium/Heavy → fling factor 0.65/0.5/0.35, friction 3.4/2.3/1.5, fade-delay scale 1.0/1.35/1.7 — heavier pieces launch slower but glide farther and linger before fading), `Fling(velocity, rng)`, `Covers(worldPoint, radius)` — the pixel-accurate overlap test behind the covered rule: early circular rejection via `ExtentRadius` (bounding circle), then a scan of a per-texture cached `AlphaMask` (one byte per 4px cell, built once from the texture's alpha channel), so debris floating in a texture's transparent margins no longer hides the bug or coins — per-frame slide+spin+fade, `QueueFree` when faded. Unclamped — swept pieces may drift over the dock while fading; only *spawning* is excluded from it. |
| `scripts/Bug.cs` | Bug display: `Setup(type, scale, camouflage)` tints toward leaf color, `TapRadius` (type radius × scale) with `ContainsPoint(world)` plus the much tighter `OcclusionRadius` (type occlusion radius × scale) that the debris-clearing rule uses, `Celebrate(centerTarget)` plays the golden discovery moment — the bug rises to `ZIndex` 100 above all debris, a shining outline (via `assets/shaders/gold_outline.gdshader`) fades in while it swells to 1.45×, then it flies to the screen center — and emits `CelebrationFinished` when the win overlay may seat it. |
| `scripts/GustCoin.cs` | One collectible gold gust coin (reuses `assets/icons/coin.svg` + the gold outline shader, with the **wind icon** on its face like the dock's gust button): sits at `ZIndex` 0 below all debris, `TapRadius`/`ContainsPoint` plus a tight `OcclusionRadius` (0.3 × size, hugging the drawn disk) like the bug; `Collect(dockTarget)` marks it collected, plays the golden swell, then flies a **shrinking 1.5-turn spiral** toward the gust button while spinning — riding above the dock (Main reparents it onto the HUD layer) — emits `CollectionFlightFinished` the instant it lands, and melts into the button before freeing itself. |
| `scripts/BugTypes.cs` | Static catalog of 17 bug types (texture path, display name, relative size, tap radius, occlusion radius — 45% of the tap radius clamped to 18–36px) with `Random()`/`ById()`. |
| `scripts/RoundConfig.cs` | Difficulty curves saturating at level 200: `Coverage` (debris density as floor-area fraction), `BugScale`, `Camouflage`. Pure functions — easy to tune. |
| `scripts/LevelStats.cs` | Round statistics: tick, swipe count, gust power uses, formatted time, and `Comment(save, bug)` template picker (praise / cozy / best-yet / time / color lines). |
| `scripts/Hud.cs` | In-game UI: a **wood dock** along the bottom (fixed `DockHeight`, bark-textured `assets/textures/wood.svg`, swipe counter left, Gust centered, Restart rightmost — both **dark-gold coin buttons** via `assets/icons/coin.svg`; the Gust coin wears a small **bronze counter badge** top-right showing the gust power balance, and the button is disabled at ×0; swallows touches; the level label sits top-middle instead) — hidden only on the menu — plus the win overlay (title, **bug seat slot** the celebrated bug is reparented into, comment, stats, Next/Menu) and a Restart confirmation dialog. `PulseGustPower` greets each landed coin with a badge pop + gold flash, an expanding ring-and-spark burst and a button glitter. Static `MakeLabel`/`MakeButton` helpers shared with the menu. |
| `scripts/MainMenu.cs` | Title screen: Play (resume), New game (shown only after ≥1 clear), lifetime progress (levels, time, gusts) + favorite critter. |
| `scripts/SaveData.cs` | Persistence: JSON at `user://save.json` via `FileAccess` + atomic `DirAccess.Rename`; corrupt-safe `Load` (returns fresh save; missing keys default to 0 — `gustPower` to `StartingGustPower` — so older saves keep working); `RecordClear` updates aggregates (swipes, gusts, seconds) + 50-entry history; `GustPower` balance persisted on every gain/spend; `BestSwipes(type)` for comments. |

## Data flow

```
touch events → Main._UnhandledInput ─┬→ Sweeper (flings debris) → onSwipeCompleted → LevelStats.CountSwipe
                                     ├→ GustCoin.ContainsPoint + no debris overlapping → CollectCoin → +1 GustPower (persist) → GustCoin.Collect spiral
                                     ├→ Bug.ContainsPoint + no debris overlapping (BugIsCovered) → WinLevel
                                     └→ covered coin/bug, or elsewhere → Sweeper.Begin
HUD wind button → Main.OnWindPressed → −1 GustPower (persist) + LevelStats.CountGust
WinLevel → LevelStats.Stop → SaveData.RecordClear → Bug.Celebrate → CelebrationFinished → Hud.ShowWin(comment, stats)
```

`ToWorld()` maps screen → world through `GetCanvasTransform().AffineInverse()`
so UI-anchored touches match world-space debris under stretch/expand.

## Rendering notes

- Base viewport **1080×2340**, stretch mode `canvas_items`, aspect `expand` —
  taller/shorter phones simply show more/less ground; the ground sprite is
  rescaled on startup, every round **and on every viewport resize**
  (`Main.OnViewportResized`), which also stretches a live round's unswept
  debris + bug positions onto the new rect so no bare floor shows mid-level.
- Portrait orientation, touch emulation from mouse for desktop testing.
- Textures are SVGs rasterized by Godot at import; the source is regenerated
  by `tools/gen_art.mjs`.

## Testing hooks

- `LEAF_AUTOPLAY=1 godot --headless` → plays a level end-to-end, verifies the
  save round-trip, exits 0/1. Used by CI-style smoke checks.
- `godot --headless --quit-after N` → boot smoke test.
