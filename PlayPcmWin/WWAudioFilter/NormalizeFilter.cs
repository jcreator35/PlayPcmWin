using System;
using System.Collections.Generic;
using System.Globalization;

namespace WWAudioFilter {
    public class NormalizeFilter : FilterBase {
        public double Magnitude { get; set; }

        private long mNumSamples;
        private WWUtil.LargeArray<double>[] mPcmAllChannels;
        private int mChannelId;

        public NormalizeFilter(double magnitude)
            : base(FilterType.Normalize) {
            if (magnitude < 0) {
                throw new ArgumentOutOfRangeException("amplitude");
            }

            Magnitude = magnitude;
        }

        public override FilterBase CreateCopy() {
            return new NormalizeFilter(Magnitude);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterNormalizeDesc,
                20.0 * Math.Log10(Magnitude));
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Magnitude);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double magnitude;
            if (!Double.TryParse(tokens[1], out magnitude) || magnitude <= Double.Epsilon) {
                return null;
            }

            return new NormalizeFilter(magnitude);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mNumSamples = inputFormat.NumSamples;

            mPcmAllChannels = new WWUtil.LargeArray<double>[inputFormat.NumChannels];
            mChannelId = inputFormat.ChannelId;

            return new PcmFormat(inputFormat);
        }

        public override bool WaitUntilAllChannelDataAvailable() {
            return true;
        }

        private double SearchMaxMagnitude() {
            double maxMagnitude = 0.0;
            foreach (var s in mPcmAllChannels) {
                for (long i = 0; i < s.LongLength; ++i) {
                    double level = Math.Abs(s.At(i));
                    if (maxMagnitude < level) {
                        maxMagnitude = level;
                    }
                }
            }

            if (maxMagnitude < float.Epsilon) {
                maxMagnitude = 1.0f;
            }

            return maxMagnitude;
        }

        public override void SetChannelPcm(int ch, WWUtil.LargeArray<double> inPcm) {
            mPcmAllChannels[ch] = inPcm;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcm) {
            // この処理で出力するチャンネルはmChannelId
            // inPcmは使用しない。

            double maxMagnitude = SearchMaxMagnitude();
            double gain = Magnitude / maxMagnitude;

            var s = mPcmAllChannels[mChannelId];

            var result = new WWUtil.LargeArray<double>(mNumSamples);
            long pos = 0;
            for (long i = 0; i < mNumSamples; ++i) {
                result.Set(pos++, s.At(i) * gain);
                if (mNumSamples <= pos) {
                    break;
                }
            }

            return result;
        }
    }
}
