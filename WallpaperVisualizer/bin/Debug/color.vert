#version 330

in vec2 v_coord;
in vec2 v_texcoord;
in vec4 v_color;

out vec2 f_texcoord;
out vec4 color;

uniform mat4 mvp;
 
void main() {
	gl_Position = mvp * vec4(v_coord,1.0,1.0);
	f_texcoord = v_texcoord;
	color = v_color;
}