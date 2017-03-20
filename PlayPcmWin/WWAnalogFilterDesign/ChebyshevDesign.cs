using System;
using WWMath;

namespace WWAnalogFilterDesign {
    public class ChebyshevDesign : ApproximationBase {
        private double mε;

        public ChebyshevDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
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

            // calc ε
            switch (bt) {
            case ApproximationBase.BetaType.BetaMax:
                mε = Math.Sqrt(h0 * h0 / hc / hc - 1);
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

                    mε = Math.Sqrt(h0 * h0 / hs / hs - 1) / cnΩs;
                }
                break;
            }
        }

        public override int NumOfPoles() {
            return mN;
        }

        public override int NumOfZeroes() {
            return 0;
        }

        public override WWComplex PoleNth(int nth) {
            double N = mN;

            if (nth < 0 || mN <= 0 || mN <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.73.
            // sk = σk + jΩk
            double σk = Math.Sin(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Sinh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));
            double Ωk = Math.Cos(( 2.0 * N + 2.0 * nth + 1 ) * Math.PI / 2.0 / N)
                * Math.Cosh(1.0 / N * WWMath.Functions.ArSinHyp(1.0 / mε));
            return new WWComplex(σk, Ωk);
        }

        private static double[] Multiply2Ω(double[] v) {
            var rv = new double[v.Length + 1];
            for (int i = 0; i < v.Length; ++i) {
                rv[i + 1] = 2.0 * v[i];
            }
            return rv;
        }

        /// <summary>
        /// Chebyshev poly coefficients of CN(Ω)
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.60.
        /// </summary>
        /// <param name="n">orderPlus1 of poly</param>
        /// <returns>rv[0] : constant coeff, rv[1] 1st orderPlus1 coeff, rv[2] 2nd orderPlus1 coeff ...</returns>
        public static double[] ChebyshevPolynomialCoefficients(int n) {
            if (n < 0) {
                throw new ArgumentException("n");
            }

            if (n == 0) {
                //                    定数項
                return new double[] { 1 };
            }
            if (n == 1) {
                //                    定数項  1次の項
                return new double[] { 0, 1 };
            }

            var p = Multiply2Ω(ChebyshevPolynomialCoefficients(n - 1));
            var pp = ChebyshevPolynomialCoefficients(n - 2);
            var rv = new double[n + 1];
            for (int i = 0; i <= n; ++i) {
                if (i < pp.Length) {
                    rv[i] = p[i] - pp[i];
                } else {
                    rv[i] = p[i];
                }
            }
            return rv;
        }

        /// <summary>
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.65.
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
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.73.
        /// </summary>
        public override double TransferFunctionConstant() {
            return mH0 / mε / Math.Pow(2.0, mN-1);
        }

        public static void Test() {
            var c0 = ChebyshevPolynomialCoefficients(0);
            var c1 = ChebyshevPolynomialCoefficients(1);
            var c2 = ChebyshevPolynomialCoefficients(2);
            var c3 = ChebyshevPolynomialCoefficients(3);
            var c4 = ChebyshevPolynomialCoefficients(4);
        }
    }
};
