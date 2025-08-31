#version 330

in vec3 fragNormal;
in vec2 fragTexCoord;

out vec4 finalColor;

uniform sampler2D texture0;

void main() {
  float diffuse = max(dot(fragNormal, vec3(0.3, 1, 0)), 0);
  float ambient = 0.4;

  float luminance = diffuse+ambient;

  vec4 texColor = texture(texture0, fragTexCoord);
  vec4 fragColor = texColor * luminance;
  fragColor.w = 1.0;

  finalColor = fragColor;
}
