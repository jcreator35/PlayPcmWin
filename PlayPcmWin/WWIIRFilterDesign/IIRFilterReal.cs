using System.Collections.Generic;

namespace WWIIRFilterDesign {
    public class IIRFilterReal {
        private List<IIRFilterBlockReal> mFilterBlockList = new List<IIRFilterBlockReal>();

        public IIRFilterReal() {
        }

        public void Add(WWMath.ComplexRationalPolynomial p) {
            var b = new IIRFilterBlockReal(p);
            mFilterBlockList.Add(b);
        }

        // 入力値xを受け取ると、出力yが出てくる。
        public double Filter(double x) {
            double y = 0;

            foreach (var b in mFilterBlockList) {
                y += b.Filter(x);
            }

            return y;
        }
    }
}
