shader_type canvas_item;

// Golden discovery glow: brightens the bug toward gold and draws a shining
// outline just outside its silhouette. Driven by the "intensity" parameter,
// which Bug.Celebrate() tweens in and out.
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float thickness : hint_range(0.0, 12.0) = 2.5;
uniform vec4 gold : source_color = vec4(1.0, 0.82, 0.25, 1.0);
uniform float shine_speed : hint_range(0.0, 20.0) = 5.0;

void fragment() {
	vec4 src = texture(TEXTURE, UV);
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
	vec4 col = src;
	col.rgb = mix(col.rgb, gold.rgb, 0.35 * intensity * src.a);
	col.rgb = mix(col.rgb, gold.rgb * (0.8 + 0.4 * shine), ring * intensity);
	col.a = max(src.a, ring * intensity);
	COLOR = col;
}
