#version 330 core
layout (location = 0) in vec3 aPosition;

out vec2 uv;

void main() {
    gl_Position = vec4(aPosition, 1.0);
    uv = vec2(aPosition.xy);
}