using System;
using WWMath;

namespace WWAnalogFilterDesign {
    public class ApproximationBase {
        public enum BetaType {
            BetaMax,
            BetaMin
        };

        protected double mH0;
        protected double mHc;
        protected double mHs;

        /// <summary>
        /// cutoff frequency. rad/s
        /// </summary>
        protected double mωc;

        /// <summary>
        /// (Ωc=1としたときの)正規化ストップバンド周波数. Ωs = ωs/ωc 
        /// </summary>
        protected double mΩs;

        /// <summary>
        /// 回路の次数 orderPlus1
        /// </summary>
        protected int mN;

        /// <summary>
        /// ノーマライズド　バターワースフィルターの最小次数 Nfmin。
        /// </summary>
        public int Order() {
            return mN;
        }

        public virtual int NumOfPoles() {
            return mN;
        }

        public virtual int NumOfZeroes() {
            return 0;
        }


        /// <summary>
        /// カットオフ周波数ωc (rad/s)。
        /// sをこの値で割る(周波数スケーリング)
        /// </summary>
        public double CutoffFrequency() {
            return mωc;
        }

        /// <summary>
        /// カットオフ周波数 (Hz)
        /// </summary>
        public double CutoffFrequencyHz() {
            return mωc / ( 2.0 * Math.PI );
        }

        public virtual WWComplex PoleNth(int nth) {
            return new WWComplex(-1.0, 0);
        }

        public virtual WWComplex ZeroNth(int nth) {
            return new WWComplex(0, 0);
        }

        public virtual double TransferFunctionConstant() { return 1; }

        /// <summary>
        /// Calc concrete value of Transfer function H(s)
        /// </summary>
        /// <param name="s">coordinate on s plane</param>
        /// <returns>transfer function value</returns>
        public WWComplex H(WWComplex s) {
            WWComplex numerator = new WWComplex(TransferFunctionConstant(),0);
            for (int i=0; i<NumOfZeroes(); ++i) {
                var b = ZeroNth(i);
                numerator = WWComplex.Mul(numerator, WWComplex.Sub(s, b));
            }

            WWComplex denominator = WWComplex.Unity();
            for (int i = 0; i < mN; ++i) {
                WWComplex a = PoleNth(i);

                denominator = WWComplex.Mul(denominator, WWComplex.Sub(s, a));
            }

            return WWComplex.Div(numerator, denominator);
        }
    }
}
