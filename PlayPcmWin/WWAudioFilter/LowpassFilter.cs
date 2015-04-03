using System;
using System.Globalization;

namespace WWAudioFilter {
    class LowpassFilter : FilterBase {
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
            DesignCutoffFilter();
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

        private void DesignCutoffFilter() {
            var fromF = new WWComplex[FILTER_LENP1];

            // バターワースフィルター
            // 1次 = 6dB/oct
            // 2次 = 12dB/oct

            double orderX2 = 2.0 * (FilterSlopeDbOct / 6.0);

            double cutoffRatio = CutoffFrequency / (SampleRate/2);

            // フィルタのF特
            fromF[0].real = 1.0f;
            for (int i=1; i <= FILTER_LENP1 / 2; ++i) {
                double omegaRatio = i * (1.0 / (FILTER_LENP1 / 2));
                double v = Math.Sqrt(1.0 / (1.0 + Math.Pow(omegaRatio / cutoffRatio, orderX2)));
                if (Math.Abs(v) < Math.Pow(0.5, 24)) {
                    v = 0.0;
                }
                fromF[i].real = v;
            }
            for (int i=1; i < FILTER_LENP1 / 2; ++i) {
                fromF[FILTER_LENP1 - i].real = fromF[i].real;
            }

            // IFFTでfromFをfromTに変換
            var fromT   = new WWComplex[FILTER_LENP1];
            {
                var fft = new WWRadix2Fft(FILTER_LENP1);
                fft.ForwardFft(fromF, fromT);

                double compensation = 1.0 / (FILTER_LENP1 * cutoffRatio);
                for (int i=0; i < FILTER_LENP1; ++i) {
                    fromT[i].Set(
                            fromT[i].real      * compensation,
                            fromT[i].imaginary * compensation);
                }
            }
            fromF = null;

            // fromTの中心がFILTER_LENGTH/2番に来るようにする。
            // delayT[0]のデータはfromF[FILTER_LENGTH/2]だが、非対称になるので入れない
            // このフィルタの遅延はFILTER_LENGTH/2サンプルある

            var delayT = new WWComplex[FILTER_LENP1];
            for (int i=1; i < FILTER_LENP1 / 2; ++i) {
                delayT[i] = fromT[i + FILTER_LENP1 / 2];
            }
            for (int i=0; i < FILTER_LENP1 / 2; ++i) {
                delayT[i + FILTER_LENP1 / 2] = fromT[i];
            }
            fromT = null;

            // Kaiser窓をかける
            var w = WWWindowFunc.KaiserWindow(FILTER_LENP1 + 1, 9.0);
            for (int i=0; i < FILTER_LENP1; ++i) {
                delayT[i].Mul(w[i]);
            }

            var delayTL = new WWComplex[FFT_LEN];
            for (int i=0; i < delayT.Length; ++i) {
                delayTL[i] = delayT[i];
            }
            delayT = null;

            // できたフィルタをFFTする
            var delayF = new WWComplex[FFT_LEN];
            {
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.ForwardFft(delayTL, delayF);

                for (int i=0; i < FFT_LEN; ++i) {
                    delayF[i].Mul(cutoffRatio);
                }
            }
            delayTL = null;

            mFilterFreq = delayF;
        }

        public override double[] FilterDo(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.LongLength <= NumOfSamplesNeeded());

            // Overlap and add continuous FFT

            var inTime = new WWComplex[FFT_LEN];
            for (int i=0; i < inPcm.LongLength; ++i) {
                inTime[i] = new WWComplex(inPcm[i], 0.0);
            }

            // FFTでinTimeをinFreqに変換
            var inFreq = new WWComplex[FFT_LEN];
            {
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.ForwardFft(inTime, inFreq);
            }
            inTime = null;

            // FFT後、フィルターHの周波数ドメインデータを掛ける
            for (int i=0; i < FFT_LEN; ++i) {
                inFreq[i].Mul(mFilterFreq[i]);
            }

            // inFreqをIFFTしてoutTimeに変換
            var outTime = new WWComplex[FFT_LEN];
            {
                var outTimeS = new WWComplex[FFT_LEN];
                var fft = new WWRadix2Fft(FFT_LEN);
                fft.InverseFft(inFreq, outTime);
            }
            inFreq = null;

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

            return outReal;
        }
    }
}
