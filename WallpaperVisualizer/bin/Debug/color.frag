#version 330

in vec2 f_texcoord;
in vec4 color;
out vec4 outputColor;

uniform sampler2D mytexture;
 
void main(void) {
	outputColor = color;
}