shader_type canvas_item;

// "Storm Round" electric sign. The label renders alone into a SubViewport
// whose texture arrives as `text_mask`, so this shader knows exactly where
// the lettering sits. It rebuilds the sign as a roiling storm cloud with
// living electricity inside: a dark cloud silhouette (breathing bumps,
// churning fbm belly, flat raining underside — clean-edged, no rim or
// mist halo) behind the lettering, a hot neon rim hugging every glyph
// edge, a deep blue aura, jagged lightning bolts that arc through the
// lettering (each vertex hashed, re-vibrated ~10x per second, blacking
// out occasionally like a failing transformer), an energy pulse that
// travels along the text line, sparks that cluster near the glyphs, and
// an occasional full-sign blackout. Pure TIME math —
// deterministic, cheap, headless-safe.
//
// `texel` (1 / mask size), `aspect` (mask width / height) and `text_mask`
// are set by StormWarn whenever the sign is laid out.

uniform sampler2D text_mask;
uniform vec2 texel = vec2(0.002, 0.01);
uniform float aspect = 4.0;

float hash11(float n)
{
    return fract(sin(n * 127.1) * 43758.5453);
}

float hash12(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float sd_seg(vec2 p, vec2 a, vec2 b)
{
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h);
}

// Smooth value noise + 3-octave fbm for the cloud's churning interior.
float vnoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash12(i), hash12(i + vec2(1.0, 0.0)), u.x),
            mix(hash12(i + vec2(0.0, 1.0)), hash12(i + vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p)
{
    return vnoise(p) * 0.62 + vnoise(p * 2.3 + 7.7) * 0.26 + vnoise(p * 5.1 + 31.0) * 0.12;
}

// Distance to the storm-cloud silhouette in panel space (x already scaled
// by aspect, y in 0..1): one broad belly plus a row of slowly breathing
// bumps along the top, soft-unioned. Negative inside the cloud.
float cloud_sd(vec2 p, float time)
{
    vec2 c = vec2(0.50 * aspect, 0.66);
    vec2 r = vec2(0.46 * aspect, 0.34);
    float d = (length((p - c) / r) - 1.0) * min(r.x, r.y);
    for (int i = 0; i < 6; i++)
    {
        float f = float(i);
        // End bumps stop at 20%/80% of the width so the silhouette keeps
        // padding inside the panel instead of clipping off the edges.
        float cx = (0.20 + 0.60 * f / 5.0 + 0.015 * sin(time * 0.4 + f * 1.9)) * aspect;
        float cy = 0.50 - 0.06 * hash11(f * 9.1 + 2.0);
        float r = 0.20 + 0.07 * hash11(f * 3.7 + 1.0) + 0.026 * sin(time * 0.6 + f * 2.3);
        d = min(d, length(p - vec2(cx, cy)) - r);
    }
    return d;
}

// One jagged bolt arcing across the label between x0..x1 around base_y.
// Envelope keeps the ends anchored so the bolt springs from the sign's
// edges, wildest mid-span. Returns additive glow; coreOut carries the
// thin white filament.
float bolt(vec2 p, float time, float base_y, float amp, float seed,
           float x0, float x1, float on_chance, out float core)
{
    float ft = floor(time * 10.0);
    float d = 1e9;
    vec2 prev = vec2(0.0);
    for (int i = 0; i < 9; i++)
    {
        float f = float(i) / 8.0;
        // Anchored ends, wildest mid-span.
        float env = sin(3.14159 * f);
        float x = mix(x0, x1, f) * aspect;
        float y = base_y
            + (hash11(f * 91.7 + seed * 57.1) - 0.5) * 2.0 * amp * env
            + (hash12(vec2(f * 57.3 + seed, ft)) - 0.5) * 0.06 * env;
        vec2 v = vec2(x, y);
        if (i > 0)
            d = min(d, sd_seg(p, prev, v));
        prev = v;
    }
    // Dropouts: some 1/10s frames the bolt vanishes entirely — crackle.
    float on = step(hash11(seed * 13.7 + ft * 0.77), on_chance);
    float flick = 0.55 + 0.45 * sin(time * 41.0 + seed * 17.0);
    core = smoothstep(0.005, 0.0008, d) * on * flick;
    return (exp(-d * d * 900.0) * 0.9 + exp(-d * 26.0) * 0.35) * on * flick;
}

// Tiny sparks popping on a jittered grid; each cell pops on its own
// period with a fast exponential decay.
float spark_field(vec2 uv, float seed, float time)
{
    vec2 g = uv * vec2(22.0, 9.0);
    vec2 id = floor(g);
    float h = hash12(id + seed);
    if (h < 0.86)
        return 0.0;
    float period = 0.6 + 0.9 * hash12(id * 1.71 + seed * 3.3);
    float ph = fract(time / period + h * 7.13);
    vec2 cell = fract(g);
    vec2 pos = vec2(0.25 + 0.5 * hash12(id + 4.1), 0.25 + 0.5 * hash12(id + 9.2));
    float d = length(cell - pos);
    return smoothstep(0.16, 0.02, d) * exp(-ph * 9.0) * (0.4 + 0.6 * h);
}

void fragment()
{
    // StormWarn fades the whole sign by tweening the CanvasItem's
    // modulate alpha — Godot delivers that chain in COLOR, so multiply
    // it into the result or every fade is a silent no-op (the sign
    // holds, then hard-cuts when hidden).
    vec4 modulate = COLOR;
    vec2 uv = UV;
    float t = TIME;
    vec2 p = vec2(uv.x * aspect, uv.y);

    // Glyph mask: the label's rendered alpha, blurred at two radii via a
    // golden-angle disc so the rim and aura hug the lettering itself.
    float m = texture(text_mask, uv).a;
    float near_b = 0.0;
    float far_b = 0.0;
    for (int i = 0; i < 12; i++)
    {
        float a = float(i) * 2.399963;
        float c = cos(a);
        float s = sin(a);
        near_b += texture(text_mask, uv + vec2(c, s) * texel * 2.5).a;
        far_b += texture(text_mask, uv + vec2(c, s) * texel * 7.0).a;
    }
    near_b /= 12.0;
    far_b /= 12.0;
    // rim = a band just outside the glyph contour; halo = the wide aura.
    float rim = clamp(near_b * 1.5 - m, 0.0, 1.0);
    float halo = max(far_b * 0.85, m);
    // Bolts and sparks burn hotter where the lettering is.
    float near_f = clamp(near_b * 1.6, 0.0, 1.0);
    float boost = 0.45 + 1.30 * near_f;

    // Failing-neon flicker: rare full-sign blackouts plus buzz.
    float neon_on = step(0.05, hash11(floor(t * 13.0) * 1.37));
    float flick = neon_on * (0.82 + 0.12 * sin(t * 53.0) + 0.06 * sin(t * 131.0 + 1.7));

    // Bolts: one crosses through the lettering, one tall-arc crashes
    // mid-sign (the arc-around top/bottom pairs were cut — they boxed the
    // sign in). Each re-jitters on its own 10Hz
    // clock and blacks out at its own rate.
    float c3, c4;
    float glow = bolt(p, t, 0.50, 0.17, 4.2, -0.05, 1.05, 0.55, c3) * 0.7
        + bolt(p, t, 0.50, 0.38, 9.4, 0.30, 0.70, 0.35, c4) * 0.8;
    float core = c3 * 0.7 + c4 * 0.8;

    // An energy pulse racing along the text line.
    float pulse = exp(-abs(uv.x - fract(t * 0.33)) * 13.0)
        * exp(-abs(uv.y - 0.5) * 4.0);

    // Sparks cluster near the glyphs.
    float sparks = (spark_field(uv, 3.7, t) + spark_field(uv, 11.3, t))
        * (0.30 + 1.25 * near_f);

    vec3 electric = vec3(0.45, 0.68, 1.0);
    vec3 aura_col = vec3(0.13, 0.26, 0.80);
    vec3 hot = vec3(0.95, 0.98, 1.0);

    // Storm-cloud backdrop — a breathing, churning cloud body instead of a
    // dark card, so the lettering rides inside an actual storm cloud.
    float cd = cloud_sd(p, t);
    float cloud = smoothstep(0.020, -0.012, cd);
    // Flat, raining underside: classic cumulonimbus base.
    cloud *= 1.0 - smoothstep(0.74, 0.88, uv.y);
    // Interior churn drifting downwind (+x, same wind as the storm overlay).
    vec2 qq = vec2(p.x - t * 0.05, p.y) * 2.0;
    float churn = fbm(qq + fbm(qq * 1.9 + vec2(t * 0.04, 0.0)));
    vec3 cloud_col = vec3(0.08, 0.10, 0.16) + vec3(0.16, 0.19, 0.28) * churn;
    // Faint moonlit crest on the top bumps (edge band reused from below);
    // deliberately NO dark rim or outer mist — both read as a vignette
    // hugging the rectangular panel bounds.
    float edge = smoothstep(-0.10, -0.005, cd);
    cloud_col += vec3(0.42, 0.50, 0.64) * smoothstep(0.58, 0.26, uv.y) * edge * 0.55;
    // Lightning simmering inside the belly, on the neon flicker's clock.
    cloud_col += electric * (0.04 + 0.10 * churn) * flick;

    vec3 col = cloud_col * cloud;
    col += aura_col * halo * 1.1 * flick;
    col += electric * rim * 2.2 * flick;
    col += electric * glow * boost;
    col += hot * core * boost;
    col += hot * sparks;
    col += electric * pulse * 0.30 * (0.35 + 0.65 * m);

    // Glyph cores stay readable: near-white neon with a cold shimmer.
    vec3 text_col = vec3(0.90, 0.96, 1.0) * (0.82 + 0.13 * flick + 0.05 * pulse);
    col = mix(col, text_col, m);

    float a = cloud * 0.96 + halo * 0.9 + rim * 1.35 + glow * 0.85 + core * 0.9 + sparks + m;
    COLOR = vec4(col, clamp(a, 0.0, 1.0)) * modulate;
}
