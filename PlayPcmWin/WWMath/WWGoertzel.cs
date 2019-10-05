// 日本語。

using System;
using WWUtil;

namespace WWMath {
    // Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 840
    public class WWGoertzel {
        int mN;
        DelayT<WWComplex> mDelay;
        WWQuadratureOscillatorInt mOsc;

        /// <summary>
        /// NサンプルDFTのm番目の周波数binの値を戻す。
        /// </summary>
        /// <param name="N">DFTサイズ。</param>
        public WWGoertzel(int N) {
            if (N <= 0) {
                throw new ArgumentOutOfRangeException("N");
            }

            mN = N;
            mDelay = new DelayT<WWComplex>(N);
            mDelay.Fill(WWComplex.Zero());

            mOsc = new WWQuadratureOscillatorInt(1, N);
        }

        public WWComplex Filter(double x) {
            var c = mOsc.Next();
            var m = WWComplex.Mul(c, x);
            var prev = mDelay.Filter(m);

            return WWComplex.Add(m, prev);
        }
    }
}
