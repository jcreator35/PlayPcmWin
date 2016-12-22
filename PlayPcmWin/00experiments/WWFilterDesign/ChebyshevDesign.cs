using System;

namespace WWAudioFilter {
    public class ChebyshevDesign : ApproximationBase {
        private int mOrder;
        private double mε;
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

        public ChebyshevDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
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

            mOrder = CalcOrder();

            switch (bt) {
            case ApproximationBase.BetaType.BetaMax:
                mε = Math.Sqrt(h0*h0/hc/hc -1);
                break;
            case ApproximationBase.BetaType.BetaMin:
                break;
            }
        }

        private int CalcOrder() {
            double numer = WWMath.Functions.ArCosHypPositive(
                    Math.Sqrt((mH0 * mH0 / mHs / mHs - 1)
                        / (mH0 * mH0 / mHc / mHc - 1)));
            double denom = WWMath.Functions.ArCosHypPositive(mΩs);

            return (int)Math.Ceiling(numer /denom);
        }
    }
};
