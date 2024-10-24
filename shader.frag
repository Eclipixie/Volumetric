#version 330 core
out vec4 FragColor;

in vec2 uv;

uniform float aspect;

float sdf_sphere(vec3 p, float r) {
    return sqrt(pow(p.x,2) + pow(p.y,2) + pow(p.z,2)) - r;
}

float map(vec3 v) {
    return sdf_sphere(vec3(0,0,5) - v, 2);
}

void main() {
    vec3 o = vec3(0.);
    float travel = 0;
    vec3 dir = normalize(vec3(uv.x*aspect, uv.y, 1.));

    float maxDist = 256.;
    int maxIter = 256;
    float epsilon = 0.01;

    for (int i = 0; i < maxIter; i++) {
        float d = map(o + (travel * dir));
        travel += d;

        if (d <= epsilon || travel >= maxDist) break;
    }

    vec3 background = vec3(.05);
    vec3 shape = vec3(1);

    float t = travel / maxDist;
    FragColor = vec4((1-t)*shape + t*background, 1);
}