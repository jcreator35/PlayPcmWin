// Schneider17: John B.Schneider, Understanding the Finite-Difference Time-domain method, 2017

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWDirectComputeCS;

namespace WWWaveSimulatorCS {
    public class WaveSim1D {
        WWWave1DGpu mCS;

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
        /// (最大音速をc0とすると、その場所の音速ca = cr *c0、0 &lt; cR &le; 1)
        /// </summary>
        float[] mCr;

        int mGridW; // x

        /// <summary>
        /// シミュレーションtick。Δtを掛けると時間になる。
        /// </summary>
        int mTimeTick;

        private float mC0 = 334.0f;  // 334 (m/s) 最も速い場所の音速。(cr==1の場所)。 ca=cr * c0
        private float mΔt = 1.0e-5f; // 1x10^-5 (s)
        private float mΔx = 0;       // 334 * 10^-5 (m)
        private float mSc = 0;       // c0 * Δt / Δx;
        private float mWallReflectivity;

        private List<WaveEvent> mWaveEventList = new List<WaveEvent>();

        public WaveSim1D(int gridW, float c0, float Δt, float Δx, float wallReflectivity) {
            int hr = 0;

            mGridW = gridW;
            mC0 = c0;
            mΔt = Δt;
            mΔx = Δx;
            mWallReflectivity = wallReflectivity;

            mSc = mC0 * mΔt / mΔx;

            Reset();

            mCS = new WWWave1DGpu();

            do {
                hr = mCS.Init();
                if (hr < 0) {
                    return;
                }

                WWWave1DParams p;
                p.dataCount = gridW;
                p.deltaT = mΔt;
                p.sc = mSc;
                p.c0 = mC0;

                hr = mCS.Setup(p, mLoss, mRoh, mCr);
            } while (false);

            if (hr < 0) {
                Console.WriteLine("E: WaveSim1D DirectCompute failed {0:X8}", hr);
            }
        }

        public void Term() {
            mCS.Term();
        }

        public void Reset() {
            mP = new float[mGridW];
            mV = new float[mGridW];
            mRoh = new float[mGridW];
            mCr = new float[mGridW];
            mLoss = new float[mGridW];

            // 相対音速。0 < Cr <= 1
            for (int i = 0; i < mGridW; ++i) {
                mCr[i] = 1; // 1.0f;
            }

            /*
             * Ca == Cr * C0
             * Sc = C0Δt/Δx (1Dのときは1以下、2Dのときは0.7以下)
             * 
             * 音響インピーダンスη=ρ*Ca (Schneider17, pp.63, pp.325)
             * η1から前進しη2の界面に達した波が界面で反射するとき
             *
             *             η2-η1
             * 反射率 r = ────────
             *             η2+η1
             * 
             * 媒質1のインピーダンスη1と反射率 → 媒質2のインピーダンスη2 を得る式:
             *
             *       -(r+1)η1
             * η2 = ─────────
             *         r-1
             */

            for (int i = 0; i < mGridW; ++i) {
                mRoh[i] = 1.0f;
            }

            // 左右端領域は反射率rの壁になっている。
            float r = mWallReflectivity; // 0.9 == 90%
            float roh2 = -(r + 1) * 1.0f / (r - 1);
            for (int i = 0; i < mGridW * 1 / 20; ++i) {
                mRoh[i] = roh2;
                mLoss[i] = 0.1f;
            }
            for (int i = mGridW *19/20; i < mGridW; ++i) {
                mRoh[i] = roh2;
                mLoss[i] = 0.1f;
            }

            mWaveEventList.Clear();

            mTimeTick = 0;
        }

        public void AddStimulus(WaveEvent.EventType t, int x, float freq, float magnitude) {
            var ev = new WaveEvent(t, mSc, x, freq, magnitude, mΔt);
            mWaveEventList.Add(ev);
        }

        private float mMagnitude = 0;
        
        public float Magnitude() {
            return mMagnitude;
        }

        public int Update(int nTimes) {
            if (mCS.Available) {
                UpdateGPU(nTimes);

                // Stimuli更新
                for (int i = 0; i < nTimes; ++i) {
                    var toRemove = new List<WaveEvent>();
                    foreach (var v in mWaveEventList) {
                        if (v.UpdateTime()) {
                            toRemove.Add(v);
                        }
                    }
                    if (0 < toRemove.Count) {
                        foreach (var v in toRemove) {
                            mWaveEventList.Remove(v);
                        }
                    }
                }

                mTimeTick += nTimes;
            } else {
                for (int i = 0; i < nTimes; ++i) {
                    UpdateCPU1();
                }
            }

            // mMagnitude計算。
            float pMax = 0.0f;
            for (int i = 1; i < mP.Length; ++i) {
                if (pMax < Math.Abs(mP[i])) {
                    pMax = Math.Abs(mP[i]);
                }
            }

            mMagnitude = pMax;

            return mWaveEventList.Count;
        }

        const int N_STIM = 4;

        public void UpdateGPU(int nTimes) {

            WWWave1DStim[] stim = new WWWave1DStim[N_STIM];
            for (int i = 0; i < N_STIM; ++i) {
                stim[i] = new WWWave1DStim();
            }

            int nStim = N_STIM;
            if (mWaveEventList.Count < nStim) {
                nStim = mWaveEventList.Count;
            }

            for (int i=0; i<nStim; ++i) {
                var w = mWaveEventList[i];
                stim[i].type = (int)w.mType;
                stim[i].counter = w.mTime;
                stim[i].pos = w.mPos;
                stim[i].magnitude = w.mMagnitude;
                stim[i].halfPeriod = w.HalfPeriod;
                stim[i].width = w.GaussianWidth;
                stim[i].omega = (float)(2.0f * Math.PI * w.mFreq);
                stim[i].period = WaveEvent.SINE_PERIOD;
            }

            mCS.Run(nTimes, nStim, stim);
            mCS.GetResultVP(mGridW, mV, mP);
        }

        public void UpdateCPU1() {
            // Stimuli更新。
            var toRemove = new List<WaveEvent>();
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

#if true
            Parallel.For(0, mV.Length-1,  i => {
                /*
                float loss = mLoss[i];
                float Cv = 2.0f * mSc / ((mRoh[i] + mRoh[i + 1]) * mC0);
                mV[i] = (1.0f - loss) / (1.0f + loss) * mV[i] - (Cv / (1.0f + loss)) * (mP[i + 1] - mP[i]);
                 */
                mV[i] = mV[i] - (mP[i + 1] - mP[i]);
            });
#else
            for (int i = 0; i < mV.Length-1; ++i) {
                float loss = mLoss[i];
                float Cv = 2.0f * mSc / ((mRoh[i] + mRoh[i + 1]) * mC0);
                mV[i] = (1.0f-loss)/(1.0f+loss) * mV[i] - (Cv/(1.0f+loss)) * (mP[i + 1] - mP[i]);
            }
#endif

            // ABC for P (Schneider17, pp.53)
            mP[0] = mP[1];
            // Update P (Schneider17, pp.325)
#if true
            Parallel.For(1, mP.Length, i => {
                /*
                float loss = mLoss[i];
                float Cp = mRoh[i] * mCr[i] * mCr[i] * mC0 * mSc;
                mP[i] = (1.0f - loss) / (1.0f + loss) * mP[i] - (Cp / (1.0f + loss)) * (mV[i] - mV[i - 1]);
                 */
                mP[i] = mP[i] - (mV[i] - mV[i - 1]);
            });
#else
            for (int i = 1; i < mP.Length; ++i) {
                float loss = mLoss[i];
                float Cp = mRoh[i] * mCr[i] * mCr[i] * mC0 * mSc;
                mP[i] = (1.0f-loss)/(1.0f+loss) * mP[i] - (Cp/(1.0f+loss)) * (mV[i] - mV[i - 1]);
            }
#endif
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

        public int ElapsedCount() {
            return mTimeTick;
        }
    }
}
