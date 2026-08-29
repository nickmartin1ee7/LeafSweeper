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
   A single swipe clears **at most 12 pieces of debris** — sweeps stay
   deliberate rather than screen-wiping.
   Weight matters: rocks/sticks/moss resist; leaves and petals fly easily —
   but heavier pieces glide farther and fade later, so their long slide
   reads as weight rather than the debris dying where it was swept.
3. Two helpers live in a **bottom dock** while playing, below the floor:
   **Gust** (wind icon) blows a gust across the floor, sweeping away about
   **10% of the remaining debris** in one shared direction; and
   **Restart** (circular arrow) opens a confirmation dialog before
   re-scattering the same level with a fresh swipe count. The dock is the
   game's only chrome — debris, the bug and all sweeping stay strictly
   above it; flung pieces bounce off its edge instead of sliding under.
4. Finding the bug and **tapping it** wins the round: the bug rises **above
   all debris**, bathes in a **golden shining outline** and **grows**, then
   **flies to the center of the screen** and seats itself on the win card —
   below the "Bug found!" title, above the stats — as the overlay fades in
   with a friendly comment and the round's numbers (time, swipes).
5. **Next** starts the following level. **Menu** returns to the title screen.

## Design values

- **No pressure.** No countdown timers, no fail state, no move limits. The
  bug never flees or animates away — it waits.
- **Cozy tone.** All UI copy is warm and unhurried; the win comment is the
  game's "voice" (see Comments below).
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
texture, relative size (0.85–1.15×) and tap radius so players learn
**silhouettes**:

Ladybug · Butterfly · Centipede · Moth · Grasshopper · Dragonfly · Beetle · Snail

## Debris taxonomy

| Kind            | Variants                          | Weight  |
|-----------------|-----------------------------------|---------|
| Leaves          | red maple, red simple, yellow oak, green | Light |
| Flower petals   | pink, white, purple               | Light   |
| Moss clusters   | —                                 | Medium  |
| Sticks          | —                                 | Heavy   |
| Rocks           | round, grey                       | Heavy   |

Roughly 60% leaves + petals, rest mixed; ~30% of debris spawns **above** the
bug layer so it always peeks through.

## Scoring & statistics

No score — only **friendly numbers**:

- Swipes used this round.
- Time since round start (m:ss).
- Lifetime aggregates and per-bug find counts, kept in the save file.

## Between-round comments

`scripts/LevelStats.cs` picks a comment from template tiers by comparing the
round against history:

- **Praise** — few swipes ("Just 18 swipes to find the bug!").
- **Cozy reassurance** — many swipes ("The leaves were feeling stubborn
  today.").
- **Best-yet nod** — new personal best for the bug type.
- **Time remark** — mention of the duration, phrased warmly.
- **Cozy color** — an unhurried line indexed by (level + swipes), so rounds
  rarely repeat the same flavor.

## Storage

Everything persists locally in `user://save.json` (Godot `user://` is
app-private storage on Android — no Android permissions needed):

- `currentLevel` — next level to play (Play resumes here).
- `levelsCleared`, `totalSwipes`, `totalSeconds` — lifetime aggregates.
- `bugFindCounts` — finds per bug type (drives "favorite critter").
- `history` — last 50 cleared levels `{level, swipes, seconds, bugType,
  clearedAt}`.

Saves are written **atomically** (write temp file, rename over the real one)
after every clear; a missing or corrupt file silently starts a fresh save.

## Out of scope (current MVP)

- Sound effects and music.
- Cloud sync, achievements, monetization.
- Bug movement/animation (bugs are serene statues by design).
