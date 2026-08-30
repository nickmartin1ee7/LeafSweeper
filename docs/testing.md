# LeafSweeper — Testing

## Automated (headless)

| Check | Command | Expect |
|-------|---------|--------|
| Compile | `dotnet build` | 0 errors |
| Asset import | `godot --headless --import` | exits 0, no script errors |
| Boot smoke | `godot --headless --quit-after 180` | no errors in output |
| Save round-trip | `LEAF_AUTOPLAY=1 godot --headless --quit-after 2000` | `ok=True`, exit 0 |

One-liner for a quick pass — build error count, then autoplay with output
filtered to what matters:

```sh
dotnet build 2>&1 | grep -cE " error "; LEAF_AUTOPLAY=1 godot --headless --quit-after 2000 2>&1 | grep -E "AUTOPLAY|SCRIPT ERROR"
```

`0` from the first command means a clean build; pass means `AUTOPLAY …
ok=True` lines and no `SCRIPT ERROR`.

`LEAF_AUTOPLAY` makes `Main` play a level end-to-end headlessly (covered-bug
uncover rule, **verified against alpha ground truth** — the blocker's
coverage is recomputed straight from its texture's alpha channel and must
match `Debris.Covers` for a positive and a negative case, printed as
`truthOk=True` → collect a gust coin and **await its arrival animation** →
7 sweeps → gust spend → win → save → reload) and verifies `currentLevel`,
`levelsCleared`, `totalSweeps`, `totalGusts`, `gustPower`, bug find counts
and history round-trip correctly. It is `async void` and awaits in-game
signals, so `--quit-after` must outlast the animations it waits on.

**Fresh checkout/worktree?** Run `--headless --import` once before the
autoplay, or nothing boots and it **silently exits 0 with no `AUTOPLAY`
output** — grep for `AUTOPLAY`, don't trust the exit code alone.

## Visual smoke (windowed)

Headless checks can't see pixels. When a change touches layout, layering or
rendering, capture a real screenshot before handing off:

1. Temporarily add an env-gated hook in `Main._Ready` (same pattern as
   `LEAF_AUTOPLAY`): on `LEAF_SHOT=<path>`, start a level, await ~30
   `ProcessFrame`s, save the viewport image, quit.
2. `LEAF_SHOT=/tmp/shot.png godot --path .` — must be **windowed**; headless
   mode has no framebuffer to capture.
3. Inspect the PNG, iterate, remove the hook before committing.

Real catch: the bottom dock was completely invisible (a zero-height rect
caused by setting anchors without offsets) while every headless check was
green.

## Device testing

Tested on a physical device over adb (device id `1C281FDF6002H0`). Only
game-related interactions are exercised; the OS and other apps are left
alone.

### Install

```sh
adb devices                       # confirm device is attached
adb install -r build/LeafSweeper.apk
adb logcat -c                     # clear log before a run
adb logcat | grep -iE "LeafSweeper|godot|mono|FATAL|AndroidRuntime"
```

### Manual checklist (per build)

1. **Boot** — title menu appears with "LeafSweeper", Play button.
2. **First round** — Play → debris visible, level "Level 1", sweeps 0.
3. **Sweep** — drag across the pile; leaves/petals fling easily, rocks and
   sticks resist more; fast sweeps don't miss debris (no tunneling).
4. **Find & tap bug** — petal sparkles, celebration pulse, win overlay with
   comment + stats (time · sweeps).
5. **Gust coins** — three gold coins (wind icon on the face) hide under the
   debris; uncover one and tap it: it shines golden, grows, spirals **above
   the dock** into the gust button, fires a gold ring-and-spark burst, and
   the ×N badge pulses as it ticks up; pressing Gust spends one (×0 leaves
   the button disabled).
6. **Dock** — wood tray pinned to the bottom; gust coin centered, restart
   coin rightmost, sweep counter left, level label top-middle; sweeping
   can't act through it; debris never *spawns* under it but may drift over
   it while fading.
7. **Next** — starts next level; level counter increments.
8. **Persistence** — force-stop the app, relaunch: Play resumes at the
   correct level; menu shows lifetime stats; the gust power balance resumes.
9. **New game** — after ≥1 clear, New game resets progress to level 1
   (gust power back to 3).
10. **Rotation/aspect sanity** — HUD stays pinned to edges (portrait lock is
	the shipped orientation).
11. **Double-tap burst** — two quick bare taps in one spot fling nearby
	debris outward (≤12 pieces), sweeps counter +1, gust power unchanged;
	a double-tap on already-clear ground flings nothing and costs nothing;
	a tap-then-drag never bursts.
12. **Round-start settle** — each new round the debris tumbles in from
	above in a staggered curtain (no visible bug or coins mid-fall);
	touches and the gust button do nothing until every piece has landed;
	then the bug and 3 gust coins are hidden under the fresh litter.
	The dock's restart button runs the same reshuffle: the old litter
	vanishes, a fresh curtain settles over new hiding spots, and play
	stays locked until it lands.
13. **End-of-round wind** — on a win, every piece still on the floor
	picks up into a slow clockwise swirl around the floor's center
	(speeds shear per piece, the ring breathes and bobs, pieces tumble);
	the litter keeps circling behind the win card until Next/Menu clears
	the round; resizing mid-wind keeps the gyre centered on the floor.
14. **Menu gyre** — the home screen dresses itself with a decorative
	litter riding the same clockwise gyre, idled way down
	(`MenuWindSpeedScale` 0.35): a calm backdrop behind the menu card,
	no bug or coins, touches do nothing. Starting a round tears it
	down into the settle curtain.

Useful adb helpers while testing:

```sh
adb shell am force-stop <package>          # test persistence
adb shell input tap <x> <y>                # synthetic tap (menu buttons)
adb shell input sweep <x1> <y1> <x2> <y2> <ms>   # synthetic sweep
adb shell screencap -p /sdcard/ls.png && adb pull /sdcard/ls.png
```

## Known non-goals

- No audio tests (sound is out of scope for the MVP).
- No cloud/save-migration tests (single-device local save only).
