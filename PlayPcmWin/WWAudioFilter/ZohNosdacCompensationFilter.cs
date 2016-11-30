/* NOSDAC high frequency roll-off compensation filter
 * 
 * NOSDAC frequency response (continuous-time): Hr_zoh(jΩ)=sinc(jΩ/2)
 * Compensation filter frequency response (discrete-time): Hr_comp(ω) = 1 / sinc(ω/2)
 * 
 * FIR filter coefficients calculated by Frequency-Sampling design method:
 * filter taps=M, M is odd
 * G(k) = Hr_comp(2πk/M) * (-1)^k, G(k)=-G(M-k)
 * U=(M-1)/2
 * h(n) = (1/M)*{G(0)+2*Σ_{k=1}^{U}{G(k)*cos{2πk*(n+0.5)/M}}
 * 
 * References:
 * [1] A. V. Oppenheim, R. W. Schafer, Discrete-Time Signal Processing, 3rd Ed, Prentice Hall, 2009, pp. 600-604
 * [2] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
 * [3] Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 702
 */

using System;
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
#if false
            // 畳みこみ和を計算。
            for (int i = 0; i < mCoeffs.Length; ++i) {
                v += mCoeffs[i] * mDelay.GetNthDelayedSampleValue(i);
            }
#else
            // FIRフィルター係数が左右対称なので参考文献[3]の方法で乗算回数を半分に削減できる。
            int center = mCoeffs.Length / 2;
            for (int i = 0; i < center; ++i) {
                v += mCoeffs[i] * (
                    mDelay.GetNthDelayedSampleValue(i) +
                    mDelay.GetNthDelayedSampleValue(mCoeffs.Length-i-1));
            }
            v += mCoeffs[center] * mDelay.GetNthDelayedSampleValue(center);
#endif
            return v;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            double [] outPcm = new double[inPcm.Length];

            for (long i=0; i < outPcm.Length; ++i) {
                mDelay.Filter(inPcm[i]);
                outPcm[i] = Convolution();
            }
            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
