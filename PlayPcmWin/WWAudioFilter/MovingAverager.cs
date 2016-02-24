using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    class MovingAverager {
        public int AverageSamples { get; set; }
        public MovingAverager(int averageSamples) {
            AverageSamples = averageSamples;
            mDelayX = new Delay(AverageSamples);
            mLastY = 0;
        }

        public double Filter(double x) {
            double delayedX = mDelayX.Filter(x);

            double y = x - delayedX;
            y += mLastY;

            mLastY = y;
            return y / AverageSamples;
        }

        private Delay mDelayX;
        private double mLastY;
    };
}
