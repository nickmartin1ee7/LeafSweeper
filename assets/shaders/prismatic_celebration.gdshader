// Prismatic celebration tint for the end-of-round wind: when a prismatic
// bug is found, every piece circling in the win gyre turns to a mix of
// gold and white — gold for the shaded leaf tones, white where the leaf
// is brightest — then releases back to the texture's own colors as the
// next round opens (Main tweens gold_mix back to 0 with the same 4s
// pacing as the storm label's fade). At gold_mix = 0 this is an exact
// passthrough of the default sprite pipeline.
//
// gold_mix (0..1) is tweened by Main's shared ShaderMaterial. COLOR at
// fragment entry already carries texture × modulate — recolor it in
// place; sampling TEXTURE again and multiplying it in would square the
// texture, darkening every untinted leaf and letting its hues bleed
// through the celebration.
shader_type canvas_item;

uniform float gold_mix : hint_range(0.0, 1.0) = 0.0;

// Warm celebratory gold, picked to sing against the wood dock.
const vec3 GOLD = vec3(1.0, 0.78, 0.25);

void fragment() {
	float luma = dot(COLOR.rgb, vec3(0.299, 0.587, 0.114));
	// Darker leaf parts go gold, the brightest tips bloom to white.
	vec3 celeb = mix(GOLD, vec3(1.0), smoothstep(0.30, 0.75, luma));
	COLOR.rgb = mix(COLOR.rgb, celeb, gold_mix);
}
