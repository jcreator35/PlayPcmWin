using System;

namespace WWAudioFilterCore {
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

        const double SMALLVALUE = 0.00000001;

        public override string ToString() {
            if (-SMALLVALUE < imaginary && imaginary < SMALLVALUE) {
                return string.Format("{0:0.#######}", real);
            }
            if (-SMALLVALUE < real && real < SMALLVALUE) {
                return string.Format("{0:0.#######}i", imaginary);
            }
            return string.Format("{0:0.#######} {1:+0.#######;-0.#######}i", real, imaginary);
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

        public WWComplex Mul(double v) {
            real      *= v;
            imaginary *= v;
            return this;
        }

        public WWComplex Sub(WWComplex rhs) {
            real      -= rhs.real;
            imaginary -= rhs.imaginary;
            return this;
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

        public static WWComplex Add(WWComplex a, WWComplex b) {
            var r = new WWComplex(a);
            r.Add(b);
            return r;
        }

        public static WWComplex Mul(WWComplex a, WWComplex b) {
            var r = new WWComplex(a);
            r.Mul(b);
            return r;
        }

        public static WWComplex Sub(WWComplex a, WWComplex b) {
            var r = new WWComplex(a);
            r.Sub(b);
            return r;
        }

        public static WWComplex[] Add(WWComplex[] a, WWComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWComplex[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWComplex.Add(a[i], b[i]);
            }
            return c;
        }

        public static WWComplex[] Mul(WWComplex[] a, WWComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            var c = new WWComplex[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                c[i] = WWComplex.Mul(a[i], b[i]);
            }
            return c;
        }

        public static double AverageDistance(WWComplex[] a, WWComplex[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("input array length mismatch");
            }

            double d = 0.0;
            for (int i=0; i<a.Length; ++i) {
                var s = WWComplex.Sub(a[i], b[i]);
                d += s.Magnitude();
            }

            d /= a.Length;
            return d;
        }

        public static WWComplex[] From(double[] from) {
            var to = new WWComplex[from.Length];
            for (int i = 0; i < from.Length; ++i) {
                to[i].real = from[i];
            }
            return to;
        }

        public static double[] ExtractRealPart(WWComplex[] from) {
            var to = new double[from.Length];
            for (int i = 0; i < from.Length; ++i) {
                to[i] = from[i].real;
            }
            return to;
        }
    }

}
