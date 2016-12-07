using System;
using WWMath;

namespace WWAudioFilter {
    public class ButterworthDesign {
        private double mH0;
        private double mHc;
        private double mHs;

        /// <summary>
        /// cutoff frequency. rad/s
        /// </summary>
        private double mωc;

        /// <summary>
        /// (Ωc=1としたときの)正規化ストップバンド周波数. Ωs = ωs/ωc 
        /// </summary>
        private double mΩs;
        private double mβ;
        private int mN;

        public enum BetaType {
            BetaMax,
            BetaMin
        };

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="h0">gain of 0 Hz</param>
        /// <param name="hc">gain of cutoff frequency</param>
        /// <param name="hs">stopband gain</param>
        /// <param name="ωc">cut off frequency. rad/s</param>
        /// <param name="ωs">stopband frequency. rad/s</param>
        public ButterworthDesign(double h0, double hc, double hs, double ωc, double ωs, BetaType bt) {
            if (h0 <= 0) {
                throw new System.ArgumentOutOfRangeException("h0");
            }
            if (hc <= 0 || h0 <= hc) {
                throw new System.ArgumentOutOfRangeException("hc");
            }
            if (hs <= 0 || hc <= hs) {
                throw new System.ArgumentOutOfRangeException("hs");
            }
            if (ωs <= ωc) {
                throw new System.ArgumentOutOfRangeException("ωs");
            }

            mH0 = h0;
            mHc = hc;
            mHs = hs;
            mωc = ωc;
            mΩs = ωs / ωc;

            mN = CalcNfMin();

            switch (bt) {
            case BetaType.BetaMax:
                mβ = Calcβmax();
                break;
            case BetaType.BetaMin:
                mβ = Calcβmin();
                break;
            }
        }

        /// <summary>
        /// ノーマライズド　バターワースフィルターの最小次数 Nfmin。
        /// カットオフ周波数は1 rad/s。ωc rad/sのとき sをωcで割る。
        /// </summary>
        /// <param name="h0">通過域最大ゲイン</param>
        /// <param name="hc">通過域最小ゲイン</param>
        /// <param name="hs">ストップバンド最大ゲイン</param>
        /// <param name="omegaS">ストップバンド周波数 (Hz)</param>
        /// <returns>最小次数</returns>
        private int CalcNfMin() {
            // Analog Electronic Filters pp.44
            double nfmin = Math.Log10(( ( mH0 * mH0 ) / ( mHs * mHs ) - 1.0 ) / ( ( ( mH0 * mH0 ) / ( mHc * mHc ) ) - 1.0 )) / ( 2.0 * Math.Log10(mΩs) );
            return (int)( Math.Ceiling(nfmin) );
        }

        private double Calcβmax() {
            // Analog Electronic Filters pp.42
            return Math.Sqrt(mH0 * mH0 / mHc / mHc - 1.0);
        }

        private double Calcβmin() {
            // Analog Electronic Filters pp.44
            return Math.Sqrt(mH0 * mH0 / mHs / mHs - 1.0) / Math.Pow(mΩs, mN);
        }

        /// <summary>
        /// ノーマライズド　バターワースフィルターの最小次数 Nfmin。
        /// </summary>
        public int Order() {
            return mN;
        }

        public double Beta() {
            return mβ;
        }

        /// <summary>
        /// カットオフ周波数ωc (rad/s)。
        /// sをこの値で割る(周波数スケーリング)
        /// </summary>
        public double CutoffFrequency() {
            return mωc;
        }

        /// <summary>
        /// カットオフ周波数 (Hz)
        /// </summary>
        public double CutoffFrequencyHz() {
            return mωc / (2.0 * Math.PI);
        }

        /// <summary>
        /// s平面の左半面にある極の個数。
        /// </summary>
        public int PoleNum() {
            return mN;
        }

        /// <summary>
        /// H(s)の分母の多項式の根を戻す。
        /// カットオフ周波数は1 rad/s。ωc Hzのとき sをωcで割る。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// </summary>
        public WWComplex PoleNth(int nth) {
            if (nth < 0 || mN <= 0 || mN * 2 <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // Analog Electronic Filters pp.50
            if (nth < mN) {
                // nth < Nの時 sk+
                // H(s) s平面の左半面にある曲。
                double angle = ( 2.0 * nth + 1.0 ) / 2.0 / mN * Math.PI + Math.PI / 2.0;
                double re = Math.Pow(mβ, -1.0 / mN) * Math.Cos(angle);
                double im = Math.Pow(mβ, -1.0 / mN) * Math.Sin(angle);
                return new WWComplex(re, im);
#if false
            } else {
                // N <= nthの時 sk-
                // H(-s) s平面の右半面にある曲。
                nth -= mN;

                double angle = ( 2.0 * nth + 1.0 ) / 2.0 / mN * Math.PI - Math.PI / 2.0;
                double re = Math.Pow(mβ, -1.0 / mN) * Math.Cos(angle);
                double im = Math.Pow(mβ, -1.0 / mN) * Math.Sin(angle);
                return new WWComplex(re, im);
#endif
            }

            System.Diagnostics.Debug.Assert(false);
            return new WWComplex(0,0);
        }

        /// <summary>
        /// H(s)の定数倍成分h0/βを戻す。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// </summary>
        public double TransferFunctionConstant() {
            return mH0 / mβ;
        }
    };

};
