using System;
using System.Text;

namespace WWMath {
    public class RealPolynomial {
        /// <summary>
        /// coef[0]: constant, coef[1] 1st order coef, ...
        /// </summary>
        private readonly double [] mCoeff;

        public RealPolynomial(double [] coeff) {
            mCoeff = new double[coeff.Length];
            Array.Copy(coeff, mCoeff, coeff.Length);
        }

        /// <summary>
        /// coefficient of nth order element
        /// </summary>
        public double C(int nth) {
            return mCoeff[nth];
        }
        public int Order() {
            return mCoeff.Length - 1;
        }
        
        private static bool AlmostZero(double v) {
            return Math.Abs(v) < double.Epsilon;
        }
        
        /// <summary>
        /// 最大次数の係数を0でない状態にした配列を戻す。自分自身は変更しない。
        /// </summary>
        /// <returns>最大次数の係数が0ではない多項式の係数リスト。</returns>
        public RealPolynomial ReduceOrder() {
            // decide order
            int order = 0;
            for (int i = mCoeff.Length - 1; 0 < i; --i) {
                if (!AlmostZero(mCoeff[i])) {
                    order = i;
                    break;
                }
            }

            var r = new double[order + 1];
            Array.Copy(mCoeff, r, order + 1);
            return new RealPolynomial(r);
        }

        /// <summary>
        /// calc derivative of all-real polynomial
        /// </summary>
        /// <returns>derivative</returns>
        public RealPolynomial Derivative() {

            var deriv = new double[mCoeff.Length - 1];
            for (int i = 0; i < deriv.Length; ++i) {
                deriv[i] = (i + 1) * mCoeff[i + 1];
            }
            return new RealPolynomial(deriv);
        }

        /// <summary>
        /// Calculate all-real polynomial value at position x
        /// </summary>
        /// <param name="x">x position</param>
        /// <returns>y</returns>
        public double Evaluate(double x) {
            double y = 0;
            double xPower = 1;
            for (int i = 0; i < mCoeff.Length; ++i) {
                y += mCoeff[i] * xPower;
                xPower *= x;
            }

            return y;
        }
        
        public string ToString(string variableName) {
            var sb = new StringBuilder();
            bool bFirst = true;
            for (int i = mCoeff.Length - 1; 0 <= i; --i) {
                if (!AlmostZero(mCoeff[i])) {
                    if (!bFirst) {
                        sb.Append(" +");
                    } else {
                        bFirst = false;
                    }

                    if (i == 0) {
                        sb.AppendFormat(" {0}", mCoeff[i]);
                    } else if (i == 1) {
                        sb.AppendFormat(" {0} * {1}", mCoeff[i], variableName);
                    } else {
                        sb.AppendFormat(" {0} * {1}^{2}", mCoeff[i], variableName, i);
                    }
                }
            }

            if (bFirst) {
                sb.Append("0");
            }

            return sb.ToString();
        }

        public override string ToString() {
            return ToString("x");
        }
    }
}
