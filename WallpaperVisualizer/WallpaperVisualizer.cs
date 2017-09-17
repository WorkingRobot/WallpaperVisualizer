using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Graphics;

namespace WallpaperVisualizer
{
    internal class WallpaperVisualizer : GameWindow
    {
        public static WallpaperVisualizer MainWindow = null;
        public RectangleF CurrentView = new RectangleF(0, 0, 800, 600);

        private int ibo_elements;
        private Dictionary<string, int> textures = new Dictionary<string, int>();
        private List<Sprite> sprites = new List<Sprite>();
        private Matrix4 ortho;
        private bool updated = false;
        private float avgfps = 60;
        private Random r = new Random();

        TextRenderer renderer;
        ColorTexture colorPool;
        ShaderProgram shader;
        private int counter;

        static AudioGetter audioGetter;
        static Spotify spotify;

        [STAThread]
        public static void Main()
        {
            using (WallpaperVisualizer window = new WallpaperVisualizer())
            {
                audioGetter = new AudioGetter(44100, 3);
                spotify = new Spotify();
                MainWindow = window;
                window.Run(60.0, 60.0);
            }
        }

        public WallpaperVisualizer()
            : base(800, 600, new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(8, 8, 8, 8), 3, 3, 4), "OpenTK Sprite Demo", GameWindowFlags.Default, DisplayDevice.Default, 3, 0, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
        {
            CurrentView.Size = new SizeF(ClientSize.Width, ClientSize.Height);
            ortho = Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, 1.0f, 50.0f);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            renderer = new TextRenderer(40 * 5, 15 * 5);
            audioGetter.Start();
            colorPool = new ColorTexture();
            GL.ClearColor(Color.CornflowerBlue);
            GL.Viewport(0, 0, Width, Height);

            // Load textures from files
            textures.Add("opentksquare", loadImage("opentksquare.png"));
            textures.Add("opentksquare2", loadImage("opentksquare2.png"));
            textures.Add("opentksquare3", loadImage("opentksquare3.png"));
            textures.Add("opentksquare4", renderer.Texture);

            // Load shader
            shader = new ShaderProgram("sprite.vert", "sprite.frag", true);
            GL.UseProgram(shader.ProgramID);

            GL.GenBuffers(1, out ibo_elements);

            // Enable blending based on the texture alpha
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            for (int i = 0; i < 500; i++)
            {
                sprites.Add(newSprite(i, 0, 1, r.Next(500), Sprite.SpriteType.GRAPH));
            }
            Sprite s1 = new Sprite(renderer.Texture, 50, 50, shader, Sprite.SpriteType.MISC);
            s1.Position = new Vector2(500, 500);
            //s1.Size = new SizeF(1f, 1f);
            //s1.Rotation = 0f;
            sprites.Add(s1);
        }
        public Sprite newSprite(float x, float y, int width, int height, int texture)
        {
            Sprite ret = newSprite(x, y, width, height, Sprite.SpriteType.MISC);
            ret.TextureID = texture;
            ret.Shader = shader;
            return ret;
        }
        public Sprite newSprite(float x, float y, int width, int height, Sprite.SpriteType type)
        {
            Sprite ret = new Sprite(colorPool.GetTexture(Color.OrangeRed), width, height, shader, type);
            ret.Position = new Vector2(x, y);
            return ret;
        }

        public Vector3 convertColor(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (updated)
            {
                base.OnRenderFrame(e);
                GL.Viewport(0, 0, Width, Height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                int offset = 0;

                GL.UseProgram(shader.ProgramID);
                shader.EnableVertexAttribArrays();
                foreach (Sprite s in sprites)
                {
                    if (s.IsVisible)
                    {
                        GL.UseProgram(s.Shader.ProgramID);

                        GL.BindTexture(TextureTarget.Texture2D, s.TextureID);

                        GL.UniformMatrix4(s.Shader.GetUniform("mvp"), false, ref s.ModelViewProjectionMatrix);
                        GL.Uniform1(shader.GetAttribute("mytexture"), s.TextureID);
                        //Console.WriteLine(s.color.Z);
                        GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, offset * sizeof(uint));
                        offset += 6;
                    }
                }

                shader.DisableVertexAttribArrays();

                GL.Flush();
                SwapBuffers();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ortho = Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, -1.0f, 2.0f);
            CurrentView.Size = new SizeF(ClientSize.Width, ClientSize.Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            audioGetter.writeToData = true;
            double[] data = audioGetter.Data.Last();
            audioGetter.writeToData = false;
            int i = 0;
            foreach (Sprite s in sprites)
            {
                if (s.Type != Sprite.SpriteType.GRAPH) continue;
                s.TextureID = colorPool.GetTexture(Utils.HsvToRgb(((double)i / data.Length) * 255, 1, 1));
                if (i >= data.Length) continue;
                s.Size = new Size(1, (int)(data[i]*10d));
                i++;
            }
            audioGetter.writeToData = false;
            KeyboardState keyboardState = OpenTK.Input.Keyboard.GetState();

            // Quit if requested
            if (keyboardState[Key.Escape])
            {
                audioGetter.Stop();
                renderer.Dispose();
                Exit();
            }

            // Move view based on key input
            float moveSpeed = 200.0f * ((keyboardState[Key.ShiftLeft] || keyboardState[Key.ShiftRight]) ? 3.0f : 1.0f); // Hold shift to move 3 times faster!

            // Up-down movement
            if (keyboardState[Key.Up])
            {
                CurrentView.Y += moveSpeed * (float) e.Time;
            }
            else if (keyboardState[Key.Down])
            {
                CurrentView.Y -= moveSpeed * (float) e.Time;
            }

            // Left-right movement
            if (keyboardState[Key.Left])
            {
                CurrentView.X -= moveSpeed * (float) e.Time;
            }
            else if (keyboardState[Key.Right])
            {
                CurrentView.X += moveSpeed * (float) e.Time;
            }

            int viscount = 0;
            // Update graphics
            List<Vector2> verts = new List<Vector2>();
            List<Vector2> texcoords = new List<Vector2>();
            List<int> inds = new List<int>();
            //List<Vector3> colors = new List<Vector3>();

            int vertcount = 0;
                

            // Get data for visible sprites
            foreach (Sprite s in sprites)
            {
                if (s.IsVisible)
                {
                    verts.AddRange(s.GetVertices());
                    texcoords.AddRange(s.GetTexCoords());
                    inds.AddRange(s.GetIndices(vertcount));
                    //colors.Add(s.color);
                    vertcount += 4;
                    viscount++;

                    s.CalculateModelMatrix();
                    s.ModelViewProjectionMatrix = s.ModelMatrix * ortho;
                }
            }

            // Buffer vertex coordinates
            GL.BindBuffer(BufferTarget.ArrayBuffer, shader.GetBuffer("v_coord"));
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(verts.Count * Vector2.SizeInBytes), verts.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.GetAttribute("v_coord"), 2, VertexAttribPointerType.Float, false, 0, 0);

            // Buffer texture coords
            GL.BindBuffer(BufferTarget.ArrayBuffer, shader.GetBuffer("v_texcoord"));
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(texcoords.Count * Vector2.SizeInBytes), texcoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.GetAttribute("v_texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Buffer indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(inds.Count * sizeof(int)), inds.ToArray(), BufferUsageHint.StaticDraw);

            

            updated = true;

            // Display average FPS and sprite statistics in title bar
            avgfps = (avgfps + (1.0f / (float) e.Time)) / 2.0f;
            Title = String.Format("OpenTK Sprite Demo ({0} sprites, {1} drawn, FPS:{2:0.00})", sprites.Count, viscount, avgfps);

            counter++;
            if (counter % 40 == 0)
            {
                renderer.Clear(Color.Transparent);
                renderer.DrawString(Math.Round(avgfps, 1).ToString(), new Font(FontFamily.GenericSansSerif, 11 * 5),Brushes.White,PointF.Empty);
                doNothing(renderer.Texture);
            }
        }
        private void doNothing(Object o) { return; }
        /// <summary>
        /// Loads a texture from a Bitmap
        /// </summary>
        /// <param name="image">Bitmap to make a texture from</param>
        /// <returns>ID of texture, or -1 if there is an error</returns>
        private int loadImage(Bitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        /// <summary>
        /// Overload to make a texture from a filename
        /// </summary>
        /// <param name="filename">File to make a texture from</param>
        /// <returns>ID of texture, or -1 if there is an error</returns>
        private int loadImage(string filename)
        {
            try
            {
                Image file = Image.FromFile(filename);
                return loadImage(new Bitmap(file));
            }
            catch (FileNotFoundException e)
            {
                return -1;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            // Selection example
            // First, find coordinates of mouse in global space
            Vector2 clickPoint = new Vector2(e.X, e.Y);
            clickPoint.X += CurrentView.X;
            clickPoint.Y = ClientSize.Height - clickPoint.Y + CurrentView.Y;

            // Find target Sprite
            Sprite clickedSprite = null;
            foreach (Sprite s in sprites)
            {
                // We can only click on visible Sprites
                if (s.IsVisible)
                {
                    if (s.IsInside(clickPoint))
                    {
                        // We store the last sprite found to get the topmost one (they're searched in the same order they're drawn)
                        clickedSprite = s;
                    }
                }
            }

            // Change the texture on the clicked Sprite
            if (clickedSprite != null)
            {
                if (clickedSprite.TextureID == textures["opentksquare"])
                {
                    clickedSprite.TextureID = textures["opentksquare2"];
                }
                else if (clickedSprite.TextureID == textures["opentksquare2"])
                {
                    clickedSprite.TextureID = textures["opentksquare3"];
                }
                else
                {
                    clickedSprite.TextureID = textures["opentksquare"];
                }
            }
        }
    }
}