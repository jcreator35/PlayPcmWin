using System;

namespace WWAudioFilter {
    public struct WWComplexF {
        public float real;
        public float imaginary;

        public WWComplexF(float real, float imaginary) {
            this.real      = real;
            this.imaginary = imaginary;
        }

        public WWComplexF(WWComplexF rhs) {
            this.real      = rhs.real;
            this.imaginary = rhs.imaginary;
        }

        public void Set(float real, float imaginary) {
            this.real = real;
            this.imaginary = imaginary;
        }

        public float Magnitude() {
            return (float)Math.Sqrt(real * real + imaginary * imaginary);
        }

        /// <summary>
        /// Phase in radians
        /// </summary>
        /// <returns>radians, -π to +π</returns>
        public float Phase() {
            if (Magnitude() < float.Epsilon) {
                return 0;
            }

            return (float)Math.Atan2(imaginary, real);
        }

        public WWComplexF Add(WWComplexF rhs) {
            real      += rhs.real;
            imaginary += rhs.imaginary;
            return this;
        }

        public WWComplexF Mul(float v) {
            real      *= v;
            imaginary *= v;
            return this;
        }

        public WWComplexF Mul(WWComplexF rhs) {
#if false
            // straightforward but slow
            float tR = real * rhs.real      - imaginary * rhs.imaginary;
            float tI = real * rhs.imaginary + imaginary * rhs.real;
            real      = tR;
            imaginary = tI;
#else
            // more efficient way
            float k1 = real          * (rhs.real  + rhs.imaginary);
            float k2 = rhs.imaginary * (real      + imaginary);
            float k3 = rhs.real      * (imaginary - real);
            real      = k1 - k2;
            imaginary = k1 + k3;
#endif
            return this;
        }

        public void CopyFrom(WWComplexF rhs) {
            real      = rhs.real;
            imaginary = rhs.imaginary;
        }

        public WWComplexF Reciprocal() {
            float sq = real * real + imaginary * imaginary;
            real = real / sq;
            imaginary = -imaginary / sq;
            return this;
        }
    }
}
