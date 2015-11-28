using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    class OverlappedFft {
        private readonly int mFftLength;
        public int FftLength { get { return mFftLength; } }

        private WWRadix2Fft mFft;
        private double[] mOverlapInputSamples;
        private bool mFirstTime;
        private double[] mWindow;

        private double[] mLastOutputSamplesTail;

        private int mSpliceDenominator = 32;
        private int mOffset = 0;

        public OverlappedFft(int fftLength) {
            mFftLength = fftLength;
            mFft = new WWRadix2Fft(mFftLength);
            mOverlapInputSamples = new double[mFftLength / 2];
            mWindow = WWWindowFunc.BlackmanWindow(mFftLength / 2 - 1);
            mLastOutputSamplesTail = new double[mFftLength / mSpliceDenominator];
            mFirstTime = true;
        }

        public long NumOfSamplesNeeded() {
            if (mFirstTime) {
                return mFftLength*3/4;
            } else {
                return mFftLength / 2;
            }
        }

        public void Clear() {
            mFft = null;
            mOverlapInputSamples  = null;
            mWindow = null;
        }

        public WWComplex[] ForwardFft(double[] timeDomain) {
            double[] timeFull = null;
            timeFull = new double[mFftLength];
            if (mFirstTime) {
                System.Diagnostics.Debug.Assert(timeDomain.Length == mFftLength*3/4);
                Array.Copy(timeDomain, 0, timeFull, mFftLength / 4, mFftLength * 3/4);
            } else {
                System.Diagnostics.Debug.Assert(timeDomain.Length == mFftLength / 2);
                Array.Copy(mOverlapInputSamples, 0, timeFull, 0, mFftLength / 2);
                Array.Copy(timeDomain, 0, timeFull, mFftLength / 2, mFftLength / 2);
            }

            // store last half part of input samples for later processing
            Array.Copy(timeFull, mFftLength / 2, mOverlapInputSamples, 0, mFftLength / 2);

            for (int i = 0; i < mFftLength / 4; ++i) {
                timeFull[mFftLength / 4 - i - 1] *= mWindow[mFftLength / 4 + i -1];
                timeFull[mFftLength * 3 / 4 + i] *= mWindow[mFftLength / 4 + i - 1];
            }

            var timeComplex = new WWComplex[mFftLength];
            for (int i = 0; i < timeComplex.Length; ++i) {
                timeComplex[i] = new WWComplex(timeFull[i], 0);
            }

            return mFft.ForwardFft(timeComplex);
        }

        public double[] InverseFft(WWComplex[] freqDomain) {
            System.Diagnostics.Debug.Assert(freqDomain.Length == mFftLength); 

            var timeComplex = mFft.InverseFft(freqDomain);

#if false
            for (int i = 0; i < mFftLength; ++i) {
                System.Console.WriteLine("{0},{1}", i + mOffset, timeComplex[i].real);
            }
#endif
            mOffset += mFftLength / 2;

            // 逆FFT結果の時間ドメインの真ん中データを戻す。
            var result = new double[mFftLength / 2];
            for (int i = 0; i < mFftLength / 2; ++i) {
                result[i] = timeComplex[i + mFftLength / 4].real;
            }

            if (!mFirstTime) {
                // 出力結果の最初の部分を、最後の出力結果の対応部分とミックスする。
                // ブチっという音を抑制するため。
                for (int i = 0; i < mFftLength / mSpliceDenominator; ++i) {
                    double secondGain = (double)i / ( mFftLength / mSpliceDenominator);
                    double firstGain = 1.0 - secondGain;
                    result[i] = firstGain * mLastOutputSamplesTail[i] + secondGain * result[i];
                }
            }

            // 出力結果最後の先の部分を次回処理で使用するため保存する。
            for (int i = 0; i < mFftLength / mSpliceDenominator; ++i) {
                mLastOutputSamplesTail[i] = timeComplex[i + mFftLength *3 / 4].real;
            }

            mFirstTime = false;
            return result;
        }
    }
}
