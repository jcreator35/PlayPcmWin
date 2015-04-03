using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWAudioFilter;

namespace WWFirFilterTest3 {
    class Program {
        // 44.1kHz用 1kHz以下を取り出すLPF
        private readonly double[] mLpfTime = new double[] {
            0.005228327, 0.003249754, 0.004192373, 0.005265026,
            0.006468574, 0.007797099, 0.009237486, 0.010779043,
            0.012417001, 0.014132141, 0.01589555, 0.017701121,
            0.019508703, 0.021304869, 0.023059883,0.024747905,
            0.02634363, 0.027823228, 0.029158971, 0.030331066,
            0.031319484, 0.032104039, 0.032676435, 0.033022636,
            0.033138738, 0.033022636, 0.032676435, 0.032104039,
            0.031319484, 0.030331066, 0.029158971, 0.027823228,
            0.02634363, 0.024747905, 0.023059883, 0.021304869,
            0.019508703, 0.017701121, 0.01589555, 0.014132141,
            0.012417001, 0.010779043, 0.009237486, 0.007797099,
            0.006468574, 0.005265026, 0.004192373, 0.003249754,
            0.005228327 };

        private void InspectFilter(double[] coeff, int nFFT) {
            var time = new WWComplex[nFFT];
            for (int i = 0; i < time.Count(); ++i) {
                if (i < coeff.Count()) {
                    time[i] = new WWComplex(coeff[i], 0.0);
                } else {
                    time[i] = new WWComplex(0.0, 0.0);
                }
            }

            var freq = new WWComplex[nFFT];

            var fft = new WWRadix2Fft(nFFT);
            fft.ForwardFft(time, freq);

            for (int i = 0; i < freq.Count()/2; ++i) {
                Console.WriteLine("{0}, {1}, {2}", i, freq[i].Magnitude(), freq[i].Phase());
            }
        }

        private double[] BuildDelayComplementaryFilter(double[] coeff) {
            System.Diagnostics.Debug.Assert((coeff.Count() & 1) == 1);
            int centerPos = (coeff.Count() + 1) / 2;

            var result = new double[coeff.Count()];
            for (int i = 0; i < result.Count(); ++i) {
                if (i == centerPos) {
                    result[i] = 1.0 - coeff[i];
                } else {
                    result[i] = -coeff[i];
                }
            }
            return result;
        }

        private void Run() {
            int nFFT = WWRadix2Fft.NextPowerOf2(mLpfTime.Count());

            nFFT *= 8;

            Console.WriteLine("LPF coeffs");
            for (int i = 0; i < mLpfTime.Count(); ++i) {
                Console.WriteLine("{0}, {1:R}", i, mLpfTime[i]);
            }

            Console.WriteLine("");
            var complementary = BuildDelayComplementaryFilter(mLpfTime);
            Console.WriteLine("Delay complementary HPF coeffs");
            for (int i = 0; i < complementary.Count(); ++i) {
                Console.WriteLine("{0}, {1:R}", i, complementary[i]);
            }

            Console.WriteLine("");
            Console.WriteLine("LPF Freq phase(deg)");
            InspectFilter(mLpfTime, nFFT);

            Console.WriteLine("");
            Console.WriteLine("HPF Freq phase(deg)");
            InspectFilter(complementary, nFFT);
        }

        static void Main(string[] args) {
            var instance = new Program();
            instance.Run();
        }
    }
}
