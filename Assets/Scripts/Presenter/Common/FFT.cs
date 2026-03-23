using UnityEngine;

namespace NoteMaker.Common
{

    public struct Complex
    {
        public float re;
        public float im;

        public Complex(float r, float i)
        {
            re = r;
            im = i;
        }

        public float Magnitude()
        {
            return Mathf.Sqrt(re * re + im * im);
        }

        public static Complex operator +(Complex a, Complex b)
            => new Complex(a.re + b.re, a.im + b.im);

        public static Complex operator -(Complex a, Complex b)
            => new Complex(a.re - b.re, a.im - b.im);

        public static Complex operator *(Complex a, Complex b)
            => new Complex(a.re * b.re - a.im * b.im, a.re * b.im + a.im * b.re);
    }

    public static class FFT
    {
        public static void FFTInPlace(Complex[] buffer)
        {
            int n = buffer.Length;
            int bits = (int)Mathf.Log(n, 2);

            // bit-reversal
            for (int j = 1, i = 0; j < n; j++)
            {
                int bit = n >> 1;
                for (; (i & bit) != 0; bit >>= 1)
                    i &= ~bit;
                i |= bit;

                if (j < i)
                {
                    var temp = buffer[j];
                    buffer[j] = buffer[i];
                    buffer[i] = temp;
                }
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                float ang = -2f * Mathf.PI / len;
                Complex wlen = new Complex(Mathf.Cos(ang), Mathf.Sin(ang));

                for (int i = 0; i < n; i += len)
                {
                    Complex w = new Complex(1f, 0f);
                    for (int j = 0; j < len / 2; j++)
                    {
                        Complex u = buffer[i + j];
                        Complex v = buffer[i + j + len / 2] * w;

                        buffer[i + j] = u + v;
                        buffer[i + j + len / 2] = u - v;

                        w = w * wlen;
                    }
                }
            }
        }

        public static void IFFTInPlace(Complex[] buffer)
        {
            int n = buffer.Length;

            for (int i = 0; i < n; i++)
                buffer[i].im = -buffer[i].im;

            FFTInPlace(buffer);

            for (int i = 0; i < n; i++)
            {
                buffer[i].re = buffer[i].re / n;
                buffer[i].im = -buffer[i].im / n;
            }
        }

        public static Complex[] RealFFT(float[] real)
        {
            int n = real.Length;
            Complex[] data = new Complex[n];

            for (int i = 0; i < n; i++)
                data[i] = new Complex(real[i], 0f);

            FFTInPlace(data);

            int bins = n / 2 + 1;
            Complex[] outc = new Complex[bins];
            for (int k = 0; k < bins; k++)
                outc[k] = data[k];

            return outc;
        }
    }
}