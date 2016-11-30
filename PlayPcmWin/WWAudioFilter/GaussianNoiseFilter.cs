using System;
using System.Globalization;

namespace WWAudioFilter {
    public class GaussianNoiseFilter : FilterBase {
        public double NoiseLevelDb { get; set; }
        public double NoiseLevelScale() {
            return Math.Pow(10, NoiseLevelDb / 20.0);
        }

        private GaussianNoiseGenerator mGNG = new GaussianNoiseGenerator();

        public GaussianNoiseFilter(double noiseLevelDb)
                : base(FilterType.GaussianNoise) {
            NoiseLevelDb = noiseLevelDb;
        }

        public override FilterBase CreateCopy() {
            return new GaussianNoiseFilter(NoiseLevelDb);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.GaussianNoiseDesc, NoiseLevelDb);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", NoiseLevelDb);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double noiseLevelDb;
            if (!Double.TryParse(tokens[1], out noiseLevelDb)) {
                return null;
            }

            return new GaussianNoiseFilter(noiseLevelDb);
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];
            for (int i=0; i < outPcm.Length; ++i) {
                outPcm[i] = inPcm[i] + mGNG.NextFloat() * NoiseLevelScale();
            }
            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
