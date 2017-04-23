using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWUtil;
using WWIIRFilterDesign;

namespace WWOfflineResampler {
    class DsfWrite {
        private WWDsfRW.WWDsfWriter mDsfW;

        private IIRFilterGraph [] mIIRFilters;

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

        private SampleData1ch[] mSampleDataOfAllChannels;

        private double[] mDelay;

        public void Setup(WWFlacRWCS.Metadata metaW, byte[] picture) {
            mDsfW = new WWDsfRW.WWDsfWriter();
            mDsfW.EncodeInit(metaW);
            if (picture != null) {
                mDsfW.EncodeSetPicture(picture);
            }

            // サンプルデータ置き場。
            mSampleDataOfAllChannels = new SampleData1ch[metaW.channels];
            for (int ch = 0; ch < metaW.channels; ++ch) {
                mSampleDataOfAllChannels[ch] = new SampleData1ch((metaW.totalSamples + 7) / 8);
            }

            // ノイズシェイピングフィルターを設計する。
            // サンプルレート2.8MHz、20kHz以下を遮断し、100kHz以上を通過するハイパスフィルター。
            int sampleFreq = 44100 * 64;
            int nyquistFreq = sampleFreq/2;
            double fs = 20 * 1000;
            double fc = 100 * 1000;
            double stopBandRippleDb = -120;
            double cutoffGain = -3;

            double twoπ = 2.0 * Math.PI;

            var bilinear = new BilinearDesign(fc, sampleFreq);

            double fc_pw = bilinear.PrewarpωtoΩ(twoπ * (nyquistFreq - fc)) / twoπ;
            double fs_pw = bilinear.PrewarpωtoΩ(twoπ * (nyquistFreq - fs)) / twoπ;

            var afd = new WWAnalogFilterDesign.AnalogFilterDesign();
            afd.DesignLowpass(0, cutoffGain, stopBandRippleDb,
                fc_pw, fs_pw,
                WWAnalogFilterDesign.AnalogFilterDesign.FilterType.InverseChebyshev,
                WWAnalogFilterDesign.ApproximationBase.BetaType.BetaMax);

            // 連続時間伝達関数を離散時間伝達関数に変換。
            for (int i = 0; i < afd.HPfdCount(); ++i) {
                var s = afd.HPfdNth(i);
                bilinear.Add(s);
            }
            // ローパス→ハイパス変換
            bilinear.LowpassToHighpass();
            bilinear.Calc();

            // ノイズシェイピング IIRフィルターを作る。
            mIIRFilters = new IIRFilterParallel[metaW.channels];
            for (int ch = 0; ch < metaW.channels; ++ch) {
                mIIRFilters[ch] = new IIRFilterParallel();
                for (int i = 0; i < bilinear.RealHzCount(); ++i) {
                    var p = bilinear.RealHz(i);
                    Console.WriteLine("{0}", p.ToString("(z)^(-1)"));
                    mIIRFilters[ch].Add(p);
                }
            }

        }



        public int AddSampleArray(int ch, double [] sampleArray) {


            int rv = 0;

            for (int i = 0; i < sampleArray.Length; ++i) {
                // 入力を0.5倍する。

            }

            return rv;
        }

        public int OutputFile(string path) {
            int rv;

            for (int ch=0; ch<mDsfW.NumChannels; ++ch) {
                mDsfW.EncodeAddPcm(ch, mSampleDataOfAllChannels[ch].sdmData);
            }

            rv = mDsfW.EncodeRun(path);
            mDsfW.EncodeEnd();
            return rv;
        }
    }
}
