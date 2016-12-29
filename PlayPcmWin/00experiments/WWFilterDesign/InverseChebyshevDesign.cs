using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWAudioFilter {
    class InverseChebyshevDesign : ApproximationBase {
                private double mε;

        public InverseChebyshevDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
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

            mN = CalcOrder();
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.118
            // calc ε
            switch (bt) {
            case ApproximationBase.BetaType.BetaMax:
                mε = Math.Sqrt(h0 * h0 / hs / hs - 1);
                break;
            case ApproximationBase.BetaType.BetaMin: {
                    // Calc cn(Ωs)
                    var cn = ChebyshevPolynomialCoefficients(mN);
                    double cnΩs = 0;
                    double Ω = 1;
                    for (int i=0; i < cn.Length; ++i) {
                        cnΩs += Ω * cn[i];
                        Ω *= mΩs;
                    }

                    mε = Math.Sqrt(h0 * h0 / hc / hc - 1) / cnΩs;
                }
                break;
            }
        }

        public override int NumOfPoles() {
            return mN;
        }

        public override int NumOfZeroes() {
            return (mN/2)*2;
        }

        public override WWComplex ZeroNth(int nth) {
            if (nth < 0 || mN <= 0 || ((mN / 2)*2) <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.112.
            int k = nth + 1;
            if ( mN / 2 <= nth) {
                k = nth - (mN/2);
            }
            double ρk = Math.Cos((2.0 * k - 1.0) / 2.0 / mN * Math.PI);

            double Ωzk = mΩs / ρk;
            if (mN / 2 <= nth) {
                Ωzk = -Ωzk;
            }

            return new WWComplex(0, Ωzk);
        }

        public override WWComplex PoleNth(int nth) {
            double N = mN;

            if (nth < 0 || mN <= 0 || mN <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.113.
            // σkとΩkはChebyshev Approximationと同じ。
            double σk = Math.Sin(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Sinh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));
            double Ωk = Math.Cos(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Cosh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));

            return new WWComplex(mΩs * σk / (σk * σk + Ωk * Ωk), -mΩs * Ωk / (σk * σk + Ωk * Ωk));
        }

        private double[] Multiply2Ω(double[] v) {
            var rv = new double[v.Length + 1];
            for (int i=0; i < v.Length; ++i) {
                rv[i + 1] = 2.0 * v[i];
            }
            return rv;
        }

        /// <summary>
        /// Chebyshev polynomial coefficients of CN(Ω)
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.60.
        /// </summary>
        /// <param name="n">order of polynomial</param>
        /// <returns>rv[0] : constant coeff, rv[1] 1st order coeff, rv[2] 2nd order coeff ...</returns>
        private double[] ChebyshevPolynomialCoefficients(int n) {
            if (n < 0) {
                throw new ArgumentException("n");
            }

            if (n == 0) {
                return new double[] { 1 };
            }
            if (n == 1) {
                return new double[] { 1, 0 };
            }

            var p = Multiply2Ω(ChebyshevPolynomialCoefficients(n - 1));
            var pp = ChebyshevPolynomialCoefficients(n - 2);
            var rv = new double[n + 1];
            for (int i=0; i <= n; ++i) {
                if (i < pp.Length) {
                    rv[i] = p[i] - pp[i];
                } else {
                    rv[i] = p[i];
                }
            }
            return rv;
        }

        private int CalcOrder() {
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.65.
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.117.
            double numer = WWMath.Functions.ArCosHypPositive(
                    Math.Sqrt((mH0 * mH0 / mHs / mHs - 1)
                        / (mH0 * mH0 / mHc / mHc - 1)));
            double denom = WWMath.Functions.ArCosHypPositive(mΩs);

            return (int)Math.Ceiling(numer /denom);
        }

        /// <summary>
        /// H(s)の定数倍成分を戻す。
        /// </summary>
        public override double TransferFunctionConstant() {
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.112.
            int η = mN & 1;
            double K = mε * Math.Abs(Cη());
            if (η == 1) {
                K *= mΩs;
            } else {
                K /= Math.Sqrt(1.0 + mε * mε * Cη() * Cη());
            }
            return mH0 * K;
        }

        private double Cη() {
            int η= mN&1;
            double rv = 1;
            if (η==1){
                rv = mN;
            }
            if ((((mN + 3 * η)/2) & 1) == 1) {
                rv = -rv;
            }
            return rv;
        }
    }
}
