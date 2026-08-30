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
├── StormOverlay (CanvasLayer 1)  storm veil/rain/mist over the world
├── StormWarn (CanvasLayer 4)    "Storm Round" electrical warning sign
├── Hud (CanvasLayer 3)         wood dock + top labels + win overlay
├── BugBook (CanvasLayer 90)  full-screen collection book; above the HUD
│                             so the dim blocks the dock while it's open
└── Menu (CanvasLayer 2)        title screen
```

CanvasLayer order is an explicit ladder (storm 1 → menu 2 → hud 3 → warn 4
→ book 90): same-layer canvases draw in a non-deterministic order, so every
layer gets its own index.

## State machine

`Main` holds `GameState { Menu, Playing, Won }`:

- **Menu** — `MainMenu` visible, level cleared.
- **Playing** — input routed to the sweeper and the bug tap check.
- **Won** — celebration + `Hud.ShowWin(comment, statsLine, bug, grandiose)`;
  a prismatic find passes `grandiose: true` for the radiant gold card.
  Next → `StartLevel(save.CurrentLevel)` (RecordClear already advanced it),
  Menu → title.

## Modules

| Script | Responsibility |
|--------|----------------|
| `scripts/Main.cs` | Controller: builds tree, state machine, level setup/teardown, input routing (a tap only wins when the bug is uncovered — `BugIsCovered` runs the pixel-accurate `Debris.Covers` test against the bug's occlusion area, far tighter than its tap area; a covered tap starts sweeping instead; uncovered gust coins are collected the same way via `DebrisOverlaps`/`SelectableCoinAt` against the coin's occlusion radius), win flow, debris spawn layout (inside the playable rect above the HUD dock) with the round-start **settle-in** (pieces drop in tumbling, staggered along the diagonal — touches and gusts stay locked until the floor is dressed, then `OnSettleFinished` places the bug and the 3 gust coins at new random spots underneath and starts the round clock; the dock restart button funnels through the same `StartLevel` settle, so restarting reshuffles the litter and the hiding spots too), gust coin collection (`CollectCoin` lifts the coin onto the HUD layer so it flies above the dock; the +1 `gustPower` is banked when the coin arrives, then `Hud.PulseGustPower` fires), wind gust (clears ~25% of remaining debris with streak effects; spends one gust power and counts the use per round), win-flow **end-of-round wind** (`StartEndRoundWind` lifts every leftover piece into a clockwise gyre around the floor's center via `Debris.StartEndRoundWind` — the litter keeps circling while the win card is up; `WindCenter` is the playable floor's middle during a round and the whole viewport's middle on the menu, refreshed on viewport resize), **menu gyre** (`SpawnMenuDebris` scatters a decorative litter over the whole screen and lifts it into the same gyre idled down to `MenuWindSpeedScale` so the home card floats over a slowly spinning floor), restart handling, petal sparkle, and the **prismatic roll**: `OnSettleFinished` rolls a 5% chance (`PrismaticChance`; `LEAF_PRISMATIC=1` forces it) of a prismatic bug, places it uncamouflaged, and a find spawns the `SunFlare` at the winning tap and shows the grandiose win card (`WinLevel` → `RecordClear(..., prismatic)` → `OnBugCelebrationFinished` → `ShowWin(..., grandiose)`). Also hosts the `LEAF_AUTOPLAY=1` headless self-test, which verifies the coverage rule against alpha ground truth sampled straight from the blocker texture, asserts the menu litter is riding the gyre, exercises the restart handler (settle gate must re-engage) and asserts the win wind picks every piece into a clockwise-moving gyre. While a round is live and settled, `_Process` counts down 2–4s and `TriggerAmbientRustle` lets a stray draft shiver a random at-rest piece plus the closest 4–7 of its neighbors along a shared direction (see `Debris.Rustle`) — cosmetic only. On storm rounds the same gate runs the storm pacing: `TickStormDrops` re-litters each recorded cleared spot on its own 4–6s timer (spots via `RecordClearedSpot`, pool capped at 400) and `TickClusterDrops` dumps a cluster of 6–12 brand-new pieces onto random floor spots every 4–6s (shared `SpawnStormDebris` spawn path), stopping for the round once the live debris count reaches 3× the round's starting litter (`StormFloodCapMultiplier`) while spot restoration keeps going. |
| `scripts/Sweeper.cs` | Input-to-interaction: converts touch stream into flings. Uses a **segment-vs-circle sweep test** (sweep radius 55 + 30 margin) between successive touch positions so fast sweeps can't tunnel over debris, and enforces a **per-sweep cap** of 12 cleared debris (`MaxDebrisPerSweep`). Emits `onSweepCompleted` only for finished touches that cleared at least one piece of debris, so bare taps never count as sweeps; a second simultaneous touch is ignored while a gesture is in flight. Each flung piece is also reported through the `onDebrisSwept` constructor callback (before it flies) so storm rounds can record the vacated spot. The double-tap **burst** (`Burst`) is the drag-free cousin: pieces nearest the tap within `BurstRadius` (130) fling radially, sharing the cap, the callback and the `onSweepCompleted` accounting. |
| `scripts/Debris.cs` | One debris item: weight class (Light/Medium/Heavy → fling factor 0.65/0.5/0.35, friction 3.4/2.3/1.5, fade-delay scale 1.0/1.35/1.7 — heavier pieces launch slower but glide farther and linger before fading), `Fling(velocity, rng)`, `Covers(worldPoint, radius)` — the pixel-accurate overlap test behind the covered rule: early circular rejection via `ExtentRadius` (bounding circle), then a scan of a per-texture cached `AlphaMask` (one byte per 4px cell, built once from the texture's alpha channel), so debris floating in a texture's transparent margins no longer hides the bug or coins — per-frame slide+spin+fade, `QueueFree` when faded. Round-start `SettleIn(rng, delay)` drops the piece onto its assigned spot from above with a tumble and quart-out landing (fade-in over the first third; `IsSettling` gates play until every piece lands). After a win, `StartEndRoundWind(center, rng)` puts survivors on a clockwise orbiting gyre — per-piece speed jitter shears the ring, the lane radius breathes, the whole gyre bobs, pieces tumble and never fade while `IsRidingWind`. `Rustle(dir, falloff, rng)` lets an ambient draft shiver the piece in place for ~0.5s — the wobble lives on the child sprite only (weight-scaled amplitude: leaves flick, rocks barely budge), so the node transform behind `Covers`/sweep/gyre math never moves. Unclamped — swept pieces may drift over the dock while fading; only *spawning* is excluded from it. |
| `scripts/Bug.cs` | Bug display: `Setup(variant, scale, camouflage, prismatic)` applies the variant texture and tints toward leaf color, `TapRadius` (type radius × scale) with `ContainsPoint(world)` plus the much tighter `OcclusionRadius` (type occlusion radius × scale) that the debris-clearing rule uses, `Celebrate(centerTarget)` plays the golden discovery moment — the bug rises to `ZIndex` 100 above all debris, a shining outline (via `assets/shaders/gold_outline.gdshader`) fades in while it swells to 1.45×, then it flies to the screen center — and emits `CelebrationFinished` when the win overlay may seat it. When `prismatic` is set the sprite's material swaps to `assets/shaders/prismatic.gdshader` (hue-crawling rainbow + sparkle glints, gold outline for the celebrate tween) and camouflage is bypassed; the bug stays at ZIndex 0 below every debris layer so the effect can never leak through the leaves. |
| `scripts/GustCoin.cs` | One collectible gold gust coin (reuses `assets/icons/coin.svg` + the gold outline shader, with the **wind icon** on its face like the dock's gust button): sits at `ZIndex` 0 below all debris, `TapRadius`/`ContainsPoint` plus a tight `OcclusionRadius` (0.3 × size, hugging the drawn disk) like the bug; `Collect(dockTarget)` marks it collected, plays the golden swell, then **winds up a rising counter-clockwise loop** above its pick-up spot (`LoopTurns`/`LoopSeconds`, radius from the coin→button distance clamped to `LoopRadiusMin..Max`, tightened by `LoopShrink`) before **dashing down onto the gust button** — riding above the dock (Main reparents it onto the HUD layer); every loop point is clamped into the viewport inset by `PathScreenMargin`, so the loop hugs the screen edges instead of arcing out of view — emits `CollectionFlightFinished` the instant it lands, and melts into the button before freeing itself. |
| `scripts/BugTypes.cs` | Static catalog of 39 species (texture path, display name, relative size, tap radius, occlusion radius — 45% of the tap radius clamped to 18–36px) each with **4 color variants** (`BugVariant`: id, display name like "Yellow Ladybug", texture path, back-reference to its species) — 156 book entries. The `Sp` helper builds a species: the base texture becomes the first variant under the bare species id (older saves keyed by species id keep counting), then `id_suffix` variants follow. `RandomVariant()` drives spawns, `VariantById()` resolves save keys. |
| `scripts/RoundConfig.cs` | Difficulty curves saturating at level 200: `Coverage` (debris density as floor-area fraction), `BugScale`, `Camouflage`. Storm schedule is a curve too: `IsStormLevel` (every 10th level from level 10). Pure functions — easy to tune. |
| `scripts/LevelStats.cs` | Round statistics: tick, sweep count, gust power uses, formatted time, and `Comment(save, bug)` template picker (praise / cozy / best-yet / time / color lines). |
| `scripts/Hud.cs` | In-game UI: a **wood dock** along the bottom (fixed `DockHeight`, bark-textured `assets/textures/wood.svg`, three equal expand-fill columns so the middle stays centered: **book coin** left, Gust coin centered, Restart coin right — dark-gold coin buttons via `assets/icons/coin.svg`; the Gust coin wears a small **bronze counter badge** top-right showing the gust power balance, and the button is disabled at ×0) — hidden only on the menu — plus the **sweeps counter panel top-right** and the **level label top-left**, the win overlay (title, **bug seat slot** the celebrated bug is reparented into, comment, stats, Next/Menu — `ShowWin(..., grandiose)` swaps the panel to a radiant gold style and adds the `PrismaticGlow` rays/sparkles + a looping prismatic title color cycle behind the title) and a Restart confirmation dialog. `PulseGustPower` greets each landed coin with a badge pop + gold flash, an expanding ring-and-spark burst and a button glitter. Static `MakeLabel`/`MakeButton` helpers shared with the menu. |
| `scripts/MainMenu.cs` | Title screen: Play (resume), New game (shown only after ≥1 clear), lifetime progress (levels, time, gusts) + favorite critter. |
| `scripts/SaveData.cs` | Persistence: JSON at `user://save.json` via `FileAccess` + atomic `DirAccess.Rename`; corrupt-safe `Load` (returns fresh save; missing keys default to 0 — `gustPower` to `StartingGustPower` — so older saves keep working); `RecordClear` updates aggregates (sweeps, gusts, seconds), increments the **variant-keyed** find count and `prismaticFinds` when the bug was prismatic, + 50-entry history; `GustPower` balance persisted on every gain/spend; `BestSweeps()` for comments. |
| `scripts/BugBook.cs` | The Bug Collection Book: full-screen `CanvasLayer` (Layer 90, above the HUD) showing **one oversized page at a time** sized to the viewport (3×5 collection grid in portrait, 5×4 in landscape). Opens on the leather cover and waits; paging via **dog-eared corners** (custom `Polygon2D` fold + drop shadow + arrow: top-right forward, bottom-right back) with a spine-hinged page-flip tween; tapping the full-rect dim (which swallows every touch so the dock stays inert) closes with a sink-and-fade. Unfound entries render their sprite through `assets/shaders/bug_mist.gdshader` (black silhouette + drifting off-black mist) labeled "??? (x0)". Content is rebuilt per page from `BugBookModel` so it always matches the save. |
| `scripts/BugBookModel.cs` | Pure (UI-free) book model: the 156 entries in stable catalog order with found state, counts and labels ("Name (xN)" / "??? (x0)"), plus the stats-page numbers (total bugs, variants/species discovered, best round, totals, prismatic finds, favorite). Autoplay asserts against the same model the UI renders. |
| `scripts/SunFlare.cs` | Yellow-sun lens flare for a prismatic discovery: additive core, streaks, rotating rays and expanding rings at the winning tap, `ZIndex` 101 (above the celebrate's 100), self-freeing after ~1.8s. |
| `scripts/PrismaticGlow.cs` | Grandiose win-card dressing: a Control behind the card title drawing 14 rotating rays plus 8 looping diamond sparkles in prismatic hues; freed with the win overlay. |
| `scripts/StormOverlay.cs` | Storm weather: a `CanvasLayer` (Layer 1) holding a full-rect, input-transparent `ColorRect` running `assets/shaders/storm.gdshader`. `FadeIn()`/`FadeOut()` tween the shader's intensity uniform (0↔1); the layer hides itself at zero intensity so non-storm rounds cost nothing. |
| `assets/shaders/storm.gdshader` | Full-screen weather shader: cold dark veil (~0.42 alpha) + vignette, three depth layers of *individual* rain drops (per-drop length/brightness/x-offset/speed — no uniform curtain, no global sway) falling downward and leaning downwind, rolling cloud shadows, drifting mist and fog wisps that flare in and dissipate, and a ~7s lightning double-flash; alpha-composited over the scene (no screen texture — cheaper on mobile, works headless), driven by one 0–1 `intensity` uniform. |
| `scripts/StormWarn.cs` | The "Storm Round" warning sign: a `CanvasLayer` (Layer 4) shown during the end-of-round wind of the round before a storm round (`Main.NextRoundIsStorm`), hidden when the next level starts or the menu returns. The label renders into a `SubViewport` whose texture feeds `assets/shaders/warn_sparks.gdshader` on a full-rect ColorRect — the shader knows where every glyph sits and repaints the sign as neon rim + lightning bolts + sparks over a soft borderless dark card; the viewport updates only while the sign is visible; fades via root `Modulate`. |
| `assets/shaders/prismatic.gdshader` | Bug-sprite overlay shader: hue-crawling rainbow wash (time + diagonal UV drift), twinkling sparkle glints on a jittered grid, and the gold outline (shared logic with `gold_outline.gdshader`) so the celebrate intensity tween still works. |

## Data flow

```
touch events → Main._UnhandledInput ─┬→ Sweeper (flings debris) → onSweepCompleted → LevelStats.CountSweep
                                     ├→ GustCoin.ContainsPoint + no debris overlapping → CollectCoin → +1 GustPower (persist) → GustCoin.Collect spiral
                                     ├→ Bug.ContainsPoint + no debris overlapping (BugIsCovered) → WinLevel
                                     ├→ two dead taps inside the double-tap window → Sweeper.Burst (radial fling, same 12-piece cap → onSweepCompleted)
                                     └→ covered coin/bug, or elsewhere → Sweeper.Begin
HUD wind button → Main.OnWindPressed → −1 GustPower (persist) + LevelStats.CountGust
WinLevel → LevelStats.Stop → SaveData.RecordClear → Bug.Celebrate → CelebrationFinished → Hud.ShowWin(comment, stats)
         └→ StartEndRoundWind → every unswept piece rides a clockwise gyre (Debris.StartEndRoundWind) until the round is torn down
         └→ next round is a storm round → StormWarn.ShowWarning (electrical "Storm Round" sign; hidden when the next level starts)
StartLevel → SpawnDebris (SettleIn tumble-drop, diagonal stagger) → [touches locked] → OnSettleFinished → bug + 3 gust coins hidden at random spots under the settled debris, round clock starts
Storm round (live + settled) → each recorded cleared spot fires on its own 4–6s timer (Main.TickStormDrops) → one fresh piece tumbles onto that exact ground (SettleIn tumble, spot consumed, pool capped at 400) → fresh unswept debris re-covers the bug/coins via the normal overlap rules
Storm flood (live + settled) → every 4–6s (Main.TickClusterDrops) a cluster of 6–12 brand-new pieces tumbles onto random floor spots (same SpawnStormDebris path) → the litter grows past the round's starting count → once the live count reaches 3× the start (StormFloodCapMultiplier, final cluster truncated to fit) the flood abates for the round; spot restoration continues regardless
Dock restart → OnRestartConfirmed → StartLevel (same settle reshuffle: fresh curtain, new hiding spots)
Main._Process (round live + settled) → every 2–4s → TriggerAmbientRustle → epicenter + the closest 4–7 at-rest pieces shiver along a shared draft (Debris.Rustle, sprite-only wiggle)
App boot / Menu → SpawnMenuDebris (scatter over the full screen, no drop-in) → StartEndRoundWind(MenuWindSpeedScale) → litter slowly circles behind the menu card
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

- `LEAF_AUTOPLAY=1 godot --headless` → plays a level end-to-end and prints
  one `AUTOPLAY ... True` line per assertion (menu, catalog 39/156, restart,
  rustle, uncover, prismatic spawn, burst, coins, storm drops landing on
  recorded cleared spots with the pool shrinking, flood clusters growing the
  litter and stopping dead at the 3× cap, wind, save, reload, book
  model, prismatic win chain: flare + grand card + `prismaticFinds == 1`
  after a save round-trip). The autoplay round forces the prismatic roll so
  the rare path is exercised every run, and forces the storm weather too.

- `LEAF_AUTOPLAY=1 godot --headless` → verifies the save round-trip and exits
  0/1 (grep for `AUTOPLAY` first — the exit code alone is not trustworthy).
  Used by CI-style smoke checks.
- `LEAF_PRISMATIC=1 godot` → forces the prismatic roll so the rare spawn can
  be tested by hand (autoplay forces it internally every run).
- `LEAF_STORM=1 godot` → forces the storm weather on any level (normally
  every 10th level; autoplay forces it internally every run).
- `godot --headless --quit-after N` → boot smoke test.
