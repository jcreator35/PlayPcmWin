using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWIIRFilterDesign {
    public class IIRFilter {
        private List<IIRFilterBlock> mFilterBlockList = new List<IIRFilterBlock>();

        public IIRFilter() {
        }

        public void Add(WWMath.RationalPolynomial p) {
            var b = new IIRFilterBlock(p);
            mFilterBlockList.Add(b);
        }

        // 入力値xを受け取ると、出力yが出てくる。
        public double Filter(double xReal) {
            var x = new WWComplex(xReal, 0);
            var y = WWComplex.Zero();

            foreach (var b in mFilterBlockList) {
                y = WWComplex.Add(y, b.Filter(x));
            }

            return y.real;
        }
    }
}
