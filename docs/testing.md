# LeafSweeper — Testing

## Automated (headless)

| Check | Command | Expect |
|-------|---------|--------|
| Compile | `dotnet build` | 0 errors |
| Asset import | `godot --headless --import` | exits 0, no script errors |
| Boot smoke | `godot --headless --quit-after 180` | no errors in output |
| Save round-trip | `LEAF_AUTOPLAY=1 godot --headless --quit-after 300` | `ok=True`, exit 0 |

`LEAF_AUTOPLAY` makes `Main` play a level end-to-end headlessly (7 swipes →
win → save → reload) and verifies `currentLevel`, `levelsCleared`,
`totalSwipes`, bug find counts and history round-trip correctly.

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
2. **First round** — Play → debris visible, level "Level 1", swipes 0.
3. **Sweep** — drag across the pile; leaves/petals fling easily, rocks and
   sticks resist more; fast swipes don't miss debris (no tunneling).
4. **Find & tap bug** — petal sparkles, celebration pulse, win overlay with
   comment + stats (time · swipes).
5. **Next** — starts next level; level counter increments.
6. **Persistence** — force-stop the app, relaunch: Play resumes at the
   correct level; menu shows lifetime stats.
7. **New game** — after ≥1 clear, New game resets progress to level 1.
8. **Rotation/aspect sanity** — HUD stays pinned to edges (portrait lock is
   the shipped orientation).

Useful adb helpers while testing:

```sh
adb shell am force-stop <package>          # test persistence
adb shell input tap <x> <y>                # synthetic tap (menu buttons)
adb shell input swipe <x1> <y1> <x2> <y2> <ms>   # synthetic sweep
adb shell screencap -p /sdcard/ls.png && adb pull /sdcard/ls.png
```

## Known non-goals

- No audio tests (sound is out of scope for the MVP).
- No cloud/save-migration tests (single-device local save only).
