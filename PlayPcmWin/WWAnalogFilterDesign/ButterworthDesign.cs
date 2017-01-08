using System;
using WWMath;

namespace WWAnalogFilterDesign {
    public class ButterworthDesign : ApproximationBase {
        private double mβ;

        /// <summary>
        /// Calculate the orderPlus1 of Transfer function and βmax from the filter specification.
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.41
        /// </summary>
        /// <param name="h0">gain of 0 Hz</param>
        /// <param name="hc">gain of cutoff frequency</param>
        /// <param name="hs">stopband gain</param>
        /// <param name="ωc">cut off frequency. rad/s</param>
        /// <param name="ωs">stopband frequency. rad/s</param>
        public ButterworthDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
            if (h0 <= 0) {
                throw new System.ArgumentOutOfRangeException("h0");
            }
            if (hc <= 0 || h0 <= hc) {
                throw new System.ArgumentOutOfRangeException("hc");
            }
            if (hs <= 0 || hc <= hs) {
                throw new System.ArgumentOutOfRangeException("hs");
            }
            if (ωs <= ωc) {
                throw new System.ArgumentOutOfRangeException("ωs");
            }

            mH0 = h0;
            mHc = hc;
            mHs = hs;
            mωc = ωc;
            mΩs = ωs / ωc;

            mN = CalcNfMin();

            switch (bt) {
            case ApproximationBase.BetaType.BetaMax:
                mβ = Calcβmax();
                break;
            case ApproximationBase.BetaType.BetaMin:
                mβ = Calcβmin();
                break;
            }
        }

        /// <summary>
        /// ノーマライズド　バターワースフィルターの最小次数 Nfmin。
        /// カットオフ周波数は1 rad/s。ωc rad/sのとき sをωcで割る。
        /// </summary>
        /// <param name="h0">通過域最大ゲイン</param>
        /// <param name="hc">通過域最小ゲイン</param>
        /// <param name="hs">ストップバンド最大ゲイン</param>
        /// <param name="omegaS">ストップバンド周波数 (Hz)</param>
        /// <returns>最小次数</returns>
        private int CalcNfMin() {
            // Analog Electronic Filters pp.44
            double nfmin = Math.Log10(( ( mH0 * mH0 ) / ( mHs * mHs ) - 1.0 ) / ( ( ( mH0 * mH0 ) / ( mHc * mHc ) ) - 1.0 )) / ( 2.0 * Math.Log10(mΩs) );
            return (int)( Math.Ceiling(nfmin) );
        }

        private double Calcβmax() {
            // Analog Electronic Filters pp.42
            return Math.Sqrt(mH0 * mH0 / mHc / mHc - 1.0);
        }

        private double Calcβmin() {
            // Analog Electronic Filters pp.44
            return Math.Sqrt(mH0 * mH0 / mHs / mHs - 1.0) / Math.Pow(mΩs, mN);
        }

        public override int NumOfPoles() {
            return mN;
        }

        public override int NumOfZeroes() {
            return 0;
        }

        /// <summary>
        /// H(s)の分母の多項式の根を戻す。
        /// カットオフ周波数は1 rad/s。ωc Hzのとき sをωcで割る。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.50.
        /// </summary>
        public override WWComplex PoleNth(int nth) {
            if (nth < 0 || mN <= 0 || mN <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // Analog Electronic Filters pp.50
            // sk+
            // H(s) s平面の左半面にある曲。
            double angle = ( 2.0 * nth + 1.0 ) / 2.0 / mN * Math.PI + Math.PI / 2.0;
            double magnitude = Math.Pow(mβ, -1.0 / mN);
            double re = magnitude * Math.Cos(angle);
            double im = magnitude * Math.Sin(angle);
            return new WWComplex(re, im);
        }

        /// <summary>
        /// H(s)の定数倍成分h0/βを戻す。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// </summary>
        public override double TransferFunctionConstant() {
            return mH0 / mβ;
        }
    };

};
