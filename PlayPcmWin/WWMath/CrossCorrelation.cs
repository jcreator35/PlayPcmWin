using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWMath {
    public class CrossCorrelation {

        /// <summary>
        /// Circular Cross-Correlation.
        /// when a==b, result is circular autocorrelation.
        /// </summary>
        public static double[] CalcCircularCrossCorrelation(double[] a, double[] b) {
            if (a.Length != b.Length) {
                throw new NotImplementedException("size of array is different");
            }

            int count = a.Length;

            var c = new double[count];

#if true
            Parallel.For(0, count, n => {
#else
            for (int n = 0; n < count; ++n) {
#endif
                double sum = 0;
                for (int k = 0; k < count; ++k) {
                    int aPos = (k + n) % count;
                    sum += b[k] * a[aPos];
                }

                c[n] = sum / (count + 1);
            });



            return c;
        }
    }
}
