using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;

namespace WallpaperVisualizer
{
    class ColorTexture
    {
        public Dictionary<Color, int> colorDict = new Dictionary<Color, int>();
		public int GetTexture(Color color)
        {
            int ret = 0;
			if (colorDict.TryGetValue(color, out ret))
            {
                return ret;
            }
            ret = createTexture(color);
            colorDict.Add(color, ret);
            return ret;
        }
		private int createTexture(Color color)
        {
            int textureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            Bitmap bmp = new Bitmap(1, 1);
            bmp.SetPixel(0, 0, color);
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0,0,1,1),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                0, 0, 1, 1,
                PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bmp.UnlockBits(data);
            return textureID;
        }
    }
}
