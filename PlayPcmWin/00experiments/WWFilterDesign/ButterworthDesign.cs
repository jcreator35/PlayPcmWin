using System;

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
            double nfmin = Math.Log(( ( mH0 * mH0 ) / ( mHs * mHs ) - 1.0 ) / ( ( ( mH0 * mH0 ) / ( mHc * mHc ) ) - 1.0 )) / ( 2.0 * Math.Log(mΩs) );
            return (int)( Math.Ceiling(nfmin) );
        }

        private double Calcβmax() {
            return Math.Sqrt(mH0 * mH0 / mHc / mHc - 1.0);
        }

        private double Calcβmin() {
            return Math.Sqrt(mH0 * mH0 / mHs / mHs - 1.0) / Math.Pow(mΩs, mN);
        }

        public int Order() {
            return mN;
        }

        public int PoleNum() {
            return 2 * mN;
        }

        /// <summary>
        /// H(s)の分母の多項式の根を戻す。
        /// カットオフ周波数は1 rad/s。ωc Hzのとき sをωc*πで割る。
        /// H(s) = frac{h0/β}{\Pi_{k=0}^{N-1}{\(s-sk+\)}}
        /// </summary>
        public WWComplex PoleNth(int nth) {
            if (nth < 0 || mN <= 0 || mN * 2 <= nth) {
                throw new System.ArgumentOutOfRangeException("nth");
            }

            // Analog Electronic Filters p.50
            if (nth < mN) {
                // nth < Nの時 sk+
                // s平面の左側のポール
                double angle = ( 2.0 * nth + 1.0 ) / 2.0 / mN * Math.PI + Math.PI / 2.0;
                double re = Math.Pow(mβ, -1.0 / mN) * Math.Cos(angle);
                double im = Math.Pow(mβ, -1.0 / mN) * Math.Sin(angle);
                return new WWComplex(re, im);
            } else {
                // N <= nthの時 sk-
                // s平面の右側のポール
                nth -= mN;

                double angle = ( 2.0 * nth + 1.0 ) / 2.0 / mN * Math.PI - Math.PI / 2.0;
                double re = Math.Pow(mβ, -1.0 / mN) * Math.Cos(angle);
                double im = Math.Pow(mβ, -1.0 / mN) * Math.Sin(angle);
                return new WWComplex(re, im);
            }
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
