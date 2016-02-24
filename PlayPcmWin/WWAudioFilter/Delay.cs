using System;

namespace WWAudioFilter {
    class Delay {
        public int DelaySamples { get { return mDelay.Length -1; } }

        // ring buffer
        private int mReadPos;
        private int mWritePos;
        private double[] mDelay;

        public Delay(int n) {
            if (n < 1) {
                throw new ArgumentOutOfRangeException("n");
            }

            mDelay = new double[n+1];
            mReadPos = 1;
            mWritePos = 0;
        }

        public double Filter(double x) {
            mDelay[mWritePos] = x;
            double y = mDelay[mReadPos];

            // advance positions
            ++mWritePos;
            if (mDelay.Length <= mWritePos) {
                mWritePos = 0;
            }

            ++mReadPos;
            if (mDelay.Length <= mReadPos) {
                mReadPos = 0;
            }

            return y;
        }

    }
}
