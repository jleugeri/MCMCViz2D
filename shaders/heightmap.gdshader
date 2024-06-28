shader_type spatial;

// color map for mesh
uniform sampler2D colorMap : hint_default_black;

// highlight map for mesh
uniform sampler2D highlightMap : hint_default_black;

// fade between color map and highlight map
uniform float fade : hint_range(0.0, 1.0) = 1.0;

uniform vec2 highlightPosition = vec2(0.5, 0.5);

// uniform vec3 baseColor : source_color = vec3(0.984375,0.98828125,0.74609375);

void vertex() {}

void fragment() {
	// compute the UV coordinates of the point
	// relative to the current origin of the distribution
	vec2 off = clamp(UV-highlightPosition+vec2(0.5, 0.5), 0.0, 1.0);

    // fade between the inherent height map and the highlight map
	vec3 height_map_color = mix(
        texture(colorMap, vec2(COLOR.r*0.99+0.005, 0.0)).rgb, 
        texture(colorMap, vec2(texture(highlightMap, off).r*0.99+0.005, 0.0)).rgb,
        fade
    );
    ALBEDO = height_map_color;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
