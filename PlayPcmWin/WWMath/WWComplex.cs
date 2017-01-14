using System;

namespace WWMath {
    public struct WWComplex {
        public readonly double real;
        public readonly double imaginary;

        public WWComplex(double real, double imaginary) {
            this.real      = real;
            this.imaginary = imaginary;
        }

        public double Magnitude() {
            return Math.Sqrt(real * real + imaginary * imaginary);
        }

        /// <summary>
        /// 大体ゼロのときtrueを戻す。使用する際は大体ゼロの範囲の広さに注意。
        /// </summary>
        public bool AlmostZero() {
            return Math.Abs(real) < double.Epsilon &&
                Math.Abs(imaginary) < double.Epsilon;
        }

        /// <summary>
        /// 内容が全く同じときtrue
        /// </summary>
        public bool EqualValue(WWComplex rhs) {
            return real == rhs.real && imaginary == rhs.imaginary;
        }

        /// <summary>
        /// Phase in radians
        /// </summary>
        /// <returns>radians, -π to +π</returns>
        public double Phase() {
            if (Magnitude() < Double.Epsilon) {
                return 0;
            }

            return Math.Atan2(imaginary, real);
        }

        /// <summary>
        /// create copy and copy := L + R, returns copy.
        /// </summary>
        public static WWComplex Add(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs.real+rhs.real, lhs.imaginary+rhs.imaginary);
        }
        /// <summary>
        /// create copy and copy := L - R, returns copy.
        /// </summary>
        public static WWComplex Sub(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs.real - rhs.real, lhs.imaginary - rhs.imaginary);
        }
        /// <summary>
        /// create copy and copy := L * R, returns copy.
        /// </summary>
        public static WWComplex Mul(WWComplex lhs, WWComplex rhs) {
#if false
            // straightforward but slow
            double tR = real * rhs.real      - imaginary * rhs.imaginary;
            double tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            double k1 = lhs.real * ( rhs.real + rhs.imaginary );
            double k2 = rhs.imaginary * ( lhs.real + lhs.imaginary );
            double k3 = rhs.real * ( lhs.imaginary - lhs.real );
            double real = k1 - k2;
            double imaginary = k1 + k3;
#endif
            return new WWComplex(real,imaginary);
        }
        public static WWComplex Mul(WWComplex lhs, double v) {
            return new WWComplex(lhs.real * v, lhs.imaginary * v);
        }
        public static WWComplex Reciprocal(WWComplex uni) {
            double sq = uni.real * uni.real + uni.imaginary * uni.imaginary;
            double real = uni.real / sq;
            double imaginary = -uni.imaginary / sq;
            return new WWComplex(real, imaginary);
        }
        /// <summary>
        /// create copy and copy := L / R, returns copy.
        /// </summary>
        public static WWComplex Div(WWComplex lhs, WWComplex rhs) {
            var recip = Reciprocal(rhs);
            return Mul(lhs, recip);
        }
        public static WWComplex Div(WWComplex lhs, double rhs) {
            var recip = 1.0 / rhs;
            return Mul(lhs, recip);
        }
        /// <summary>
        /// create copy and copy := -uni, returns copy.
        /// argument value is not changed.
        /// </summary>
        public static WWComplex Minus(WWComplex uni) {
            return new WWComplex(-uni.real, -uni.imaginary);
        }

        /// <summary>
        /// create copy and copy := complex conjugate of uni, returns copy.
        /// </summary>
        public static WWComplex ComplexConjugate(WWComplex uni) {
            return new WWComplex(uni.real, -uni.imaginary);
        }

        public override string ToString() {
            if (Math.Abs(imaginary) < 0.0001) {
                return string.Format("{0:G4}", real);
            }
            if (Math.Abs(real) < 0.0001) {
                return string.Format("{0:G4}i", imaginary);
            }

            if (imaginary < 0) {
                // マイナス記号が自動で出る。
                return string.Format("{0:G4}{1:G4}i", real, imaginary);
            } else {
                return string.Format("{0:G4}+{1:G4}i", real, imaginary);
            }
        }

        static WWComplex mUnity = new WWComplex(1, 0);
        static WWComplex mZero = new WWComplex(0, 0);
        public static WWComplex Unity() {
            return mUnity;
        }

        public static WWComplex Zero() {
            return mZero;
        }
    }
}
