#define USE_CPP

using System;
using WWFilterCppCs;

namespace WWDigitalFilter {

    public class LoopFilter : IDisposable {
#if USE_CPP
        private WWFilterCpp[] mLoopFiltersCpp;
#else
        private LoopFilterCRFB[] mLoopFilters;
#endif
        
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
#if USE_CPP
            for (int ch = 0; ch < mLoopFiltersCpp.Length; ++ch) {
                mLoopFiltersCpp[ch].Dispose();
            }
            mLoopFiltersCpp = null;
#endif
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// mLoopFiltersを設計する。
        /// </summary>
        /// <param name="order">フィルターの次数(3,5,7)。</param>
        /// <param name="numChannels">音声のチャンネル数。</param>
        public void Design(int order, int numChannels) {
            var ntfHz = new NTFHzcoeffs(order);

            // フィードバック係数g。Hzの分子の零(単位円上)のz=1からの角度 ω → g = 2 - 2cos(ω)
            var g = new double[order / 2];
            for (int i = 0; i < g.Length; ++i) {
                // Hzの分子の零の位置r。
                var r = ntfHz.ZeroNth(i);
                var cosω = r.real;
                g[i] = 2.0 - 2.0 * cosω;
            }

            var d = new double[order + 1];
            for (int i = 0; i < d.Length; ++i) {
                d[i] = ntfHz.D(i);
            }

            // CRFB構造のノイズシェイピングフィルターの係数a[]。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.95,96
            var a = new double[order];
            switch (order) {
            case 2:
            a[0] = d[0] + d[1] - g[0] + 1;
            a[1] = 1 - d[0];
            break;
            case 3:
            a[0] = 1 + d[0] + d[1] + d[2];
            a[1] = 2 - d[0] + d[2] - g[0];
            a[2] = 1 + d[0];
            break;
            case 4:
            a[0] = 1 + d[0] + d[1] + d[2] + d[3] - 3 * g[0] - d[0] * g[0] - d[3] * g[0] + g[0] * g[0];
            a[1] = 2 - 2 * d[0] - d[1] + d[3] - g[0] + d[0] * g[0];
            a[2] = 3 + d[0] + d[3] - g[0] - g[1];
            a[3] = 1 - d[0];
            break;
            case 5:
            a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4];
            a[1] = 3 - 2 * d[0] - d[1] + d[3] + 2 * d[4] - 4 * g[0] + d[0] * g[0] - d[4] * g[0] + g[0] * g[0];
            a[2] = 3 + 3 * d[0] + d[1] + d[4] - g[0] - d[0] * g[0];
            a[3] = 4 - d[0] + d[4] - g[0] - g[1];
            a[4] = 1 + d[0];
            break;
            case 6:
            a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4] + d[5] - 6 * g[0] - 3 * d[0] * g[0] - d[1] * g[0] - d[4] * g[0] - 3 * d[5] * g[0] + 5 * g[0] * g[0] + d[0] * g[0] * g[0] + d[5] * g[0] * g[0] - g[0] * g[0] * g[0];
            a[1] = 3 - 3 * d[0] - 2 * d[1] - d[2] + d[4] + 2 * d[5] - 4 * g[0] + 4 * d[0] * g[0] + d[1] * g[0] - d[5] * g[0] + g[0] * g[0] - d[0] * g[0] * g[0];
            a[2] = 6 + 3 * d[0] + d[1] + d[4] + 3 * d[5] - 5 * g[0] - d[0] * g[0] - d[5] * g[0] + g[0] * g[0] - 5 * g[1] - d[0] * g[1] - d[5] * g[1] + g[0] * g[1] + g[1] * g[1];
            a[3] = 4 - 4 * d[0] - d[1] + d[5] - g[0] + d[0] * g[0] - g[1] + d[0] * g[1];
            a[4] = 5 + d[0] + d[5] - g[0] - g[1] - g[2];
            a[5] = 1 - d[0];
            break;
            case 7:
            a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4] + d[5] + d[6];
            a[1] = 4 - 3 * d[0] - 2 * d[1] - d[2] + d[4] + 2 * d[5] + 3 * d[6] - 10 * g[0] + 4 * d[0] * g[0] + d[1] * g[0] - d[5] * g[0] - 4 * d[6] * g[0] + 6 * g[0] * g[0] - d[0] * g[0] * g[0] + d[6] * g[0] * g[0] - g[0] * g[0] * g[0];
            a[2] = 6 + 6 * d[0] + 3 * d[1] + d[2] + d[5] + 3 * d[6] - 5 * g[0] - 5 * d[0] * g[0] - d[1] * g[0] - d[6] * g[0] + g[0] * g[0] + d[0] * g[0] * g[0];
            a[3] = 10 - 4 * d[0] - d[1] + d[5] + 4 * d[6] - 6 * g[0] + d[0] * g[0] - d[6] * g[0] + g[0] * g[0] - 6 * g[1] + d[0] * g[1] - d[6] * g[1] + g[0] * g[1] + g[1] * g[1];
            a[4] = 5 + 5 * d[0] + d[1] + d[6] - g[0] - d[0] * g[0] - g[1] - d[0] * g[1];
            a[5] = 6 - d[0] + d[6] - g[0] - g[1] - g[2];
            a[6] = 1 + d[0];
            break;
            default:
            throw new NotImplementedException();
            }

            // CRFBでSTF==1になるようなb[]の係数。
            var b = new double[order + 1];
            for (int i = 0; i < order; ++i) {
                b[i] = a[i];
            }
            b[order] = 1.0;

            /*
            Console.Write("double [] g = {");
            for (int i = 0; i < g.Length; ++i) {
                Console.Write("{0:R}, ", g[i]);
            }
            Console.WriteLine("};");

            Console.Write("double [] a = {");
            for (int i = 0; i < a.Length; ++i) {
                Console.Write("{0:R}, ", a[i]);
            }
            Console.WriteLine("};");

            Console.Write("double [] b = {");
            for (int i = 0; i < b.Length; ++i) {
                Console.Write("{0:R}, ", b[i]);
            }
            Console.WriteLine("};");

            Console.Write("double [] n = {");
            for (int i = 0; i < order+1; ++i) {
                Console.Write("{0:R}, ", ntfHz.N(i));
            }
            Console.WriteLine("};");

            Console.Write("double [] d = {");
            for (int i = 0; i < d.Length; ++i) {
                Console.Write("{0:R}, ", d[i]);
            }
            Console.WriteLine("};");
            */

#if USE_CPP
            mLoopFiltersCpp = new WWFilterCpp[numChannels];
            for (int ch = 0; ch < numChannels; ++ch) {
                var p = new WWFilterCpp();
                p.BuildCrfb(order, a, b, g, 0.5);
                mLoopFiltersCpp[ch] = p;
            }
#else
            mLoopFilters = new LoopFilterCRFB[numChannels];
            for (int ch = 0; ch < numChannels; ++ch) {
                mLoopFilters[ch] = new LoopFilterCRFB(a, b, g);
            }

#endif
        }

        public byte[] Filter(int ch, double[] sampleArray) {
            var buffOut = new byte[sampleArray.Length / 8];
#if USE_CPP
            mLoopFiltersCpp[ch].FilterCrfb(sampleArray.Length, sampleArray, buffOut);
#else
            for (int i = 0; i < sampleArray.Length / 8; ++i) {
                byte sdm = 0;

                for (int j = 0; j < 8; ++j) {
                    // 入力を0.5倍して投入(ループフィルターに大体0.5よりも大きいサンプル値を入れると不安定になるので)。
                    int b = mLoopFilters[ch].Filter(0.5 * sampleArray[i * 8 + j]);
                    if (0 < b) {
                        sdm += (byte)(1 << j);
                    }
                }

                buffOut[i] = sdm;
            }
#endif
            return buffOut;
        }
    }
}
