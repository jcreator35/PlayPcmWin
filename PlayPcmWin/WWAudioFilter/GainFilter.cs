using System;
using System.Globalization;

namespace WWAudioFilter {
    public class GainFilter : FilterBase {
        public double Amplitude { get; set; }

        public GainFilter(double amplitude)
            : base(FilterType.Gain) {
            if (amplitude < 0) {
                throw new ArgumentOutOfRangeException("amplitude");
            }

            Amplitude = amplitude;
        }

        public override FilterBase CreateCopy() {
            return new GainFilter(Amplitude);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterGainDesc,
                Amplitude, 20.0 * Math.Log10(Amplitude));
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

            return new GainFilter(amplitude);
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];
            for (int i=0; i < outPcm.Length; ++i) {
                outPcm[i] = inPcm[i] * Amplitude;
            }
            return new PcmDataLib.LargeArray<double>(outPcm);
        }
    }
}
