using System;
using System.Collections.Generic;

namespace WWAudioFilter {
    class Delay {
        public Delay(int n) {
            if (n < 1) {
                throw new ArgumentOutOfRangeException("n");
            }

            DelaySamples = n;
            mDelay = new List<double>();
            for (int i = 0; i < DelaySamples+1; ++i) {
                mDelay.Add(0);
            }
        }

        public double Filter(double x) {
            mDelay.Add(x);
            mDelay.RemoveAt(0);

            return mDelay[0];
        }

        public int DelaySamples { get; set; }
        private List<double> mDelay;
    }
}
