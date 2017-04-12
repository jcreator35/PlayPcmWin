using System;
using System.Collections.Generic;
using WWMath;

namespace WWIIRFilterDesign {
    public class BilinearDesign {
        private double mTd;
        private List<RealRationalPolynomial> mRealHzList = new List<RealRationalPolynomial>();

        public int RealHzCount() {
            return mRealHzList.Count;
        }

        public RealRationalPolynomial RealHz(int nth) {
            return mRealHzList[nth];
        }

        public double Td {
            get { return mTd; }
        }

        public BilinearDesign(double td = 1.0) {
            mTd = td;
        }

        private List<FirstOrderComplexRationalPolynomial> mH_s = new List<FirstOrderComplexRationalPolynomial>();
        private List<FirstOrderComplexRationalPolynomial> mComplexHzList = new List<FirstOrderComplexRationalPolynomial>();

        public int HsNum() {
            return mH_s.Count;
        }

        public FirstOrderComplexRationalPolynomial HsNth(int nth) {
            return mH_s[nth];
        }

        public int HzNum() {
            return mComplexHzList.Count;
        }

        public FirstOrderComplexRationalPolynomial HzNth(int nth) {
            return mComplexHzList[nth];
        }

        public WWMath.Functions.TransferFunctionDelegate TransferFunction;

        /// <summary>
        /// 離散時間角周波数ωを連続時間角周波数Ωにprewarpする。
        /// Discrete-time signal processing 3rd ed. pp.534, eq 7.26
        /// </summary>
        /// <param name="ω">離散時間角周波数ω (π==ナイキスト周波数)</param>
        /// <param name="Td">1.0で良い。</param>
        /// <returns>Ω</returns>
        public double PrewarpωtoΩ(double ω) {
            double Ω = 2.0 / Td * Math.Tan(ω / 2.0);
            return Ω;
        }

        /// <summary>
        /// 連続時間(s平面)の零の座標(0,Ω)は、
        /// 離散時間(z平面)のe^{jω}に移動する。
        /// Discrete-time signal processing 3rd ed. pp534 eq7.27
        /// </summary>
        public double WarpΩtoω(double Ω) {
            double ω = 2.0 * Math.Atan(Ω * Td / 2.0);
            return ω;
        }

        /// <summary>
        /// Discrete-time signal processing 3rd ed. pp534 eq7.27
        /// </summary>
        public WWComplex StoZ(WWComplex s) {
            return WWComplex.Div(
                new WWComplex(1.0 + s.real*Td/2, s.imaginary*Td/2),
                new WWComplex(1.0 - s.real*Td/2, -s.imaginary*Td/2)
                );
        }

        /// <summary>
        /// 連続時間フィルターの伝達関数を離散時間フィルターの伝達関数にBilinear transformする。
        /// Discrete-time signal processing 3rd ed. pp.533
        /// Benoit Boulet, 信号処理とシステムの基礎 pp.681-682
        /// </summary>
        /// <param name="ps">連続時間フィルターの伝達関数</param>
        /// <returns>離散時間フィルターの伝達関数</returns>
        public FirstOrderComplexRationalPolynomial StoZ(FirstOrderComplexRationalPolynomial ps) {
            /*
                   n1s + n0
             ps = ──────────
                   d1s + d0
             
             z^{-1} = zM とする。
             
             Bilinear transform:
                  2     1-zM
             s → ─── * ──────
                  Td    1+zM
             
             2/Td = kとすると以下のように書ける。
             
                 k(1-zM)
             s → ───────
                  1+zM
             
                        k(1-zM)          n1*k(1-zM) + n0(1+zM)
                   n1 * ─────── + n0     ─────────────────────
                         1+zM                    1+zM              n1*k(1-zM) + n0(1+zM)   (n0-n1*k)zM + n0+n1*k
             pz = ─────────────────── = ──────────────────────── = ───────────────────── = ─────────────────────
                        k(1-zM)          d1*k(1-zM) + d0(1+zM)     d1*k(1-zM) + d0(1+zM)   (d0-d1*k)zM + d0+d1*k
                   d1 * ─────── + d0     ─────────────────────
                         1+zM                    1+zM
             
             */

            var n0  = ps.N(0);
            var n1k = WWComplex.Mul(ps.N(1), 2.0 / Td);
            var d0  = ps.D(0);
            var d1k = WWComplex.Mul(ps.D(1), 2.0 / Td);

            var pz = new FirstOrderComplexRationalPolynomial(
                WWComplex.Sub(n0, n1k), WWComplex.Add(n0, n1k),
                WWComplex.Sub(d0, d1k), WWComplex.Add(d0, d1k));

            return pz;
        }

        public void Add(FirstOrderComplexRationalPolynomial ps) {
            mH_s.Add(ps);
            mComplexHzList.Add(StoZ(ps));
        }

        /// <summary>
        /// Addし終わったら呼ぶ。
        /// </summary>
        public void Calc() {
            // mH_zに1次の関数が入っている。

            //　係数が全て実数のmRealHzListを作成する。
            // mRealHzListは、多項式の和を表現する。
            mRealHzList.Clear();
            for (int i = 0; i < mComplexHzList.Count / 2; ++i) {
                var p0 = mComplexHzList[i];
                var p1 = mComplexHzList[mComplexHzList.Count - 1 - i];
                var p = WWPolynomial.Add(p0, p1).ToRealPolynomial();
                mRealHzList.Add(p);
            }
            if ((mComplexHzList.Count & 1) == 1) {
                var p = mComplexHzList[mComplexHzList.Count / 2];

                mRealHzList.Add(new RealRationalPolynomial(
                    new double[] { p.N(0).real, p.N(1).real },
                    new double[] { p.D(0).real, p.D(1).real }));
            }

            var gainR = 0.0;
            foreach (var p in mRealHzList) {
                gainR += p.Evaluate(1.0);
            }
            Console.WriteLine("gainR={0}", gainR);

            TransferFunction = (WWComplex z) => { return TransferFunctionValue(z); };
        }

        private WWComplex TransferFunctionValue(WWComplex z) {
            // 1次有理多項式の和の形の式で計算。
            var zRecip = WWComplex.Reciprocal(z);
            var result = WWComplex.Zero();
            foreach (var H in mComplexHzList) {
                result = WWComplex.Add(result, H.Evaluate(zRecip));
            }
            return result;
        }
    }
}
