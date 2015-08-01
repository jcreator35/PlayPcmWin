using System;
using System.Globalization;

namespace WWAudioFilter {
    public class UnevenBitDacFilter : FilterBase {

        public double LsbScalingDb { get; set; }

        public UnevenBitDacFilter(double lsbScalingDb)
            : base(FilterType.UnevenBitDac) {

                LsbScalingDb = lsbScalingDb;
        }

        public override FilterBase CreateCopy() {
            return new UnevenBitDacFilter(LsbScalingDb);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterUnevenBitDacDesc,
                LsbScalingDb);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", LsbScalingDb);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double lsbScalingDb;
            if (!Double.TryParse(tokens[1], out lsbScalingDb)) {
                return null;
            }

            return new UnevenBitDacFilter(lsbScalingDb);
        }

        public override double[] FilterDo(double[] inPcm) {

            double scaleLsb = Math.Pow(10, LsbScalingDb / 20.0);

            double maxScaling = 0.0;
            for (int b=0; b<15; ++b) {
                double scale = 1.0 * b / 14.0 + scaleLsb * (14-b)/14.0;
                maxScaling += scale * (1 << b);
            }

            var outPcm = new double[inPcm.Length];

            for (long i = 0; i < outPcm.Length; ++i) {
                double d = inPcm[i] * (Int16.MaxValue+1);
                bool plus = true;
                int v;

                if (Int16.MaxValue < d) {
                    v = Int16.MaxValue;
                } else if (d < Int16.MinValue+1) {
                    v = Int16.MinValue+1;
                } else {
                    v = (int)d;
                }

                if (v < 0) {
                    v = -v;
                    plus = false;
                }

                double result = 0.0;
                for (int b = 0; b < 15; ++b) {
                    if ((v & (1 << b)) != 0) {
                        double scale = 1.0 * b / 14.0 + scaleLsb * (14 - b) / 14.0;
                        result += (1 << b) * scale;
                    }
                }

                if (!plus) {
                    result = -result;
                }

                result /= maxScaling;

                outPcm[i] = result;
            }

            return outPcm;
         }
    }
}
