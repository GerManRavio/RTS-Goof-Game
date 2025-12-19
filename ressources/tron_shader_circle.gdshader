shader_type spatial;

uniform vec3 base_color : source_color = vec3(0.0, 0.05, 0.05);
uniform vec3 line_color : source_color = vec3(0.0, 1.0, 1.0);
uniform float grid_size = 15.0;
uniform float line_thickness : hint_range(0.01, 0.2) = 0.04;
uniform float pulse_speed = 0.5;
uniform float emission_strength = 3.0;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
    vec2 uv = UV * grid_size;
    vec2 id = floor(uv);
    vec2 gv = fract(uv) - 0.5;
    
    float mask = 0.0;
    float h = hash(id);

    if (h < 0.25) {
        mask = smoothstep(line_thickness, 0.0, abs(gv.x));
    } else if (h < 0.5) {
        mask = smoothstep(line_thickness, 0.0, abs(gv.y));
    } else if (h < 0.75) {
        mask = smoothstep(line_thickness, 0.0, abs(max(gv.x, gv.y)));
    }

    float nodes = 0.0;
    if (hash(id + 0.5) > 0.8) {
        float circle = length(gv);
        nodes = smoothstep(0.2, 0.1, circle);
    }

    float signal = sin(h * 10.0 + TIME * pulse_speed) * 0.5 + 0.5;
    float final_mask = max(mask, nodes);
    
    ALBEDO = base_color;
    
    vec3 glow = line_color * final_mask * (emission_strength + signal * 2.0);
    EMISSION = glow;
}