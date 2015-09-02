using System;

namespace DecimalFft {
    struct WWDecimalComplex {
        public decimal real;
        public decimal imaginary;

        public WWDecimalComplex(decimal real, decimal imaginary) {
            this.real      = real;
            this.imaginary = imaginary;
        }

        public WWDecimalComplex(WWDecimalComplex rhs) {
            this.real      = rhs.real;
            this.imaginary = rhs.imaginary;
        }

        public void Set(decimal real, decimal imaginary) {
            this.real = real;
            this.imaginary = imaginary;
        }

        public decimal Magnitude() {
            return WWDecimalMath.Sqrt(real * real + imaginary * imaginary);
        }

        public override string ToString() {
            if (WWDecimalMath.IsExtremelySmall(imaginary)) {
                return string.Format("{0:0.#######}", real);
            }
            if (WWDecimalMath.IsExtremelySmall(real)) {
                return string.Format("{0:0.#######}i", imaginary);
            }
            return string.Format("{0:0.#######} {1:+0.#######;-0.#######}i", real, imaginary);
        }

        /*
        /// <summary>
        /// Phase in radians
        /// </summary>
        /// <returns>radians, -π to +π</returns>
        public decimal Phase() {
            if (WWDecimalMath.IsTooSmall(Magnitude())) {
                return 0;
            }

            return WWDecimalMath.Atan2(imaginary, real);
        }
        */

        public WWDecimalComplex Add(WWDecimalComplex rhs) {
            real      += rhs.real;
            imaginary += rhs.imaginary;
            return this;
        }

        public WWDecimalComplex Mul(decimal v) {
            real      *= v;
            imaginary *= v;
            return this;
        }

        public WWDecimalComplex Sub(WWDecimalComplex rhs) {
            real      -= rhs.real;
            imaginary -= rhs.imaginary;
            return this;
        }

        public WWDecimalComplex Mul(WWDecimalComplex rhs) {
#if false
            // straightforward but slow
            decimal tR = real * rhs.real      - imaginary * rhs.imaginary;
            decimal tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            decimal k1 = real          * (rhs.real  + rhs.imaginary);
            decimal k2 = rhs.imaginary * (real      + imaginary);
            decimal k3 = rhs.real      * (imaginary - real);
            real      = k1 - k2;
            imaginary = k1 + k3;
#endif
            return this;
        }

        public void CopyFrom(WWDecimalComplex rhs) {
            real      = rhs.real;
            imaginary = rhs.imaginary;
        }

        public static WWDecimalComplex Add(WWDecimalComplex a, WWDecimalComplex b) {
            var r = new WWDecimalComplex(a);
            r.Add(b);
            return r;
        }

        public static WWDecimalComplex Mul(WWDecimalComplex a, WWDecimalComplex b) {
            var r = new WWDecimalComplex(a);
            r.Mul(b);
            return r;
        }

        public static WWDecimalComplex Sub(WWDecimalComplex a, WWDecimalComplex b) {
            var r = new WWDecimalComplex(a);
            r.Sub(b);
            return r;
        }

        public static WWDecimalComplex[] Add(WWDecimalComplex[] a, WWDecimalComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWDecimalComplex[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWDecimalComplex.Add(a[i], b[i]);
            }
            return c;
        }

        public static WWDecimalComplex[] Mul(WWDecimalComplex[] a, WWDecimalComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWDecimalComplex[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWDecimalComplex.Mul(a[i], b[i]);
            }
            return c;
        }

        public static decimal AverageDistance(WWDecimalComplex[] a, WWDecimalComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            decimal d = 0M;
            for (int i=0; i<a.Length; ++i) {
                var s = WWDecimalComplex.Sub(a[i], b[i]);
                d += s.Magnitude();
            }

            d /= a.Length;
            return d;
        }

        public static WWDecimalComplex[] From(decimal[] from) {
            var to = new WWDecimalComplex[from.Length];
            for (int i = 0; i < from.Length; ++i) {
                to[i].real = from[i];
            }
            return to;
        }

        public static decimal[] ExtractRealPart(WWDecimalComplex[] from) {
            var to = new decimal[from.Length];
            for (int i = 0; i < from.Length; ++i) {
                to[i] = from[i].real;
            }
            return to;
        }
    }

}
