shader_type canvas_item;

// Prismatic discovery look: the bug's body cycles through the rainbow while
// sparkle glints twinkle across it, and the golden discovery outline from the
// base glow shader still works for the win celebration. Applied only to the
// bug's own sprite — the bug sits below every debris layer, so none of this
// can ever show through the leaves.

uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float thickness : hint_range(0.0, 12.0) = 2.5;
uniform vec4 gold : source_color = vec4(1.0, 0.82, 0.25, 1.0);
uniform float shine_speed : hint_range(0.0, 20.0) = 5.0;
uniform float hue_speed : hint_range(0.0, 1.0) = 0.14;

// Godot 4 has no hsv2rgb built-in, so provide the standard GLSL recipe.
vec3 hsv2rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment() {
	vec4 src = texture(TEXTURE, UV);
	// Rainbow wash: hue drifts through time and slides diagonally across
	// the sprite, so colors crawl over the body instead of strobing.
	float hue = fract(TIME * hue_speed + (UV.x + UV.y) * 0.22);
	vec3 rainbow = hsv2rgb(vec3(hue, 0.85, 1.0));
	vec4 col = src;
	col.rgb = mix(src.rgb, src.rgb * (0.35 + rainbow * 1.15), src.a * 0.88);

	// Sparkle glints: twinkling points on a jittered grid inside the body.
	vec2 grid = UV * 16.0;
	vec2 cell = floor(grid);
	vec2 f = fract(grid) - 0.5;
	float seed = fract(sin(dot(cell, vec2(12.9898, 78.233))) * 43758.5453);
	float beat = fract(seed + TIME * 0.4);
	float twinkle = smoothstep(0.86, 0.99, beat) + smoothstep(0.14, 0.01, beat);
	float glint = smoothstep(0.24, 0.0, length(f)) * twinkle;
	col.rgb += vec3(1.0, 1.0, 0.85) * glint * src.a;

	// Golden outline (shared with gold_outline.gdshader) for Celebrate().
	vec2 px = TEXTURE_PIXEL_SIZE * thickness;
	float nb = 0.0;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			if (x == 0 && y == 0) continue;
			nb = max(nb, texture(TEXTURE, UV + vec2(float(x), float(y)) * px).a);
		}
	}
	float ring = clamp(nb - src.a, 0.0, 1.0);
	float shine = 0.65 + 0.35 * sin(TIME * shine_speed);
	col.rgb = mix(col.rgb, gold.rgb * (0.8 + 0.4 * shine), ring * intensity);
	col.a = max(col.a, ring * intensity);
	COLOR = col;
}
