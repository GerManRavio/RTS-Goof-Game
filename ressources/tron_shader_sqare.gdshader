shader_type spatial;

uniform vec3 grid_color : source_color = vec3(0.0, 1.0, 1.0);
uniform vec3 background_color : source_color = vec3(0.0, 0.0, 0.1);
uniform float grid_thickness : hint_range(0.0, 0.1) = 0.03;
uniform float grid_size = 20.0;
uniform float emission_strength = 3.0;

void fragment() {
    vec2 derivative = fwidth(UV * grid_size);
    vec2 grid = abs(fract(UV * grid_size - 0.5) - 0.5);

    float line = min(grid.x, grid.y);
    
    float target_thickness = grid_thickness + (derivative.x + derivative.y) * 0.5;
    
    float mask = smoothstep(target_thickness, target_thickness - 0.01, line);
    
    ALBEDO = mix(background_color, grid_color, mask);
    EMISSION = grid_color * mask * emission_strength;
}