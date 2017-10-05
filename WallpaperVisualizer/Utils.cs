using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WallpaperVisualizer
{
    sealed class Utils
    {
        public sealed class Shaders
        {
            public static Dictionary<string, string> shaders = new Dictionary<string, string>();
            static Shaders()
            {
                shaders.Add("color.frag", @"
#version 330

in vec2 f_texcoord;
in vec4 color;
out vec4 outputColor;

uniform sampler2D mytexture;
 
void main(void) {
	outputColor = color;
}");
                shaders.Add("color.vert", @"
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
}");
                shaders.Add("sprite.frag", @"
#version 330

in vec2 f_texcoord;
out vec4 outputColor;

uniform sampler2D mytexture;
 
void main(void) {
	outputColor = texture2D(mytexture, f_texcoord);
}
");
                shaders.Add("sprite.vert", @"
#version 330

in vec2 v_coord;
in vec2 v_texcoord;

out vec2 f_texcoord;

uniform mat4 mvp;
uniform vec4 _color;
 
void main() {
	gl_Position = mvp * vec4(v_coord,1.0,1.0);
	f_texcoord = v_texcoord;
}
");
            }
        }


        /// <summary>
        /// Convert HSV to RGB
        /// h is from 0-360
        /// s,v values are 0-1
        /// r,g,b values are 0-255
        /// </summary>
        public static Vector4 HsvToRgb(double h, double S, double V)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            return new Vector4(Clamp(R),Clamp(G),Clamp(B),1);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        static float Clamp(double i)
        {
            if (i < 0) return 0;
            if (i > 1) return 1;
            return (float)i;
        }

        public static double[][] Zip(List<double[]> inp)
        {
            if (inp.Count < 1) { return new double[0][]; }
            List<double[]> input;
            try
            {
                input = new List<double[]>(inp);
            }
            catch (ArgumentException)
            {
                input = new List<double[]>();
                for (int i = 0; i < inp.Count;++i)
                {
                    input.Add(inp[i]);
                }
            }
            double[][] ret = new double[input[0].Length][];
            for(int i = 0; i < ret.Length; ++i)
            {
                ret[i] = new double[input.Count];
                for (int j = 0; j < input.Count; ++j)
                {
                    if (input[j] == null) continue;
                    ret[i][j] = input[j][i]*Config.config.display.weight[j];
                }
            }
            return ret;
        }

        public static double[] Average(double[][] input)
        {
            return (from i in input select i.Average()).ToArray();
        }

        public static string Trim(string input, int length)
        {
            if (input.Length > length)
            {
                return input.Substring(0, length) + "...";
            }
            return input;
        }

        public static FontFamily GetFontFamily(string fontname)
        {
            FontFamily[] families = FontFamily.Families;
            foreach (FontFamily family in families)
            {
                if (family.Name == fontname)
                {
                    return family;
                }
            }
            return FontFamily.GenericSansSerif;
        }

        public static Vector2[] GetCircleVerts(int verts)
        {
            Vector2[] output = new Vector2[verts+1];
            output[0] = Vector2.Zero;
            double angle = (Math.PI * 2) / verts;
            for (int i = 0; i < verts; ++i)
            {
                output[i+1] = new Vector2((float)Math.Cos(i * angle), (float)Math.Sin(i * angle));
            }
            return output;
        }

        public static int[] GetCircleTriangles(int verts, int offset = 0)
        {
            int[] output = new int[verts * 3];
            for (int i = 0; i < verts; ++i)
            {
                output[i * 3] = 0 + offset;
                output[i * 3 + 1] = i + 1 + offset;
                output[i * 3 + 2] = verts >= i+2 ? i+2 + offset : 1 + offset;
            }
            return output;
        }

        public static int GetTaskbarHeight()
        {
            return Screen.PrimaryScreen.Bounds.Height-Screen.PrimaryScreen.WorkingArea.Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        private enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder buf, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        public enum DesktopWindow
        {
            ProgMan,
            SHELLDLL_DefViewParent,
            SHELLDLL_DefView,
            SysListView32
        }

        public static IntPtr GetDesktopWindow(DesktopWindow desktopWindow)
        {
            IntPtr _SHELLDLL_DefViewParent = GetShellWindow();
            IntPtr _SHELLDLL_DefView = FindWindowEx(_SHELLDLL_DefViewParent, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (_SHELLDLL_DefView == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    const int maxChars = 256;
                    StringBuilder cn = new StringBuilder(maxChars);
                    string className = "";
                    if (GetClassName(hwnd, cn, maxChars) > 0)
                    {
                        className = cn.ToString();
                    }
                    if (className == "WorkerW")
                    {
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero)
                        {
                            _SHELLDLL_DefViewParent = hwnd;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            return _SHELLDLL_DefViewParent;
        }

        public static bool IsDesktopCovered()
        {
            IntPtr fgwindow = GetForegroundWindow();
            const int maxChars = 256;
            StringBuilder cn = new StringBuilder(maxChars);
            string className = "";
            if (GetClassName(fgwindow, cn, maxChars) > 0)
            {
                className = cn.ToString();
            }
            if (fgwindow == GetShellWindow() || fgwindow == FindWindow("Shell_TrayWnd", null) || fgwindow == WallpaperSetter.workerw || className == "WorkerW")
            {
                return false;
            }
            return true;
        }
    }
}
