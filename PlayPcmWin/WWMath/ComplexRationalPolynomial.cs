
namespace WWMath {
    public abstract class ComplexRationalPolynomial {

        public abstract WWComplex N(int nth);

        public abstract WWComplex D(int nth);

        public abstract int Order();
        public abstract int NumerOrder();
        public abstract int DenomOrder();

        public abstract string ToString(string variableSymbol, string imaginaryUnit);

    }
}
