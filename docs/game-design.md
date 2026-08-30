# LeafSweeper — Game Design

## One-line pitch

Tidy up a patch of forest floor by sweeping away leaves, petals and sticks
until you uncover the small creature hiding beneath — then tap it. Cozy,
endless, unhurried.

## Core loop

```
Title menu → Play → Round (sweep debris, find bug) → Tap bug
     ↑                                                    ↓
     └────── Menu button ← Win overlay (comment + stats) ──┘
```

1. A round scatters **~1365 debris items** (weighted mix, growing to ~2123
   by level 200) on a jittered
   grid, guaranteeing the entire floor is covered — the forest floor is never
   visible until swept — and hides **one random bug** from the catalog
   underneath the litter.
2. The player **drags a finger** to sweep. Debris inside the sweep radius is
   flung with velocity + spin, slides with friction, fades and is removed.
   A single sweep clears **at most 12 pieces of debris** — sweeps stay
   deliberate rather than screen-wiping. Only gestures that sweep at least
   one piece count toward the sweep counter — bare taps are free.
   A **double-tap** — two quick bare taps in one spot — fires a radial
   **gust burst**: the debris nearest the tap (within 130 px) flings
   outward, capped at the same 12 pieces per burst and counted as a
   sweep. It's the drag-free way to dig out a buried bug or coin.
   Weight matters: rocks/sticks/moss resist; leaves and petals fly easily —
   but heavier pieces glide farther and fade later, so their long slide
   reads as weight rather than the debris dying where it was swept.
3. Two helpers live in a **wooden dock** along the bottom of the screen while
   playing: **Gust** (wind icon on a dark-gold coin) blows a gust across the
   floor, sweeping away about **25% of the remaining debris** in one shared
   direction — each gust **spends one gust power** from the player's balance,
   and a small **counter circle on the coin's top-right** shows how many are
   left (the button greys out at ×0); and **Restart** (circular arrow on a
   dark-gold coin) opens a
   confirmation dialog before re-scattering the same level with a fresh
   sweep count. The dock is the game's only chrome — the **Level label sits
   at the top-middle** of the screen, and **nothing ever spawns under the
   dock**; swept debris may drift over it while fading away.
4. Finding the bug and **tapping it** wins the round — but the bug hides
   **below every debris piece**, so it only becomes selectable once
   **no unswept debris overlaps its visible body** — a tight occlusion
   radius around the drawn critter, far smaller than the tap area, so
   players never clear empty space around it. Tapping a covered bug just
   starts sweeping from that spot. On a winning tap the bug rises **above
   all debris**, bathes in a **golden shining outline** and **grows**, then
   **flies to the center of the screen** and seats itself on the win card —
   below the "Bug found!" title, above the stats — as the overlay fades in
   with a friendly comment and the round's numbers (time, sweeps, gusts).
5. **Gold gust coins** — marked with the gust icon — hide below the debris
   too, **three per round**, and follow the same uncovering rule (debris is
   cleared from the coin's visible disk, not its tap area). Tapping an
   uncovered coin doesn't end the round: it shines golden and **grows, then
   spirals into the dock's gust button**, flying **above everything — the
   dock included**. On arrival a **golden burst** fires and the counter
   **pulses as it banks +1 gust power**. The balance **persists across
   rounds** (new games start with 3).
6. **Next** starts the following level. **Menu** returns to the title screen.

## Design values

- **No pressure.** No countdown timers, no fail state, no move limits. The
  bug waits patiently and never animates away.
- **Cozy tone.** All UI copy is warm and unhurried; the win comment is the
  game's "voice" (see Comments below).
- **A living floor.** Every few seconds a stray draft rustles a little
  patch of the litter — the meadow breathes even when you're idle. It is
  decoration only and never moves the gameplay.
- **Casual-tuned difficulty.** By level 200 the game is only moderately
  harder than level 1. Nothing ever spikes.

## Difficulty curve

Implemented in `scripts/RoundConfig.cs`, all curves saturate at level 200:

| Parameter        | Level 1 | Level 200 | Curve                                  |
|------------------|---------|-----------|----------------------------------------|
| Debris coverage  | ~1365   | ~2123     | smoothstep growth (floor-area × density) |
| Bug scale        | 1.00    | 0.75      | linear ease                             |
| Camouflage blend | 0       | 0.25 max  | 0 until ~level 60, then gentle ramp     |

Camouflage tints the bug slightly toward the leaf palette — a whisper of
extra challenge in late levels, never a color hunt.

## Bug catalog

Each round picks a random bug type (`scripts/BugTypes.cs`). Types differ in
texture, relative size (0.6–1.15×) and tap radius so players learn
**silhouettes**:

Ladybug · Butterfly · Centipede · Moth · Grasshopper · Dragonfly · Beetle · Snail · Firefly · Bumblebee · Caterpillar · Mantis · Stick Insect · Weevil · Pill Bug · Ant · Fly

## Debris taxonomy

| Kind            | Variants                          | Weight  |
|-----------------|-----------------------------------|---------|
| Leaves          | red maple, red simple, yellow oak, green | Light |
| Flower petals   | pink, white, purple               | Light   |
| Moss clusters   | —                                 | Medium  |
| Sticks          | —                                 | Heavy   |
| Rocks           | round, grey                       | Heavy   |

Roughly 60% leaves + petals, rest mixed. Debris is split into two layers —
both **render above the bug** (explicit `ZIndex` 1/2 vs the bug's 0), so a
corner of it always peeks through no matter where it spawns.

## Scoring & statistics

No score — only **friendly numbers**:

- Sweeps used this round.
- Gust powers blown this round (shown on the win card only when used).
- Gust power balance (coins found − gusts spent) — persists across rounds.
- Time since round start (m:ss).
- Lifetime aggregates (levels cleared, sweeps, gusts, time) and per-bug find
  counts, kept in the save file.

## Between-round comments

`scripts/LevelStats.cs` picks a comment from template tiers by comparing the
round against history:

- **Praise** — few sweeps ("Just 18 sweeps to find the bug!").
- **Cozy reassurance** — many sweeps ("The leaves were feeling stubborn
  today.").
- **Best-yet nod** — new personal best for the bug type.
- **Time remark** — mention of the duration, phrased warmly.
- **Cozy color** — an unhurried line indexed by (level + sweeps), so rounds
  rarely repeat the same flavor.

## Storage

Everything persists locally in `user://save.json` (Godot `user://` is
app-private storage on Android — no Android permissions needed):

- `currentLevel` — next level to play (Play resumes here).
- `levelsCleared`, `totalSweeps`, `totalGusts`, `totalSeconds` — lifetime
  aggregates.
- `gustPower` — current gust power balance (starts at 3; coins add +1,
  each gust spent takes −1; missing on old saves → 3).
- `bugFindCounts` — finds per bug type (drives "favorite critter").
- `history` — last 50 cleared levels `{level, sweeps, gusts, seconds, bugType,
  clearedAt}`.

Saves are written **atomically** (write temp file, rename over the real one)
after every clear; a missing or corrupt file silently starts a fresh save.

## Out of scope (current MVP)

- Sound effects and music.
- Cloud sync, achievements, monetization.
- Bug movement/animation (bugs are serene statues by design).
