using System;
using System.Globalization;
using WWMath;

namespace WWAudioFilterCore {
    public class FftUpsampler : FilterBase {
        private const int DEFAULT_FFT_LENGTH = 262144;

        private int mFactor;
        public int Factor { get { return mFactor; } }
        public int FftLength { get { return mFFT.FftLength; } }
        public int UpsampleFftLength { get { return FftLength * Factor; } }

        private WWOverlappedFft mFFT;

        private PcmFormat mInputPcmFormat;
        private PcmFormat mOutputPcmFormat;
        private double[] mFreqFilter;


        public FftUpsampler(int factor, int fftLength)
                : base(FilterType.FftUpsampler) {

            if (factor <= 1 || !IsPowerOfTwo(factor)) {
                throw new ArgumentException("factor must be power of two integer and larger than 1");
            }
            mFactor = factor;

            System.Diagnostics.Debug.Assert(IsPowerOfTwo(FftLength));

            mFFT = new WWOverlappedFft(fftLength);
        }

        public override FilterBase CreateCopy() {
            return new FftUpsampler(Factor, FftLength);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterFftUpsampleDesc, Factor, FftLength);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Factor, FftLength);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length < 2 || 4 < tokens.Length) {
                return null;
            }

            int factor;
            if (!Int32.TryParse(tokens[1], out factor) || factor <= 1 || !IsPowerOfTwo(factor)) {
                return null;
            }

            int fftLength = DEFAULT_FFT_LENGTH;

            if (3 <= tokens.Length) {
                if (!Int32.TryParse(tokens[2], out fftLength) || fftLength < 1024 || !IsPowerOfTwo(fftLength)) {
                    return null;
                }
            }

            return new FftUpsampler(factor, fftLength);
        }

        public override long NumOfSamplesNeeded() {
            return mFFT.WantSamples;
        }

        public override void FilterStart() {
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mInputPcmFormat = inputFormat;

            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;

            mOutputPcmFormat = r;

            DesignFreqFilter();

            mFFT.SetNumSamples(r.NumSamples);

            return r;
        }

        private void DesignFreqFilter() {
            var filter = ButterworthFilter.Design(mInputPcmFormat.SampleRate, mInputPcmFormat.SampleRate * 0.465, FftLength, 600);

            mFreqFilter = new double[UpsampleFftLength];
            mFreqFilter[0] = filter[0].Magnitude();
            for (int i = 1; i < mFreqFilter.Length; ++i) {
                if (i <= FftLength / 2) {
                    mFreqFilter[i] = filter[i].Magnitude();
                    mFreqFilter[UpsampleFftLength - i] = mFreqFilter[i];
                }
            }
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength == NumOfSamplesNeeded());
            var inPcm = inPcmLA.ToArray();

            // inPcmTをFFTしてinPcmFを得る。
            var inPcmF = mFFT.ForwardFft(inPcm);

            // inPcmFを0で水増ししたデータoutPcmFを作ってローパスフィルターを通し逆FFTしoutPcmを得る。

            var outPcmF = new WWComplex[UpsampleFftLength];
            for (int i=0; i < outPcmF.Length; ++i) {
                if (i <= FftLength / 2) {
                    outPcmF[i] = inPcmF[i];
                } else if (UpsampleFftLength - FftLength / 2 <= i) {
                    int pos = i + FftLength - UpsampleFftLength;
                    outPcmF[i] = inPcmF[pos];
                } else {
                    outPcmF[i] = WWComplex.Zero();
                }
                outPcmF[i] = WWComplex.Mul(outPcmF[i], mFreqFilter[i]);
            }
            inPcmF = null;

            var outPcm = mFFT.InverseFft(outPcmF);
            outPcmF = null;

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
