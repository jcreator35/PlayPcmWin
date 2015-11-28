using System;
using System.Globalization;

namespace WWAudioFilter {
    public class AddFundamentalsFilter : FilterBase {
        public double Gain { get; set; }
        private int mFftLength;
        private OverlappedFft mOverlapSaveFft = null;
        private PcmFormat mPcmFormat;

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
            return mOverlapSaveFft.NumOfSamplesNeeded();
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mPcmFormat = new PcmFormat(inputFormat);
            mFftLength = WWUtil.NextPowerOf2(mPcmFormat.SampleRate / 10);
            mOverlapSaveFft = new OverlappedFft(mFftLength);

            return inputFormat;
        }

        public override void FilterStart() {
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mOverlapSaveFft.Clear();
        }

        public override double[] FilterDo(double[] inPcm) {
            var pcmF = mOverlapSaveFft.ForwardFft(inPcm);

            int idx20Hz = (int)(20.0 * mFftLength / mPcmFormat.SampleRate);
            int idx40Hz = (int)(40.0 * mFftLength / mPcmFormat.SampleRate);

            for (int i = 0; i < idx40Hz - idx20Hz; ++i) {
                // 正の周波数
                {
                    var v = pcmF[i + idx40Hz];
                    v.Mul(Gain);
                    pcmF[i + idx20Hz].Add(v);
                }

                // 負の周波数
                {
                    var v = pcmF[mFftLength - (i + idx40Hz)];
                    v.Mul(Gain);
                    pcmF[mFftLength - (i + idx20Hz)].Add(v);
                }
            }

            return mOverlapSaveFft.InverseFft(pcmF);
        }
    }
}
