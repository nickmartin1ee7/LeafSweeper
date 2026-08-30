shader_type canvas_item;

// Unknown-entry look for the bug book: the critter's sprite is flattened to
// near-black while a soft off-black mist breathes around its outline. TIME
// drifts the halo so the silhouette never sits perfectly still.

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    // Two offset halo samples whose offsets orbit out of phase, so the mist
    // appears to seep and swirl instead of pulsing in place.
    vec2 drift = vec2(sin(TIME * 0.9), cos(TIME * 0.63)) * 0.045;
    float haloA = texture(TEXTURE, UV + drift).a * 0.55;
    float haloB = texture(TEXTURE, UV - drift * 0.62).a * 0.38;
    float body = tex.a * 0.92;
    // Off-black mist tone against a pure black core.
    vec3 mist = vec3(0.13, 0.12, 0.15);
    vec3 core = vec3(0.015, 0.015, 0.02);
    vec3 col = mix(mist, core, clamp(body, 0.0, 1.0));
    float alpha = clamp(body + (haloA + haloB) * 0.55, 0.0, 0.8);
    COLOR = vec4(col, alpha);
}
