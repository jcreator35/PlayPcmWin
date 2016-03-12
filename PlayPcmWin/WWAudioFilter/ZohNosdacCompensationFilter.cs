using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilter {
    class ZohNosdacCompensationFilter : FilterBase {
        public int Taps {
            get;
            set;
        }

        private static readonly double[] mCoeffs9 = {
            0.002211813,-0.008064955,0.020997753,-0.070913171,0.795624617,-0.070913171,0.020997753,-0.008064955,0.002211813};

        private static readonly double[] mCoeffs17 = {
            0.000327547, -0.001039416, 0.001946927, -0.003287828, 0.0055792, -0.010215734, 0.022170167, -0.070146039, 0.770574283,
            -0.070146039, 0.022170167, -0.010215734, 0.0055792, -0.003287828, 0.001946927, -0.001039416, 0.000327547 };

        private static readonly double[] mCoeffs33 = {
            4.44017E-05, -0.000135225, 0.000232335,-0.000340729,0.0004668,-0.000619343,0.000811212,-0.001062254,
            0.001404865,-0.001895252,0.002638312,-0.00384863,0.006022133,-0.010520534,0.022220703,-0.069297152,
            0.756880242,-0.069297152,0.022220703,-0.010520534,0.006022133,-0.00384863,0.002638312,-0.001895252,
            0.001404865,-0.001062254,0.000811212,-0.000619343,0.0004668,-0.000340729,0.000232335,-0.000135225,4.44017E-05};

        private readonly double[] mCoeffs;
        private Delay mDelay;

        public ZohNosdacCompensationFilter(int taps)
                : base(FilterType.ZohNosdacCompensation) {
            Taps = taps;
            switch (taps) {
            case 9:
                mCoeffs = mCoeffs9;
                break;
            case 17:
                mCoeffs = mCoeffs17;
                break;
            case 33:
                mCoeffs = mCoeffs33;
                break;
            default:
                throw new System.ArgumentException("taps");
            }
            mDelay = new Delay(taps);
        }

        public override FilterBase CreateCopy() {
            return new ZohNosdacCompensationFilter(Taps);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterZohNosdacCompensationDesc, Taps);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Taps);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int taps;
            if (!Int32.TryParse(tokens[1], out taps)) {
                return null;
            }

            return new ZohNosdacCompensationFilter(taps);
        }

        public override void FilterStart() {
            base.FilterStart();
            mDelay.FillZeroes();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        private double Convolution() {
            double v = 0.0;
            for (int i = 0; i < mCoeffs.Length; ++i) {
                v += mCoeffs[i] * mDelay.GetNthDelayedSampleValue(i);
            }
            return v;
        }

        public override double[] FilterDo(double[] inPcm) {
            double [] outPcm = new double[inPcm.Length];

            for (long i=0; i < outPcm.Length; ++i) {
                mDelay.Filter(inPcm[i]);
                outPcm[i] = Convolution();
            }
            return outPcm;
        }
    }
}
