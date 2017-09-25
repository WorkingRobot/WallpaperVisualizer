using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WallpaperVisualizer
{
    sealed class Utils
    {
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
                    ret[i][j] = input[j][i];
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
    }
}
