using System;
using System.Globalization;
using WWMath;

namespace WWAudioFilterCore {
    public class AddFundamentalsFilter : FilterBase {
        public double Gain { get; set; }
        private int mFftLength;
        private PcmFormat mPcmFormat;
        private WWTimeDependentForwardFourierTransform mFFTfwd;
        private WWTimeDependentInverseFourierTransform mFFTinv;

        public AddFundamentalsFilter(double gain)
            : base(FilterType.AddFundamentals) {
            if (gain < 0) {
                throw new ArgumentOutOfRangeException("gain");
            }

            Gain = gain;
        }

        public override FilterBase CreateCopy() {
            return new AddFundamentalsFilter(Gain);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterAddFundamentalsDesc,
                20.0 * Math.Log10(Gain));
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Gain);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double gain;
            if (!Double.TryParse(tokens[1], out gain) || gain <= Double.Epsilon) {
                return null;
            }

            return new AddFundamentalsFilter(gain);
        }

        public override long NumOfSamplesNeeded() {
            return mFFTfwd.WantSamples;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mPcmFormat = new PcmFormat(inputFormat);

            /* 周波数の精度 = メインローブの幅/2
             * Bartlett窓やHann窓のとき 4π/M ラジアン、M == FFTsize
             * 人間の耳は、超低音域の音程は長3音(=1.26倍)ずれていても気にならないという。
             */
            mFftLength = Functions.NextPowerOf2(mPcmFormat.SampleRate);
            mFFTfwd = new WWTimeDependentForwardFourierTransform(mFftLength,
                WWTimeDependentForwardFourierTransform.WindowType.Hann);
            mFFTinv = new WWTimeDependentInverseFourierTransform(mFftLength);

            return inputFormat;
        }

        public override void FilterStart() {
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var pcmF = mFFTfwd.Process(inPcm);
            if (pcmF.Length == 0) {
                return new WWUtil.LargeArray<double>(0);
            }

            int idx20Hz = (int)(20.0 * mFftLength / mPcmFormat.SampleRate);
            int idx40Hz = (int)(40.0 * mFftLength / mPcmFormat.SampleRate);

            for (int i = 0; i < idx40Hz - idx20Hz; ++i) {
                // 正の周波数
                {
                    var v = pcmF[i + idx40Hz];
                    v = WWComplex.Mul(v, Gain);
                    pcmF[i + idx20Hz] = WWComplex.Add(pcmF[i+idx20Hz], v);
                }

                // 負の周波数
                {
                    var v = pcmF[mFftLength - (i + idx40Hz)];
                    v = WWComplex.Mul(v, Gain);
                    pcmF[mFftLength - (i + idx20Hz)] = WWComplex.Add(pcmF[mFftLength - (i + idx20Hz)], v);
                }
            }

            return new WWUtil.LargeArray<double>(mFFTinv.Process(pcmF));
        }
    }
}
