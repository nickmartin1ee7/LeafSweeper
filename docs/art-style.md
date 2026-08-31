# LeafSweeper — Art Style

The full visual direction comes from the studio's reference sheet:
[`art-reference.jpg`](art-reference.jpg). All in-game textures are
**procedurally generated SVGs** — stylized, chunky, friendly shapes with a
warm autumn palette — reproducible from a single script.

## Reference sheet summary

- **Bugs** are cartoon-cute, round, with big simple features: ladybug,
  butterfly, centipede, moth, grasshopper, dragonfly, beetle, snail,
  firefly, bumblebee, caterpillar, mantis, stick insect, weevil,
  pill bug, ant, fly.
- **Debris** falls into a fixed taxonomy: leaves (red / yellow / green),
  moss clusters, sticks, rocks, flower petals.
- **Ground** is a soft, low-contrast forest floor so debris and bugs pop.

## Palette

| Use | Colors |
|-----|--------|
| Autumn leaves | `#c0392b` red, `#e67e22` orange-red, `#f1c40f` yellow, `#7d9b4e` green |
| Petals | soft pink `#f2b9c4`, white `#f7f3ea`, lilac `#c3a6d8` |
| Ground | deep forest brown `#3d3223` → lighter patch, subtle speckles |
| UI | warm cream text `#f5e8cd`, leaf-green buttons `#6f9a44`, muted wood `#a08a68` |

## Style rules

1. **Shapes over detail** — every item reads at a glance on a 1080×2340
   phone; no thin lines or fine noise.
2. **Outline everything** — dark soft outlines keep items separate from the
   busy ground.
3. **Bugs sit still** — bug art is posed calmly; they never animate away.
4. **Camouflage is a tint, not a texture swap** — late levels blend the bug
   toward leaf colors by at most 25%.

## Weather & shaders

Atmosphere beyond textures lives in `assets/shaders/`. Storm levels fade in
`storm.gdshader` on a full-screen layer: a heavy cold dark veil (~42% alpha)
with a deep vignette, three depth layers of rain built from *individual*
drops — each with its own length, brightness, x offset and fall speed, and
per-column speeds, so the downpour reads as weather, never as a uniform
dashed curtain (and there is deliberately no global sway: swaying a slanted
rain pattern reads as drops sliding along their streaks) — all falling
downward and leaning downwind (screen right, one consistent wind for rain,
clouds, mist and wisps) — plus rolling cloud shadows, drifting mist, fog
wisps that flare in and dissipate on their own cycles,
and a ~7s lightning double-flash. A `snow` uniform flips the same shader
into the winter blizzard: the rain dries up, the lightning stops, three
depths of round snowflakes replace the drops (per-flake sway — snow
wanders where rain leans; flakes cap at 70% opacity so bright dots never
bury the bug), and the fog veil thickens ×0.9 toward a pale grey-blue
whiteout (`BlizzardFogGain`, the named tunable the playtest gates — the
first blizzard buried the floor at ×1.55 and the bug became unfindable
even on swept ground). The whiteout also **breathes**: every ~9s the wind
lulls and the fog thins by 45% (`BlizzardLullSeconds` / `BlizzardLullDepth`),
giving a fair window to read the floor before it closes again — the
challenge slows the eye, it never blinds it. The round before a storm round shows the
"Storm Round" warning sign: `warn_sparks.gdshader` repaints the lettering
(from its SubViewport glyph mask) as living electricity — a neon rim hugging
every glyph, jagged bolts arcing through the letters, sparks and
failing-neon flicker — set into a roiling storm cloud the same shader paints
behind the lettering (breathing puffs over a flat raining underside, fbm
churn, lightning simmering in the belly). All shaders are driven by a
single 0–1 intensity uniform (or just TIME) and stay alpha-composited over
the scene (no screen texture) to keep them cheap on mobile. The fall's
season event adds `water.gdshader` (`scripts/WaterStream.cs`, full-floor,
world-space so the storm veil still dims it): a translucent sheet of
flowing streaks in three scales with foam flecks riding the brightest —
one `intensity` knob runs the whole show, from the slow shimmer telegraph
pulses to the racing wash, and a `direction` uniform flips the flow to
match the downstream slide of the litter.

## Seasons (vibe grade, ground, litter mix)

Every level grades itself with the season's general mood
(`assets/shaders/season.gdshader` on `scripts/SeasonGrade.cs`, canvas
layer 1, directly above the world and below the storm veil so weather
draws on top of it and UI above that):

- **Spring** — fresh & green: slight green lift and vibrance plus a soft
  fresh-green cast.
- **Summer** — warm golden haze: warm tint, brightness lift, plus a
  screen-blended golden cast — the mood reads at a glance.
- **Fall** — amber low-sun: strong amber tint plus a deep amber cast over
  a slightly dimmer floor.
- **Winter** — cold and pale: strong desaturation (62%) plus a cool blue
  tint and cast; difficult *visibility* comes from the blizzard weather,
  not the grade.

Every season pairs its tint with a **cast** — the season's hue
screen-blended over the graded image (lifts shadows toward the hue,
keeps highlights). The first pass shipped tint-only grades and the
playtest couldn't tell spring/summer/fall apart; the cast is what makes
each season's atmosphere definite.

The grade is the game's **only screen-reading node** (one full-screen
back-buffer copy per frame — accepted cost, per-pixel math is a few dot
products). If a device playtest shows the cost, `SeasonGrade` ships a
fallback: flip its `ScreenReadGrade` const off and the grade becomes a
plain alpha-composite tint veil (storm-style, no screen read). The grade
fades in over ~1.4s with the round and fades out on the menu.

Two more seasonal touches ride along: **winter swaps the ground sprite**
for `ground_winter.svg` — snow-covered floor, frost-killed grass, ice
glints (a tint can't fake snow) — and the **litter mix follows the
season** (`Main.EffectiveFrequency` re-weights the shared debris palette:
summer leans green leaves and moss, fall goes red/gold and drops petals,
winter thins leaves and mixes white snow-fleck petals in). Vibe only —
weights never change piece weights or sweep behavior. Blizzard storm
rounds drop chunky `snow_pile.svg` mounds in the flood clusters, and the
frozen bug sits inside an `ice_chunk.svg` block whose fracture overlays
(`ice_crack_1/2.svg` — jagged radial hairlines that thicken per stage)
deepen with each crack until the shatter burst scatters tumbling
chunk shards. The rescue key is a hand-drawn `assets/icons/hammer.svg`
mallet (warm rust head, wooden handle, 100×100 icon grid like the coin
and wind icons) — found under the litter, collected with the gold
outline shine, and parked floating at the top middle of the screen
while it arms the ice cracks.
The summer storm rounds add the tornado prop: a hand-drawn
`tornado_funnel.svg` cone (swirl bands tightening toward the ground tip,
leaf flecks flung off it) over a spinning `dust_ring.svg` skirt — the
code tilts and sways the cone rather than rotating it, so it never tips
over.

## Regenerating assets

All textures are written by one Node.js script into `assets/textures/`:
## Generating bugs and variants

Every texture in the game comes from one Node.js script,
[`tools/gen_art.mjs`](../tools/gen_art.mjs): run `node tools/gen_art.mjs`
and it writes every SVG into `assets/textures/` and `assets/textures/bugs/`.
The bug catalog lives in the generator as one drawing function per
**species** and one short call per **variant**. This section is the complete
recipe for adding a new species or new color variants of an existing one.

### How the pieces fit together

```
tools/gen_art.mjs            scripts/BugTypes.cs            the game
species generator  ──writes──▶ assets/textures/bugs/<id>.svg
variant calls      ──writes──▶ assets/textures/bugs/<id>_<suffix>.svg
Sp(id, name, size, tap, (suffix, color)...)  ──registers──▶ spawn + book entries
BugFlavor["<id>"]            ──registers──▶ win-card comments
```

One species = one silhouette generator + N palettes = N textures.
One species registered in `BugTypes` = N book entries (the base look plus
its variants). The book, spawns, save keys and the win card all derive
from `BugTypes` automatically.

### Anatomy of a species generator

Each species is a function in `tools/gen_art.mjs` that takes a palette
object and a texture name, and writes one 100×100 SVG:

```js
function ladybug(p = {}, name = "ladybug") {
  const {
    b1 = "#e8453c", b2 = "#b02a24",       // body gradient, light → dark
    outline = "#5e1713",                   // soft dark rim of the body
    dark = "#26201c", headStroke = "#0f0d0b",
    spots = [[36, 44, 5], /* [cx, cy, r] triplets */],
  } = p;
  const svg = `<svg xmlns="..." viewBox="0 0 100 100"> ... </svg>`;
  write(join(BUGS, name + ".svg"), svg);
}
```

Conventions every generator follows (and new ones must too):

- **`p` is the palette object; every key has the species' natural look as
  its default.** Calling `ladybug()` draws the classic red ladybug; a
  variant is the same function with a different `p` and a suffixed name.
- **100×100 viewBox, creature fills the frame**, with a soft elliptical
  shadow (`opacity 0.14`) under it.
- **Outline everything** in a *darker shade of the body color* (not black)
  so variants stay warm; pure near-black is reserved for eyes/limbs.
- **Chunky shapes, no thin lines**: strokes ≥ 2.4, appendages are filled
  paths or thick round-capped strokes.
- **Layer order**: shadow → far appendages (legs/antennae) → body → head →
  face → pattern details.
- **Appendages must physically attach**: a rotated wing's inner tip has to
  land inside the body's stroke; legs start under the body outline.
- Only flat shapes + one radial gradient per body — no filters, no blur.

### The palette object

There is no global palette type; each species declares its own keys with
natural-color defaults. Typical key families across the catalog:

| Key family | Meaning |
|------------|---------|
| `b1` / `b2` | main body radial gradient, light center → dark rim |
| `outline`, `headStroke` | body rim and head rim (darker shades of `b2`) |
| `limb`, `legs`, `limbDark` | legs / antennae / wing frames |
| `hi`, `legHi` | highlight fills (top-left catchlight, leg sheen) |
| `w1` / `w2`, `wingFill`, `wingStroke` | wing gradients and rims |
| species-specific (`spots`, `bands`, `skirt`, `mantle`, `eyespotFill`…) | pattern details |

A variant only overrides the keys that change its look — everything else
inherits the natural default. Keep at least **light, mid and dark value
steps** per variant so the silhouette still reads on the busy ground.

### The variant table

The bottom of `gen_art.mjs` is the call list that emits every texture.
The first call for a species is the base (bare id); the next three are
variants (suffixed ids):

```js
ladybug();                                       // ladybug.svg          (base)
ladybug({ b1: "#f2c53d", b2: "#d19a1f", outline: "#7a5a10" }, "ladybug_yellow");
ladybug({ b1: "#f08a3c", b2: "#cf6620", outline: "#7a3a12" }, "ladybug_orange");
ladybug({ b1: "#e884a8", b2: "#c25579", outline: "#7a2c48" }, "ladybug_pink");
```

Rules of the table:

- **Exactly four variants per species** (base + 3): the book grid is laid
  out around 4 entries per species and the catalog reads evenly.
- Variant palettes are **natural** colors the species really comes in —
  browns, greens, rusts, creams. Stay away from neon/synthetic hues.
- The base call must stay **palette-default** and keep its name: old saves
  count finds under the bare species id.

### Naming rules

- Texture files and save keys: `snake_case`. Base = `<species>.svg` /
  `<species>`; variants = `<species>_<suffix>.svg` / `<species>_<suffix>`,
  e.g. `stag_beetle_mahogany`.
- Suffix is a color or pattern word (`yellow`, `banded`, `dark`, `rust`);
  it never repeats within one species.
- Display names are built by registration: `<Color> <Species>` — the
  suffix's catalog color word becomes "Yellow Ladybug", "Banded Snail".

### Size, tap radius and occlusion

Species difficulty is two numbers in `BugTypes.cs`:

- `size` — relative draw scale (0.55 aphid … 1.15 butterfly/stick insect).
  Bigger = easier to spot.
- `tapRadius` — finger-forgiving touch radius in design pixels
  (44 aphid … 88 butterfly). Roughly `size × 76`.
- Occlusion radius (debris must clear this to reveal the bug) is derived:
  `clamp(tapRadius × 0.45, 18, 36)` — it hugs the drawn body, never the
  tap target. Don't set it by hand; make the *art* honest instead: the
  creature should fill its 100×100 frame proportionally to its `size`.

### Registering a species (BugTypes.cs + BugFlavor.cs)

`scripts/BugTypes.cs` — one field per species via the `Sp` helper:

```csharp
private static readonly BugType StagBeetle = Sp("stag_beetle", "Stag Beetle",
    0.95f, 72f, ("black", "Black"), ("red", "Red"), ("mahogany", "Mahogany"));
```

`Sp(id, name, size, tap, variants...)` builds the species, makes the base
texture the first variant under the bare id (save compatibility), and
creates the `id_suffix` / `<Color> <Name>` variants after it. Then add the
species to the `All` array — everything else (spawns, book pages, counts)
follows automatically.

`scripts/BugFlavor.cs` — add a `["<id>"]` pool of 6+ **real fun facts**
(one-sentence, kid-friendly, verified — no folklore or fantasy praise)
keyed by the **species id** (variants reuse the species' pool):

```csharp
["stag_beetle"] = new[]
{
    "Stag beetle grubs live in rotting wood for 3 to 7 years.",
    /* 5–6 more verified facts */
},
```

### Preview checklist

Generate, then render each new SVG against a ground-colored backdrop
(the busy floor is what silhouettes must survive):

```sh
node tools/gen_art.mjs
rsvg-convert -w 400 -h 400 -b "#6a5c43" \
  assets/textures/bugs/<bug>.svg -o /tmp/<bug>.png
```

Then `godot-mono --headless --import` so Godot picks up new files.

Each generated bug passes all of these before it's committed:

- Silhouette reads at a glance at phone size; one recognizable critter.
- Appendages attach where they anatomically should (wings on the body,
  not the head; legs under the body, not stroked across it).
- Appendages physically reach the body — no floating parts.
- No limb extends to the floor shadow; nothing pokes through the body fill.
- No stray strokes, accidental "mouths", or asymmetric features.
- Variants of one species are instantly told apart by color alone.
- The book's unknown-bug state (black silhouette) still reads as the
  species' shape.

If a preview fails, fix the **generator function** and regenerate — never
hand-edit an SVG; the next generator run overwrites it.

### Recipe: add a new species in 7 steps

1. **Pick the critter** and study it against the reference sheet
   (`docs/art-reference.jpg`): chunky cartoon body, big simple features,
   warm autumn palette.
2. **Write the generator** in `tools/gen_art.mjs` — copy the closest
   existing species (beetle-like → `beetle`, long → `stickInsect`, …),
   rename its palette keys to the new species' needs, and give every key
   the species' natural default. Follow the generator conventions above.
3. **Call it four times** in the variant table: base + 3 natural variants
   (light, mid, dark values; pattern variant welcome).
4. **Register it** in `scripts/BugTypes.cs` with `Sp(...)`, choosing
   `size`/`tapRadius` from the existing range, and add it to `All`.
5. **Give it flavor** — a `BugFlavor` pool of 6+ real, verified fun facts
   keyed by the species id.
6. **Generate + import + preview**: `node tools/gen_art.mjs`,
   `godot-mono --headless --import`, then rsvg-convert every new SVG and
   run the preview checklist.
7. **Validate headlessly**: `dotnet build`, then
   `LEAF_AUTOPLAY=1 godot-mono --headless --quit-after 3000` and confirm
   the `AUTOPLAY catalog:` line counts the new species (39 → 40) and
   variants (156 → 160), all `True`.

Adding **variants only** to an existing species is steps 3, 4 (extend the
`Sp` variant list), 6 and 7.
