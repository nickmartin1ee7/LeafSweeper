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
   too — **one per round on normal rounds, three on storm rounds** (the
   storm flood keeps re-burying swept ground, so storms pay in gusts) —
   and follow the same uncovering rule (debris is
   cleared from the coin's visible disk, not its tap area). Tapping an
   uncovered coin doesn't end the round: it shines golden and **grows, then
   winds up a rising counter-clockwise loop before snapping down onto the
   dock's gust button**, flying **above everything — the dock included**.
   The flight path is clamped to the visible screen, so the loop hugs the
   screen edges instead of leaving the view. On arrival a **golden burst**
   fires and the counter
   **pulses as it banks +1 gust power**. The balance **persists across
   rounds** (new games start with 3).
6. **Next** starts the following level. **Menu** returns to the title screen.

## Design values

- **No pressure.** No countdown timers, no fail state, no move limits. The
  bug waits patiently and never animates away.
- **Cozy tone.** All UI copy is warm and unhurried; the win comment is the
  game's "voice" (see Comments below).
- **A living floor.** Every couple of seconds a stray draft rustles a little
  cluster of the litter — the meadow breathes even when you're idle. It is
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

Each round picks a random species, then a random color variant of it
(`scripts/BugTypes.cs`). Species differ in texture, relative size
(0.55–1.15×) and tap radius so players learn **silhouettes**; variants are
what actually spawn and what the collection counts, under names like
"Yellow Ladybug" or "Mahogany Stag Beetle":

- **39 species** — the original seventeen (Ladybug · Butterfly · Centipede ·
  Moth · Grasshopper · Dragonfly · Beetle · Snail · Firefly · Bumblebee ·
  Caterpillar · Mantis · Stick Insect · Weevil · Pill Bug · Ant · Fly) plus
  twenty-two more (Aphid · Barklouse · Cicada · Click Beetle · Damselfly ·
  Earwig · Earthworm · Froghopper · Glowworm · Jewel Beetle · Lacewing ·
  Lanternfly · Leafhopper · Mayfly · Rhinoceros Beetle · Shield Bug ·
  Silverfish · Slug · Stag Beetle · Tiger Beetle · Tortoise Beetle · Water
  Strider).
- **4 variants each** — one natural base look plus three natural color
  palettes: 156 book entries in total.
- Win-card comments stay **per species** (`scripts/BugFlavor.cs`); the
  card's title uses the variant's display name.

### Prismatic bugs

Every fresh round rolls a **5% chance** (`LEAF_PRISMATIC=1` forces it for
testing) that the bug is *prismatic*: an overlay state on whatever variant
spawned, not a catalog entry. The bug's sprite wears a hue-crawling rainbow
shader with sparkle glints — applied to the bug's sprite only, and the bug
sits below every debris layer, so the effect can never show through the
leaves. Camouflage is bypassed so the rare find always reads clearly.
Finding one erupts a **yellow-sun lens flare** at the winning tap and
swaps the win card for a **grandiose** variant — radiant gold panel,
rotating rays, looping sparkles, prismatic title — and the find is counted
in the save (`prismaticFinds`) and on the book's stats page.

### Storm levels

Every 10th level from level 10 (`RoundConfig.IsStormLevel`, every
`StormEveryLevels`-th) is a **storm round**: the weather turns and the floor
itself fights back — with your memory, not your reflexes.

- **Weather.** A full-screen shader (`assets/shaders/storm.gdshader`, driven
  by `scripts/StormOverlay.cs`) fades in a cold, darker veil with vignette,
  three depth layers of rain made of *individual* drops — each with its own
  length, brightness, x offset and fall speed, all falling down and leaning
  downwind, so the downpour never reads as a
  uniform dashed curtain — plus rolling cloud shadows, ground mist, fog
  wisps that flare in, drift downwind and dissipate, and a lightning
  double-flash every ~7s. It fades out on win/menu and never appears on the
  home screen.
- **Storm Round warning.** The round *before* a storm round ends with a
  warning: an electrical "Storm Round" sign (`scripts/StormWarn.cs` +
  stray sparks, set into a roiling storm cloud) crackles on above the win
  card during the end-of-round wind
  and lingers two seconds into the storm round before dissolving out over
  four more seconds.
- **Falling debris.** While the round is settled, **each swept patch
  re-litters itself on its own timer**: 4–6 seconds (`StormSpotDelayMin` /
  `StormSpotDelayMax`) after a spot is vacated, one fresh debris piece
  tumbles down onto exactly that ground. Spots vacated by sweeps *and* by
  gusts alike are recorded — a piece swept mid-tumble counts as clearing the
  ground it was falling toward, never its transient mid-air position. Each
  spot is consumed when its drop lands, and the newly dropped pieces count
  as ordinary unswept debris — so they re-hide the bug
  and gust coins, exactly like the round-start litter.
- **The flood.** On top of the restoration, the storm *escalates*: every
  4–6 seconds (the same cadence) a gust dumps a whole **cluster of 6–12
  brand-new pieces** (`StormClusterMin` / `StormClusterMax`) onto random
  floor spots — litter that was never swept, so even untouched ground
  thickens as the round drags on. Each cluster tumbles in like any storm
  drop and follows the normal overlap rules, re-covering the bug and coins.
  The flood relents for the round once the live debris count reaches
  **3× the round's starting litter** (`StormFloodCapMultiplier` — the final
  cluster is truncated so the cap is exact); after that only the per-spot
  restoration continues, so swept patches never stay clean but the floor
  never drowns past 3×.
- **Anti-hoarding.** Because gust-cleared ground is re-littered the same way
  as swept ground, banking gust coins can't stockpile permanent clean
  space — a storm round rewards a mental map of where you've been, and
  punishes "save everything for later" play. The flood doubles down: even
  ground you never touched thickens, so "clean enough" is a moving target
  until the cap.
- **No pressure, still.** There is no fail state and no timer pressure; the
  storm only makes the floor harder to *hold in your head*. Restarting
  reshuffles everything, including the storm drops' future landing spots.

## The Bug Book

The gold book coin at the dock's bottom-left opens the **Bug Collection
Book**: a full-screen, single-page book (everything sized for a phone —
no squinting at a two-page spread).

- **Opening state**: the leather cover rises with the dim and holds —
  the player turns the page themselves to reach the stats.
- **Paging**: dog-eared page corners — top-right (folded paper, drop
  shadow, ▶) turns forward; bottom-right (same convention, ◀) turns back —
  or swipe the page itself: flick left for forward, right for back. Both
  feed the same turn. Going forward the page folds square into the spine
  on its left edge (the next page already sits beneath it) and unfolds on
  the far side through the 90° beat; going back the incoming sheet lays
  down over the page from the spine outward — one even motion either way,
  and the leather cover flips whole.
- **Collection pages**: 3×5 grids (5×4 in landscape) of every variant.
  Found bugs show their art and a count ("Firefly (x3)"); unfound bugs are
  entirely black silhouettes wrapped in subtly drifting off-black mist,
  named "??? (x0)".
- **Closing**: tap anywhere off the page border — the book sinks away, and
  the dim swallows every tap while open so nothing underneath (dock
  buttons) reacts.
- **Stats page**: bugs found, variants & species discovered, best round,
  total sweeps, gusts blown, time in the leaves, prismatic finds, favorite
  critter.

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
- `bugFindCounts` — finds per bug **variant** (drives "favorite critter"
  and the book's counts; the 17 original base variants keep their bare
  species ids so older saves migrate seamlessly).
- `prismaticFinds` — lifetime count of prismatic bugs found.
- `history` — last 50 cleared levels `{level, sweeps, gusts, seconds, bugType,
  clearedAt}` (bugType is the variant id).

Saves are written **atomically** (write temp file, rename over the real one)
after every clear; a missing or corrupt file silently starts a fresh save.

## Out of scope (current MVP)

- Sound effects and music.
- Cloud sync, achievements, monetization.
- Bug movement/animation (bugs are serene statues by design).
