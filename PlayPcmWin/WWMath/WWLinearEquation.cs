using System;
using System.Linq;
using System.Collections.Generic;

namespace WWMath {
    public class WWLinearEquation {

        /// <summary>
        /// Solve and find f for Ku = f
        /// </summary>
        /// <param name="K">N * N matrix</param>
        /// <param name="f">N elem vertical vector</param>
        /// <returns>u : N elem vertical vector. zero elem vector returns when failed.</returns>
        public static double[] SolveKu_eq_f(WWMatrix K, double[] f) {
            int N = K.Row;
            if (K.Column != N) {
                throw new ArgumentException("K.col != K.row");
            }
            if (f.Length != N) {
                throw new ArgumentException("f.Length != K.row");
            }

            var u = new double[N];

            WWMatrix L;
            WWMatrix P;
            WWMatrix U;
            var r = WWMatrix.LUdecompose2(K, out L, out P, out U);
            if (r != WWMatrix.ResultEnum.Success) {
                return new double[0];
            }

            // Reorder f
            var fr = P.Mul(f);

            var c = new double[N];

            // 前進消去 Lc=fによりcを求める。
            // L c = f
            // L → L (既知)
            // c → c (既知)
            // f → fr (既知)
            for (int i = 0; i < N; ++i) {
                double t = fr[i];
                for (int j = 0; j<i; ++j) {
                    // i: row番号、j:col番号。
                    t -= L.At(i, j) * c[j];
                }
                c[i] = t / L.At(i, i);
            }
            // cが求まった。

            // U u = c
            // U → U (既知)
            // u → u (解)
            // c → c (既知)
            // 後退代入で解uを得る。
            for (int i = N-1; 0<=i; --i) {
                double t = c[i];

                // j=i+1行目より下のu_jは求まっている。
                for (int j = i + 1; j<N ; ++j) {
                    t -= U.At(i,j) * u[j];
                }

                u[i] = t / U.At(i, i);
            }

            return u;
        }
    }
}
