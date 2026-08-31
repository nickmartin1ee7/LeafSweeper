shader_type canvas_item;

// The fall wash: a translucent sheet of flowing water streaks that slides
// across the playable floor while the shuffle moves the litter downstream.
// Alpha-composited (no screen read — same cost model as the storm veil),
// driven by a single `intensity` knob the node animates: slow shimmer
// pulses as the telegraph, a bright racing wash for the show, then out.

// +1 wash flows right, -1 flows left.
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float direction = 1.0;
uniform vec4 shallow : source_color = vec4(0.62, 0.8, 0.92, 1.0);
uniform vec4 deep : source_color = vec4(0.3, 0.52, 0.72, 1.0);
uniform vec4 foam : source_color = vec4(0.95, 0.98, 1.0, 1.0);

void fragment() {
	vec2 uv = UV;
	// Streak flow speed scales with intensity: a lazy shimmer at the
	// telegraph, a racing wash at the show.
	float t = TIME * (1.5f + 2.5f * intensity) * direction;
	// Three layers of flowing streaks at different scales, folded to 0..1.
	float s = 0.0;
	s += 0.5f * sin(uv.x * 28.0f + uv.y * 3.0f + t * 6.0f + sin(uv.y * 21.0f) * 1.2f);
	s += 0.3f * sin(uv.x * 53.0f - uv.y * 7.0f + t * 9.0f + sin(uv.y * 37.0f) * 0.8f);
	s += 0.2f * sin(uv.x * 91.0f + t * 14.0f);
	s = s * 0.5f + 0.5f;
	// Base sheet: a subtle top-to-bottom depth gradient shaded by streaks.
	vec4 col = mix(shallow, deep, uv.y * 0.4f + s * 0.25f);
	// Foam flecks ride the brightest streaks.
	float foam_mask = smoothstep(0.82f, 0.98f, s);
	col = mix(col, foam, foam_mask * 0.8f);
	float alpha = intensity * (0.55f + 0.45f * s);
	COLOR = vec4(col.rgb, alpha);
}