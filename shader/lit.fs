#version 330

in vec3 fragNormal;
in vec2 fragTexCoord;

out vec4 finalColor;

uniform sampler2D texture0;

void main() {
  vec4 texColor = texture(texture0, fragTexCoord);

  finalColor = texColor;
}
