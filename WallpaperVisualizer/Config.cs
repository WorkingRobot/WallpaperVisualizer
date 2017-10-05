using Newtonsoft.Json;
using System.IO;

namespace WallpaperVisualizer
{
    class Config
    {
        public static Config config { get; private set; }

        public Colors colors { get; private set; }
        public Spotify spotify { get; private set; }
        public Display display { get; private set; }

        public class Colors
        {
            public Color bg;
            public Color text;
            public Color progressbg;
            public Color progressfg;
            public double taskbar;
        }

        public class Spotify
        {
            public int artsize;
            public int artsongspace;
            public int trim;
            public int textsize;
            public string textfont;
            public int progressy;
            public int progressw;
            public int progressh;
        }

        public class Display
        {
            public int[] smoothing;
            public double barmultiplier;
            public int responsiveness;
            public double[] weight;
            public int tbthreshold;
        }

        public struct Color
        {
            public byte r, g, b;
            public Color(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public OpenTK.Vector4 ToVec4()
            {
                return new OpenTK.Vector4(r/255f, g/255f, b/255f, 1);
            }

            public System.Drawing.Color ToColor()
            {
                return System.Drawing.Color.FromArgb(r, g, b);
            }
        }

        public Config()
        {
            colors = new Colors();
            colors.bg = new Color(26,26,26);
            colors.text = new Color(175, 175, 175);
            colors.progressbg = new Color(58, 58, 58);
            colors.progressfg = new Color(42, 238, 42);
            colors.taskbar = 0.5;

            spotify = new Spotify();
            spotify.artsize = 256;
            spotify.artsongspace = 50;
            spotify.trim = 50;
            spotify.textsize = 24;
            spotify.textfont = "Arial";
            spotify.progressy = 225;
            spotify.progressw = 600;
            spotify.progressh = 15;

            display = new Display();
            display.smoothing = new int[] { 5, 2 };
            display.barmultiplier = 0.6;
            display.responsiveness = 15;
            display.weight = new double[] { 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.067 };
            display.tbthreshold = 60;
            config = this;
        }

        public Config(string filename)
        {
            if (File.Exists(filename))
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filename));
            }
            else
            {
                File.WriteAllText(filename, @"
{
    ""colors"": {
        """": ""NOTE: r, g, and b are numbers between 0 and 255."",
        """": ""255 being the strongest color of r/red, g/green, b/blue, respectively."",

        """": ""Color of the background"",
        ""bg"": {
            ""r"": 26,
            ""g"": 26,
            ""b"": 26
        },

        """": ""Color of the text"",
        ""text"": {
            ""r"": 175,
            ""g"": 175,
            ""b"": 175
        },

        """": ""Color of the progress bar that isn't filled"",
        ""progressbg"": {
            ""r"": 58,
            ""g"": 58,
            ""b"": 58
        },

        """": ""Color of the progress bar that is filled"",
        ""progressfg"": {
            ""r"": 42,
            ""g"": 238,
            ""b"": 42
        },

        """": ""A number between 0 and 1. 0 meaning the default color is black. 1 meaning the default color is white."",
        """": ""This technically means the color of your taskbar"",
        ""taskbar"": 0.5
    },
    ""spotify"": {
        """": ""The size of the album thumbnail"",
        ""artsize"": 256,

        """": ""The amount of space (pixels) between the album thumbnail and the song titles."",
        ""artsongspace"": 50,

        """": ""The number of characters a line has until it is trimmed with an ellipses at the end."",
        ""trim"": 50,

        """": ""The font size of your song text"",
        ""textsize"": 24,

        """": ""The font family of your song text. The font must be installed into your computer."",
        ""textfont"": ""Arial"",

        """": ""The location where the progress bar will be located. This many pixels less from the center of the screen."",
        ""progressy"": 225,

        """": ""The width of your progress bar."",
        ""progressw"": 600,

        """": ""The height of your progress bar."",
        ""progressh"": 15
    },
    ""display"": {
        """": ""The amount of smoothing done onto the graph. Both numbers are needed."",
        """": ""The second number is more influential than the first."",
        """": ""NOTE: This also affects the taskbar colors a bit too."",
        ""smoothing"": [5, 2],

        """": ""A number that expresses the height of the bars. The higher the numbers, the higher the bars."",
        """": ""The lower the number, the lower the bars."",
        """": ""NOTE: This also affects the taskbar colors, unless you also change the taskbar threshold value, too."",
        """": ""NOTE: If you change the volume of your computer, this program doesn't adapt to it. The higher the."",
        """": ""volume, the higher the bars. You must edit this value if you want to make it shorter."",
        ""barmultiplier"": 1,

        """": ""How responsive the display is to the current sound."",
        ""responsiveness"": 15,

        """": ""How responsive the display is based on how long ago the sound was."",
        """": ""It must be as long as the responsiveness value. The last value is the most"",
        """": ""current sound being played. The first value is the last remembered sound being played."",
        """": ""The higher the value, the more influential it is. The numbers must sum up to 1."",
        ""weight"": [0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.066, 0.067],
        
        """": ""If the graph reaches higher than this value (pixels), it is 100% of its RGB color."",
        ""tbthreshold"": 60
    }
}");
                new Config();
            }
        }
    }
}
