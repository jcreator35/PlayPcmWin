using WWMath;

namespace WWIIRFilterDesign {
    public interface IIRFilterGraph {
        void Add(RealRationalPolynomial p);
        double Filter(double x);
        int BlockCount();
        IIRFilterBlockReal GetNthBlock(int nth);

        /// <summary>
        /// 同じフィルター特性、ディレイの状態も同じだが、別実体のディレイを持つインスタンスを作る。
        /// </summary>
        IIRFilterGraph CreateCopy();
    }
}
