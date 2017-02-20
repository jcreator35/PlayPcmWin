using System;
using System.Collections.Generic;

namespace WWMath {
    public class PolynomialRootFinding {
        public static void Test() {
            var rf = new PolynomialRootFinding();

            {
                var r = rf.FindRoots(new double[] { 2, 3 });
                Console.WriteLine("3x+2=0 : x={0}", r[0]);
            }
            {
                var r = rf.FindRoots(new double[] { 1, 2, 1 });
                Console.WriteLine("x^2+2x+1=0 : x={0},{1}", r[0], r[1]);
            }
            {
                var r = rf.FindRoots(new double[] { 1, 0, 1 });
                Console.WriteLine("x^2+1=0 : x={0},{1}", r[0], r[1]);
            }
            {
                var r = rf.FindRoots(new double[] { -1, 0, 1 });
                Console.WriteLine("x^2-1=0 : x={0},{1}", r[0], r[1]);
            }
            {
                var r = rf.FindRoots(new double[] { 2, 3, 1 });
                Console.WriteLine("x^2+3x+2=0 : x={0},{1}", r[0], r[1]);
            }
            {
                // 1, -0.5±√3/2i
                var r = rf.FindRoots(new double[] { -1, 0, 0, 1 });
                Console.WriteLine("x^3+x^2+x-3=0 : x={0},{1},{2}", r[0], r[1], r[2]);
            }
            {
                // 1, -1±√2i
                var r = rf.FindRoots(new double[] { -3, 1, 1, 1 });
                Console.WriteLine("x^3+x^2+x-3=0 : x={0},{1},{2}", r[0], r[1], r[2]);
            }
            {
                // 1,2,2
                var r = rf.FindRoots(new double[] { -4, 8, -5, 1 });
                Console.WriteLine("x^3-5x^2+8x-4=0 : x={0},{1},{2}", r[0], r[1], r[2]);
            }
            {
                // 1,2,3
                var r = rf.FindRoots(new double[] { -6, 11, -6, 1 });
                Console.WriteLine("x^3-6x^2+11x-6=0 : x={0},{1},{2}", r[0], r[1], r[2]);
            }
            {
                // 1,2,3,4
                var r = rf.FindRoots(new double[] { 24, -50, 35, -10, 1 });
                Console.WriteLine("x^4-10x^3+35x^2-50x+24=0 : x={0},{1},{2},{3}", r[0], r[1], r[2], r[3]);
            }

            {
                // -1,2,-3,-1/3,1/2
                var r = rf.FindRoots(new double[] { 6,11,-33,-33,11,6 });
                Console.WriteLine("6x^5+11x^4-33x^3-33x^2+11x+6=0 : x={0},{1},{2},{3},{4}", r[0], r[1], r[2], r[3],r[4]);
            }
        }

        private List<WWComplex> mRoots = new List<WWComplex>();
        private List<double> mCoeffs;

        public List<WWComplex> FindRoots(double[] coeffs) {
            var c = new List<double>();
            for (int i = 0; i < coeffs.Length; ++i) {
                c.Add(coeffs[i]);
            }
            return FindRoots(c);
        }

        /// 実係数多項式の根を計算する。
        /// @param mCoeffs 多項式の係数のリスト。coeffs[0]:定数項、coeffs[1]:1次の項…。
        /// @return 根のリストを戻す。
        public List<WWComplex> FindRoots(List<double> coeffs) {
            mRoots.Clear();
            mCoeffs = new List<double>();
            for (int i = 0; i < coeffs.Count; ++i) {
                mCoeffs.Add(coeffs[i]);
            }

            while (0 < mCoeffs.Count) {
                FindRoot1();
            }
            return mRoots;
        }

        private void FindRootLinear(double[] coeffs) {
            // 1次多項式 linear polynomial equation
            System.Diagnostics.Debug.Assert(2 == coeffs.Length);
            double root = -coeffs[0] / coeffs[1];
            mRoots.Add(new WWComplex(root, 0));
        }

        private void FindRootQuadratic(double[] coeffs) {
            // 2次多項式 quadratic polynomial equation
            System.Diagnostics.Debug.Assert(3 == coeffs.Length);

            double a = coeffs[2];
            double b = coeffs[1];
            double c = coeffs[0];

            double discriminant = b * b - 4 * a * c;
            if (0 <= discriminant) {
                double rootP = (-b + Math.Sqrt(discriminant)) / 2 / a;
                double rootM = (-b - Math.Sqrt(discriminant)) / 2 / a;

                mRoots.Add(new WWComplex(rootP, 0));
                mRoots.Add(new WWComplex(rootM, 0));
            } else {
                double real = -b / 2 / a;
                double imag = Math.Sqrt(-discriminant) / 2 / a;

                mRoots.Add(new WWComplex(real, +imag));
                mRoots.Add(new WWComplex(real, -imag));
            }
        }

        private static bool AlmostZero(double v) {
            return Math.Abs(v) < 0.00000001;
        }

        private void FindRootCubic(double[] coeffs) {
            // 3次多項式 cubic polynomial equation
            // https://en.wikipedia.org/wiki/Cubic_function#Algebraic_solution
            System.Diagnostics.Debug.Assert(4 == coeffs.Length);
            double a = coeffs[3];
            double b = coeffs[2];
            double c = coeffs[1];
            double d = coeffs[0];

            double Δ =
                18 * a * b * c * d
                - 4 * b * b * b * d
                + 1 * b * b * c * c
                - 4 * a * c * c * c
                - 27 * a * a * d * d;

            double Δ0 = b * b - 3 * a * c;

            // ↓ 雑な0かどうかの判定処理。
            if (AlmostZero(Δ)) {
                if (AlmostZero(Δ0)) {
                    // triple root
                    double root = -b / 3 / a;
                    mRoots.Add(new WWComplex(root, 0));
                    mRoots.Add(new WWComplex(root, 0));
                    mRoots.Add(new WWComplex(root, 0));
                    return;
                }
                // double root and a simple root
                double root2 = (9 * a * d - b * c) / 2 / Δ0;
                double root1 = (4 * a * b * c - 9 * a * a * d - b * b * b);
                mRoots.Add(new WWComplex(root2, 0));
                mRoots.Add(new WWComplex(root2, 0));
                mRoots.Add(new WWComplex(root1, 0));
                return;
            }

            {
                double Δ1 =
                    +  2 * b * b * b
                    -  9 * a * b * c
                    + 27 * a * a * d;

                WWComplex C;
                {
                    WWComplex c1 = new WWComplex(Δ1/2.0, 0);

                    double c2Sqrt = Math.Sqrt(Math.Abs(-27 * a * a * Δ)/4);
                    WWComplex c2;
                    if (Δ < 0) {
                        //C2 is real number
                        c2 = new WWComplex(c2Sqrt,0);
                    } else {
                        //C2 is imaginary number
                        c2 = new WWComplex(0, c2Sqrt);
                    }

                    WWComplex c1c2;
                    WWComplex c1Pc2 = WWComplex.Add(c1, c2);
                    WWComplex c1Mc2 = WWComplex.Sub(c1, c2);
                    if (c1Mc2.Magnitude() <= c1Pc2.Magnitude()) {
                        c1c2 = c1Pc2;
                    } else {
                        c1c2 = c1Mc2;
                    }

                    // 3乗根 = 大きさが3乗根で位相が3分の1．
                    double magnitude = c1c2.Magnitude();
                    double phase = c1c2.Phase();
                    double cMag = Math.Pow(magnitude,1.0/3.0);
                    double cPhase = phase/3;

                    C = new WWComplex(cMag * Math.Cos(cPhase), cMag * Math.Sin(cPhase));
                }

                var ζ = new WWComplex(-1.0 / 2.0, 1.0 / 2.0 * Math.Sqrt(3.0));
                var ζ2 = new WWComplex(-1.0 / 2.0, -1.0 / 2.0 * Math.Sqrt(3.0));
                var r3a = new WWComplex(-1.0 / 3.0 / a, 0);

                WWComplex root0 = WWComplex.Mul(r3a, WWComplex.Add(
                    WWComplex.Add(new WWComplex(b, 0), C),
                    WWComplex.Div(new WWComplex(Δ0, 0), C)));

                WWComplex root1 = WWComplex.Mul(r3a, WWComplex.Add(
                    WWComplex.Add(new WWComplex(b, 0), WWComplex.Mul(ζ, C)),
                    WWComplex.Div(new WWComplex(Δ0, 0), WWComplex.Mul(ζ, C))));

                WWComplex root2 = WWComplex.Mul(r3a, WWComplex.Add(
                    WWComplex.Add(new WWComplex(b, 0), WWComplex.Mul(ζ2, C)),
                    WWComplex.Div(new WWComplex(Δ0, 0), WWComplex.Mul(ζ2, C))));

                mRoots.Add(root0);
                mRoots.Add(root1);
                mRoots.Add(root2);
                return;
            }
        }

        private void FindRoot1() {
            System.Diagnostics.Debug.Assert(2 <= mCoeffs.Count);

            if (mCoeffs.Count == 2) {
                FindRootLinear(new double[] { mCoeffs[0], mCoeffs[1] });
                mCoeffs.Clear();
                return;
            }
            if (mCoeffs.Count == 3) {
                FindRootQuadratic(new double[] { mCoeffs[0], mCoeffs[1], mCoeffs[2] });
                mCoeffs.Clear();
                return;
            }
            if (mCoeffs.Count == 4) {
                FindRootCubic(new double[] { mCoeffs[0], mCoeffs[1], mCoeffs[2], mCoeffs[3] });
                mCoeffs.Clear();
                return;
            }

            {
                // 4次以上。
                /// Bairstow's method https://en.wikipedia.org/wiki/Bairstow%27s_method
                // a=入力多項式。
                // b=見つかった2次多項式成分で割った後の、次数が2次下がった多項式の係数リスト。
                double[] a = new double[mCoeffs.Count];
                for(int i=0; i<a.Length; ++i) {
                    a[i] = mCoeffs[i];
                }
                double [] b = new double[a.Length];
                double [] f = new double[a.Length];

                double u = a[a.Length - 2] / a[a.Length - 1];
                double v = a[a.Length - 3] / a[a.Length - 1];

                double improvement = 1.0;
                do {
                    for (int i = a.Length - 3; 0 <= i; --i) {
                        b[i] = a[i + 2] - u * b[i + 1] - v * b[i + 2];
                    }
                    double c = a[1] - u * b[0] - v * b[1];
                    double d = a[0] - v * b[0];

                    for (int i=a.Length-5; 0<=i; --i) {
                        f[i] = b[i+2]-u*f[i+1] -v*f[i+2];
                    }
                    double g = b[1]-u*f[0]-v*f[1];
                    double h = b[0]-v*f[0];

                    double t = -1/(v*g*g+h*(h-u*g));
                    double newU = u + t * (-h * c + g * d);
                    double newV = v + t * (-g*v + (g*u-h) * d);
                    if (Math.Abs(u) < Math.Abs(v)) {
                        improvement = Math.Abs((v - newV) / v);
                    } else {
                        improvement = Math.Abs((u - newU) / u);
                    }
                    u = newU;
                    v = newV;
                } while (0.00000001 < improvement); // ←雑な終了条件。


                // x^2 +ux +v = 0の解を足す。
                FindRootQuadratic(new double[] { v, u, 1 });

                // mCoeffsを更新する。新しい多項式の係数はb[]
                mCoeffs.Clear();
                for (int i = 0; i < a.Length - 2; ++i) {
                    mCoeffs.Add(b[i]);
                }
            }
        }
    }
}
