using System;
using System.Globalization;
using WWMath;

namespace WWAudioFilterCore {
    public class FftDownsampler : FilterBase {
        private const int DEFAULT_FFT_LENGTH = 262144;

        private int mFactor;
        public int Factor { get { return mFactor; } }
        public int BeforeFftLength { get { return mFFT.FftLength; } }
        public int AfterFftLength { get { return BeforeFftLength / Factor; } }

        private WWOverlappedFft mFFT;

        public FftDownsampler(int factor, int fftLength)
                : base(FilterType.FftDownsampler) {

            if (factor <= 1 || !IsPowerOfTwo(factor)) {
                throw new ArgumentException("factor must be power of two integer and larger than 1");
            }

            if (fftLength < factor * 4) {
                throw new ArgumentException("factor must be equal or smaller than fftLength/4");
            }

            mFactor = factor;

            System.Diagnostics.Debug.Assert(IsPowerOfTwo(fftLength));

            mFFT = new WWOverlappedFft(fftLength, fftLength / mFactor);
            mFFT.SetGain(1.0 / mFactor);
        }

        public override FilterBase CreateCopy() {
            return new FftDownsampler(Factor, BeforeFftLength);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterFftDownsampleDesc, Factor, BeforeFftLength);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Factor, BeforeFftLength);
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

            return new FftDownsampler(factor, fftLength);
        }

        public override long NumOfSamplesNeeded() {
            return mFFT.WantSamples;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {

            var r = new PcmFormat(inputFormat);
            r.SampleRate /= Factor;
            r.NumSamples /= Factor;

            // mFFT.SetNumOutSamples(r.NumSamples);
            return r;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength == NumOfSamplesNeeded());
            var inPcm = inPcmLA.ToArray();

            // inPcmTをFFTしてinPcmFを得る。
            // inPcmFの低周波成分を取り出しoutPcmFを作って逆FFTしoutPcmを得る。

            var inPcmF = mFFT.ForwardFft(inPcm);

            var outPcmF = new WWComplex[AfterFftLength];

            for (int i=0; i < outPcmF.Length; ++i) {
                if (i <= outPcmF.Length / 2) {
                    outPcmF[i] = inPcmF[i];
                } else if (outPcmF.Length/ 2 <= i) {
                    int pos = i + BeforeFftLength - AfterFftLength;
                    outPcmF[i] = inPcmF[pos];
                } else {
                    outPcmF[i] = WWComplex.Zero();
                }
            }
            inPcmF = null;

            var outPcm = mFFT.InverseFft(outPcmF);
            outPcmF = null;

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
