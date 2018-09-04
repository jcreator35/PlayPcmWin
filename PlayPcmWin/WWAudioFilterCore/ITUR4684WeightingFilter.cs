// 日本語

using System;
using System.Globalization;
using System.Collections.Generic;

namespace WWAudioFilterCore {
    /// <summary>
    /// ITU-R 468-4 Weighting filter
    /// </summary>
    public class ITUR4684WeightingFilter : FilterBase {
        private List<BiquadFilterD2> mF = new List<BiquadFilterD2>();
        private double mScale = 0.0;

        public int Fs { get; set; }

        public ITUR4684WeightingFilter(int fs)
            : base(FilterType.ITUR4684Weighting) {
            Fs = fs;
            switch (fs) {
            case 44100:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.099834976485079,   0.001823920257866,   1.000000000000000,  -0.911083358669928,   0.362683657347307 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   1.042160175267164,   0.222194326913706,   1.000000000000000,  -0.308892430960038,   0.455521500611870 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.997304085003384,                   0,   1.000000000000000,  -0.568116163853408,                   0 }));
                mScale = 0.758859012967963;
                break;
            case 48000:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.108289462707726,   0.001756771388686,   1.000000000000000,  -0.463364533597581,   0.484818148250297 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   1.135184380803486,   0.285771159057626,   1.000000000000000,  -0.992253016126179,   0.390919149560131 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.997066387585128,                   0,   1.000000000000000,  -0.589228231091391,                   0 }));
                mScale = 0.582156539826359;
                break;
            case 88200:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.620721962256355,   0.025043512506016,   1.000000000000000,  -1.279833956439169,   0.667408900098660 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.840755439675208,   0.367619267149259,   1.000000000000000,  -1.421061544246675,   0.572864142984866 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.998535111992445,                   0,   1.000000000000000,  -0.714305159042408,                   0 }));
                mScale = 0.063520782368164;
                break;
            case 96000:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.681913781315122,   0.071605922966308,   1.000000000000000,  -1.537340720966455,   0.655161070159617 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.344344417618475,   0.557688457809786,   1.000000000000000,  -1.366918966931035,   0.698933274272170 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.964640242912782,   0.184995989542600,   1.000000000000000,  -1.059021785179427,   0.179762335903644 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.999986124561157,                   0,   1.000000000000000,  -0.397603338006891,                   0 }));
                mScale = -0.063834910750527;
                break;
            case 176400:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -1.126926124979187,   0.127562240280719,   1.000000000000000,  -1.708487013765021,   0.818593087642721 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.829932306368341,   0.768567396362318,   1.000000000000000,  -1.718273704867002,   0.761556125637391 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.886188895886803,                   0,   1.000000000000000,  -0.848963684705303,                   0 }));
                mScale = 0.012776459077083;
                break;
            case 192000:
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -1.299190232676342,   0.299716495433575,   1.000000000000000,  -1.737499011662205,   0.774780395938837 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,  -0.744550413015919,   0.674090764681233,   1.000000000000000,  -1.736872039963454,   0.830645748356061 }));
                mF.Add(new BiquadFilterD2(new double[] {1.000000000000000,   0.397233561812466,   0.773093235779680,   1.000000000000000,  -1.180977137295962,   0.277906520569314 }));
                mScale = 0.007156445609637;
                break;
            default:
                throw new ArgumentOutOfRangeException("fs");
            }
        }

        public override FilterBase CreateCopy() {
            return new ITUR4684WeightingFilter(Fs);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterITUR4684WeightingDesc, Fs);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Fs);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int fs;
            if (!Int32.TryParse(tokens[1], out fs)) {
                return null;
            }

            return new ITUR4684WeightingFilter(fs);
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        public override void FilterStart() {
            foreach (var f in mF) {
                f.Reset();
            }
        }

        public override void FilterEnd() {
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var y = new WWUtil.LargeArray<double>(inPcmLA.LongLength);

            for (long pos = 0; pos < inPcmLA.LongLength; ++pos) {
                double x = inPcmLA.At(pos);

                x = mScale * x;

                foreach (var f in mF) {
                    x = f.Filter(x);
                }

                y.Set(pos, x);
            }

            return y;
        }
    }
}
