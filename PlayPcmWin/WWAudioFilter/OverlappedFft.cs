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
        private double[] mOverlapOutputSamples;
        private bool mFirstTime;

        public OverlappedFft(int fftLength) {
            mFftLength = fftLength;
            mFft = new WWRadix2Fft(mFftLength);
            mOverlapInputSamples = new double[mFftLength / 2];
            mOverlapOutputSamples = new double[mFftLength / 2];
            mFirstTime = true;
        }

        public long NumOfSamplesNeeded() {
            if (mFirstTime) {
                return mFftLength;
            } else {
                return mFftLength / 2;
            }
        }

        public void Clear() {
            mFft = null;
            mOverlapInputSamples  = null;
            mOverlapOutputSamples = null;
        }

        public WWComplex[] ForwardFft(double[] timeDomain) {
            double[] timeFull = null;
            if (mFirstTime) {
                System.Diagnostics.Debug.Assert(timeDomain.Length == mFftLength);
                timeFull = timeDomain;
            } else {
                System.Diagnostics.Debug.Assert(timeDomain.Length == mFftLength / 2);

                timeFull = new double[mFftLength];
                Array.Copy(mOverlapInputSamples, 0, timeFull, 0, mFftLength / 2);
                Array.Copy(timeDomain, 0, timeFull, mFftLength / 2, mFftLength / 2);
            }

            // store last half part of input samples for later processing
            Array.Copy(timeFull, mFftLength / 2, mOverlapInputSamples, 0, mFftLength / 2);

            var timeComplex = new WWComplex[mFftLength];
            for (int i = 0; i < timeComplex.Length; ++i) {
                timeComplex[i] = new WWComplex(timeFull[i], 0);
            }

            return mFft.ForwardFft(timeComplex);
        }

        public double[] InverseFft(WWComplex[] freqDomain) {
            System.Diagnostics.Debug.Assert(freqDomain.Length == mFftLength); 

            var timeComplex = mFft.InverseFft(freqDomain);

            var timeReal = new double[mFftLength];
            for (int i = 0; i < timeReal.Length; ++i) {
                timeReal[i] = timeComplex[i].real;
            }

            var firstHalf = new double[mFftLength / 2];
            Array.Copy(timeReal, 0, firstHalf, 0, mFftLength / 2);

            double[] result = null;
            if (mFirstTime) {
                // returns first half part
                result = firstHalf;
            } else {
                // returns first half part mixed with last overlap
                result = WWUtil.Crossfade(mOverlapInputSamples, firstHalf);
            }

            // store last half part of timeReal for later processing
            Array.Copy(timeReal, mFftLength / 2, mOverlapOutputSamples, 0, mFftLength / 2);

            mFirstTime = false;
            return result;
        }
    }
}
