using System.Collections.Generic;
using WWMath;

namespace WWIIRFilterDesign {
    public class IIRFilterComplex {
        private List<IIRFilterBlockComplex> mFilterBlockList = new List<IIRFilterBlockComplex>();

        public IIRFilterComplex() {
        }

        public void Add(WWMath.ComplexRationalPolynomial p) {
            var b = new IIRFilterBlockComplex(p);
            mFilterBlockList.Add(b);
        }

        // 入力値xを受け取ると、出力yが出てくる。
        public double Filter(double xReal) {
            WWComplex x = new WWComplex(xReal, 0);
            WWComplex y = WWComplex.Zero();

            foreach (var b in mFilterBlockList) {
                y = WWComplex.Add(y, b.Filter(x));
            }

            return y.real;
        }
    }
}
