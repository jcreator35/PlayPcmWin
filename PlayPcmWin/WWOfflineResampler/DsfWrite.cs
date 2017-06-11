#define USE_CPP

using System;
using WWIIRFilterDesign;
using WWUtil;
using WWMath;
using WWFilterCppCs;

namespace WWOfflineResampler {
    class DsfWrite {
        private const int FILTER_ORDER = 5;

        private WWDsfRW.WWDsfWriter mDsfW;
#if USE_CPP
        private WWFilterCpp[] mLoopFiltersCpp;
#else
        private LoopFilterCRFB [] mLoopFilters;
#endif

        class SampleData1ch {
            public LargeArray<byte> sdmData;
            public long pos;
            public SampleData1ch(long size) {
                sdmData = new LargeArray<byte>(size);
                pos = 0;

                // 念のため無音をセットする。
                for (long i = 0; i < size; ++i) {
                    sdmData.Set(i, 0x69);
                }
            }
        };

        private SampleData1ch[] mSampleData;

        public void Setup(WWFlacRWCS.Metadata metaW, byte[] picture) {
            mDsfW = new WWDsfRW.WWDsfWriter();
            mDsfW.EncodeInit(metaW);
            if (picture != null) {
                mDsfW.EncodeSetPicture(picture);
            }

            // サンプルデータ置き場。
            mSampleData = new SampleData1ch[metaW.channels];
            for (int ch = 0; ch < metaW.channels; ++ch) {
                mSampleData[ch] = new SampleData1ch((metaW.totalSamples + 7) / 8);
            }

            // ノイズシェイピングフィルターmLoopFiltersを作る。
            DesignLoopFilter(FILTER_ORDER, metaW.channels);
        }

        /// <summary>
        /// mLoopFiltersを設計する。
        /// </summary>
        /// <param name="order">フィルターの次数(3,5,7)。</param>
        /// <param name="numChannels">音声のチャンネル数。</param>
        private void DesignLoopFilter(int order, int numChannels) {
            var ntfHz = new NTFHzcoeffs(order);

            // フィードバック係数g。Hzの分子の零(単位円上)のz=1からの角度 ω → g = 2 - 2cos(ω)
            var g = new double[order / 2];
            for (int i = 0; i < g.Length; ++i) {
                // Hzの分子の零の位置r。
                var r = ntfHz.ZeroNth(i);
                var cosω = r.real;
                g[i] = 2.0 - 2.0 * cosω;
            }

            var d = new double[order+1];
            for (int i=0; i<d.Length;++i) {
                d[i] = ntfHz.D(i);
            }

            // CRFB構造のノイズシェイピングフィルターの係数a[]。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.95,96
            var a = new double[order];
            switch (order) {
            case 2:
                a[0] = d[0] +d[1] -g[0] +1;
                a[1] = 1 - d[0];
                break;
            case 3:
                a[0] = 1 + d[0] + d[1] + d[2];
                a[1] = 2 - d[0] + d[2] - g[0];
                a[2] = 1 + d[0];
                break;
            case 4:
                a[0] = 1 + d[0] + d[1] + d[2] + d[3] - 3* g[0] - d[0]* g[0] - d[3] *g[0] + g[0]*g[0];
                a[1] = 2 - 2* d[0] - d[1] + d[3] - g[0] + d[0]* g[0];
                a[2] = 3 + d[0] + d[3] - g[0] - g[1];
                a[3] = 1 - d[0];
                break;
            case 5:
                a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4];
                a[1] = 3 - 2* d[0] - d[1] + d[3] + 2* d[4] - 4* g[0] + d[0]* g[0] - d[4]* g[0] + g[0]*g[0];
                a[2] = 3 + 3* d[0] + d[1] + d[4] - g[0] - d[0]* g[0];
                a[3] = 4 - d[0] + d[4] - g[0] - g[1];
                a[4] = 1 + d[0];
                break;
            case 6:
                a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4] + d[5] - 6* g[0] - 3* d[0]* g[0] - d[1]* g[0] - d[4]* g[0] - 3* d[5]* g[0] + 5* g[0] * g[0] + d[0] * g[0] * g[0] + d[5]* g[0]*g[0] - g[0]* g[0] * g[0];
                a[1] = 3 - 3* d[0] - 2* d[1] - d[2] + d[4] + 2* d[5] - 4* g[0] + 4* d[0]* g[0] + d[1]* g[0] - d[5]* g[0] + g[0]* g[0] - d[0]* g[0] * g[0];
                a[2] = 6 + 3* d[0] + d[1] + d[4] + 3* d[5] - 5* g[0] - d[0]* g[0] - d[5]* g[0] + g[0]* g[0] - 5* g[1] - d[0]* g[1] - d[5]* g[1] + g[0]* g[1] + g[1]* g[1];
                a[3] = 4 - 4* d[0] - d[1] + d[5] - g[0] + d[0]* g[0] - g[1] + d[0]* g[1];
                a[4] = 5 + d[0] + d[5] - g[0] - g[1] - g[2];
                a[5] = 1 - d[0];
                break;
            case 7:
                a[0] = 1 + d[0] + d[1] + d[2] + d[3] + d[4] + d[5] + d[6];
                a[1] = 4 - 3* d[0] - 2* d[1] - d[2] + d[4] + 2* d[5] + 3* d[6] - 10* g[0] + 4* d[0]* g[0] + d[1]* g[0] - d[5]* g[0] - 4* d[6]* g[0] + 6* g[0] *g[0] - d[0]* g[0]* g[0] + d[6]* g[0] * g[0] - g[0]* g[0]* g[0];
                a[2] = 6 + 6* d[0] + 3* d[1] + d[2] + d[5] + 3* d[6] - 5* g[0] - 5* d[0]* g[0] - d[1]* g[0] - d[6]* g[0] + g[0]* g[0] + d[0]* g[0]* g[0];
                a[3] = 10 - 4* d[0] - d[1] + d[5] + 4* d[6] - 6* g[0] + d[0]* g[0] - d[6]* g[0] + g[0] * g[0] - 6* g[1] + d[0]* g[1] - d[6]* g[1] + g[0]* g[1] + g[1]* g[1];
                a[4] = 5 + 5* d[0] + d[1] + d[6] - g[0] - d[0]* g[0] - g[1] - d[0]* g[1];
                a[5] = 6 - d[0] + d[6] - g[0] - g[1] - g[2];
                a[6] = 1 + d[0];
                break;
            default:
                throw new NotImplementedException();
            }

            // CRFBでSTF==1になるようなb[]の係数。
            var b = new double[order+1];
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

        public int AddSampleArray(int ch, double [] sampleArray) {
            int rv = 0;

            // 8で割り切れる。
            System.Diagnostics.Debug.Assert((sampleArray.Length & 7) == 0);

#if USE_CPP
            var buffOut = new byte[sampleArray.Length/8];
            mLoopFiltersCpp[ch].FilterCrfb(sampleArray.Length, sampleArray, buffOut);
            mSampleData[ch].sdmData.CopyFrom(buffOut, 0, mSampleData[ch].pos, buffOut.Length);
#else
            for (int i = 0; i < sampleArray.Length/8; ++i) {
                byte sdm = 0;

                for (int j = 0; j < 8; ++j) {
                    // 入力を0.5倍して投入(ループフィルターに大体0.5よりも大きいサンプル値を入れると不安定になるので)。
                    int b = mLoopFilters[ch].Filter(0.5 * sampleArray[i*8+j]);
                    if (0 < b) {
                        sdm += (byte)(1 << j);
                    }
                }

                long pos = mSampleData[ch].pos;
                mSampleData[ch].sdmData.Set(pos + i, sdm);
            }
#endif
            mSampleData[ch].pos += sampleArray.Length / 8;

            return rv;
        }

        public int OutputFile(string path) {
            int rv;

            for (int ch=0; ch<mDsfW.NumChannels; ++ch) {
                mDsfW.EncodeAddPcm(ch, mSampleData[ch].sdmData);
            }

            rv = mDsfW.EncodeRun(path);

            // 修了処理。

            mDsfW.EncodeEnd();

#if USE_CPP
            for (int ch = 0; ch < mLoopFiltersCpp.Length; ++ch) {
                mLoopFiltersCpp[ch].Dispose();
            }
            mLoopFiltersCpp = null;
#endif

            return rv;
        }
    }
}
