using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class LowpassFilter : FilterBase {
        private readonly int FILTER_LENP1;
        private readonly int FILTER_DELAY;

        // FILTER_LENP1 * 4程度にする
        private readonly int FFT_LEN;

        public int SampleRate { get; set; }
        public double CutoffFrequency { get; set; }
        public int FilterLength { get; set; }
        public int FilterSlopeDbOct { get; set; }

        private WWComplex [] mFilterFreq;
        private double [] mIfftAddBuffer;
        private bool mFirstFilterDo;

        public LowpassFilter(double cutoffFrequency, int filterLength, int filterSlopeDbOct)
                : base(FilterType.LowPassFilter) {
            if (cutoffFrequency < 0.0) {
                throw new ArgumentOutOfRangeException("cutoffFrequency");
            }
            CutoffFrequency = cutoffFrequency;

            if (!IsPowerOfTwo(filterLength+1)) {
                throw new ArgumentException("filterLength +1 must be power of two integer");
            }
            FilterLength = filterLength;

            FILTER_LENP1 = FilterLength+1;
            FILTER_DELAY = FILTER_LENP1/2;
            FFT_LEN = FILTER_LENP1*4;

            System.Diagnostics.Debug.Assert(IsPowerOfTwo(FILTER_LENP1) && IsPowerOfTwo(FFT_LEN) && FILTER_LENP1 < FFT_LEN);

            if (filterSlopeDbOct <= 0) {
                throw new ArgumentOutOfRangeException("filterSlopeDbOct");
            }
            FilterSlopeDbOct = filterSlopeDbOct;
        }

        public override FilterBase CreateCopy() {
            return new LowpassFilter(CutoffFrequency, FilterLength, FilterSlopeDbOct);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            SampleRate = inputFormat.SampleRate;
            mFilterFreq = ButterworthFilter.Design(SampleRate, CutoffFrequency, FFT_LEN, FilterSlopeDbOct);
            mIfftAddBuffer = null;
            mFirstFilterDo = true;

            return new PcmFormat(inputFormat);
        }

        public override void FilterStart() {
            base.FilterStart();

            mIfftAddBuffer = null;
            mFirstFilterDo = true;
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mIfftAddBuffer = null;
            mFirstFilterDo = true;
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterLpfDesc, CutoffFrequency, FilterSlopeDbOct, FilterLength);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", CutoffFrequency, FilterLength, FilterSlopeDbOct);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 4) {
                return null;
            }

            double cutoffFrequency;
            if (!Double.TryParse(tokens[1], out cutoffFrequency) || cutoffFrequency <= 0) {
                return null;
            }

            int filterLength;
            if (!Int32.TryParse(tokens[2], out filterLength) || filterLength <= 0 || !IsPowerOfTwo(filterLength+1)) {
                return null;
            }

            int filterSlope;
            if (!Int32.TryParse(tokens[3], out filterSlope) || filterSlope <= 0) {
                return null;
            }

            return new LowpassFilter(cutoffFrequency, filterLength, filterSlope);
        }

        public override long NumOfSamplesNeeded() {
            return FFT_LEN - FILTER_LENP1;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength <= NumOfSamplesNeeded());
            var inPcm = inPcmLA.ToArray();

            var fft = new WWRadix2Fft(FFT_LEN);

            // Overlap and add continuous FFT

            var inTime = new WWComplex[FFT_LEN];
            for (int i = 0; i < inPcm.Length; ++i) {
                inTime[i].real = inPcm[i];
            }

            // FFTでinTimeをinFreqに変換
            var inFreq = fft.ForwardFft(inTime);
            inTime = null;

            // FFT後、フィルターHの周波数ドメインデータを掛ける
            var mulFreq = WWComplex.Mul(inFreq, mFilterFreq);
            inFreq = null;

            // inFreqをIFFTしてoutTimeに変換
            var outTime = fft.InverseFft(mulFreq);
            mulFreq = null;

            double [] outReal;
            if (mFirstFilterDo) {
                // 最初のFilterDo()のとき、フィルタの遅延サンプル数だけ先頭サンプルを削除する。
                outReal = new double[NumOfSamplesNeeded() - FILTER_DELAY];
                for (int i=0; i < outReal.Length; ++i) {
                    outReal[i] = outTime[i+FILTER_DELAY].real;
                }
                mFirstFilterDo = false;
            } else {
                outReal = new double[NumOfSamplesNeeded()];
                for (int i=0; i < outReal.Length; ++i) {
                    outReal[i] = outTime[i].real;
                }
            }

            // 前回のIFFT結果の最後のFILTER_LENGTH-1サンプルを先頭に加算する
            if (null != mIfftAddBuffer) {
                for (int i=0; i < mIfftAddBuffer.Length; ++i) {
                    outReal[i] += mIfftAddBuffer[i];
                }
            }

            // 今回のIFFT結果の最後のFILTER_LENGTH-1サンプルをmIfftAddBufferとして保存する
            mIfftAddBuffer = new double[FILTER_LENP1];
            for (int i=0; i < mIfftAddBuffer.Length; ++i) {
                mIfftAddBuffer[i] = outTime[outTime.Length - mIfftAddBuffer.Length + i].real;
            }
            outTime = null;

            return new WWUtil.LargeArray<double>(outReal);
        }
    }
}
