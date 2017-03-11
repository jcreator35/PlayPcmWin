using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class NewtonsMethod {
        /// <summary>
        /// Find one real root of specified real polynomial
        /// </summary>
        /// <param name="coeffs">polynomial coeffs. coef[0]: constant, coef[1] 1st order coeff</param>
        /// <param name="initialX">initial estimate of the root position</param>
        /// <returns>x</returns>
        public static double FindRoot(RealPolynomial rpoly, double initialX, int loopCount) {
            var deriv = rpoly.Derivative();

            double x = initialX;
            for (int i = 0; i < loopCount; ++i) {
                double f = rpoly.Evaluate(x);
                double fPrime = deriv.Evaluate(x);
                x = x - f / fPrime;
            }

            return x;
        }

        public static void Test() {
            double x = FindRoot(new RealPolynomial(new double [] {-1, 0, 1}), 2, 20);
            Console.WriteLine("one of the root of x^2-1=0 exists near {0}", x);
        }
    }
}
