using System;
using WWIIRFilterDesign;
using WWUtil;
using WWMath;

namespace WWOfflineResampler {
    class DsfWrite {
        private WWDsfRW.WWDsfWriter mDsfW;

        private LoopFilterCRFB [] mLoopFilters;

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
            DesignLoopFilter(3, metaW.channels);
        }

        /// <summary>
        /// mLoopFiltersを設計する。
        /// </summary>
        /// <param name="order">フィルターの次数(3,5,7)。</param>
        /// <param name="numChannels">音声のチャンネル数。</param>
        private void DesignLoopFilter(int order, int numChannels) {
            //var twoπ = 2.0 * Math.PI;
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
            case 3:
                a[0] = d[1] + d[2] + d[3] + 1;
                a[1] = d[1] - d[3] - g[0] + 2;
                a[2] = d[3] + 1;
                break;
            case 4:
                a[0] = 1 + d[1] + d[2] + d[3] + d[4] - 3 *g[0] - d[1]* g[0] - d[4] * g[0] + g[0]*g[0];
                a[1] = 2 + d[1] - d[3] - 2 *d[4] - g[0] + d[4] *g[0];
                a[2] = 3 + d[1] + d[4] - g[0] - g[1];
                a[3] = 1 - d[4];
                break;
            case 5:
                a[0] = d[1] + d[2] + d[3] + d[4] + d[5] + 1;
                a[1] = 3 - 4 * g[0] + g[0] * g[0] - 2 * d[5] + d[5] * g[0] - d[4] + d[2] + 2 * d[1] - d[1] * g[0];
                a[2] = d[1] + d[4] - d[5] * g[0] + 3 * d[5] - g[0] + 3;
                a[3] = d[1] - d[5] - g[0] - g[1] + 4;
                a[4] = d[5] + 1;
                break;
            case 6:
                a[0] = 1 + d[1] + d[2] + d[3] + d[4] + d[5] + d[6] - 6* g[0] - 3* d[1]* g[0] - d[2]* g[0] - d[5]* g[0] - 3* d[6]* g[0] + 5* g[0]*g[0] + d[1]* g[0]*g[0] + d[6]* g[0]*g[0] - g[0]*g[0]*g[0];
                a[1] = 3 + 2* d[1] + d[2] - d[4] - 2* d[5] - 3* d[6] - 4* g[0] - d[1]* g[0] + d[5]* g[0] + 4* d[6]* g[0] + g[0]*g[0] - d[6]* g[0]*g[0];
                a[2] = 6 + 3* d[1] + d[2] + d[5] + 3* d[6] - 5* g[0] - d[1]* g[0] - d[6]* g[0] + g[0]*g[0] - 5* g[1] -  d[1]* g[1] - d[6]* g[1] + g[0]* g[1] + g[1]*g[1];
                a[3] = 4 + d[1] - d[5] - 4* d[6] - g[0] + d[6]* g[0] - g[1] + d[6]* g[1];
                a[4] = 5 + d[1] + d[6] - g[0] - g[1] - g[2];
                a[5] = 1 - d[6];
                break;
            case 7:
                a[0] = 1 + d[1] + d[2] + d[3] + d[4] + d[5] + d[6] + d[7];
                a[1] = 4 + 3 * d[1] + 2 * d[2] + d[3] - d[5] - 2 * d[6] - 3 * d[7] - 10 * g[0] - 4 * d[1] * g[0] - d[2] * g[0] + d[6] * g[0] + 4 * d[7] * g[0] + 6 * g[0] * g[0] + d[1] * g[0] * g[0] - d[7] * g[0] * g[0] - g[0] * g[0] * g[0];
                a[2] = 6 + 3 *d[1] + d[2] + d[5] + 3* d[6] + 6 *d[7] - 5 *g[0] - d[1]* g[0] - d[6]* g[0] -  5 *d[7]* g[0] + g[0]* g[0] + d[7]*g[0]*g[0];
                a[3] = 10 + 4 *d[1] + d[2] - d[6] - 4 *d[7] - 6 *g[0] - d[1]* g[0] + d[7]* g[0] + g[0]*g[0] - 6* g[1] -  d[1] *g[1] + d[7]* g[1] + g[0]* g[1] + g[1]*g[1];
                a[4] = 5 + d[1] + d[6] + 5 *d[7] - g[0] - d[7]* g[0] - g[1] - d[7]* g[1];
                a[5] = 6 + d[1] - d[7] - g[0] - g[1] - g[2];
                a[6] = 1 + d[7];
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

            mLoopFilters = new LoopFilterCRFB[numChannels];
            for (int ch = 0; ch < numChannels; ++ch) {
                mLoopFilters[ch] = new LoopFilterCRFB(a, b, g);
            }
        }

        public int AddSampleArray(int ch, double [] sampleArray) {
            int rv = 0;

            // 8で割り切れる。
            System.Diagnostics.Debug.Assert((sampleArray.Length & 7) == 0);

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

            mSampleData[ch].pos += sampleArray.Length / 8;

            return rv;
        }

        public int OutputFile(string path) {
            int rv;

            for (int ch=0; ch<mDsfW.NumChannels; ++ch) {
                mDsfW.EncodeAddPcm(ch, mSampleData[ch].sdmData);
            }

            rv = mDsfW.EncodeRun(path);
            mDsfW.EncodeEnd();
            return rv;
        }
    }
}
