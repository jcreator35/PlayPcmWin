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

            // ノイズシェイピングフィルターを設計する。
            // サンプルレート2.8MHz、20kHz以下を遮断し、100kHz以上を通過するハイパスフィルター。
            // オールポールフィルター。
            int sampleFreq = 44100 * 64;
            int nyquistFreq = sampleFreq/2;
            double fs = 20 * 1000;
            double fc = 100 * 1000;
            double stopBandRippleDb = -20;
            double cutoffGain = -3;

            double twoπ = 2.0 * Math.PI;

            var bilinear = new BilinearDesign(fc, sampleFreq);

            double fc_pw = bilinear.PrewarpωtoΩ(twoπ * (nyquistFreq - fc)) / twoπ;
            double fs_pw = bilinear.PrewarpωtoΩ(twoπ * (nyquistFreq - fs)) / twoπ;

            var afd = new WWAnalogFilterDesign.AnalogFilterDesign();
            afd.DesignLowpass(0, cutoffGain, stopBandRippleDb,
                fc_pw, fs_pw,
                WWAnalogFilterDesign.AnalogFilterDesign.FilterType.Butterworth,
                WWAnalogFilterDesign.ApproximationBase.BetaType.BetaMax);

            // 連続時間伝達関数を離散時間伝達関数に変換。
            for (int i = 0; i < afd.HPfdCount(); ++i) {
                var s = afd.HPfdNth(i);
                bilinear.Add(s);
            }
            // ローパス→ハイパス変換
            bilinear.LowpassToHighpass();
            bilinear.Calc();

            // CIFB構造のノイズシェイピングフィルターの係数a[]。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.95,96
            var hz = bilinear.HzCombined();
            int degree = hz.DenomDegree();

            // フィードバック係数g。Hzの分子の ω → g = 2 - 2cos(ω)
            var g = new double[degree / 2];
            for (int i = 0; i < g.Length; ++i) {
                var r = WWComplex.Div(bilinear.HzNth(i).N(0).Minus(), bilinear.HzNth(i).N(1));
                var cosω = r.real;
                g[i] = 2.0 - 2.0 * cosω;
            }

            var a = new double[degree];

            switch (degree) {
            case 2:
                a[0] = hz.D(1) + hz.D(2) - g[0] + 1;
                a[1] = 1 - hz.D(2);
                break;
            default:
                throw new NotImplementedException();
            }

            var b = new double[degree+1];
            for (int i = 0; i < degree; ++i) {
                b[i] = a[i];
            }
            b[degree] = 1.0;

            mLoopFilters = new LoopFilterCRFB[metaW.channels];
            for (int ch=0; ch < metaW.channels; ++ch) {
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
                    // 入力を0.5倍して投入する。
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
