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

## Regenerating assets

All textures are written by one Node.js script into `assets/textures/`:

```sh
node tools/gen_art.mjs
```

- `assets/textures/*.svg` — debris variants + 1080×2340 ground.
- `assets/textures/bugs/*.svg` — the 17-bug catalog (100×100 viewBox).

After regenerating, re-run `godot --headless --import` so Godot reimports
the changed SVGs — but a green import does **not** mean the art is right.
Render previews and look at every changed asset before trusting it:

```sh
rsvg-convert -w 400 -h 400 -b "#6a5c43" assets/textures/bugs/<bug>.svg -o /tmp/<bug>.png
```

(the `#6a5c43` backdrop mimics the in-game ground so silhouettes read).

**Preview checklist** — each generated bug should pass all of these:

- Silhouette reads at a glance at phone size; one recognizable insect.
- Appendages attach where they anatomically should (wings on the body,
  not the head; legs under the body, not stroked across it).
- Appendages physically reach the body — rotated wings must have their
  inner tip land inside the body's stroke, not float beside it.
- No limb extends to the floor shadow; nothing pokes through the body fill.
- No stray strokes, accidental "mouths", or asymmetric features.

If a preview fails, fix the **generator function** in `tools/gen_art.mjs`
and regenerate — never hand-edit an SVG, the next run overwrites it.

## Adding a new variant

1. Add a generator entry in `tools/gen_art.mjs` (follow an existing leaf/petal
   entry).
2. Run the generator; commit both the script change and the new SVG.
3. If it's a debris kind, add it to the weighted `palette` array in
   `scripts/Main.cs` (`SpawnDebris`); if it's a bug, add an entry to
   `scripts/BugTypes.cs`.
