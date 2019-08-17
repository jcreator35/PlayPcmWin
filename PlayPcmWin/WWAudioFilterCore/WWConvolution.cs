using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWMath;

namespace WWAudioFilterCore {
    public class WWConvolution {
        static WWComplex Get(WWComplex[] v, int pos) {
            if (pos < 0 || v.Length <= pos) {
                return WWComplex.Zero();
            }
            return v[pos];
        }

        public WWComplex[] ConvolutionBruteForce(WWComplex[] f, WWComplex[] g) {
            var r = new WWComplex[f.Length + g.Length - 1];

            Parallel.For(0, r.Length, i => {
                WWComplex v = WWComplex.Zero();

                for (int j = 0; j < f.Length; ++j) {
                    int fpos = f.Length - j - 1;
                    int gpos = i + j - (f.Length - 1);
                    v = WWComplex.Add(v, WWComplex.Mul(Get(f, fpos), Get(g, gpos)));
                }
                r[i] = v;
            });

            return r;
        }

        public WWComplex[] ConvolutionFft(WWComplex[] h, WWComplex[] x) {
            var r = new WWComplex[h.Length + x.Length - 1];
            int fftSize = Functions.NextPowerOf2(r.Length);
            var h2 = new WWComplex[fftSize];
            Array.Copy(h, 0, h2, 0, h.Length);
            var x2 = new WWComplex[fftSize];
            Array.Copy(x, 0, x2, 0, x.Length);

            var fft = new WWRadix2Fft(fftSize);
            var H = fft.ForwardFft(h2);
            var X = fft.ForwardFft(x2);

            var Y = WWComplex.Mul(H, X);

            var y = fft.InverseFft(Y);

            Array.Copy(y, 0, r, 0, r.Length);
            return r;
        }

        public WWComplex[] ConvolutionContinuousFft(WWComplex[] h, WWComplex[] x) {
            int fftSize = Functions.NextPowerOf2(h.Length * 4);
            int fragmentSize = fftSize - h.Length+1;

            if (x.Length <= fragmentSize) {
                // 1回のFFTで計算する。
                return ConvolutionFft(h, x);
            }

            var fft = new WWRadix2Fft(fftSize);

            var r = new WWComplex[h.Length + x.Length - 1];

            var h2 = new WWComplex[fftSize];
            Array.Copy(h, 0, h2, 0, h.Length);
            var Hf = fft.ForwardFft(h2);

            // x(n)をfragmentSize要素ごとのデータ列に分解し、
            // それぞれ長さfftSizeになるように末尾に0を追加してx0(n)、x1(n)を得る。
            //Parallel.For(0, x.Length / fragmentSize, i => {
            for (int i=0; i<(x.Length+fragmentSize-1) / fragmentSize; ++i) {
                var xf = new WWComplex[fftSize];
                int count = fragmentSize;
                if (x.Length < (i+1)*fragmentSize) {
                    count = x.Length - i*fragmentSize;
                }
                Array.Copy(x, i * fragmentSize, xf, 0, count);
                var Xf = fft.ForwardFft(xf);
                var Yf = WWComplex.Mul(Hf, Xf);
                var yf = fft.InverseFft(Yf);

                for (int j = 0; j < fftSize; ++j) {
                    if (r.Length <= i * fragmentSize + j) {
                        break;
                    }
                    r[i * fragmentSize + j] = WWComplex.Add(r[i * fragmentSize + j], yf[j]);
                }
            };

            return r;
        }
    }
}
