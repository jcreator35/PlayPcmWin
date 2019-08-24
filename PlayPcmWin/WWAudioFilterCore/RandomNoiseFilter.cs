using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class RandomNoiseFilter : FilterBase {
        public double NoiseLevelDb { get; set; }
        public double NoiseLevelScale() {
            return Math.Pow(10, NoiseLevelDb / 20.0);
        }

        public enum NoiseTypeEnum {
            TPDF,
            RPDF,
        };
        public NoiseTypeEnum NoiseType {
            get;
            set;
        }

        private Random mRand = new Random();


        public RandomNoiseFilter(NoiseTypeEnum nt, double noiseLevelDb)
                : base(FilterType.RandomNoise) {
            NoiseType = nt;
            NoiseLevelDb = noiseLevelDb;
        }

        public override FilterBase CreateCopy() {
            return new RandomNoiseFilter(NoiseType, NoiseLevelDb);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.RandomNoiseDesc, NoiseType, NoiseLevelDb);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", NoiseType, NoiseLevelDb);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            NoiseTypeEnum nt;
            switch (tokens[1]) {
            case "TPDF":
                nt = NoiseTypeEnum.TPDF;
                break;
            case "RPDF":
                nt = NoiseTypeEnum.RPDF;
                break;
            default:
                return null;
            }


            double noiseLevelDb;
            if (!Double.TryParse(tokens[2], out noiseLevelDb)) {
                return null;
            }

            return new RandomNoiseFilter(nt, noiseLevelDb);
        }

        public double GenRandomNoise() {
            switch (NoiseType) {
            case NoiseTypeEnum.RPDF: {
                    double r = mRand.NextDouble() * 2.0 - 1.0;
                    r *= Math.Sqrt(3) * NoiseLevelScale();
                    return r;
                }
            case NoiseTypeEnum.TPDF: {
                    double r = mRand.NextDouble() - mRand.NextDouble();
                    r *= Math.Sqrt(6) * NoiseLevelScale();
                    return r;
                }
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];
            for (int i=0; i < outPcm.Length; ++i) {
                outPcm[i] = inPcm[i] + GenRandomNoise();
            }
            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
