using System;

namespace WWUtil {
    public class DelayT<T> {
        public int DelaySamples { get { return mDelay.Length; } }

        // ring buffer
        private int mPos;
        private T[] mDelay;

        public DelayT<T> CreateCopy() {
            var r = new DelayT<T>(mDelay.Length);
            
            r.mPos = mPos;
            Array.Copy(mDelay, r.mDelay, mDelay.Length);

            return r;
        }

        /// <summary>
        /// n samples delay。初期状態はnullが詰まっている。必要に応じてFill()で詰める。
        /// </summary>
        public DelayT(int n) {
            if (n < 1) {
                throw new ArgumentOutOfRangeException("n");
            }

            mDelay = new T[n];
            mPos = 0;
        }

        public T Filter(T x) {
            System.Diagnostics.Debug.Assert(0 <= mPos && mPos < mDelay.Length);

            // 元々mDelay[mPos]に入っていた値をyに複製してからxで上書きする。
            // この2行は順番が重要だ
            T y = mDelay[mPos];
            mDelay[mPos] = x;

            // advance the position
            ++mPos;
            if (mDelay.Length <= mPos) {
                mPos = 0;
            }

            return y;
        }

        /// <summary>
        /// nthサンプル過去のサンプル値を戻す。
        /// </summary>
        /// <param name="nth">0: 最新のサンプル、1: 1サンプル過去のサンプル。</param>
        public T GetNthDelayedSampleValue(int nth) {
            int pos = mPos - 1 - nth;

            if (pos < 0) {
                pos += mDelay.Length;
            }

            System.Diagnostics.Debug.Assert(0 <= pos && pos < mDelay.Length);
            return mDelay[pos];
        }

        public void Fill(T v) {
            for (int i = 0; i < mDelay.Length; ++i) {
                mDelay[i] = v;
            }
            mPos = 0;
        }
    }
}
