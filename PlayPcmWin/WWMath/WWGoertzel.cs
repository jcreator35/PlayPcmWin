// 日本語。

using System;
using WWUtil;

namespace WWMath {
    /// <summary>
    /// Stable Goertzel algorithm
    /// Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 840
    /// </summary>
    public class WWGoertzel {
        int mN;
        DelayT<WWComplex> mDelay;
        WWQuadratureOscillatorInt mOsc;

        /// <summary>
        /// NサンプルDFTのm番目の周波数binの値を戻す。
        /// </summary>
        /// <param name="m">周波数binの番号。</param>
        /// <param name="N">DFTサイズ。</param>
        public WWGoertzel(int m, int N) {
            if (N <= 0) {
                throw new ArgumentOutOfRangeException("N");
            }

            mN = N;
            mDelay = new DelayT<WWComplex>(1);
            mDelay.Fill(WWComplex.Zero());

            mOsc = new WWQuadratureOscillatorInt(-m, N);
        }

        /// <summary>
        /// 時間ドメイン値xを1サンプル入力、N点DFTの周波数ドメイン値Xのm番目の周波数成分値を出力。
        /// </summary>
        /// <param name="x">時間ドメイン値x</param>
        /// <returns>X^m(q)</returns>
        public WWComplex Filter(double x) {
            var c = mOsc.Next();
            var m = WWComplex.Mul(c, x);

            var prev = mDelay.GetNthDelayedSampleValue(0);

            var r = WWComplex.Add(m, prev);

            mDelay.Filter(r);

            return r;
        }
    }
}
