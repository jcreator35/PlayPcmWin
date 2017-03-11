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
            numer = new double[aNumer.Order() + 1];
            denom = new double[aDenom.Order() + 1];

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

        public int Order() {
            int r = NumerOrder();
            if (r < DenomOrder()) {
                r = DenomOrder();
            }

            return r;
        }

        public int NumerOrder() {
            return numer.Length - 1;
        }

        public int DenomOrder() {
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
