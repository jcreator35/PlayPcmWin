using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWMath {
    class CrossCorrelation {

        /// <summary>
        /// Cross-correlation.
        /// when a==b, result is autocorrelation.
        /// 
        /// https://en.wikipedia.org/wiki/Cross-correlation
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double[] CalcCrossCorrelation(double[] a, double[] b) {
            var c = new double[b.Length];

            for (int n = 0; n < b.Length; ++n) {
                c[n] = 0;
                for (int m = 0; m < a.Length; ++m) {
                    if (b.Length <= m + n) {
                        break;
                    }
                    c[n] += a[m] * b[m + n];
                }
            }

            return c;
        }

        /// <summary>
        /// Circular Cross-Correlation.
        /// when a==b, result is circular autocorrelation.
        /// </summary>
        public static double[] CalcCircularCrossCorrelation(double[] a, double[] b) {
            var c = new double[b.Length];

#if true
            Parallel.For(0, b.Length, n => {
#else
            for (int n = 0; n < b.Length; ++n) {
#endif
                c[n] = 0;
                for (int m = 0; m < a.Length; ++m) {
                    int bPos = (m + n) % b.Length;
                    c[n] += a[m] * b[bPos];
                }
            });

            return c;
        }
    }
}
