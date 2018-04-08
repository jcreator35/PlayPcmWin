using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWWaveSimulatorCS {
    public class WaveEvent {
        public enum EventType {
            Gaussian,
            Sine,
            Pulse,
        };

        private EventType mType;
        private int mTime;
        private int mX;
        private float mSc;
        private float mFreq;
        private float mMagnitude;
        private float mΔt;
        private const float PULSE_MAGNITUDE = 1.0f;
        private const float SINE_PERIOD = 1000000.0f;
        private const int GAUSSIAN_PERIOD = 200;

        public WaveEvent(EventType t, float Sc, float x, float freq, float magnitude, float Δt) {
            mType = t;
            mSc = Sc;
            mFreq = freq;
            mMagnitude = magnitude;
            mΔt = Δt;

            switch (t) {
            case EventType.Gaussian:
                mTime = (int)(GAUSSIAN_PERIOD/ Sc);
                break;
            case EventType.Sine:
                mTime = (int)(SINE_PERIOD / Sc);
                break;
            case EventType.Pulse:
                mTime = 1;
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
            case EventType.Sine:
                result = UpdateSine(P);
                break;
            case EventType.Pulse:
                result = UpdatePulse(P);
                break;
            }

            return result;
        }

        private bool UpdateGaussian(float[] P) {
            float halfPeriod = (float)(GAUSSIAN_PERIOD/mSc/2);
            float width = 0.001f;

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

        /// <summary>
        /// 経過時間 (秒)
        /// </summary>
        public float ElapsedTime() {
            return  ((int)(SINE_PERIOD / mSc) - mTime) * mΔt;
        }

        private bool UpdateSine(float[] P) {
            // ω: 角周波数
            // f: 周波数
            // T: 周期
            // ω = 2π * f = 2π/T;

            var ω = 2.0f * Math.PI * mFreq;

            P[mX] += mMagnitude * (float)Math.Sin(ω * ElapsedTime());

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
