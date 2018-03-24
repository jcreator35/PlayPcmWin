// John B.Schneider, Understanding the Finite-Difference Time-domain method, pp.325-328


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
        float[] mρ;

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

        private static float c0 = 1.0f; //
        private static float Δt = 1.0f; //
        private static float Δx = 1.0f; // 1m
        private static float Sc = 0; // c0 * Δt / Δx;

        private List<WaveEvent> mWaveEventList = new List<WaveEvent>();

        public WaveSim1D(int gridW) {
            mGridW = gridW;

            Sc = c0 * Δt / Δx;

            Reset();
        }

        public void Reset() {
            mP = new float[mGridW];
            mV = new float[mGridW];
            mρ = new float[mGridW];
            for (int i = 0; i < mGridW; ++i) {
                mρ[i] = 1.0f;
            }

            mCr = new float[mGridW];
            for (int i = 0; i < mGridW; ++i) {
                mCr[i] = 1.0f;
            }

            mWaveEventList.Clear();

            mTimeTick = 0;
        }

        public void AddStimula(float x) {
            var ev = new WaveEvent(WaveEvent.EventType.Gaussian, Sc, x);
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
            
            // ABC for V (pp.53)
            mV[mV.Length - 1] = mV[mV.Length - 2];
            // Update V (John B.Schneider, Understanding the Finite-Difference Time-domain method, pp.328)
            for (int i = 0; i < mV.Length-1; ++i) {
                float Cv = 2.0f * Sc / ((mρ[i] + mρ[i + 1]) * c0);
                mV[i] -= Cv * (mP[i + 1] - mP[i]);
            }

            // ABC for P (pp.53)
            mP[0] = mP[1];
            // Update P (John B.Schneider, Understanding the Finite-Difference Time-domain method, pp.325)
            for (int i = 1; i < mP.Length; ++i) {
                float Cp = mρ[i] * mCr[i] * mCr[i] * c0 * Sc;
                mP[i] -= Cp * (mV[i] - mV[i - 1]);
            }

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
            if (mρ.Length <= x) {
                return mρ[mρ.Length - 1];
            }
            return mρ[x];
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
            return mTimeTick * Δt;
        }
    }
}
