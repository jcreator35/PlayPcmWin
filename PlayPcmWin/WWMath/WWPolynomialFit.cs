namespace WWMath {
    public class WWPolynomialFit {
        public class Result {
            /// <summary>
            /// c.Length : poly order
            /// c[0] : constant value
            /// c[1] : x coefficient
            /// c[2] : x^2 coefficient
            /// ...
            /// y = c[0] + c[1] x + c[2] x^2 + ...
            /// </summary>
            public double [] c;
        };

        /// <summary>
        /// Polynomial function fitting.
        /// </summary>
        /// <param name="knownYs">known y values.</param>
        /// <param name="knownXs">known x values.</param>
        /// <param name="nthOrder">Order of fitting polynomial function.</param>
        /// <returns>List of nth order polynomial function coefficients.</returns>
        public static Result Calc(double[] knownYs, double[] knownXs, int nthOrder) {
            System.Diagnostics.Debug.Assert(knownYs.Length == knownXs.Length);
            System.Diagnostics.Debug.Assert(0 < nthOrder);
            System.Diagnostics.Debug.Assert(nthOrder < knownXs.Length);

            // 2d coordinate data sample count
            int count = knownXs.Length;

            int maxOrderXn = 2 * nthOrder;
            var sumXn = new double[maxOrderXn + 1];
            for (int i = 0; i < count; ++i) {
                double x = knownXs[i];

                for (int h = 0; h <= maxOrderXn; ++h) {
                    sumXn[h] += System.Math.Pow(x, h);
                }
            }

            // Create Vandermonde matrix vm
            var vm = new WWMatrix(nthOrder + 1, nthOrder + 1);
            for (int y = 0; y <= nthOrder; ++y) {
                for (int x = 0; x <= nthOrder; ++x) {
                    int order = (nthOrder - x) + (nthOrder - y);
                    vm.Set(y, x, sumXn[order]);
                }
            }

            var sumXnY = new double[nthOrder + 1];
            for (int i = 0; i < count; ++i) {
                double x = knownXs[i];
                double y = knownYs[i];

                for (int h = 0; h <= nthOrder; ++h) {
                    sumXnY[h] += System.Math.Pow(x, h) * y;
                }
            }

            // 若干わかり難いが、
            // f[0]        = sumXnY[nthOrder]
            // f[nthOrder] = sumXnY[0] なので、
            // fはnthOrder+1要素必要。
            var f = new double[nthOrder + 1];
            for (int i = 0; i <= nthOrder; ++i) {
                f[i] = sumXnY[nthOrder - i];
            }

            // Solve vm u = f to get u
            var u = WWLinearEquation.SolveKu_eq_f(vm, f);

            var r = new Result();
            r.c = new double[nthOrder + 1];
            for (int i = 0; i <= nthOrder; ++i) {
                r.c[i] = u[nthOrder - i];
            }
            return r;
        }
    }
}
