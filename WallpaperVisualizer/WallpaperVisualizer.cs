using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using NAudio.CoreAudioApi;
using System.Threading;

namespace WallpaperVisualizer
{
    internal class WallpaperVisualizer : GameWindow
    {
        public static WallpaperVisualizer MainWindow = null;
        public RectangleF CurrentView = new RectangleF(0, -37.5f, DisplayDevice.Default.Width, DisplayDevice.Default.Height);

        private int ibo_elements;
        private Dictionary<string, int> textures = new Dictionary<string, int>();
        private List<Sprite> sprites = new List<Sprite>();
        private Sprite songArtwork;
        private Sprite spotifyText;
        private Matrix4 ortho;
        private bool updated = false;
        private Random r = new Random();

        TextRenderer fpsRenderer;
        TextRenderer spotifyRenderer;
        ShaderProgram shader;
        ShaderProgram colorShader;
        MMDevice speakers;
        private int counter;
        private bool hidden;

        static AudioGetter audioGetter;
        static Spotify spotify;
        static TransparentTaskbar tb;

        public int a = 5;
        public int b = 2;
        public int c = 363;
        private const int trimpoint = 50;

        [STAThread]
        public static void Main()
        {
            tb = new TransparentTaskbar();
            audioGetter = new AudioGetter(44100, 15);
            spotify = new Spotify();
            // A bit messy, but just in case. Added in case the desktop switches in some manner in Windows 10's task view
            // and it resets the taskbar attributes. This also happens when opening the start menu.
            Timer TBTimer = new Timer(tb.SetToTransparent, null, 100, 10);
            using (WallpaperVisualizer window = new WallpaperVisualizer())
            {
                tb.SetToTransparent();
                MainWindow = window;
                window.Run(60.0, 60.0);
            }
            TBTimer.Dispose();
            tb.SetToDefault();
        }

        public WallpaperVisualizer()
            : base(DisplayDevice.Default.Width, DisplayDevice.Default.Height, new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(8, 8, 8, 8), 3, 3, 4), "OpenTK Sprite Demo", GameWindowFlags.Fullscreen, DisplayDevice.Default, 4, 0, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
        {
            WallpaperSetter.SetToWallpaper(WindowInfo.Handle);
            //CurrentView.Size = new SizeF(ClientSize.Width, ClientSize.Height);
            ortho = Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, 1.0f, 50.0f);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active))
            {
                if (speakers == null) speakers = device;
                if (!speakers.FriendlyName.Contains("Speakers") && device.FriendlyName.Contains("Speakers")) speakers = device;
                if (device.FriendlyName.Contains("Speakers") && !speakers.FriendlyName.Contains("Realtek")) speakers = device;
            }
            //Console.WriteLine(new MMDeviceEnumerator().GetDevice(audioGetter.waveIn.DeviceNumber.ToString()).AudioEndpointVolume.MasterVolumeLevelScalar);
            fpsRenderer = new TextRenderer(40 * 5, 15 * 5);
            spotifyRenderer = new TextRenderer(400, 50);
            audioGetter.Start();
            GL.ClearColor(Color.CornflowerBlue);
            GL.Viewport(0, 0, Width, Height);

            // Load textures from files
            textures.Add("opentksquare", loadImage("opentksquare.png"));
            textures.Add("opentksquare2", loadImage("opentksquare2.png"));
            textures.Add("opentksquare3", loadImage("opentksquare3.png"));

            // Load shader
            shader = new ShaderProgram("sprite.vert", "sprite.frag", true);
            colorShader = new ShaderProgram("color.vert", "color.frag", true);
            GL.UseProgram(shader.ProgramID);

            GL.GenBuffers(1, out ibo_elements);

            // Enable blending based on the texture alpha
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            double barwidth = CurrentView.Width / 498;
            Console.WriteLine(DisplayDevice.Default.Width + "  " + DisplayDevice.Default.Height);
            Console.WriteLine(CurrentView.Width + "  " + CurrentView.Height + "  " + CurrentView.Width / 498);
            for (short i = 2; i < 500; i++)
            {
                Sprite t = newSprite(Math.Ceiling(i*barwidth)+2, 0, (int)Math.Ceiling(barwidth), 0, Sprite.SpriteType.GRAPH, i);
                t.color = Utils.HsvToRgb(((double)i / 498) * 255, 1, 1);
                sprites.Add(t);
                if (i % 2 == 0)
                {
                    sprites.Add(newSprite(Math.Ceiling(i*barwidth)+2, -32, 2*(int)Math.Ceiling(barwidth), 32, Sprite.SpriteType.TASKBAR, i));
                }
            }
            Sprite s1 = new Sprite(fpsRenderer.Texture, fpsRenderer.width, fpsRenderer.height, shader, Sprite.SpriteType.MISC, -1);
            s1.Position = new Vector2(500, 500);
            spotifyText = new Sprite(spotifyRenderer.Texture, spotifyRenderer.width, spotifyRenderer.height, shader, Sprite.SpriteType.SPOTIFY, 0);
            spotifyText.Position = new Vector2(0, 0);
            songArtwork = new Sprite(GL.GenTexture(), 128, 128, shader, Sprite.SpriteType.SPOTIFY, 1);
            songArtwork.Position = new Vector2(200, 200);
            sprites.Add(s1);
            sprites.Add(spotifyText);
            sprites.Add(songArtwork);
            hidden = false;
        }
        public Sprite newSprite(double x, double y, int width, int height, Sprite.SpriteType type, short name)
        {
            Sprite ret = new Sprite(1, width, height, colorShader, type, name);
            ret.color = new Vector4(1, 1, 1, 1); // white with 100% opacity
            ret.Position = new Vector2((float)x, (float)y);
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
                    if (!hidden || s.Type!=Sprite.SpriteType.GRAPH)
                    {
                        GL.UseProgram(s.Shader.ProgramID);

                        GL.BindTexture(TextureTarget.Texture2D, s.TextureID);

                        GL.UniformMatrix4(s.Shader.GetUniform("mvp"), false, ref s.ModelViewProjectionMatrix);
                        GL.Uniform4(s.Shader.GetUniform("_color"), ref s.color);
                        GL.Uniform1(shader.GetAttribute("mytexture"), s.TextureID);
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
            //CurrentView.Size = new SizeF(ClientSize.Width, ClientSize.Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            double[] data = Utils.Average(Utils.Zip(audioGetter.Data));
            double volume = speakers.AudioEndpointVolume.MasterVolumeLevelScalar;
            double scale = CurrentView.Height / 1000 / 0.5;
            foreach (Sprite s in sprites)
            {
                if (s.Type == Sprite.SpriteType.GRAPH || s.Type == Sprite.SpriteType.TASKBAR)
                {
                    if (s.Name >= data.Length) continue;
                    int height = (int)(data[s.Name] * c * volume * scale);
                    
                    if (s.Type == Sprite.SpriteType.GRAPH && !hidden)
                    {
                        s.Size = new SizeF(s.Size.Width,height);
                    }
                    else if (s.Type==Sprite.SpriteType.TASKBAR)
                    {
                        s.color = Utils.HsvToRgb(((double)s.Name / data.Length) * 255, Math.Min(1,height/(60d*scale)), 0.9d);
                    }
                }
            }
            KeyboardState keyboardState = OpenTK.Input.Keyboard.GetState();

            // Quit if requested
            if (keyboardState[Key.Escape])
            {
                audioGetter.Stop();
                fpsRenderer.Dispose();
                spotifyRenderer.Dispose();
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
            
            // Update graphics
            List<Vector2> verts = new List<Vector2>();
            List<Vector2> texcoords = new List<Vector2>();
            List<int> inds = new List<int>();
            List<Vector4> colors = new List<Vector4>();

            int vertcount = 0;
                

            // Get data for visible sprites
            foreach (Sprite s in sprites)
            {
                if (!hidden || s.Type != Sprite.SpriteType.GRAPH)
                {
                    verts.AddRange(s.GetVertices());
                    texcoords.AddRange(s.GetTexCoords());
                    inds.AddRange(s.GetIndices(vertcount));
                    vertcount += 4;

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

            counter++;
            if (counter % 40 == 0)
            {
                fpsRenderer.Clear(Color.Transparent);
                double fps = Math.Min(60,Math.Round((UpdateFrequency + RenderFrequency) / 2, 1));
                fpsRenderer.DrawString(fps.ToString(), new Font(FontFamily.GenericSansSerif, 11*5),fps>25 ? Brushes.White : Brushes.Red,PointF.Empty);
                if (spotify.newSong)
                {
                    spotifyRenderer.Clear(Color.Transparent);
                    spotifyRenderer.DrawString(String.Format("{0}\n{1}\n{2}", Utils.Trim(spotify.result.track.track_resource.name, trimpoint), Utils.Trim(spotify.result.track.artist_resource.name, trimpoint), Utils.Trim(spotify.result.track.album_resource.name, trimpoint)), new Font(FontFamily.GenericSansSerif, 11), Brushes.White, PointF.Empty);
                    loadImage(spotify.artwork, songArtwork.TextureID);

                    // 25 is the space between the 2 objects.
                    int x = (int)((CurrentView.Width - (spotifyRenderer.text_size.Width + songArtwork.Size.Width + 25)) / 2);
                    songArtwork.Position = new Vector2(x, (CurrentView.Height/2) - songArtwork.Size.Height / 2);
                    spotifyText.Position = new Vector2(CurrentView.Width - x - spotifyRenderer.text_size.Width, (CurrentView.Height / 2) - spotifyRenderer.text_size.Height / 2);
                    spotify.newSong = false;
                }
                doNothing(spotifyRenderer.Texture);
                doNothing(fpsRenderer.Texture);
            }
        }
        private void doNothing(Object o) { return; }
        /// <summary>
        /// Loads a texture from a Bitmap
        /// </summary>
        /// <param name="image">Bitmap to make a texture from</param>
        /// <param name="texID">Texture ID to set</param>
        /// <returns>ID of texture, or -1 if there is an error</returns>
        private int loadImage(Bitmap image, int texID)
        {
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
                return loadImage(new Bitmap(file), GL.GenTexture());
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
        }

        public int loadImage(Bitmap bitmap)
        {
            return loadImage(bitmap, GL.GenTexture());
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
                if (!hidden || s.Type != Sprite.SpriteType.GRAPH)
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

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                a++;
            }
            else if (e.Key == Key.A)
            {
                a--;
            }
            if (e.Key == Key.W)
            {
                b++;
            }
            else if (e.Key == Key.S)
            {
                b--;
            }
            if (e.Key == Key.E)
            {
                c++;
            }
            else if (e.Key == Key.D)
            {
                c--;
            }
            Console.WriteLine("A: " + a + " B: " + b + " C: " + c);
        }
    }
}