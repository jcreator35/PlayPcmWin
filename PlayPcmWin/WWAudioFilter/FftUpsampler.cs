using System;
using System.Globalization;

namespace WWAudioFilter {
    class FftUpsampler : FilterBase {
        private const int DEFAULT_FFT_LENGTH = 262144;

        public int Factor { get; set; }
        public int FftLength { get; set; }
        public int UpsampleFftLength { get { return FftLength * Factor; } }

        public enum OverlapType {
            Half,
            ThreeFourth,

            NUM
        }

        public OverlapType Overlap { get; set; }

        /// <summary>
        /// オーバーラップ処理される入力信号のサンプル数の半分
        /// </summary>
        private int HalfOverlapLength {
            get {
                switch (Overlap) {
                case OverlapType.Half:
                default:
                    return FftLength / 4;
                case OverlapType.ThreeFourth:
                    return 3 * (FftLength / 8);
                }
            }
        }

        private int OverlapLength {
            get {
                return 2 * HalfOverlapLength;
            }
        }

        private bool mFirst;
        private double [] mOverlapSamples;

        public FftUpsampler(int factor, int fftLength, OverlapType overlap)
                : base(FilterType.FftUpsampler) {
            Overlap = overlap;

            if (factor <= 1 || !IsPowerOfTwo(factor)) {
                throw new ArgumentException("factor must be power of two integer and larger than 1");
            }
            Factor = factor;

            FftLength = fftLength;
            System.Diagnostics.Debug.Assert(IsPowerOfTwo(FftLength));
        }

        public override FilterBase CreateCopy() {
            return new FftUpsampler(Factor, FftLength, Overlap);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterFftUpsampleDesc, Factor, FftLength, Overlap);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Factor, FftLength, Overlap);
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

            OverlapType overlap = OverlapType.Half;

            if (4 <= tokens.Length) {
                for (int i = 0; i < (int)OverlapType.NUM; ++i) {
                    OverlapType t = (OverlapType)i;
                    if (0 == string.Compare(tokens[3], t.ToString())) {
                        overlap = t;
                    }
                }
            }

            return new FftUpsampler(factor, fftLength, overlap);
        }

        public override long NumOfSamplesNeeded() {
            if (mFirst) {
                return FftLength - HalfOverlapLength;
            } else {
                return FftLength - HalfOverlapLength * 2;
            }
        }

        public override void FilterStart() {
            base.FilterStart();

            mOverlapSamples = null;
            mFirst = true;
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mOverlapSamples = null;
            mFirst = true;
        }

        private PcmFormat mInputPcmFormat;
        private PcmFormat mOutputPcmFormat;

        private double[] mFreqFilter;

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mInputPcmFormat = inputFormat;

            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;

            mOutputPcmFormat = r;

            DesignFreqFilter();

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

            var inPcmR = new double[FftLength];
            if (mFirst) {
                Array.Copy(inPcm, 0, inPcmR, HalfOverlapLength, inPcm.Length);
            } else {
                System.Diagnostics.Debug.Assert(mOverlapSamples != null);
                System.Diagnostics.Debug.Assert(mOverlapSamples.Length == HalfOverlapLength * 2);
                Array.Copy(mOverlapSamples, 0, inPcmR, 0, HalfOverlapLength * 2);
                mOverlapSamples = null;
                Array.Copy(inPcm, 0, inPcmR, HalfOverlapLength * 2, inPcm.Length);
            }

            var inPcmT = new WWComplex[FftLength];
            for (int i=0; i < inPcmT.Length; ++i) {
                inPcmT[i] = new WWComplex(inPcmR[i], 0);
            }

            {
                // inPcmTの出力されず捨てられる領域に窓関数を掛ける。
                // Kaiser窓(α==9)をかける
                var w = WWWindowFunc.KaiserWindow(HalfOverlapLength * 2, 9.0);
                for (int i = 0; i < HalfOverlapLength; ++i) {
                    inPcmT[i].Mul(w[i]);
                    inPcmT[FftLength - i - 1].Mul(w[i]);
                }
            }

            // inPcmTをFFTしてinPcmFを得る。
            WWComplex[] inPcmF;
            {
                var fft = new WWRadix2Fft(FftLength);
                inPcmF = fft.ForwardFft(inPcmT);
            }
            inPcmT = null;

            // inPcmFを0で水増ししたデータoutPcmFを作ってローパスフィルターを通し逆FFTしoutPcmTを得る。

            var outPcmF = new WWComplex[UpsampleFftLength];
            for (int i=0; i < outPcmF.Length; ++i) {
                if (i <= FftLength / 2) {
                    outPcmF[i].CopyFrom(inPcmF[i]);
                } else if (UpsampleFftLength - FftLength / 2 <= i) {
                    int pos = i + FftLength - UpsampleFftLength;
                    outPcmF[i].CopyFrom(inPcmF[pos]);
                } else {
                    // do nothing
                }
                outPcmF[i].Mul(mFreqFilter[i]);
            }
            inPcmF = null;

            WWComplex[] outPcmT;
            {
                var fft = new WWRadix2Fft(UpsampleFftLength);
                outPcmT = fft.InverseFft(outPcmF, 1.0 / FftLength);
            }
            outPcmF = null;

            // outPcmTの実数成分を戻り値とする。
            var outPcm = new double[Factor * (FftLength - HalfOverlapLength * 2)];
            for (int i=0; i < outPcm.Length; ++i) {
                outPcm[i] = outPcmT[i + Factor * HalfOverlapLength].real;
            }
            outPcmT = null;

            // 次回計算に使用するオーバーラップ部分のデータをmOverlapSamplesに保存。
            // オーバラップ部分==inPcmRの最後の方
            mOverlapSamples = new double[HalfOverlapLength * 2];
            Array.Copy(inPcmR, inPcmR.Length - HalfOverlapLength * 2, mOverlapSamples, 0, HalfOverlapLength * 2);

            if (mFirst) {
                mFirst = false;
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
