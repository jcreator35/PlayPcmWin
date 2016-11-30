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

        public void Set(double real, double imaginary) {
            this.real = real;
            this.imaginary = imaginary;
        }

        public double Magnitude() {
            return Math.Sqrt(real * real + imaginary * imaginary);
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

        public WWComplex Add(WWComplex rhs) {
            real      += rhs.real;
            imaginary += rhs.imaginary;
            return this;
        }
        public WWComplex Sub(WWComplex rhs) {
            real -= rhs.real;
            imaginary -= rhs.imaginary;
            return this;
        }

        public WWComplex Mul(double v) {
            real      *= v;
            imaginary *= v;
            return this;
        }

        public WWComplex Div(WWComplex rhs) {
            var denom = new WWComplex(rhs).Reciprocal();
            return Mul(denom);
        }

        public WWComplex Mul(WWComplex rhs) {
#if false
            // straightforward but slow
            double tR = real * rhs.real      - imaginary * rhs.imaginary;
            double tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            double k1 = real          * (rhs.real  + rhs.imaginary);
            double k2 = rhs.imaginary * (real      + imaginary);
            double k3 = rhs.real      * (imaginary - real);
            real      = k1 - k2;
            imaginary = k1 + k3;
#endif
            return this;
        }

        public void CopyFrom(WWComplex rhs) {
            real      = rhs.real;
            imaginary = rhs.imaginary;
        }

        public WWComplex Reciprocal() {
            double sq = real * real + imaginary * imaginary;
            real = real / sq;
            imaginary = -imaginary / sq;
            return this;
        }

        public static WWComplex Add(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Add(rhs);
        }
        public static WWComplex Sub(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Sub(rhs);
        }
        public static WWComplex Mul(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Mul(rhs);
        }
        public static WWComplex Div(WWComplex lhs, WWComplex rhs) {
            return new WWComplex(lhs).Div(rhs);
        }

        public static WWComplex Minus(WWComplex uni) {
            return new WWComplex(-uni.real, -uni.imaginary);
        }

        public override string ToString() {
            if (Math.Abs(imaginary) < 0.0001) {
                return string.Format("{0:G4}", real);
            }
            if (Math.Abs(real) < 0.0001) {
                return string.Format("{0:G4}j", imaginary);
            }

            if (imaginary < 0) {
                // マイナス記号が自動で出る。
                return string.Format("{0:G4}{1:G4}j", real, imaginary);
            } else {
                return string.Format("{0:G4}+{1:G4}j", real, imaginary);
            }
        }
    }
}
