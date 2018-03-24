using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWWaveSimulatorCS {
    class WaveEvent {
        public enum EventType {
            Gaussian,
            Pulse,
            Sine,
        };

        private EventType mType;
        private int mTime;
        private int mX;
        private float mSc;
        private const float PULSE_MAGNITUDE = 1.0f;
        private const float SINE_PERIOD = 50.0f;
        private const int GAUSSIAN_PERIOD = 50;

        public WaveEvent(EventType t, float Sc, float x) {
            mType = t;
            mSc = Sc;

            switch (t) {
            case EventType.Gaussian:
                mTime = (int)(GAUSSIAN_PERIOD/ Sc);
                break;
            case EventType.Pulse:
                mTime = 1;
                break;
            case EventType.Sine:
                mTime = (int)(SINE_PERIOD / Sc);
                break;
            }

            mX = (int)x;
        }

        /// <returns>true: 更新完了。false:まだ続く。</returns>
        public bool Update(float[] P) {
            if (IsFinished()) {
                return true;
            }

            bool result = true;
            switch (mType) {
            case EventType.Gaussian:
                result = UpdateGaussian(P);
                break;
            case EventType.Pulse:
                result = UpdatePulse(P);
                break;
            case EventType.Sine:
                result = UpdateSine(P);
                break;
            }

            return result;
        }

        private bool UpdateGaussian(float[] P) {
            float halfPeriod = (float)(GAUSSIAN_PERIOD/mSc/2);
            float width = 0.01f;

            float fr = (float)Math.Exp(-(mTime - halfPeriod) * (mTime - halfPeriod) * width);

            P[mX] += fr;

            --mTime;
            return IsFinished();
        }

        private bool UpdatePulse(float[] P) {
            P[mX] += PULSE_MAGNITUDE;

            --mTime;
            return IsFinished();
        }

        private bool UpdateSine(float[] P) {
            P[mX] += 0.5f * (float)Math.Sin(2.0 * Math.PI * (float)(SINE_PERIOD - mSc * mTime) / SINE_PERIOD);

            --mTime;
            return IsFinished();
        }

        public bool IsFinished() {
            if (mTime <= 0) {
                return true;
            }
            return false;
        }
    };
}
