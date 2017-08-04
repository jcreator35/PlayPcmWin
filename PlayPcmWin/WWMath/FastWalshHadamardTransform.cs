using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class FastWalshHadamardTransform {
        private static bool IsPowerOf2(int v) {
            return (v & (v-1)) == 0;
        }

        public static double[] Transform(double[] from) {
            if (!IsPowerOf2(from.Length)) {
                throw new ArgumentException("from length is not power of 2");
            }

            int L = from.Length;

            // Lは2のN乗。
            int N = 0;
            for (int i = L; 1 < i; i /= 2) {
                ++N;
            }

            var m = new Matrix(L, N+1);
            for (int r = 0; r < L; ++r) {
                m.Set(r, 0, from[r]);
            }

            for (int c = 0; c < N; ++c) {
                for (int r = 0; r < L / 2; ++r) {
                    m.Set(r, c + 1, m.At(r*2, c) + m.At(r*2 + 1, c));
                }
                for (int r = 0; r < L / 2; ++r) {
                    m.Set(r+L/2, c + 1, m.At(r * 2, c) - m.At(r * 2 + 1, c));
                }
            }

            var rv = new double[L];
            for (int r = 0; r < L; ++r) {
                rv[r] = m.At(r, N);
            }

            return rv;
        }
    }
}
