shader_type spatial;

uniform sampler2D colorMap : hint_default_black;
uniform float fade : hint_range(0.0, 1.0) = 1.0;
uniform vec2 samplePos = vec2(0.0, 0.0);
uniform vec2 sampleInvCov = vec2(1.0, 1.0);
uniform vec3 baseColor : source_color = vec3(0.984375,0.98828125,0.74609375);

void vertex() {}

void fragment() {
	// compute offset from the sampled point
	vec2 off = UV - samplePos;
	
	// compute probability of the sample
	float prob = exp(-dot(off,sampleInvCov*off));
	
	// compute the color from the color map
	vec3 sampled_color = texture(colorMap, vec2(COLOR.r*0.99+0.005, 0.0)).rgb;

	ALBEDO = mix(sampled_color, baseColor, fade*(1.0-prob));
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
