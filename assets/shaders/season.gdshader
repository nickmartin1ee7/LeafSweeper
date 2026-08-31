shader_type canvas_item;

// Seasonal vibe grade: a per-season color grade over the whole floor so
// every level of a season carries its general mood — Spring fresh & clear,
// Summer warm golden, Fall amber low-sun, Winter cold and pale. The game
// drives it entirely through uniforms (see scripts/SeasonGrade.cs): the
// per-season tint/saturation/brightness triple and `intensity`, which the
// game tweens 0↔1 so the grade fades in like weather, not a light switch.
//
// This is the only screen-reading node in the game, so the one
// full-screen back-buffer copy per frame is an accepted, documented cost
// (docs/art-style.md). If a device playtest shows the cost, SeasonGrade
// falls back to a plain alpha-composite veil (no screen read) — the
// ScreenReadGrade const. UI on higher CanvasLayers is untouched: this
// layer sits below every UI (explicit ladder, docs/architecture.md).

uniform sampler2D screen_tex : hint_screen_texture, filter_linear;
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
// Per-season look, set from SeasonGrade.Grades: tint multiplies the
// graded color, saturation is 0 = grey..1 = untouched, brightness scales.
uniform vec3 tint = vec3(1.0);
uniform float saturation = 1.0;
uniform float brightness = 1.0;
// Season atmosphere cast: the season's hue screen-blended over the graded
// image (lifts shadows toward the season color, keeps highlights) so the
// mood reads at a glance — a bare tint multiply was too subtle to notice.
uniform vec3 cast = vec3(1.0);
uniform float cast_amount : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec3 c = texture(screen_tex, SCREEN_UV).rgb;
	float lum = dot(c, vec3(0.299, 0.587, 0.114));
	c = mix(vec3(lum), c, saturation);
	c *= tint;
	c *= brightness;
	c = mix(c, 1.0 - (1.0 - c) * (1.0 - cast), cast_amount);
	COLOR = vec4(mix(texture(screen_tex, SCREEN_UV).rgb, c, intensity), 1.0);
}
