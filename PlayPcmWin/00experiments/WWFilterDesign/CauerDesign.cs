using System;
using WWMath;
using System.Collections.Generic;

namespace WWAudioFilter {
    /// <summary>
    /// Elliptic lowpass filter
    /// </summary>
    public class CauerDesign : ApproximationBase {
        private double mε;
        private double mC;
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
                mHc = hc = CalcHcMax();
                break;
            }

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012.
            // pp.192 Equation 4.82
            double Λ = 1.0 / 2.0 / Math.PI / mN
                * Math.Log((Math.Sqrt(mε * mε + 1) + 1)
                         / (Math.Sqrt(mε * mε + 1) - 1));

            // Equation 4.84
            double σ = Functions.JacobiTheta1h(Λ, Functions.JacobiNomeQ(1.0 / mΩs))
                     / Functions.JacobiTheta0h(Λ, Functions.JacobiNomeQ(1.0 / mΩs));

            int η = mN & 1;
            double k = 1.0/mΩs;

            // 定数Cの計算。
            // Equation 4.93
            if (η == 0) {
                mC = mH0 / Math.Sqrt(1.0 + mε * mε) / Math.Pow(mΩs, (2.0 * mN - 3.0 * η)/2);
            } else {
                mC = mH0 * σ / Math.Pow(mΩs, (2.0 * mN - 3.0 * η)/2);
            }
            for (int m = 1; m <= (mN-η)/2; ++m) {
                double Ωzrm = Ω_ZR(m,k);
                double Ω0m = Ω_0(m,k,σ);
                mC *= Ωzrm * Ωzrm * Ω0m * Ω0m;
            }

            // 分子
            // Equation 4.81
            for (int i = 0; i < mN / 2; ++i) {
                int m = i + 1;
                double b = mΩs / Ω_ZR(m, k);
                mZeroList.Add(new WWComplex(0, -b));
                mZeroList.Insert(0, new WWComplex(0, b));
            }

            // 分母
            if (η == 1) {
                // Equation 4.91
                double Ω_R = σ * Math.Sqrt(mΩs);
                mPoleList.Add(new WWComplex(-Ω_R,0));
            }
            for (int i = 0; i < (mN / 2); ++i) {
                int m = i + 1;
                // Equation 4.90
                double Ω0m = Ω_0(m, k, σ);
                double Q0m = Q_0(m, k, σ);
                double σHm = -Ω0m / 2.0 / Q0m;
                double ΩHm = Ω0m * Math.Sqrt(1.0 - 1.0 / 4.0 / Q0m / Q0m);
                mPoleList.Add(new WWComplex(σHm, ΩHm));
                mPoleList.Insert(0, new WWComplex(σHm, -ΩHm));
            }
        }

        private double Ω_ZR(double m, double k) {
            // Analog Electronic Filters pp.178
            // Equation 4.34

            double z = (2.0 * m - 1) / 2 / mN + 0.5;
            double y = Math.Exp(-Math.PI * Functions.AGM(1, Math.Sqrt(1.0 - k * k)) / Functions.AGM(1, k));

            return 1.0 / Math.Sqrt(k) * Functions.JacobiTheta1(z, y)
                                      / Functions.JacobiTheta0(z,y);
        }

        /// <summary>
        /// Ω_0(m)
        /// </summary>
        private double Ω_0(double m, double k, double σ) {
            // Equation 4.87
            double Ωzrm = Ω_ZR(m,k);
            return Math.Sqrt(mΩs * ( mΩs * σ * σ + Ωzrm * Ωzrm ) / ( mΩs + σ * σ * Ωzrm * Ωzrm));
        }

        /// <summary>
        /// Q_0(m)
        /// </summary>
        private double Q_0(double m, double k, double σ) {
            // Equation 4.89
            double Ωzrm = Ω_ZR(m, k);
            return Ωzrm / 2.0 / σ * Math.Sqrt((σ * σ + Ωzrm * Ωzrm / mΩs) * (σ * σ + mΩs / Ωzrm / Ωzrm)
                / mΩs / (1.0 - Ωzrm * Ωzrm / mΩs / mΩs) / (1.0 - Ωzrm * Ωzrm));
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

        /// <summary>
        /// discremination factor L_N(Ωs)
        /// </summary>
        private static double L_N(double Ωs, int N) {
#if false
            // Analog Electronic Filters pp.178
            // Equation 4.36
            double k = 1.0 / Ωs;
            double numer = Math.Pow(Ωs, N);
            double denom = 1;
            double Kk = Functions.CompleteEllipticIntegralK(k);
            int η = N&1;
            
            double x = Math.Exp(-Math.PI * Functions.AGM(1,Math.Sqrt(1.0-k*k)) / Functions.AGM(1,k));

            for (int m = 1; m <= (N-η) / 2; ++m) {
                denom *= Math.Pow(Functions.EllipticSine(((2.0 * m - 1 + η) / N + 1) * Kk, k), 4);
            }
            return numer / denom;
#else
            // Analog Electronic Filters pp.178
            // Equation 4.37
            int η = N & 1;
            double k = 1.0 / Ωs;
            double r = 1.0;
            if (η == 1) {
                r = 1.0 / k;
            }
            for (int m = 1; m <= ( N - η ) / 2; ++m) {
                double z = (2.0*m-1+η)/2/N+1.0/2.0;
                double y = Math.Exp(-Math.PI * Functions.AGM(1.0, Math.Sqrt(1.0 - k * k))
                                            / Functions.AGM(1.0, k));
                var θ04 = Math.Pow(Functions.JacobiTheta0(z, y), 4);
                var θ14 = Math.Pow(Functions.JacobiTheta1(z, y), 4);
                r *= θ04 / θ14;
            }

            return r;
#endif
        }

        private double CalcHcMax() {
            // Analog Electronic Filters pp.185
            // Equation 4.64
            double lnΩs = L_N(mΩs, mN);
            double r = mH0 / Math.Sqrt(1.0 + ( mH0 * mH0 / mHs / mHs - 1 ) / lnΩs / lnΩs);
            return r;
        }

        private double Calcβmax() {
            // Analog Electronic Filters pp.42 (pp.195)
            return Math.Sqrt(mH0 * mH0 / mHc / mHc - 1.0);
        }

        private double Calcβmin() {
            // Analog Electronic Filters pp.197
            return Math.Sqrt(mH0 * mH0 / mHs / mHs - 1.0) / L_N(mΩs, mN);
        }

        public override int NumOfPoles() {
            return mPoleList.Count;
        }

        public override int NumOfZeroes() {
            return mZeroList.Count;
        }

        public override WWComplex ZeroNth(int nth) {
            if (nth < 0 || mZeroList.Count <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            return mZeroList[nth];
        }

        /// <summary>
        /// H(s)の分母の多項式の根を戻す。
        /// </summary>
        public override WWComplex PoleNth(int nth) {
            if (nth < 0 || mPoleList.Count <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            return mPoleList[nth];
        }

        /// <summary>
        /// H(s)の定数倍成分Cを戻す。
        /// </summary>
        public override double TransferFunctionConstant() {
            return mC;
        }
    };

};
