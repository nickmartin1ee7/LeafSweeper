shader_type canvas_item;

// Storm atmosphere for every 10th level: a heavy cold veil that deepens
// toward the screen edges, individual rain drops that fall downward and
// lean downwind (screen right — each with its own length, brightness,
// x offset and speed, never a uniform curtain), rolling cloud shadows,
// ground mist, occasional fog wisps that flare in and dissipate, and a
// periodic lightning double-flash. Everything animates off TIME; the
// game only tweens the `intensity` uniform to fade the storm in and out.
//
// The overlay composites as ordinary alpha blending over the world (no
// screen-texture copy), which keeps it cheap on mobile: the veil color
// is near-black blue-grey so blending darkens the scene, and the rain is
// pale so it reads against the litter.

uniform float intensity : hint_range(0.0, 1.0) = 0.0;

float hash11(float n)
{
    return fract(sin(n * 127.1) * 43758.5453);
}

// One depth layer of rain. Every drop is its own thing: a random slice
// of its cell, a random length and brightness, a per-drop x jitter and
// a per-column fall speed. Neighbouring streaks therefore disagree and
// drift apart instead of swaying as one curtain. The scroll direction
// matters: sampling with (p.y - time * spd) makes drops FALL downward;
// a plus sign there scrolls the whole storm upward instead. There is
// deliberately NO global oscillation either: swaying the whole sheared
// pattern horizontally reads as drops sliding along their streaks.
float rain_layer(vec2 uv, float aspect, float time, float scale,
                 float fall_speed, float slant, float thickness, float seed)
{
    vec2 p = vec2(uv.x * aspect, uv.y) * scale;
    // Wind shear: the further a drop has fallen, the further it has
    // drifted downwind. Static lean — wind gusts live in the cloud
    // shadows, not in bending the streaks.
    p.x += p.y * slant;

    float col = floor(p.x);
    float colRnd = hash11(col * 3.17 + seed * 61.7);
    // Per-column speed jitter: the curtain breaks up.
    float spd = fall_speed * (0.72 + 0.56 * colRnd);
    float thick = thickness * (0.65 + 0.7 * hash11(col * 2.71 + seed * 4.3));
    float colShift = hash11(col * 5.31 + seed * 9.1) - 0.5;

    // Subtract time so the pattern scrolls DOWN the screen: drops fall.
    float y = p.y - time * spd;
    float cell = floor(y);
    float f = fract(y);

    // Per-drop character; some cells are empty so density clusters and
    // thins in ragged patches rather than a metronome grid.
    float present = step(0.22, hash11(cell * 1.71 + col * 7.7 + seed * 3.7));
    float len = 0.30 + 0.45 * hash11(cell * 9.13 + col * 5.7 + seed * 13.1);
    float head = hash11(cell * 3.71 + col * 17.3 + seed * 29.1) * (1.0 - len);
    float bright = 0.30 + 0.85 * hash11(cell * 5.53 + col * 11.9 + seed * 2.3);
    float dropShift = (hash11(cell * 7.7 + col * 3.3 + seed * 5.9) - 0.5) * 0.7;

    // Streak trailing above the head: a rounded bright tip at the
    // leading (lower) edge and a tail fading upward, like the motion
    // blur behind a falling drop. d measures up the tail from the head.
    float d = head + len - f;
    float dash = smoothstep(0.0, 0.06, d) * (1.0 - smoothstep(len * 0.7, len, d));
    float column = smoothstep(thick, 0.0, abs(fract(p.x + colShift + dropShift) - 0.5));
    return column * dash * bright * present;
}

// Slow, soft blotches of ground fog drifting downwind (screen right)
// across the floor. Horizontal drift only.
float mist(vec2 uv, float time)
{
    // Minus time: the sample origin shifts +x over TIME, so the fog
    // visibly moves right instead of left.
    vec2 p = uv * vec2(2.6, 1.9) - vec2(time * 0.06, 0.0);
    float n = sin(p.x * 2.1 + sin(p.y * 3.3)) * sin(p.y * 1.6 + cos(p.x * 2.7));
    return smoothstep(0.1, 0.9, n * 0.5 + 0.5);
}

// Rolling cloud shadows: big soft darkness bands pushing over the floor
// downwind (both terms drift in +x, the same direction the rain leans).
float cloud_bands(vec2 uv, float time)
{
    float w = sin(uv.x * 2.4 + uv.y * 1.1 - time * 0.30) * 0.5
        + sin(uv.x * 1.3 - uv.y * 0.7 - time * 0.18) * 0.3;
    return smoothstep(0.15, 0.95, w * 0.5 + 0.5);
}

// A wisp of fog: one soft blob carried downwind that flares in and
// dissipates on its own irregular cycle. Wraps across the screen with a
// hidden edge fade; rise and fall are asymmetric so no wisp ever feels
// like it is pulsing on a metronome.
float wisp(vec2 uv, float aspect, float time, vec2 base, vec2 drift,
           float radius, float cycle, float phase)
{
    vec2 pos = base + drift * time;
    pos.x = fract(pos.x);
    float edge = smoothstep(0.0, 0.18, pos.x) * (1.0 - smoothstep(0.82, 1.0, pos.x));
    vec2 d = (uv - pos) * vec2(aspect, 1.0);
    float body = smoothstep(1.0, 0.2, length(d) / radius);
    float life = 0.5 + 0.5 * sin(time * 6.28318 / cycle + phase);
    float a = smoothstep(0.50, 0.80, life) * (1.0 - smoothstep(0.80, 1.0, life));
    // A slow flutter while visible — fog is never steady.
    a *= 0.75 + 0.25 * sin(time * 2.3 + phase * 3.7);
    return body * a * edge;
}

// Lightning: a sharp strike followed by a softer echo, then darkness.
// Each strike carries its own brightness so the rhythm never feels
// mechanical. Pure time function — deterministic and free.
float lightning(float time)
{
    float period = 7.0;
    float strike = floor(time / period);
    float x = fract(time / period);
    float amp = 0.55 + 0.45 * fract(sin(strike * 91.17) * 43758.5453);
    float bolt = exp(-x * 26.0)
        + 0.55 * exp(-max(x - 0.11, 0.0) * 30.0) * step(0.11, x);
    return clamp(bolt * amp, 0.0, 1.0);
}

void fragment()
{
    // Screen aspect from the per-pixel screen size (1/width over 1/height),
    // so streak geometry stays uniform across phone aspect ratios.
    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
    vec2 uv = UV;
    float t = TIME;

    // Dark veil: heavy cool storm tint, much deeper toward the edges —
    // the vignette reaches far into the frame.
    vec2 centered = (uv - 0.5) * vec2(aspect, 1.0);
    float vig = smoothstep(0.30, 1.10, length(centered));
    vec3 veil = vec3(0.09, 0.12, 0.17);
    float veil_alpha = intensity * (0.42 + 0.40 * vig);

    // Cloud shadows roll across on top of the veil.
    veil_alpha += intensity * 0.16 * cloud_bands(uv, t);

    // Wind-carried rain at three depths: far is thin and dense, near is
    // fat, fast and streaky. Negative slant leans every layer downwind
    // (screen right), matching the cloud, mist and wisp drift.
    float rain = 0.0;
    rain += rain_layer(uv, aspect, t, 30.0, 3.2, -0.30, 0.050, 0.0) * 0.55;
    rain += rain_layer(uv, aspect, t, 46.0, 4.6, -0.42, 0.042, 1.7) * 0.45;
    rain += rain_layer(uv, aspect, t, 68.0, 6.4, -0.55, 0.034, 4.2) * 0.35;
    rain = clamp(rain, 0.0, 1.0) * intensity;

    // Ground mist drifts through the dim.
    float mist_alpha = intensity * 0.20 * mist(uv, t);

    // Fog wisps flare in, drift with the wind and dissipate.
    float wisps = wisp(uv, aspect, t, vec2(0.25, 0.62), vec2(0.030, 0.004), 0.16, 11.0, 0.0)
        + wisp(uv, aspect, t, vec2(0.68, 0.35), vec2(0.022, 0.003), 0.13, 16.0, 2.7)
        + wisp(uv, aspect, t, vec2(0.85, 0.78), vec2(0.026, -0.002), 0.19, 13.0, 5.1);
    wisps = clamp(wisps, 0.0, 1.0);
    float wisp_alpha = intensity * 0.15 * wisps;

    // Lightning brightens the whole wash toward pale storm-white.
    float flash = lightning(t) * intensity;

    // Compose: veil + cloud shadows + mist + wisps underneath, pale rain
    // on top, then the strike lifts everything with a blue-white wash.
    vec3 col = veil;
    float alpha = veil_alpha + mist_alpha + wisp_alpha;
    col = mix(col, vec3(0.56, 0.63, 0.74), wisps * 0.5);
    col = mix(col, vec3(0.78, 0.85, 0.95), rain);
    alpha = max(alpha, rain * 0.9);
    col = mix(col, vec3(0.85, 0.90, 1.0), flash * 0.55);
    alpha = max(alpha, flash * 0.38);
    COLOR = vec4(col, clamp(alpha, 0.0, 1.0));
}
