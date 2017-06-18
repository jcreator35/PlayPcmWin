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
namespace WWIIRFilterDesign {

    public class ZohNosdacCompensation {
        public int Taps {
            get;
            set;
        }

        private static readonly double[] mCoeffs9 = {
        0.002211813,-0.008064955,0.020997753,-0.070913171,0.795624617,-0.070913171,0.020997753,-0.008064955,0.002211813};

        private static readonly double[] mCoeffs17 = {
        0.000327547, -0.001039416, 0.001946927, -0.003287828, 0.0055792, -0.010215734, 0.022170167, -0.070146039, 0.770574283,
        -0.070146039, 0.022170167, -0.010215734, 0.0055792, -0.003287828, 0.001946927, -0.001039416, 0.000327547 };

#if false
        private static readonly double[] mCoeffs33 = {
        4.44017E-05, -0.000135225, 0.000232335,-0.000340729,0.0004668,-0.000619343,0.000811212,-0.001062254,
        0.001404865,-0.001895252,0.002638312,-0.00384863,0.006022133,-0.010520534,0.022220703,-0.069297152,
        0.756880242,-0.069297152,0.022220703,-0.010520534,0.006022133,-0.00384863,0.002638312,-0.001895252,
        0.001404865,-0.001062254,0.000811212,-0.000619343,0.0004668,-0.000340729,0.000232335,-0.000135225,4.44017E-05};
#else
        private static readonly double[] mCoeffs33 = {
        4.46886145464570880e-05,
        -9.20001740228207980e-05,
        1.86758611740503280e-04,
        -2.49328146244324370e-04,
        3.63396389226319080e-04,
        -4.63710851526088670e-04,
        6.22168807820932200e-04,
        -8.06524194476629980e-04,
        1.06714704041711480e-03,
        -1.44810865693034960e-03,
        1.99002976828642050e-03,
        -2.96307235513420830e-03,
        4.53530224066369790e-03,
        -8.17731066323979830e-03,
        1.68891595012301450e-02,
        -5.61516276812759900e-02,
        8.07899332606445640e-01,
        -5.61516276812739910e-02,
        1.68891595012289790e-02,
        -8.17731066323960570e-03,
        4.53530224066378550e-03,
        -2.96307235513304990e-03,
        1.99002976828551240e-03,
        -1.44810865692879460e-03,
        1.06714704041607680e-03,
        -8.06524194476219720e-04,
        6.22168807821357100e-04,
        -4.63710851526716370e-04,
        3.63396389225778280e-04,
        -2.49328146243508350e-04,
        1.86758611740244940e-04,
        -9.20001740224864170e-05,
        4.46886145460032070e-05,};
#endif

        private double[] mCoeffs;
        private DelayReal mDelay;

        public ZohNosdacCompensation(int length) {
            Taps = length;
            switch (length) {
            case 9:
                mCoeffs = new double[9];
                Array.Copy(mCoeffs9, mCoeffs, mCoeffs9.Length);
                break;
            case 17:
                mCoeffs = new double[17];
                Array.Copy(mCoeffs17, mCoeffs, mCoeffs17.Length);
                mCoeffs = mCoeffs17;
                break;
            case 33:
                mCoeffs = new double[33];
                Array.Copy(mCoeffs33, mCoeffs, mCoeffs33.Length);
                /* フィルター係数が、DCゲインが1.0になるようにスケールする。
                 * */
                for (int i = 0; i < mCoeffs.Length; ++i) {
                    mCoeffs[i] *= 1.5405388308838;
                }
                break;
            default:
                throw new System.ArgumentException("length");
            }

            mDelay = new DelayReal(length);
            mDelay.FillZeroes();
        }

        private double Convolution() {
            double v = 0.0;
            // FIRフィルター係数が左右対称なので参考文献[3]の方法で乗算回数を半分に削減できる。
            int center = mCoeffs.Length / 2;
            for (int i = 0; i < center; ++i) {
                v += mCoeffs[i] * (
                    mDelay.GetNth(i) +
                    mDelay.GetNth(mCoeffs.Length - i - 1));
            }
            v += mCoeffs[center] * mDelay.GetNth(center);
            return v;
        }

        public double [] Filter(double [] inPcm) {
             var outPcm = new double[inPcm.Length];

            for (long i = 0; i < outPcm.Length; ++i) {
                mDelay.Filter(inPcm[i]);
                outPcm[i] = Convolution();
            }
            return outPcm;
        }
    }
}
