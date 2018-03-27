// [Schneider17] John B.Schneider, Understanding the Finite-Difference Time-domain method, 2017, pp.325-328


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWWaveSimulatorCS {
    public class WaveSim1D {
        /// <summary>
        /// 気圧P スカラー場
        /// </summary>
        float [] mP;

        /// <summary>
        /// 速度V スカラー場
        /// </summary>
        float [] mV;

        /// <summary>
        /// 密度ρ
        /// </summary>
        float[] mRoh;

        float[] mLoss;

        /// <summary>
        /// その場所の相対音速cr
        /// (最大音速をc0とすると、その場所の音速ca = cr *c0、0&lt;cR&le;1)
        /// </summary>
        float[] mCr;

        int mGridW; // x

        /// <summary>
        /// シミュレーションtick。Δtを掛けると時間になる。
        /// </summary>
        int mTimeTick;

        private float mC0 = 334.0f;             // 334 (m/s)
        private float mΔt = 1.0e-5f;            // 1x10^-5 (s)
        private float mΔx = 334.0f * 1.0e-5f;   // 334 * 10^-5 (m)
        private float mSc = 0; // c0 * Δt / Δx;

        private List<WaveEvent> mWaveEventList = new List<WaveEvent>();

        public WaveSim1D(int gridW, float c0, float Δt, float Δx) {
            mGridW = gridW;
            mC0 = c0;
            mΔt = Δt;
            mΔx = Δx;

            mSc = mC0 * mΔt / mΔx;

            Reset();
        }

        public void Reset() {
            mP = new float[mGridW];
            mV = new float[mGridW];
            mRoh = new float[mGridW];
            mCr = new float[mGridW];
            mLoss = new float[mGridW];

            /*
             * 音響インピーダンスη=ρ*Ca (Schneider17, pp.63, pp.325)
             * η1から前進しη2の界面に達した波が界面で反射するとき
             *
             *             η2-η1
             * 反射率 r = ────────
             *             η2+η1
             * 
             * 媒質1のインピーダンスη1と反射率→媒質2のインピーダンスη2を得る式:
             *
             *       -(r+1)η1
             * η2 = ─────────
             *         r-1
             */

            for (int i = 0; i < mGridW; ++i) {
                mRoh[i] = 1.0f;
            }
            float r = 0.9f;
            float roh2 = -(r + 1) * 1.0f / (r - 1);

            // 左右端領域は反射率90％の壁になっている。
            for (int i = 0; i < mGridW * 1 / 20; ++i) {
                mRoh[i] = roh2;
                mLoss[i] = 0.1f;
            }
            for (int i = mGridW *19/20; i < mGridW; ++i) {
                mRoh[i] = roh2;
                mLoss[i] = 0.1f;
            }

            // 相対音速。0 < Cr < 1
            for (int i = 0; i < mGridW; ++i) {
                mCr[i] = 1.0f;
            }

            mWaveEventList.Clear();

            mTimeTick = 0;
        }

        public void AddStimula(float x) {
            var ev = new WaveEvent(WaveEvent.EventType.Gaussian, mSc, x);
            mWaveEventList.Add(ev);
        }

        public void Update() {
            // Stimuli
            List<WaveEvent> toRemove = new List<WaveEvent>();
            foreach (var v in mWaveEventList) {
                if (v.Update(mP)) {
                    toRemove.Add(v);
                }
            }
            if (0 < toRemove.Count) {
                foreach (var v in toRemove) {
                    mWaveEventList.Remove(v);
                }
            }

            // ABC for V (Schneider17, pp.53)
            mV[mV.Length - 1] = mV[mV.Length - 2];
            // Update V (Schneider17, pp.328)
            for (int i = 0; i < mV.Length-1; ++i) {
                float loss = mLoss[i];
                float Cv = 2.0f * mSc / ((mRoh[i] + mRoh[i + 1]) * mC0);
                mV[i] = (1.0f-loss)/(1.0f+loss) * mV[i] - (Cv/(1.0f+loss)) * (mP[i + 1] - mP[i]);
            }

            // ABC for P (Schneider17, pp.53)
            mP[0] = mP[1];
            // Update P (Schneider17, pp.325)
            for (int i = 1; i < mP.Length; ++i) {
                float loss = mLoss[i];
                float Cp = mRoh[i] * mCr[i] * mCr[i] * mC0 * mSc;
                mP[i] = (1.0f-loss)/(1.0f+loss) * mP[i] - (Cp/(1.0f+loss)) * (mV[i] - mV[i - 1]);
            }

            float pMax = 0.0f;
            for (int i = 1; i < mP.Length; ++i) {
                if (pMax < Math.Abs(mP[i])) {
                    pMax = Math.Abs(mP[i]);
                }
            }

            Console.WriteLine("pMax={0}", pMax);

            ++mTimeTick;
        }

        public float[] P() {
            return mP;
        }

        public float[] V() {
            return mV;
        }

        public float ρ(int x) {
            System.Diagnostics.Debug.Assert(0 <= x);
            if (mRoh.Length <= x) {
                return mRoh[mRoh.Length - 1];
            }
            return mRoh[x];
        }

        public float P(int x) {
            System.Diagnostics.Debug.Assert(0 <= x);
            if (mP.Length <= x) {
                return mP[mP.Length - 1];
            }

            return mP[x];
        }

        public float V(int x) {
            System.Diagnostics.Debug.Assert(0 <= x);
            if (mV.Length <= x) {
                return mV[mV.Length - 1];
            }
            return mV[x];
        }

        public void UpdateP(int x, float p) {
            int pos = x;
            mP[pos] = p;
        }
        public void UpdateV(int x, float v) {
            int pos = x;
            mV[pos] = v;
        }

        public float ElapsedTime() {
            return mTimeTick * mΔt;
        }
    }
}
