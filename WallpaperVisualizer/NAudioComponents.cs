using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WallpaperVisualizer
{
    // Many of the values in here are personal preference that I just like.
    class AudioGetter
    {
        private WaveInEvent waveIn;
        public List<double[]> Data;
        private int responsiveness;
        private bool running;
        double i__ = 0;


        public AudioGetter(int frequency, int responsiveness)
        {
            this.responsiveness = responsiveness;
            Data = new List<double[]>(responsiveness);
            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0; //I think this is the default input device. I'm not too sure. I may need some testing to figure out which one is the stereo mix.
            waveIn.NumberOfBuffers = 10;
            waveIn.BufferMilliseconds = 10;
            waveIn.WaveFormat = new WaveFormat(frequency, 1);
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_Data);
        }

        public void Start()
        {
            if (running) return;
            running = true;
            waveIn.StartRecording();
        }
        public void Stop()
        {
            if (!running) return;
            running = false;
            waveIn.StopRecording();
        }
        private void waveIn_Data(object sender, WaveInEventArgs e)
        {
            i__ += 1;
            if (!running) return;
            double[] data = new double[e.Buffer.Length];
            for (int i = 0; i < e.Buffer.Length; ++i)
            {
                data[i] = e.Buffer[i] * FastFourierTransform.HannWindow(i, e.Buffer.Length);
            }
            data = FFT.fft(data);
            data = data.Take((int)(e.Buffer.Length / 1.764)).ToArray();
            data = CalcUtil.Smooth(CalcUtil.Smooth(data, WallpaperVisualizer.MainWindow.a), WallpaperVisualizer.MainWindow.b);
            for (int i = 0; i < data.Length; ++i)
            {
                //Looks better in my preference
                data[i] *= FastFourierTransform.HammingWindow(i, data.Length);
            }
            Data.Add(data);
            if (Data.Count > responsiveness)
            {
                Data.RemoveAt(0);
            }
        }
    }
    class FFT
    {
        public static double[] fft(double[] buffer)
        {
            int length = buffer.Length;
            Complex[] data = ToComplex(buffer);
            int size = Log2(length);
            FastFourierTransform.FFT(true, size, data);
            return ToDouble(data);
        }

        private static Complex[] ToComplex(double[] data)
        {
            int length = data.Length;

            var result = new Complex[length];

            Complex z;

            for (int i = 0; i < length; i++)
            {
                z = new Complex();

                z.X = (float)data[i];
                z.Y = 0f;

                result[i] = z;
            }

            return result;
        }

        private static int Log2(int value)
        {
            if (value == 0) throw new InvalidOperationException();
            if (value == 1) return 0;
            int result = 0;
            while (value > 1)
            {
                value >>= 1;
                result++;
            }
            return result;
        }

        private static double[] ToDouble(Complex[] data)
        {
            double[] target = new double[data.Length];
            int length = data.Length;

            for (int i = 0; i < length; i++)
            {
                //Imaginary looks better in my taste
                target[i] = Math.Abs(new System.Numerics.Complex(data[i].X, data[i].Y).Imaginary);
            }
            return target;
        }
    }
    class CalcUtil
    {
        public static double[] Smooth(double[] inp, int box_pts)
        {
            double[] h = new double[box_pts];
            for (int i = 0; i < box_pts; ++i)
                h[i] = 1d / box_pts;
            double[] y = new double[inp.Length];
            for (int i = 0; i < inp.Length; ++i)
            {
                for (int j = 0; j < box_pts; ++j)
                {
                    y[i] += inp[Math.Max(i - j, 0)] * h[j];
                }
            }
            return y;
        }
    }
}
