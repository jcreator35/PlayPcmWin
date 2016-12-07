
namespace WWMath {
    public abstract class RationalPolynomial {


        public abstract WWComplex[] NumeratorCoeffs();

        public abstract WWComplex[] DenominatorCoeffs();

        public abstract WWComplex N(int nth);

        public abstract WWComplex D(int nth);

        public abstract int Order();

        public abstract string ToString(string variableSymbol);

        public abstract void FrequencyScaling(double ωc);

    }
}
