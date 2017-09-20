#version 330

in vec2 v_coord;
in vec2 v_texcoord;

out vec2 f_texcoord;
out vec4 color;

uniform mat4 mvp;
uniform vec4 _color;
 
void main() {
	gl_Position = mvp * vec4(v_coord,1.0,1.0);
	f_texcoord = v_texcoord;
	color = _color;
}