using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWUtil;

namespace WWMath {
    // Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 744
    public class WWSlidingDFT {
        int mN;
        SlidingDFTbin[] mBins;
        Delay mDelayN;

        /// <summary>
        /// 注：この関数は、結果をN分の1します。
        /// </summary>
        /// <param name="N">DFTサイズ。</param>
        public WWSlidingDFT(int N) {
            if (N <= 0) {
                throw new ArgumentOutOfRangeException("N");
            }

            mN = N;
            mBins = new SlidingDFTbin[N];
            for (int i = 0; i < mBins.Length; ++i) {
                mBins[i] = new SlidingDFTbin(i, N);
            }

            mDelayN = new Delay(N);
        }

        /// <summary>
        /// 時間ドメイン値x(n)を1個入力すると周波数ドメイン値X^m(q)が出る。qは時間、mは周波数, 0≦m＜N
        /// </summary>
        /// <param name="x">時間ドメイン値x(n)</param>
        /// <returns>m要素の周波数ドメイン値X^m(q) mは周波数, 0≦m＜N </returns>
        public WWComplex[] Filter(double x) {
            // N comb filter
            double delay = mDelayN.Filter(x);
            double combOut = x - delay;

            // complex resonator
            var r = new WWComplex[mN];
            for (int i = 0; i < mN; ++i) {
                r[i] = mBins[i].Filter(combOut);
            }

            return r;
        }

        class SlidingDFTbin {
            int mN; //< DFT size N
            double mNinv;
            int mM; //< bin number m 0≦m＜N
            WWComplex mE;

            Delay mDelay1i;
            Delay mDelay1q;

            public SlidingDFTbin(int m, int N) {
                if (N <= 0) {
                    throw new ArgumentOutOfRangeException("N");
                }
                if (m < 0 || N <= m) {
                    throw new ArgumentOutOfRangeException("m");
                }

                mN = N;
                mNinv = 1.0 / N;
                mM = m;

                double θ = 2.0 * Math.PI * m / N;

                mE = new WWComplex(Math.Cos(θ), Math.Sin(θ));

                mDelay1i = new Delay(1);
                mDelay1q = new Delay(1);
            }

            /// <summary>
            /// 時間ドメイン値xを1サンプル投入。周波数ビン X^m(q)が出てくる。
            /// 起動後0～N-1サンプルまでは、出力値は過渡値となる。
            /// </summary>
            /// <param name="x">時間ドメインサンプル値x</param>
            /// <returns>周波数ビン X^m(q)。起動後0～N-1サンプルまでは、出力値は過渡値となる。</returns>
            public WWComplex Filter(double x) {

                // Complex resonator
                WWComplex delay2 = new WWComplex(mDelay1i.GetNthDelayedSampleValue(0), mDelay1q.GetNthDelayedSampleValue(0));
                WWComplex acc = WWComplex.Add(new WWComplex(x, 0), delay2);

                WWComplex m = WWComplex.Mul(acc, mE);

                mDelay1i.Filter(m.real);
                mDelay1q.Filter(m.imaginary);

                return WWComplex.Mul(m, mNinv);
            }
        };
    }
}
