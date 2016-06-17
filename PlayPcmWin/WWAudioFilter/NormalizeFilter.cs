using System;
using System.Collections.Generic;
using System.Globalization;

namespace WWAudioFilter {
    public class NormalizeFilter : FilterBase {
        public double Amplitude { get; set; }

        private long mNumSamples;
        private List<double[]> mSampleList = new List<double[]>();

        public NormalizeFilter(double amplitude)
            : base(FilterType.Normalize) {
            if (amplitude < 0) {
                throw new ArgumentOutOfRangeException("amplitude");
            }

            Amplitude = amplitude;
        }

        public override FilterBase CreateCopy() {
            return new NormalizeFilter(Amplitude);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterNormalizeDesc,
                20.0 * Math.Log10(Amplitude));
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Amplitude);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double amplitude;
            if (!Double.TryParse(tokens[1], out amplitude) || amplitude <= Double.Epsilon) {
                return null;
            }

            return new NormalizeFilter(amplitude);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mNumSamples = inputFormat.NumSamples;
            return new PcmFormat(inputFormat);
        }

        private long StoredSamples() {
            long n = 0;
            foreach (var s in mSampleList) {
                n += s.LongLength;
            }

            return n;
        }

        private double SearchMaxMagnitude() {
            double maxMagnitude = 0.0;
            foreach (var s in mSampleList) {
                for (int i = 0; i < s.Length; ++i) {
                    double level = Math.Abs(s[i]);
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

        public override void FilterStart() {
            base.FilterStart();
            mSampleList.Clear();
        }

        public override void FilterEnd() {
            base.FilterEnd();
            mSampleList.Clear();
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            mSampleList.Add(inPcm);

            if (mNumSamples <= StoredSamples()) {
                double maxMagnitude = SearchMaxMagnitude();

                double gain = Amplitude / maxMagnitude;

                var result = new PcmDataLib.LargeArray<double>(mNumSamples);
                long pos = 0;
                foreach (var s in mSampleList) {
                    for (int i = 0; i < s.Length; ++i) {
                        result.Set(pos++, s[i] * gain);
                        if (mNumSamples <= pos) {
                            break;
                        }
                    }
                    if (mNumSamples <= pos) {
                        break;
                    }
                }

                return result;
            } else {
                return new PcmDataLib.LargeArray<double>(0);
            }
        }
    }
}
