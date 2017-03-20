using System;

namespace WWMath {
    public class HighOrderComplexRationalPolynomial : ComplexRationalPolynomial {
        /// <summary>
        /// numer[0]=定数項。
        /// numer[1]=1次の項。
        /// numer[2]=2次の項。
        /// </summary>
        private readonly WWComplex[] numer;
        /// <summary>
        /// denom[0]=定数項。
        /// denom[1]=1次の項。
        /// denom[2]=2次の項。
        /// </summary>
        private readonly WWComplex[] denom;

        public HighOrderComplexRationalPolynomial(WWComplex[] aNumer, WWComplex[] aDenom) {
            numer = new WWComplex[aNumer.Length];
            denom = new WWComplex[aDenom.Length];
            Array.Copy(aNumer, numer, aNumer.Length);
            Array.Copy(aDenom, denom, aDenom.Length);
        }

        public HighOrderComplexRationalPolynomial(FirstOrderComplexRationalPolynomial uni) {
            int numerCount = 1;
            if (1 == uni.NumerDegree()) {
                numerCount = 2;
            }
            numer = new WWComplex[numerCount];
            for (int i = 0; i < numerCount; ++i) {
                numer[i] = uni.N(i);
            }

            int denomCount = 1;
            if (1 == uni.DenomDegree()) {
                denomCount = 2;
            }
            denom = new WWComplex[denomCount];
            for (int i = 0; i < denomCount; ++i) {
                denom[i] = uni.D(i);
            }
        }

        public override WWComplex N(int nth) {
            return numer[nth];
        }

        public override WWComplex D(int nth) {
            return denom[nth];
        }

        public ComplexPolynomial NumerPolynomial() {
            return new ComplexPolynomial(numer);
        }

        public ComplexPolynomial DenomPolynomial() {
            return new ComplexPolynomial(denom);
        }

        public override int Degree() {
            int order = NumerDegree();
            if (order < DenomDegree()) {
                order = DenomDegree();
            }
            return order;
        }

        public override int NumerDegree() {
            return numer.Length-1;
        }
        public override int DenomDegree() {
            return denom.Length - 1;
        }

        public override string ToString(string variableSymbol, string imaginaryUnit) {
            string n = WWUtil.PolynomialToString(numer, variableSymbol, WWUtil.SymbolOrder.NonInverted, imaginaryUnit);
            string d = WWUtil.PolynomialToString(denom, variableSymbol, WWUtil.SymbolOrder.NonInverted, imaginaryUnit);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }

        public string ToString(string variableSymbol, WWUtil.SymbolOrder so, string imaginaryUnit) {
            string n = WWUtil.PolynomialToString(numer, variableSymbol, so, imaginaryUnit);
            string d = WWUtil.PolynomialToString(denom, variableSymbol, so, imaginaryUnit);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }
    }
}
