using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWUtil;

namespace WWWaveSimulatorCS {
    public class WaveSim2D {
        /// <summary>
        /// 気圧P スカラー場
        /// </summary>
        float[] mP;

        /// <summary>
        /// 速度V 2要素ベクトル場
        /// </summary>
        WWVectorF2[] mV;

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
        int mGridH; // y
        int mGridCount; // x * y

        private Delay [] mDelayArray;

        private float [] mAbcCoef;

        /// <summary>
        /// シミュレーションtick。Δtを掛けると時間になる。
        /// </summary>
        int mTimeTick;

        private float mC0 = 1.0f; // 334.0f;             // 334 (m/s)
        private float mΔt = 1.0f; // 1.0e-5f;            // 1x10^-5 (s)
        private float mΔx = 1.0f; // 334.0f * 1.0e-5f;   // 334 * 10^-5 (m)
        private float mSc = 1.0f; // クーラント数は1.0/sqrt(2)     c0 * Δt / Δx;

        private List<WaveEvent> mWaveEventList = new List<WaveEvent>();

        public WaveSim2D(int gridW, int gridH, float c0, float Δt, float Δx) {
            mGridW = gridW;
            mGridH = gridH;
            mGridCount = mGridW * mGridH;

            mSc = 1.0f / (float)Math.Sqrt(2.0);

#if false
            mC0 = c0;
            mΔt = Δt;
            mΔx = Δx;

            mSc = mC0 * mΔt / mΔx;
#endif

            mDelayArray = new Delay[(gridW + gridH) * 2];
            for (int i = 0; i < mDelayArray.Length; ++i) {
                mDelayArray[i] = new Delay(2);
            }

            Reset();
        }

        public void Reset() {
            mP = new float[mGridCount];
            mV = new WWVectorF2[mGridCount];
            for (int i = 0; i < mGridCount; ++i) {
                mV[i] = new WWVectorF2();
            }

            mRoh = new float[mGridCount];
            mCr = new float[mGridCount];
            mLoss = new float[mGridCount];

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
             *         
             * Courant number Sc = c0 Δt / Δx
             */

            for (int i = 0; i < mGridCount; ++i) {
                // 相対密度。
                mRoh[i] = 1.0f;

                // 相対音速。0 < Cr < 1
                mCr[i] = 1.0f;
            }

#if false
            // 上下左右端領域は反射率80％の壁になっている。
            float r = 1.0f; // 0.8 == 80%
            float roh2 = -(r + 1) * 1.0f / (r - 1);
            float loss2 = 0.1f;
            for (int y = 0; y < mGridH; ++y) {
                for (int x = 0; x < mGridW * 1 / 20; ++x) {
                    SetRoh(x, y, roh2);
                    SetLoss(x, y, loss2);
                }
                for (int x = mGridW * 19 / 20; x < mGridW; ++x) {
                    SetRoh(x, y, roh2);
                    SetLoss(x, y, loss2);
                }
            }
            for (int x = 0; x < mGridW; ++x) {
                for (int y = 0; y < mGridH * 1 / 20; ++y) {
                    SetRoh(x, y, roh2);
                    SetLoss(x, y, loss2);
                }
                for (int y = mGridH * 19 / 20; y < mGridH; ++y) {
                    SetRoh(x, y, roh2);
                    SetLoss(x, y, loss2);
                }
            }
#endif

            mWaveEventList.Clear();

            // 2次のABC用の過去データ置き場。
            for (int i = 0; i < mDelayArray.Length; ++i) {
                mDelayArray[i].FillZeroes();
            }
            // ABCの係数。
            mAbcCoef = new float[3];
            float ScPrime = 1.0f;
            float denom = 1.0f / ScPrime + 2.0f + ScPrime;
            mAbcCoef[0] = -(1.0f / ScPrime - 2.0f + ScPrime) / denom;
            mAbcCoef[1] = +(2.0f * ScPrime - 2.0f / ScPrime) / denom;
            mAbcCoef[2] = -(4.0f * ScPrime + 4.0f / ScPrime) / denom;

            mTimeTick = 0;
        }

        public void AddStimulus(WaveEvent.EventType t, float x, float y, float freq, float magnitude) {
            int pos = ((int)x) + ((int)y) * mGridW;

            var ev = new WaveEvent(t, mSc, pos, freq, magnitude, mΔt);
            mWaveEventList.Add(ev);
        }

        private float mMagnitude = 0;

        public float Magnitude() {
            return mMagnitude;
        }

        public int Update() {
            int nStimuli = 0;
            // Stimuli
            var toRemove = new List<WaveEvent>();
            foreach (var v in mWaveEventList) {
                if (v.Update(mP)) {
                    toRemove.Add(v);
                }

                ++nStimuli;
            }
            if (0 < toRemove.Count) {
                foreach (var v in toRemove) {
                    mWaveEventList.Remove(v);
                }
            }

#if true
            // 2nd order ABC (pp.159)
#else
            // ABC for V (Schneider17, pp.53)
            for (int y = 0; y < mGridH; ++y) {
                SetV(mGridW - 1, y, V(mGridW - 2, y));
            }
            for (int x = 0; x < mGridW; ++x) {
                SetV(x, mGridH - 1, V(x, mGridH - 2));
            }
#endif

            // Update V (Schneider17, pp.328)
#if true
            Parallel.For(0, mGridH - 1, y => {
                for (int x = 0; x < mGridW - 1; ++x) {
                    int pos = x + y * mGridW;
                    float loss = mLoss[pos];
                    float Cv = 2.0f * mSc / ((mRoh[pos] + mRoh[pos + 1]) * mC0);

                    var v = V(x, y);
                    float vx = (1.0f - loss) / (1.0f + loss) * v.X - (Cv / (1.0f + loss)) * (P(x + 1, y) - P(x, y));
                    float vy = (1.0f - loss) / (1.0f + loss) * v.Y - (Cv / (1.0f + loss)) * (P(x, y + 1) - P(x, y));
                    SetV(x, y, new WWVectorF2(vx, vy));
                }
            });
#else
            for (int y=0; y<mGridH-1; ++y) {
                for (int x = 0; x < mGridW - 1; ++x) {
                    pos = x + y * mGridW;
                    float loss = mLoss[pos];
                    float Cv = 2.0f * mSc / ((mRoh[pos] + mRoh[pos + 1]) * mC0);
                    WWVectorF2 v = V(x, y);
                    float vx = (1.0f - loss) / (1.0f + loss) * v.X - (Cv / (1.0f + loss)) * (P(x + 1, y) - P(x, y));
                    float vy = (1.0f - loss) / (1.0f + loss) * v.Y - (Cv / (1.0f + loss)) * (P(x, y + 1) - P(x, y));
                    SetV(x, y, new WWVectorF2(vx, vy));
                }
            }
#endif

#if true
#else
            // ABC for P (Schneider17, pp.53)
            for (int y = 0; y < mGridH; ++y) {
                SetP(0, y, P(1, y));
            }
            for (int x = 0; x < mGridW; ++x) {
                SetP(x, 0, P(x, 1));
            }
#endif

            // Update P (Schneider17, pp.325)
#if true
            Parallel.For(1, mGridH, y => {
                for (int x = 1; x < mGridW; ++x) {
                    int pos = x + y * mGridW;
                    float loss = mLoss[pos];
                    var v = V(x, y);
                    float Cp = mRoh[pos] * mCr[pos] * mCr[pos] * mC0 * mSc;
                    mP[pos] = (1.0f - loss) / (1.0f + loss) * mP[pos] 
                        - (Cp / (1.0f + loss))
                        * (v.X - V(x - 1, y).X + v.Y - V(x, y - 1).Y);
                }
            });
#else
            for (int y = 1; y < mGridH; ++y) {
                for (int x = 1; x < mGridW; ++x) {
                    pos = x + y * mGridW;
                    float loss = mLoss[pos];
                    var v = V(x, y);
                    float Cp = mRoh[pos] * mCr[pos] * mCr[pos] * mC0 * mSc;
                    mP[pos] = (1.0f - loss) / (1.0f + loss) * mP[pos] - (Cp / (1.0f + loss))
                        * (v.X - V(x - 1, y).X + v.Y - V(x, y - 1).Y);
                }
            }
#endif

            #if true
            {   // Ricker wavelet
                float length = 20.0f;
                float Sc = 1.0f / (float)Math.Sqrt(2.0);
                var p = (float)Math.PI * ((Sc * mTimeTick) / length - 1.0f);

                p *= p;
                p = (1.0f - 2.0f*p)*(float)Math.Exp(-p);
                SetP(mGridW/2, mGridH/2, p);
            }
#else
            {
                // 平面波
                float d = 0.2f * (float)Math.Sin(2.0f * Math.PI * mTimeTick * 0.01f);
                Console.WriteLine("{0}", d);
                for (int y = mGridH / 20; y < mGridH * 19 / 20; ++y) {
                    int x = mGridW / 20;

                    SetP(x, y, d);
                }
            }
#endif

            float pMax = 0.0f;
            for (int i = 1; i < mP.Length; ++i) {
                if (pMax < Math.Abs(mP[i])) {
                    pMax = Math.Abs(mP[i]);
                }
            }

            mMagnitude = pMax;

            ++mTimeTick;

            return nStimuli;
        }

        private WWVectorF2 V(int x, int y) {
            int pos = x + y * mGridW;
            return mV[pos];
        }
        private void SetV(int x, int y, WWVectorF2 v) {
            int pos = x + y * mGridW;
            mV[pos] = v;
        }
        private float P(int x, int y) {
            int pos = x + y * mGridW;
            return mP[pos];
        }
        private void SetP(int x, int y, float v) {
            int pos = x + y * mGridW;
            mP[pos] = v;
        }
        private void SetRoh(int x, int y, float v) {
            int pos = x + y * mGridW;
            mRoh[pos] = v;
        }
        private void SetLoss(int x, int y, float v) {
            int pos = x + y * mGridW;
            mLoss[pos] = v;
        }

        public float[] Pshow() {
            var p = new float[mGridCount];

            Parallel.For(0, mGridH, y => {
                for (int x = 0; x < mGridW; ++x) {
                    int pos = x + y * mGridW;
                    p[pos] = Math.Abs(mP[pos]);
                }
            });

            return p;
        }

        public float ElapsedTime() {
            return mTimeTick * mΔt;
        }
    }
}
