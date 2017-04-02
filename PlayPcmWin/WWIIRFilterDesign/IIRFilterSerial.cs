using System.Collections.Generic;
using WWMath;

namespace WWIIRFilterDesign {
    /// <summary>
    /// 実係数rational functionの直列接続
    /// </summary>
    public class IIRFilterSerial {
        private List<IIRFilterBlockReal> mFilterBlockList = new List<IIRFilterBlockReal>();

        public IIRFilterSerial() {
        }

        /// <summary>
        /// 多項式pは直列接続される。(p同士を掛けていく感じになる)
        /// </summary>
        public void Add(RealRationalPolynomial p) {
            var b = new IIRFilterBlockReal(p);
            mFilterBlockList.Add(b);
        }

        // 入力値xを受け取ると、出力yが出てくる。
        public double Filter(double x) {
            double y = x;

            foreach (var b in mFilterBlockList) {
                y = b.Filter(y);
            }

            return y;
        }
    }
}
