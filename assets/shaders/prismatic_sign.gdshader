shader_type canvas_item;

// The "Prismatic" celebration banner: shown while the win card is up after
// a prismatic find, riding the next round's opening like the storm sign
// rides a storm round. The label renders alone into a SubViewport whose
// texture arrives as `text_mask`, so this shader knows exactly where the
// lettering sits (same mask trick as warn_sparks.gdshader). It paints the
// lettering as living iridescence — a rainbow hue crawling across the
// glyphs over time, bright specular bands sweeping through them like light
// over foil, star glints twinkling on and around the lettering — all set
// inside a soft pastel aura that slowly shifts through the spectrum. Pure
// TIME math — deterministic, cheap, headless-safe.
//
// `texel` (1 / mask size) and `text_mask` are set by PrismaticSign
// whenever the banner is laid out.

uniform sampler2D text_mask;
uniform vec2 texel = vec2(0.002, 0.01);

float hash12(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Godot 4 has no hsv2rgb built-in, so provide the standard GLSL recipe.
vec3 hsv2rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment()
{
    // PrismaticSign fades the whole banner by tweening the CanvasItem's
    // modulate alpha — Godot delivers that chain in COLOR, so multiply it
    // into the result or every fade is a silent no-op (same trap the storm
    // sign's shader documents).
    vec4 modulate = COLOR;
    vec2 uv = UV;
    float t = TIME;

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
    float near_f = clamp(near_b * 1.6, 0.0, 1.0);

    // Rainbow wash across the lettering: hue drifts through time and
    // slides along the line, so colors crawl through the lettering instead
    // of strobing (same recipe as the bug's prismatic.gdshader).
    float hue = fract(t * 0.13 + uv.x * 0.85 + (1.0 - uv.y) * 0.12);
    vec3 rainbow = hsv2rgb(vec3(hue, 0.62, 1.0));

    // Extra shiny lettering: two specular bands sweep through the glyphs
    // like light gliding over foil — one wide and slow, one thin, brighter
    // echo chasing it.
    float sweep = fract(t * 0.21);
    float band = exp(-abs(uv.x - sweep) * 14.0);
    float echo = exp(-abs(uv.x - fract(sweep + 0.5)) * 26.0) * 0.6;
    float shine = clamp(band + echo, 0.0, 1.0);

    // Star glints on a jittered grid, clustered near the lettering.
    vec2 g = uv * vec2(26.0, 10.0);
    vec2 id = floor(g);
    float h = hash12(id + 3.7);
    float ph = fract(t / (0.7 + 0.9 * hash12(id * 1.73)) + h * 7.13);
    vec2 cell = fract(g) - vec2(0.25 + 0.5 * hash12(id + 4.1), 0.25 + 0.5 * hash12(id + 9.2));
    float spark = smoothstep(0.14, 0.02, length(cell)) * exp(-ph * 7.0)
        * step(0.84, h) * (0.5 + 1.4 * near_f);

    // Slow-shifting pastel aura behind the lettering.
    vec3 aura_col = hsv2rgb(vec3(fract(t * 0.045), 0.55, 1.0));

    vec3 glyph = hsv2rgb(vec3(hue, 0.70, 1.0)) * (0.80 + 0.20 * sin(t * 1.7 + uv.x * 5.0));
    // Bright foil body under the specular bands: the "extra shiny" pass.
    vec3 col = glyph * (0.72 + 0.5 * near_f);
    col = mix(col, vec3(1.0, 0.99, 0.92), shine * 0.85);
    // Gold-white rim hugging the glyph contour, tinted by the aura.
    vec3 rim_col = mix(vec3(1.0, 0.88, 0.55), aura_col, 0.35 + 0.3 * sin(t * 0.6));
    col += rim_col * rim * 1.5;
    // Soft pastel aura breathing behind the lettering.
    col += aura_col * halo * 0.85;
    col += vec3(1.0, 1.0, 0.9) * spark;

    float a = m + rim * 1.05 + halo * 0.85 + spark + shine * m * 0.4;
    COLOR = vec4(col, clamp(a, 0.0, 1.0)) * modulate;
}
