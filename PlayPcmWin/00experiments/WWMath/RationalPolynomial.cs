
namespace WWMath {
    public abstract class RationalPolynomial {


        public abstract WWComplex[] NumeratorCoeffs();

        public abstract WWComplex[] DenominatorCoeffs();

        public abstract WWComplex NumeratorCoeff(int nth);

        public abstract WWComplex DenominatorCoeff(int nth);

        public abstract int Order();

        public abstract string ToString(string variableSymbol);

        public abstract void FrequencyScaling(double ωc);

    }
}
