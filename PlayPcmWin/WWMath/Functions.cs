// 日本語

using System;

namespace WWMath {
    public class Functions {
        /// <summary>
        /// area cos hyp, Inverse hyperbolic cosine
        /// 双曲線関数coshの逆関数 y=arcosh(p)の正の解。x1つに対してyが正負計2つ対応するがそのうち正のyの値を戻す。
        /// 高木貞治, 解析概論 改訂第3版, 岩波書店, 1983, pp.209
        /// </summary>
        /// <param name="p">xの定義域は 1≤p </param>
        public static double ArCosHypPositive(double x) {
            return Math.Log(x+Math.Sqrt(x*x-1));
        }

        /// <summary>
        /// area cos hyp, Inverse hyperbolic cosine
        /// 双曲線関数coshの逆関数 y=arcosh(p)の負の解。x1つに対してyが正負計2つ対応するがそのうち負のyの値を戻す。
        /// 高木貞治, 解析概論 改訂第3版, 岩波書店, 1983, pp.209
        /// </summary>
        /// <param name="p">xの定義域は 1≤p </param>
        public static double ArCosHypNegative(double x) {
            return -Math.Log(x + Math.Sqrt(x * x - 1));
        }

        /// <summary>
        /// area sin hyp, Inverse hyperbolic sine
        /// 双曲線関数sinhの逆関数 y=arsinh(p)。xとyは1対1の対応。
        /// 高木貞治, 解析概論 改訂第3版, 岩波書店, 1983, pp.209
        /// </summary>
        public static double ArSinHyp(double x) {
            return Math.Log(x+Math.Sqrt(x*x+1));
        }

        /// <summary>
        /// Arithmetic Geometric Mean 算術幾何平均
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. 
        /// pp.170
        /// </summary>
        public static double AGM(double x, double y) {
            int i=0;
            while (1e-15 < Math.Abs(x - y)/x) {  //< この計算打ち切り条件は、テキトウなシミュレーションによってまずまずの精度が得られる事を確かめてある。
                if (Math.Abs(x - y) < float.Epsilon) {
                    return x;
                }

                double am = (x + y) / 2;
                double gm = Math.Sqrt(x * y);

                x = am;
                y = gm;

                ++i;
            }
            //Console.WriteLine("AGM {0}", i);

            return x;
        }

        /// <summary>
        /// Complete Elliptic Integral of first kind K(k) 第1種完全楕円積分
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.170.
        /// </summary>
        public static double CompleteEllipticIntegralK(double k) {
            if (k < 0.0 || 1.0 <= k) {
                throw new ArgumentOutOfRangeException("k");
            }

            // Equation 4.5
            return Math.PI / 2/AGM(1-k, 1+k);
        }

        /// <summary>
        /// Jacobi Nome q(p) (Modular constant)
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.170.
        /// </summary>
        public static double JacobiNomeQ(double x) {
            // Equation 4.6
            return Math.Exp(LnJacobiNomeQ(x));
        }

        /// <summary>
        /// ln(q(p)), q(p) == Jacobi Nome q
        /// </summary>
        public static double LnJacobiNomeQ(double x) {
            // p.170
            // Equation 4.7
            return -Math.PI * AGM(1.0, Math.Sqrt(1.0 - x * x)) / AGM(1.0, x);
        }

        /// <summary>
        /// Jacobi theta function θ0(z,y) テータ関数
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.171.
        /// </summary>
        /// <param name="y"> -1 &lt; y &lt; 1</param>
        public static double JacobiTheta0(double z, double y) {
            if (1.0 < Math.Abs(y)) {
                throw new ArgumentOutOfRangeException("y");
            }

            // Equation 4.10

            double r = 1.0;

            int m = 1;
            int s = -1;
            double rn = 0.0;
            do {
                rn = s * 2.0 * Math.Pow(y, m * m) * Math.Cos(2.0 * m * Math.PI * z);
                r += rn;

                s *= -1;
                ++m;
            } while (float.Epsilon < Math.Abs(rn));  //< 計算打ち切り条件が怪しい。
            return r;
        }

        /// <summary>
        /// Jacobi theta function θ1(z,y) テータ関数
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.171.
        /// </summary>
        /// <param name="y"> -1 &lt; y &lt; 1</param>
        public static double JacobiTheta1(double z, double y) {
            if (1.0 < Math.Abs(y)) {
                throw new ArgumentOutOfRangeException("y");
            }

            // Equation 4.11

            double r = 0.0;

            int m = 0;
            int s = 1;
            double rn = 0.0;
            do {
                rn = 2.0 * Math.Pow(y, 0.25) * s * Math.Pow(y, m * (m+1)) * Math.Sin((m * 2.0 +1.0)* Math.PI * z);
                r += rn;

                s *= -1;
                ++m;
            } while (float.Epsilon < Math.Abs(rn));  //< 計算打ち切り条件が怪しい。
            return r;
        }

        /// <summary>
        /// Elliptic sine sn(u,p)
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.171.
        /// </summary>
        public static double EllipticSine(double u, double x) {
            double kx = CompleteEllipticIntegralK(x);
            double qx = JacobiNomeQ(x);
            return JacobiTheta1(u / 2 / kx, qx) / Math.Sqrt(Math.PI) / JacobiTheta0(u / 2 / kx, qx);
        }

        /// <summary>
        /// Jacobi theta function θ0h(z,y) テータ関数の亜種
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.192.
        /// </summary>
        public static double JacobiTheta0h(double z, double y) {
            if (1.0 < Math.Abs(y)) {
                throw new ArgumentOutOfRangeException("y");
            }

            double r = 1.0;

            int m = 1;
            int s = -1;
            double rn = 0.0;
            do {
                rn = s * 2.0 * Math.Pow(y, m * m) * Math.Cosh(2.0 * m * Math.PI * z);
                r += rn;

                s *= -1;
                ++m;
            } while (double.Epsilon < Math.Abs(rn));  //< 計算打ち切り条件が怪しい。
            return r;
        }

        /// <summary>
        /// Jacobi theta function θ1h(z,y) テータ関数の亜種
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design and synthesis, Springer, 2012. pp.192.
        /// </summary>
        /// <param name="y"> -1 &lt; y &lt; 1</param>
        public static double JacobiTheta1h(double z, double y) {
            if (1.0 < Math.Abs(y)) {
                throw new ArgumentOutOfRangeException("y");
            }

            double r = 0.0;

            int m = 0;
            int s = 1;
            double rn = 0.0;
            do {
                rn = 2.0 * Math.Pow(y, 0.25) * s * Math.Pow(y, m * (m + 1)) * Math.Sinh((m * 2.0 + 1.0) * Math.PI * z);
                r += rn;

                s *= -1;
                ++m;
            } while (double.Epsilon < Math.Abs(rn));  //< 計算打ち切り条件が怪しい。
            return r;
        }

        /// <summary>
        /// 階乗 n!
        /// </summary>
        public static long Factorial(int n) {
            if (n < 0 || 20 < n) {
                throw new ArgumentOutOfRangeException("n");
            }

            long rv = 1;
            for (int i = 2; i <= n; ++i) {
                rv = rv * i;
            }

            return rv;
        }

        /// <summary>
        /// 2つの数の最小公倍数を戻す。
        /// </summary>
        public static long LCM(int a, int b) {
            return Math.Abs(((long)a * b)) / GCD(a, b);
        }

        /// <summary>
        /// 2つの数の最大公約数を戻す。 Euclidean algorithm
        /// </summary>
        public static int GCD(int a, int b) {
            if (a < 0 || b < 0) {
                throw new NotImplementedException();
            }

            if (a < b) {
                // swap a and b to be a > b
                int tmp = a;
                a = b;
                b = tmp;
            }

            while (b != 0) {
                int tmp = a % b;
                a = b;
                b = tmp;
            }
            return a;
        }

        /// <summary>
        /// 2項係数
        /// </summary>
        public static int BinomialCoeff(int n, int b) {
            if (n <= 0 || b < 0 || n < b) {
                throw new ArgumentOutOfRangeException();
            }

            return (int)(Factorial(n) / Factorial(n - b) / Factorial(b));
        }

        public delegate WWComplex TransferFunctionDelegate(WWComplex s);
        public delegate double TimeDomainResponseFunctionDelegate(double t);

        public static void Test() {
            for (double k=0.9999; k < 1; k += 0.00001) {
                var v0=AGM(1, Math.Sqrt(1 - k * k));
                var v1=AGM(1 - k, 1 + k);
                var v2 = CompleteEllipticIntegralK(k);
                Console.WriteLine("{0} {1} {2} {3}", k, v0, v1,v2);
            }
            for (double u=0; u < 10; u += 0.5) {
                var v0=EllipticSine(u,0.975);
                var v1=EllipticSine(u, 0.25);
                Console.WriteLine("{0} {1} {2}", u, v0, v1);
            }
        }
    }
}
