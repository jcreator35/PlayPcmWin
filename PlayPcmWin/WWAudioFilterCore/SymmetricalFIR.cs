// 日本語

namespace WWAudioFilterCore {
    public class SymmetricalFIR {
        private double[] mHalfCoeffs;

        /// <summary>
        /// 係数の数が奇数か偶数か。
        /// </summary>
        private WWMath.WWParity mParity;

        private WWUtil.Delay mDelay;

        /// <summary>
        /// 偶関数のFIRフィルター。
        /// </summary>
        /// <param name="parity">係数の数が奇数か偶数か。奇数の時フィルター係数の総数はhalfCoeffs*2-1。</param>
        /// <param name="halfCoeffs">フィルター係数の半分または半分+1個</param>
        public SymmetricalFIR(WWMath.WWParity parity, double[] halfCoeffs) {
            // halfCoeffs.Length=3のとき coeffのLength=5
            // mDelayの蓄積サンプル数=5

            mHalfCoeffs = halfCoeffs;
            mParity = parity;

            if (mParity == WWMath.WWParity.Odd) {
                mDelay = new WWUtil.Delay(mHalfCoeffs.Length * 2 - 1);
            } else {
                mDelay = new WWUtil.Delay(mHalfCoeffs.Length * 2);
            }
            mDelay.FillZeroes();
        }

        public void Reset() {
            mDelay.FillZeroes();
        }

        public int FilterLength {
            get {
                if (mParity == WWMath.WWParity.Odd) {
                    return mHalfCoeffs.Length * 2 - 1;
                } else {
                    return mHalfCoeffs.Length * 2;
                }
            }
        }

        /// <summary>
        /// この直線位相FIRフィルターのディレイ。(サンプル)
        /// </summary>
        public double FilterDelay {
            get {
                if (mParity == WWMath.WWParity.Odd) {
                    return mHalfCoeffs.Length - 1;
                } else {
                    return mHalfCoeffs.Length + 0.5;
                }
            }
        }

        public double[] Filter(double[] inPcm) {
            var outPcm = new double[inPcm.Length];

            if (mParity == WWMath.WWParity.Odd) {
                int lastOffs = mHalfCoeffs.Length * 2 - 2;

                for (int pos = 0; pos < inPcm.Length; ++pos) {
                    double x = inPcm[pos];

                    mDelay.Filter(x);

                    double y = 0;
                    for (int i = 0; i < mHalfCoeffs.Length - 1; ++i) {
                        y += mHalfCoeffs[i] * (mDelay.GetNthDelayedSampleValue(i)
                                             + mDelay.GetNthDelayedSampleValue(lastOffs - i));
                    }
                    y += mHalfCoeffs[mHalfCoeffs.Length - 1] * mDelay.GetNthDelayedSampleValue(mHalfCoeffs.Length - 1);

                    outPcm[pos] = y;
                }
            } else {
                int lastOffs = mHalfCoeffs.Length * 2 - 1;

                for (int pos = 0; pos < inPcm.Length; ++pos) {
                    double x = inPcm[pos];

                    mDelay.Filter(x);

                    double y = 0;
                    for (int i = 0; i < mHalfCoeffs.Length; ++i) {
                        y += mHalfCoeffs[i] * (mDelay.GetNthDelayedSampleValue(i)
                                             + mDelay.GetNthDelayedSampleValue(lastOffs - i));
                    }

                    outPcm[pos] = y;
                }
            }

            return outPcm;
        }
    }
}
