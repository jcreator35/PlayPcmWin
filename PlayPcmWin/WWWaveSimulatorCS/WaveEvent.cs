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

        public EventType mType;
        public int mTime;
        public int mX;
        public float mSc;
        public float mFreq;
        public float mMagnitude;
        public float mΔt;
        public const float SINE_PERIOD = 1000000.0f;
        private const int GAUSSIAN_PERIOD = 200;
        private const int PULSE_PERIOD = 1;

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
                mTime = (int)(PULSE_PERIOD/Sc);
                break;
            }

            mX = (int)x;
        }

        /// <summary>
        /// 時間を進める。
        /// </summary>
        /// <returns>true: 処理完了。</returns>
        public bool UpdateTime() {
            if (IsFinished()) {
                return true;
            }
            --mTime;

            return IsFinished();
        }

        /// <returns>true: 更新完了。false:まだ続く。</returns>
        public bool Update(float[] P) {
            if (IsFinished()) {
                return true;
            }

            switch (mType) {
            case EventType.Gaussian:
                UpdateGaussian(P);
                break;
            case EventType.Sine:
                UpdateSine(P);
                break;
            case EventType.Pulse:
                UpdatePulse(P);
                break;
            }

            return UpdateTime();
        }

        public float HalfPeriod {
            get {
                return (float)(GAUSSIAN_PERIOD / mSc / 2);
            }
        }

        public float GaussianWidth {
            get { return 0.001f; }
        }

        private void UpdateGaussian(float[] P) {
            float fr = (float)Math.Exp(-(mTime - HalfPeriod) * (mTime - HalfPeriod) * GaussianWidth);

            P[mX] += mMagnitude * fr;
        }

        private void UpdateSine(float[] P) {
            // ω: 角周波数
            // f: 周波数
            // T: 周期
            // ω = 2π * f = 2π/T;

            var ω = 2.0f * Math.PI * mFreq;

            P[mX] += mMagnitude * (float)Math.Sin(ω * ElapsedTime());
        }

        private void UpdatePulse(float[] P) {
            P[mX] += mMagnitude;
        }

        /// <summary>
        /// 経過時間 (秒)
        /// </summary>
        public float ElapsedTime() {
            return ((int)(SINE_PERIOD / mSc) - mTime) * mΔt;
        }

        public bool IsFinished() {
            if (mTime <= 0) {
                return true;
            }
            return false;
        }
    };
}
