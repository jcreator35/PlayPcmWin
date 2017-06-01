using WWMath;

namespace WWIIRFilterDesign {
    public interface IIRFilterGraph {
        void Add(RealRationalPolynomial p);
        double Filter(double x);
        int BlockCount();
        IIRFilterBlockReal GetNthBlock(int nth);
    }
}
