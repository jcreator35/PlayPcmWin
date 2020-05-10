// 日本語。
using System;
using System.Collections.Generic;

namespace WWMath {

    /// 実係数n次多項式p(x)の定積分 ∫_a^b{p(x)}dxの値を、多項式の積分を行うことなく求める。
    /// https://en.wikipedia.org/wiki/Gaussian_quadrature
    public class WWGaussianQuadrature {
        /// <summary>
        /// 多項式を評価する点の数。
        /// </summary>
        public static int NumberOfEvalPoints(RealPolynomial p) {
            return p.Degree / 2 + 1;
        }

        public class ξw {
            public double ξ;
            public double w;
            public ξw(double aξ, double aw) {
                ξ = aξ;
                w = aw;
            }
        };

        private List<ξw[]> mξwList;

        public WWGaussianQuadrature() {
            // ξwListを準備します。
            mξwList = new List<ξw[]>();

            {
                // 0次 = 定数。
                // 1次多項式。
                var r = new ξw[1];
                r[0] = new ξw(0, 2);
                mξwList.Add(r);
            }
            {
                // 2次多項式。
                // 3次多項式。
                var r = new ξw[2];
                r[0] = new ξw(-1.0 / Math.Sqrt(3), 1);
                r[1] = new ξw(+1.0 / Math.Sqrt(3), 1);
                mξwList.Add(r);
            }
            {
                // 4次多項式。
                // 5次多項式。
                var r = new ξw[3];
                r[0] = new ξw(-Math.Sqrt(3.0 / 5.0), 5.0 / 9.0);
                r[1] = new ξw(0, 8.0/9.0);
                r[2] = new ξw(+Math.Sqrt(3.0 / 5.0), 5.0/9.0);
                mξwList.Add(r);
            }
            {
                // 6次多項式。
                // 7次多項式。
                var r = new ξw[4];
                r[0] = new ξw(-Math.Sqrt(3.0 / 7.0 + 2.0 / 7.0 * Math.Sqrt(6.0 / 5.0)), (18.0 - Math.Sqrt(30)) / 36.0);
                r[1] = new ξw(-Math.Sqrt(3.0 / 7.0 - 2.0 / 7.0 * Math.Sqrt(6.0 / 5.0)), (18.0 + Math.Sqrt(30))/36.0);
                r[2] = new ξw(+Math.Sqrt(3.0 / 7.0 - 2.0 / 7.0 * Math.Sqrt(6.0 / 5.0)), (18.0 + Math.Sqrt(30))/36.0);
                r[3] = new ξw(+Math.Sqrt(3.0 / 7.0 + 2.0 / 7.0 * Math.Sqrt(6.0 / 5.0)), (18.0 - Math.Sqrt(30))/36.0);
                mξwList.Add(r);
            }
            {
                // 8次多項式。
                // 9次多項式。
                var r = new ξw[5];
                r[0] = new ξw(-1.0 / 3.0 * Math.Sqrt(5.0 + 2.0 * Math.Sqrt(10.0 / 7.0)), (322.0 - 13.0 * Math.Sqrt(70)) / 900.0);
                r[1] = new ξw(-1.0 / 3.0 * Math.Sqrt(5.0 - 2.0 * Math.Sqrt(10.0 / 7.0)), (322.0 + 13.0 * Math.Sqrt(70))/900.0);
                r[2] = new ξw(0, 128.0/225.0);
                r[3] = new ξw(+1.0 / 3.0 * Math.Sqrt(5.0 - 2.0 * Math.Sqrt(10.0 / 7.0)), (322.0 + 13.0 * Math.Sqrt(70))/900.0);
                r[4] = new ξw(+1.0 / 3.0 * Math.Sqrt(5.0 + 2.0 * Math.Sqrt(10.0 / 7.0)), (322.0 - 13.0 * Math.Sqrt(70))/900.0);
                mξwList.Add(r);
            }
        }

        /// <summary>
        /// 多項式評価地点のx座標値ξと重み係数wを戻す。
        /// </summary>
        /// <param name="np">評価する点の数。</param>
        public ξw[] GetξwList(int np) {
            System.Diagnostics.Debug.Assert(0 < np);

            int idx = np - 1;

            if (mξwList.Count <= idx) {
                // 10次多項式以上の場合。
                throw new NotImplementedException();
            }

            return mξwList[idx];
        }

        /// <summary>
        /// 実係数n次多項式p(x)の定積分 ∫_a^b{p(x)dx}を求める。
        /// </summary>
        /// <param name="p">実係数n次多項式p(x)。</param>
        /// <param name="a">区間a。</param>
        /// <param name="b">区間b。</param>
        /// <returns>∫_a^b{p(x)dx}</returns>
        public double Calc(RealPolynomial p, double a, double b) {
            // xの積分区間 [a b]
            // ξの積分区間 [-1 +1]

            double flipInterval = 1;
            if (b < a) {
                // swap a and b
                double t = a;
                a = b;
                b = t;
                flipInterval = -1;
            }

            int np = NumberOfEvalPoints(p);
            var ξwList = GetξwList(np);

            // 定積分結果値r。
            double r = 0;

            for (int k = 0; k < np; ++k) {
                var ξ=ξwList[k].ξ;
                var w=ξwList[k].w;

                double x = ((a + b) + ξ * (b - a)) / 2.0;

                // 地点xでpを評価：p(x)を求める。
                double px = p.Evaluate(x);

                r += w * px;
            }

            // 積分変数をx→ξに置き換えたことによるスケーリング。
            r *= (b - a) / 2.0;

            r *= flipInterval;

            return r;
        }
    }
}
