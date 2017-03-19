
namespace WWMath {
    public abstract class ComplexRationalPolynomial {

        public abstract WWComplex N(int nth);

        public abstract WWComplex D(int nth);

        public abstract int Degree();
        public abstract int NumerDegree();
        public abstract int DenomDegree();

        public abstract string ToString(string variableSymbol, string imaginaryUnit);

    }
}
