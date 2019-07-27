using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWMath {
    /// <summary>
    /// https://en.wikipedia.org/wiki/Discrete_sine_transform
    /// </summary>
    public class DiscreteSineTransform {
        public double[] ForwardDST1(double[] x) {
            int N = x.Length;
            var f = new double[N];

            // Xk = Σ_{n=0}^{N-1}{x_nSin{(π(n+1)(k+1)/(N+1)}}
            // k=0...N-1
            //
            // N=3のとき
            // 入力: [x0 x1 x2]
            // 出力: [X0 X1 X2]
            // N==3, k==0: X0 = x0Sin(π/4)  + x1Sin(2π/4) + x2Sin(3π/4)
            // N==3, k==1: X1 = x0Sin(2π/4) + x1Sin(4π/4) + x2Sin(6π/4)
            // N==3, k==2: X2 = x0Sin(3π/4) + x1Sin(6π/4) + x2Sin(9π/4)

            for (int k = 0; k < N; ++k) {
                f[k] = 0;
                for (int n = 0; n < N; ++n) {
                    f[k] += x[n] * Math.Sin(Math.PI * (n + 1) * (k + 1) / (N + 1));
                }
            }

            return f;
        }

#if false
        public DiscreteSineTransform () {
            int N = 7;
            var x = new double[N];
            for (int i=0; i<N; ++i) {
                x[i] = Math.Sin(Math.PI * (i + 1) / (N + 1));
            }

            var f = ForwardDST1(x);
            for (int i=0; i<N; ++i) {
                f[i] /= (N + 1)/2;
                Console.WriteLine("{0} : {1:0.000}", i, f[i]);
            }
        }
#endif
    }
}
