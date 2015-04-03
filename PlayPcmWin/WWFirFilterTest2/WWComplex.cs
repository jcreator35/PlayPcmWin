using System;

namespace WWAudioFilter {
    struct WWComplex {
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

        public WWComplex Mul(double v) {
            real      *= v;
            imaginary *= v;
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
    }
}
