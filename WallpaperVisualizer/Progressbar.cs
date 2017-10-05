using OpenTK;

namespace WallpaperVisualizer
{
    class ProgressBar
    {
        Sprite bga;
        Sprite bgb;
        Sprite bgc;
        Sprite fga;
        Sprite fgb;
        Sprite fgc;
        int x, y, width, height;
        int centery { get { return y + (height / 2); } }
        int halfy { get { return height / 2; } }
        double progress { get { return WallpaperVisualizer.spotify.result.playing_position / WallpaperVisualizer.spotify.result.track.length; } }
        public ProgressBar(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            bga = new Circle(-1, height/2, height/2, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -1);
            bga.color = Config.config.colors.progressbg.ToVec4();
            bga.Position = new Vector2(x + halfy, centery);
            bgb = new Circle(-1, height/2, height/2, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -2);
            bgb.color = Config.config.colors.progressbg.ToVec4();
            bgb.Position = new Vector2(x + halfy + width, centery);
            bgc = new Sprite(-1, width, height, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -3);
            bgc.color = Config.config.colors.progressbg.ToVec4();
            bgc.Position = new Vector2(x + halfy, y);

            fga = new Circle(-1, height / 2, height / 2, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -4);
            fga.color = Config.config.colors.progressfg.ToVec4();
            fga.Position = new Vector2(x + halfy, centery);
            fgb = new Circle(-1, height / 2, height / 2, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -5);
            fgb.color = Config.config.colors.progressfg.ToVec4();
            fgb.Position = new Vector2((float)(x + halfy + width*progress), centery);
            fgc = new Sprite(-1, (int)(width*progress), height, WallpaperVisualizer.colorShader, Sprite.SpriteType.SPOTIFY, -6);
            fgc.color = Config.config.colors.progressfg.ToVec4();
            fgc.Position = new Vector2(x + halfy, y);
        }
        public void Hide()
        {
            WallpaperVisualizer.sprites.Remove(bga);
            WallpaperVisualizer.sprites.Remove(bgb);
            WallpaperVisualizer.sprites.Remove(bgc);
            WallpaperVisualizer.sprites.Remove(fga);
            WallpaperVisualizer.sprites.Remove(fgb);
            WallpaperVisualizer.sprites.Remove(fgc);
        }
        public void Show()
        {
            WallpaperVisualizer.sprites.Add(bga);
            WallpaperVisualizer.sprites.Add(bgb);
            WallpaperVisualizer.sprites.Add(bgc);
            WallpaperVisualizer.sprites.Add(fga);
            WallpaperVisualizer.sprites.Add(fgb);
            WallpaperVisualizer.sprites.Add(fgc);
        }
        public void Update()
        {
            fgb.Position = new Vector2((float)(x + halfy + width * progress), centery);
            fgc.Size = new System.Drawing.SizeF((float)(width * progress), height);
        }
    }

    class Circle : Sprite
    {

        public Circle(int textureID, int width, int height, ShaderProgram shader, SpriteType type, short name) : base(textureID, width, height, shader, type, name) {}

        public override int IndiceCount { get { return 180 * 3; } }
        public override int VertCount { get { return 181; } }

        /// <summary>
        /// Gets an array of vertices for the quad of this Sprite
        /// </summary>
        /// <returns></returns>
        public override Vector2[] GetVertices()
        {
            return Utils.GetCircleVerts(180);
        }

        /// <summary>
        /// Gets the indices to draw this Sprite
        /// </summary>
        /// <param name="offset">Value to offset the indice values by (number of verts before this Sprite)</param>
        /// <returns>Array of indices to draw</returns>
        public override int[] GetIndices(int offset = 0)
        {
            return Utils.GetCircleTriangles(180, offset);
        }

        public override Vector2[] GetTexCoords()
        {
            Vector2[] output = new Vector2[180];
            for (int i = 0; i < 180; ++i)
            {
                switch (i % 4)
                {
                    case 0: output[i] = new Vector2(TexRect.Left, TexRect.Bottom);break;
                    case 1: output[i] = new Vector2(TexRect.Left, TexRect.Top); break;
                    case 2: output[i] = new Vector2(TexRect.Right, TexRect.Top); break;
                    case 3: output[i] = new Vector2(TexRect.Right, TexRect.Bottom); break;
                }
            }
            return output;
        }
    }
}
