using System;
using System.Globalization;

namespace WWAudioFilter {
    class FftUpsampler : FilterBase {
        private const int DEFAULT_FFT_LENGTH = 262144;

        public int Factor { get; set; }
        public int FftLength { get; set; }

        public enum OverlapType {
            Half,
            ThreeFourth
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
                if (0 == string.Compare(tokens[3], "ThreeFourth")) {
                    overlap = OverlapType.ThreeFourth;
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
        
        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            return r;
        }

        public override double[] FilterDo(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.LongLength == NumOfSamplesNeeded());

            var inPcmR = new double[FftLength];
            if (mFirst) {
                Array.Copy(inPcm, 0, inPcmR, HalfOverlapLength, inPcm.LongLength);
                mFirst = false;
            } else {
                System.Diagnostics.Debug.Assert(mOverlapSamples != null);
                System.Diagnostics.Debug.Assert(mOverlapSamples.LongLength == HalfOverlapLength * 2);
                Array.Copy(mOverlapSamples, 0, inPcmR, 0, HalfOverlapLength * 2);
                mOverlapSamples = null;
                Array.Copy(inPcm, 0, inPcmR, HalfOverlapLength * 2, inPcm.LongLength);
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

            // inPcmFを0で水増ししたデータoutPcmFを作って逆FFTしoutPcmTを得る。

            var UPSAMPLE_FFT_LENGTH = Factor * FftLength;

            var outPcmF = new WWComplex[UPSAMPLE_FFT_LENGTH];
            for (int i=0; i < outPcmF.Length; ++i) {
                if (i <= FftLength / 2) {
                    outPcmF[i].CopyFrom(inPcmF[i]);
                    if (i == FftLength / 2) {
                        outPcmF[i].Mul(0.5);
                    }
                } else if (UPSAMPLE_FFT_LENGTH - FftLength / 2 <= i) {
                    int pos = i + FftLength - UPSAMPLE_FFT_LENGTH;
                    outPcmF[i].CopyFrom(inPcmF[pos]);
                    if (outPcmF.Length - FftLength / 2 == i) {
                        outPcmF[i].Mul(0.5);
                    }
                } else {
                    // do nothing
                }
            }
            inPcmF = null;

            WWComplex[] outPcmT;
            {
                var fft = new WWRadix2Fft(UPSAMPLE_FFT_LENGTH);
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

            return outPcm;
        }
    }
}
