using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class NewtonsMethod {
        /// <summary>
        /// Find one real root of specified real poly
        /// </summary>
        /// <param name="coeffs">poly coeffs. coef[0]: constant, coef[1] 1st degree coeff</param>
        /// <param name="initialX">initial estimate of the root position</param>
        /// <returns>p</returns>
        public static double FindRoot(RealPolynomial rpoly, double initialX, double kEpsilon, int loopCount) {
            var deriv = rpoly.Derivative();

            double prev = initialX;
            double x = initialX;
            for (int i = 0; i < loopCount; ++i) {
                double f = rpoly.Evaluate(x);
                double fPrime = deriv.Evaluate(x);
                x = x - f / fPrime;

                if (Math.Abs(prev - x) < kEpsilon) {
                    break;
                }

                prev = x;
            }

            return x;
        }

        public static void Test() {
            var p = new RealPolynomial(new double[] { -1, 0, 1 });
            double x = FindRoot(p, 2, 1e-8, 20);
            Console.WriteLine("one of the root of p^2-1=0 exists near {0}", x);
        }
    }
}
