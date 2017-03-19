using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class RealRationalPolynomial {
        private readonly double[] numer;
        private readonly double[] denom;

        public RealRationalPolynomial(double[] aNumer, double[] aDenom) {
            numer = new double[aNumer.Length];
            denom = new double[aDenom.Length];
            Array.Copy(aNumer, numer, aNumer.Length);
            Array.Copy(aDenom, denom, aDenom.Length);
        }

        public RealRationalPolynomial(RealPolynomial aNumer, RealPolynomial aDenom) {
            numer = new double[aNumer.Degree + 1];
            denom = new double[aDenom.Degree + 1];

            for (int i = 0; i < numer.Length; ++i) {
                numer[i] = aNumer.C(i);
            }

            for (int i = 0; i < denom.Length; ++i) {
                denom[i] = aDenom.C(i);
            }
        }

        public double N(int nth) {
            return numer[nth];
        }

        public double D(int nth) {
            return denom[nth];
        }

        public RealPolynomial NumerPolynomial() {
            return new RealPolynomial(numer);
        }

        public RealPolynomial DenomPolynomial() {
            return new RealPolynomial(denom);
        }

        public RealPolynomial ToPolynomial() {
            if (NumerDegree() != 0) {
                return null;
            }

            var c = new double[DenomDegree() + 1];
            for (int i = 0; i < c.Length; ++i) {
                c[i] = numer[i] / denom[0];
            }
            return new RealPolynomial(c);
        }

        public int Degree() {
            int r = NumerDegree();
            if (r < DenomDegree()) {
                r = DenomDegree();
            }

            return r;
        }

        public int NumerDegree() {
            return numer.Length - 1;
        }

        public int DenomDegree() {
            return denom.Length - 1;
        }

        public string ToString(string variableSymbol) {
            string n = new RealPolynomial(numer).ToString(variableSymbol);
            string d = new RealPolynomial(denom).ToString(variableSymbol);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }

        public override string ToString() {
            return ToString("x");
        }
    }
}
