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
├── DebrisBottom (Node2D)     70% of debris — under the bug
├── Bug (Node2D)              hidden until StartLevel
├── DebrisTop (Node2D)        30% of debris — over the bug
├── Hud (CanvasLayer)         level/swipe bar + win overlay
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
| `scripts/Main.cs` | Controller: builds tree, state machine, level setup/teardown, input routing, win flow, debris spawn layout (inside the playable rect above the HUD dock), wind gust (clears ~10% of remaining debris with streak effects), restart handling, petal sparkle. Also hosts the `LEAF_AUTOPLAY=1` headless self-test. |
| `scripts/Sweeper.cs` | Input-to-interaction: converts touch stream into flings. Uses a **segment-vs-circle sweep test** (sweep radius 55 + 30 margin) between successive touch positions so fast swipes can't tunnel over debris, and enforces a **per-swipe cap** of 12 cleared debris (`MaxDebrisPerSwipe`). Emits `onSwipeCompleted` per finished touch. |
| `scripts/Debris.cs` | One debris item: weight class (Light/Medium/Heavy → fling factor 0.65/0.5/0.35, friction 3.4/2.3/1.5, fade-delay scale 1.0/1.35/1.7 — heavier pieces launch slower but glide farther and linger before fading), `Fling(velocity, rng)`, per-frame slide+spin+fade clamped to `Bounds` with a soft bounce at the dock edge, `QueueFree` when faded. |
| `scripts/Bug.cs` | Bug display: `Setup(type, scale, camouflage)` tints toward leaf color, `ContainsPoint(world)` uses the type's tap radius × scale, `Celebrate(centerTarget)` plays the golden discovery moment — the bug rises to `ZIndex` 100 above all debris, a shining outline (via `assets/shaders/gold_outline.gdshader`) fades in while it swells to 1.45×, then it flies to the screen center — and emits `CelebrationFinished` when the win overlay may seat it. |
| `scripts/BugTypes.cs` | Static catalog of 8 bug types (texture path, display name, relative size, tap radius) with `Random()`/`ById()`. |
| `scripts/RoundConfig.cs` | Difficulty curves saturating at level 200: `Coverage` (debris density as floor-area fraction), `BugScale`, `Camouflage`. Pure functions — easy to tune. |
| `scripts/LevelStats.cs` | Round statistics: tick, swipe count, formatted time, and `Comment(save, bug)` template picker (praise / cozy / best-yet / time / color lines). |
| `scripts/Hud.cs` | In-game UI: a **bottom dock** (fixed `DockHeight`, level + swipe labels, Gust + Restart icon buttons; swallows touches and defines the playable area) — hidden only on the menu — plus the win overlay (title, **bug seat slot** the celebrated bug is reparented into, comment, stats, Next/Menu) and a Restart confirmation dialog. Static `MakeLabel`/`MakeButton` helpers shared with the menu. |
| `scripts/MainMenu.cs` | Title screen: Play (resume), New game (shown only after ≥1 clear), lifetime progress + favorite critter. |
| `scripts/SaveData.cs` | Persistence: JSON at `user://save.json` via `FileAccess` + atomic `DirAccess.Rename`; corrupt-safe `Load` (returns fresh save); `RecordClear` updates aggregates + 50-entry history; `BestSwipes(type)` for comments. |

## Data flow

```
touch events → Main._UnhandledInput ─┬→ Sweeper (flings debris) → onSwipeCompleted → LevelStats.CountSwipe
                                     └→ Bug.ContainsPoint → WinLevel
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
