using System;
using WWMath;

namespace WWAudioFilter {
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
        /// 回路の次数 order
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

        /// <summary>
        /// s平面の左半面にある極の個数。
        /// </summary>
        public int PoleNum() {
            return mN;
        }

        public virtual WWComplex PoleNth(int nth) {
            return new WWComplex(-1.0, 0);
        }

        public virtual WWComplex ZeroNth(int nth) {
            return new WWComplex(0, 0);
        }

        public virtual double TransferFunctionConstant() { return 1; }

    }
}
