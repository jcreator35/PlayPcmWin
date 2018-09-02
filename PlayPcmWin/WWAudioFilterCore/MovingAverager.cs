using WWUtil;

namespace WWAudioFilterCore {
    public class MovingAverager {
        public int AverageSamples { get { return mDelayX.DelaySamples; } }

        public MovingAverager(int averageSamples) {
            mDelayX = new Delay(averageSamples);
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
