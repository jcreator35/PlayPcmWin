using System;
using System.Collections.Generic;
using System.Globalization;

namespace WWAudioFilterCore {
    public class AWeightingFilter : FilterBase {

        private List<BiquadFilter2T> mF = new List<BiquadFilter2T>();
        private double mScale = 0.0;

        public int Fs { get; set; }

        public AWeightingFilter(int fs) : base(FilterType.AWeighting) {
            Fs = fs;
            switch (fs) {
            case 44100:
                mF.Add(new BiquadFilter2T(new double[] {1.000000000000000,  2.000000000000000, 1.000000000000000, 1.000000000000000,  -0.140536082420711,   0.004937597615540}));
                mF.Add(new BiquadFilter2T(new double[] {1.000000000000000, -2.000000000000000, 1.000000000000000, 1.000000000000000,  -1.884901217428792,   0.886421471816167}));
                mF.Add(new BiquadFilter2T(new double[] {1.000000000000000, -2.000000000000000, 1.000000000000000, 1.000000000000000,  -1.994138881266328,   0.994147469444531}));
                mScale = 0.255741125204258;
                break;
            case 48000:
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,   2.000000000000000,   1.000000000000000,   1.000000000000000,  -0.224558458059779,   0.012606625271546}));
                mF.Add(new BiquadFilter2T(new double[] {1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.893870494723070,   0.895159769094661}));
                mF.Add(new BiquadFilter2T(new double[] {1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.994614455993022,   0.994621707014084 }));
                mScale = 0.234301792299513;
                break;
            case 88200:
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,   2.000000000000000,   1.000000000000000,   1.000000000000000,  -0.788728610908997,   0.155523205416609}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.941142662727710,   0.941533948268554}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.997067292014450,   0.997069442208482}));
                mScale = 0.111887636688211;
                break;
            case 96000:
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,   2.000000000000000,   1.000000000000000,   1.000000000000000,  -0.859073102837477,   0.184501649004703}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.945824527367811,   0.946155603519763}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.997305414020089,   0.997307229218489}));
                mScale = 0.099518989759728;
                break;
            case 176400:
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,   2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.286304426932267,   0.413644769686387}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.970231862410663,   0.970331142204023}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.998533108261586,   0.998533646204429}));
                mScale = 0.039452153812125;
                break;
            case 192000:
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,   2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.334646603086623,   0.445320388782665}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.972624833530474,   0.972708737212715}));
                mF.Add(new BiquadFilter2T(new double[] { 1.000000000000000,  -2.000000000000000,   1.000000000000000,   1.000000000000000,  -1.998652253057543,   0.998652707162998}));
                mScale = 0.034332134245487;
                break;
            default:
                throw new ArgumentOutOfRangeException("fs");
            }
        }

        public override FilterBase CreateCopy() {
            return new AWeightingFilter(Fs);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterAWeightingDesc, Fs);
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

            return new AWeightingFilter(fs);
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
