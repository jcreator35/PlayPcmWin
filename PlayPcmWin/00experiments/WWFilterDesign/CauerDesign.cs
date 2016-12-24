using System;
using WWMath;
using System.Collections.Generic;

namespace WWAudioFilter {
    /// <summary>
    /// Elliptic lowpass filter
    /// </summary>
    public class CauerDesign : ApproximationBase {
        private double mε;
        private List<WWComplex> mPoleList = new List<WWComplex>();
        private List<WWComplex> mZeroList = new List<WWComplex>();

        /// <summary>
        /// Cauer Elliptic lowpass filter
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.165
        /// </summary>
        /// <param name="h0">gain of 0 Hz</param>
        /// <param name="hc">gain of cutoff frequency</param>
        /// <param name="hs">stopband gain</param>
        /// <param name="ωc">cut off frequency. rad/s</param>
        /// <param name="ωs">stopband frequency. rad/s</param>
        public CauerDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
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
                mε = Calcβmax();
                break;
            case ApproximationBase.BetaType.BetaMin:
                mε = Calcβmin();
                break;
            }

            /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.192
            double Λ = 1.0 / 2.0 / Math.PI / mN
                * Math.Log((Math.Sqrt(mε * mε + 1) + 1)
                         / (Math.Sqrt(mε * mε + 1) - 1));

            double σ = Functions.JacobiTheta1h(Λ, Functions.LnJacobiNomeQ(1.0 / mΩs))
                     / Functions.JacobiTheta0h(Λ, Functions.LnJacobiNomeQ(1.0 / mΩs));


            int η = mN & 1;
            for (int i = 0; i < mN/2; ++i) {
                /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.193
                //double Ω0m = 
                
            }
        }

        /// <summary>
        /// Cauer 楕円フィルターの最小次数 Nfmin。
        /// カットオフ周波数は1 rad/s。ωc rad/sのとき sをωcで割る。
        /// </summary>
        /// <param name="h0">通過域最大ゲイン</param>
        /// <param name="hc">通過域最小ゲイン</param>
        /// <param name="hs">ストップバンド最大ゲイン</param>
        /// <param name="omegaS">ストップバンド周波数 (Hz)</param>
        /// <returns>最小次数</returns>
        private int CalcNfMin() {
            // Analog Electronic Filters pp.182
            double k = 1.0 / mΩs;
            double g = Math.Sqrt((mH0 * mH0 / mHc / mHc - 1.0) / (mH0 * mH0 / mHs / mHs - 1.0));

            // Analog Electronic Filters pp.183
            double nfmin = Functions.LnJacobiNomeQ(g) / Functions.LnJacobiNomeQ(k);
            return (int)( Math.Ceiling(nfmin) );
        }

        private double Calcβmax() {
            // Analog Electronic Filters pp.42 (pp.195)
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
        /// </summary>
        public override WWComplex PoleNth(int nth) {
            if (nth < 0 || mN <= 0 || mN <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// H(s)の定数倍成分h0/βを戻す。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// </summary>
        public override double TransferFunctionConstant() {
            throw new NotImplementedException();
        }
    };

};
