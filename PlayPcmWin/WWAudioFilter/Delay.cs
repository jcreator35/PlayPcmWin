using System;

namespace WWAudioFilter {
    class Delay {
        public int DelaySamples { get { return mDelay.Length; } }

        // ring buffer
        private int mPos;
        private double[] mDelay;

        /// <summary>
        /// n samples delay
        /// </summary>
        public Delay(int n) {
            if (n < 1) {
                throw new ArgumentOutOfRangeException("n");
            }

            mDelay = new double[n];
            mPos = 0;
        }

        public double Filter(double x) {
            // 元々mDelay[mPos]に入っていた値をyに複製してからxで上書きする。
            // この2行は順番が重要だ
            double y = mDelay[mPos];
            mDelay[mPos] = x;

            // advance the position
            ++mPos;
            if (mDelay.Length <= mPos) {
                mPos = 0;
            }

            return y;
        }

    }
}
