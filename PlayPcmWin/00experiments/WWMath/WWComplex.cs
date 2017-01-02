using System;

namespace WWMath {
    public struct WWComplex {
        public double real;
        public double imaginary;

        public WWComplex(double real, double imaginary) {
            this.real      = real;
            this.imaginary = imaginary;
        }

        public WWComplex(WWComplex rhs) {
            this.real      = rhs.real;
            this.imaginary = rhs.imaginary;
        }

        public WWComplex CreateCopy() {
            return new WWComplex(this);
        }

        public void Set(double real, double imaginary) {
            this.real = real;
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
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Add(WWComplex rhs) {
            real      += rhs.real;
            imaginary += rhs.imaginary;
            return this;
        }
        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Sub(WWComplex rhs) {
            real -= rhs.real;
            imaginary -= rhs.imaginary;
            return this;
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Mul(double v) {
            real      *= v;
            imaginary *= v;
            return this;
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Div(double rhs) {
            var recip = 1.0 / rhs;
            return Mul(recip);
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Mul(WWComplex rhs) {
#if false
            // straightforward but slow
            double tR = real * rhs.real      - imaginary * rhs.imaginary;
            double tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            double k1 = real * (rhs.real + rhs.imaginary);
            double k2 = rhs.imaginary * (real + imaginary);
            double k3 = rhs.real * (imaginary - real);
            real = k1 - k2;
            imaginary = k1 + k3;
#endif
            return this;
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Div(WWComplex rhs) {
            var recip = new WWComplex(rhs).Reciprocal();
            return Mul(recip);
        }

        public void CopyFrom(WWComplex rhs) {
            real      = rhs.real;
            imaginary = rhs.imaginary;
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex Reciprocal() {
            double sq = real * real + imaginary * imaginary;
            real = real / sq;
            imaginary = -imaginary / sq;
            return this;
        }

        /// <summary>
        /// 自分自身が書き換わるので注意。value of this instance is changed
        /// </summary>
        public WWComplex ComplexConjugate() {
            imaginary = -imaginary;
            return this;
        }

        /// <summary>
        /// create copy and copy := lhs + rhs, returns copy.
        /// </summary>
        public static WWComplex Add(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Add(rhs);
        }
        /// <summary>
        /// create copy and copy := lhs - rhs, returns copy.
        /// </summary>
        public static WWComplex Sub(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Sub(rhs);
        }
        /// <summary>
        /// create copy and copy := lhs * rhs, returns copy.
        /// </summary>
        public static WWComplex Mul(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Mul(rhs);
        }
        /// <summary>
        /// create copy and copy := lhs / rhs, returns copy.
        /// </summary>
        public static WWComplex Div(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Div(rhs);
        }
        /// <summary>
        /// create copy and copy := lhs / rhs, returns copy.
        /// </summary>
        public static WWComplex Div(WWComplex lhs, double rhs) {
            return new WWComplex(lhs).Div(rhs);
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

        public static WWComplex Unity() {
            return new WWComplex(1, 0);
        }

        public static WWComplex Zero() {
            return new WWComplex(0, 0);
        }
    }
}
