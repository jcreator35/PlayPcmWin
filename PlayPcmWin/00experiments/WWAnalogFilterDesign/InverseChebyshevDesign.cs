using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWAnalogFilterDesign {
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
            // pp.126のFig 3.17参照。
            // calc ε
            switch (bt) {
            case ApproximationBase.BetaType.BetaMin:
                mε = 1.0 / Math.Sqrt(h0 * h0 / hs / hs - 1);
                break;
            case ApproximationBase.BetaType.BetaMax: {
                    // Calc cn(Ωs)
                    var cn = ChebyshevDesign.ChebyshevPolynomialCoefficients(mN);
                    double cnΩs = 0;
                    double Ω = 1;
                    for (int i=0; i < cn.Length; ++i) {
                        cnΩs += Ω * cn[i];
                        Ω *= mΩs;
                    }

                    mε = 1.0 / cnΩs / Math.Sqrt(h0 * h0 / hc / hc - 1);
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
            // 絶対値の大きいほうから並べる。
            int k;
            if (nth < (mN/2)) {
                k = (mN/2 - 1) - nth;
            } else {
                k = nth - mN / 2;
            }

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.112.
            // Equation 3.30
            double ρk = Math.Cos((2.0 * k + 1) * Math.PI / 2.0 / mN);
            double Ωzk = mΩs / ρk;
            if ((mN / 2) <= nth) {
                // s^2 + Ωzk^2 == 0 has two roots: s=Ωzki and s=-Ωzki
                Ωzk = -Ωzk;
            }
            return new WWComplex(0,Ωzk);
        }

        public override WWComplex PoleNth(int nth) {
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.113.
            // Equation 3.38

            double N = mN;

            if (nth < 0 || mN <= 0 || mN <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            double σk = Math.Sin(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Sinh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));
            double Ωk = Math.Cos(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Cosh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));

            double σki = mΩs * σk / (σk * σk + Ωk * Ωk);
            double Ωki = -mΩs * Ωk / (σk * σk + Ωk * Ωk);

            return new WWComplex(σki, Ωki);
        }

        /// <summary>
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.65.
        /// </summary>
        private int CalcOrder() {
            double numer = WWMath.Functions.ArCosHypPositive(
                    Math.Sqrt((mH0 * mH0 / mHs / mHs - 1)
                        / (mH0 * mH0 / mHc / mHc - 1)));
            double denom = WWMath.Functions.ArCosHypPositive(mΩs);

            return (int)Math.Ceiling(numer /denom);
        }

        /// <summary>
        /// H(s)の定数倍成分を戻す。
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.73.
        /// </summary>
        public override double TransferFunctionConstant() {
            int η=mN & 1;

            double K=mε * Math.Abs(Cη());
            if (η == 1) {
                K *= mΩs;
            } else {
                K /= Math.Sqrt(1.0+mε*mε*Cη()*Cη());
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
