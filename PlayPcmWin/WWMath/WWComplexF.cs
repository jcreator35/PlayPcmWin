using System;
using System.Collections;

namespace WWMath {
    public class WWComplexF {
        public readonly float real;
        public readonly float imaginary;

        public static string imaginaryUnit = "i";

        public WWComplexF(float real, float imaginary) {
            this.real      = real;
            this.imaginary = imaginary;
        }

        public float Magnitude() {
            return (float)Math.Sqrt(real * real + imaginary * imaginary);
        }

        /// <summary>
        /// 大体ゼロのときtrueを戻す。使用する際は大体ゼロの範囲の広さに注意。
        /// </summary>
        public bool AlmostZero() {
            return Math.Abs(real) < float.Epsilon &&
                Math.Abs(imaginary) < float.Epsilon;
        }

        /// <summary>
        /// 内容が全く同じときtrue
        /// </summary>
        public bool EqualValue(WWComplexF rhs) {
            return real == rhs.real && imaginary == rhs.imaginary;
        }

        /// <summary>
        /// Phase in radians
        /// </summary>
        /// <returns>radians, -π to +π</returns>
        public float Phase() {
            if (Magnitude() < Double.Epsilon) {
                return 0;
            }

            return (float)Math.Atan2(imaginary, real);
        }

        /// <summary>
        /// create copy and copy := L + R, returns copy.
        /// </summary>
        public static WWComplexF Add(WWComplexF lhs, WWComplexF rhs) {
            return new WWComplexF(lhs.real+rhs.real, lhs.imaginary+rhs.imaginary);
        }

        public static WWComplexF Add(WWComplexF a, WWComplexF b, WWComplexF c) {
            return new WWComplexF(a.real + b.real + c.real, a.imaginary + b.imaginary + c.imaginary);
        }

        public static WWComplexF Add(WWComplexF a, WWComplexF b, WWComplexF c, WWComplexF d) {
            return new WWComplexF(
                a.real + b.real + c.real + d.real,
                a.imaginary + b.imaginary + c.imaginary + d.imaginary);
        }

        public static WWComplexF Add(WWComplexF a, WWComplexF b, WWComplexF c, WWComplexF d, WWComplexF e) {
            return new WWComplexF(
                a.real + b.real + c.real + d.real + e.real,
                a.imaginary + b.imaginary + c.imaginary + d.imaginary + e.imaginary);
        }

        /// <summary>
        /// create copy and copy := L - R, returns copy.
        /// </summary>
        public static WWComplexF Sub(WWComplexF lhs, WWComplexF rhs) {
            return new WWComplexF(lhs.real - rhs.real, lhs.imaginary - rhs.imaginary);
        }

        /// <summary>
        /// create copy and copy := L * R, returns copy.
        /// </summary>
        public static WWComplexF Mul(WWComplexF lhs, WWComplexF rhs) {
#if false
            // straightforward but slow
            float tR = real * rhs.real      - imaginary * rhs.imaginary;
            float tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            float k1 = lhs.real * ( rhs.real + rhs.imaginary );
            float k2 = rhs.imaginary * ( lhs.real + lhs.imaginary );
            float k3 = rhs.real * ( lhs.imaginary - lhs.real );
            float real = k1 - k2;
            float imaginary = k1 + k3;
#endif
            return new WWComplexF(real,imaginary);
        }

        public static WWComplexF Mul(WWComplexF lhs, float v) {
            return new WWComplexF(lhs.real * v, lhs.imaginary * v);
        }

        public static WWComplexF Reciprocal(WWComplexF uni) {
            float sq = uni.real * uni.real + uni.imaginary * uni.imaginary;
            float real = uni.real / sq;
            float imaginary = -uni.imaginary / sq;
            return new WWComplexF(real, imaginary);
        }

        /// <summary>
        /// returns reciprocal. this instance is not changed.
        /// </summary>
        public WWComplexF Reciplocal() {
            return WWComplexF.Reciprocal(this);
        }

        /// <summary>
        /// returns conjugate reciplocal. this instance is not changed.
        /// </summary>
        public WWComplexF ConjugateReciprocal() {
            var r = Reciplocal();
            return ComplexConjugate(r);
        }

        /// <summary>
        /// create copy and copy := L / R, returns copy.
        /// </summary>
        public static WWComplexF Div(WWComplexF lhs, WWComplexF rhs) {
            var recip = Reciprocal(rhs);
            return Mul(lhs, recip);
        }

        public static WWComplexF Div(WWComplexF lhs, float rhs) {
            var recip = 1.0f / rhs;
            return Mul(lhs, recip);
        }

        /// <summary>
        /// create copy and copy := -uni, returns copy.
        /// argument value is not changed.
        /// </summary>
        public static WWComplexF Minus(WWComplexF uni) {
            return new WWComplexF(-uni.real, -uni.imaginary);
        }

        /// <summary>
        /// -1倍したものを戻す。自分自身は変更しない。
        /// </summary>
        public WWComplexF Minus() {
            return new WWComplexF(-real, -imaginary);
        }

        /// <summary>
        /// create copy and copy := complex conjugate of uni, returns copy.
        /// </summary>
        public static WWComplexF ComplexConjugate(WWComplexF uni) {
            return new WWComplexF(uni.real, -uni.imaginary);
        }

        /// <summary>
        /// scale倍する。自分自身は変更しない。
        /// </summary>
        public WWComplexF Scale(float scale) {
            return new WWComplexF(scale * real, scale * imaginary);
        }

        /// <summary>
        /// 共役複素数を戻す。 自分自身は変更しない。
        /// </summary>
        /// <returns>共役複素数。</returns>
        public WWComplexF ComplexConjugate() {
            return new WWComplexF(real, -imaginary);
        }

        /// <summary>
        /// ここで複素数に入れる順序とは、
        /// ①実数成分
        /// ②虚数成分
        /// </summary>
        private class WWComplexComparer : IComparer {
            int IComparer.Compare(Object x, Object y) {
                var cL = x as WWComplexF;
                var cR = y as WWComplexF;
                if (cL.real != cR.real) {
                    return (cR.real < cL.real) ? 1 : -1;
                }
                if (cL.imaginary != cR.imaginary) {
                    return (cR.imaginary < cL.imaginary) ? 1 : -1;
                }
                return 0;
            }
        }

        /// <summary>
        /// 複素数の配列をWWComplexComparerでソートする。
        /// </summary>
        public static WWComplexF[] SortArray(WWComplexF[] inp) {
            var outp = new WWComplexF[inp.Length];
            Array.Copy(inp, outp, inp.Length);
            Array.Sort(outp, new WWComplexComparer());
            return outp;
        }
        
        public override string ToString() {
            if (Math.Abs(imaginary) < 0.0001) {
                return string.Format("{0:G4}", real);
            }
            if (Math.Abs(real) < 0.0001) {
                return string.Format("{0:G4}{1}", imaginary, imaginaryUnit);
            }

            if (imaginary < 0) {
                // マイナス記号が自動で出る。
                return string.Format("{0:G4}{1:G4}{2}", real, imaginary, imaginaryUnit);
            } else {
                return string.Format("{0:G4}+{1:G4}{2}", real, imaginary, imaginaryUnit);
            }
        }

        public static WWComplexF[] Add(WWComplexF[] a, WWComplexF[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWComplexF[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWComplexF.Add(a[i], b[i]);
            }
            return c;
        }

        public static WWComplexF[] Mul(WWComplexF[] a, WWComplexF[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWComplexF[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWComplexF.Mul(a[i], b[i]);
            }
            return c;
        }

        public static float AverageDistance(WWComplexF[] a, WWComplexF[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            float d = 0.0f;
            for (int i = 0; i < a.Length; ++i) {
                d += Distance(a[i], b[i]);
            }

            d /= a.Length;
            return d;
        }

        public static float Distance(WWComplexF a, WWComplexF b) {
            var s = WWComplexF.Sub(a, b);
            return s.Magnitude();
        }

        static WWComplexF mUnity = new WWComplexF(1, 0);
        static WWComplexF mZero = new WWComplexF(0, 0);
        public static WWComplexF Unity() {
            return mUnity;
        }

        public static WWComplexF Zero() {
            return mZero;
        }

        /// <summary>
        /// すべての要素値が0の複素数配列をnewして戻す。
        /// </summary>
        /// <param name="count">配列の要素数。</param>
        public static WWComplexF[] ZeroArray(int count) {
            var r = new WWComplexF[count];
            for (int i = 0; i < r.Length; ++i) {
                r[i] = Zero();
            }
            return r;
        }
        public static WWComplexF[] FromRealArray(float[] r) {
            var c = new WWComplexF[r.Length];
            for (int i = 0; i < c.Length; ++i) {
                c[i] = new WWComplexF(r[i], 0);
            }

            return c;
        }

        public static WWComplexF[] FromRealArray(double[] r) {
            var c = new WWComplexF[r.Length];
            for (int i = 0; i < c.Length; ++i) {
                c[i] = new WWComplexF((float)r[i], 0);
            }

            return c;
        }

        /// <summary>
        /// 各複素数の実数成分を取り出し実数の配列とする。
        /// </summary>
        public static float[] ToRealArray(WWComplexF[] c) {
            var r = new float[c.Length];
            for (int i = 0; i < r.Length; ++i) {
                r[i] = c[i].real;
            }

            return r;
        }

        /// <summary>
        /// 各複素数の大きさを取り実数にし、配列を戻す。
        /// </summary>
        public static float[] ToMagnitudeRealArray(WWComplexF[] c) {
            var r = new float[c.Length];
            for (int i = 0; i < r.Length; ++i) {
                r[i] = c[i].Magnitude();
            }

            return r;
        }
    }
}
