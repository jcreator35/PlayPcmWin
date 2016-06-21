using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace WWAudioFilter {
    class WindowedSincDownsampler : FilterBase {
        private const int DEFAULT_WINDOW_LENGTH = 65535;
        private const int PROCESS_SLICE = 4096;

        public int Factor { get; set; }
        public int WindowLength { get; set; }

        private bool mFirst;

        private List<double> mInputDelay = new List<double>();
        private double[] mCoeffs;

        public WindowedSincDownsampler(int factor, int windowLength)
                : base(FilterType.WindowedSincDownsampler) {

            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }
            Factor = factor;

            WindowLength = windowLength;
            
            mFirst = true;
        }

        public override FilterBase CreateCopy() {
            return new WindowedSincDownsampler(Factor, WindowLength);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterWindowedSincDownsampleDesc, Factor, WindowLength);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Factor, WindowLength);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            int factor;
            if (!Int32.TryParse(tokens[1], out factor) || factor <= 1) {
                return null;
            }

            int windowLength;
            if (!Int32.TryParse(tokens[2], out windowLength) || windowLength < 4 || !IsPowerOfTwo(windowLength+1)) {
                return null;
            }

            return new WindowedSincDownsampler(factor, windowLength);
        }

        public override void FilterStart() {
            base.FilterStart();
            mFirst = true;
            mInputDelay.Clear();
        }

        public override void FilterEnd() {
            base.FilterEnd();
            mFirst = true;
            mInputDelay.Clear();
            mCoeffs = null;
        }

        private PcmFormat mInputPcmFormat;
        private PcmFormat mOutputPcmFormat;

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mInputPcmFormat = inputFormat;

            var r = new PcmFormat(inputFormat);
            r.SampleRate /= Factor;
            r.NumSamples /= Factor;

            mOutputPcmFormat = r;

            SetupCoeffs();
            return r;
        }

        private void SetupCoeffs() {
            var window = WWWindowFunc.BlackmanWindow(WindowLength);

            // ループ処理を簡単にするため最初と最後に0を置く。
            mCoeffs = new double[1 + WindowLength+1];
            int center = WindowLength / 2;

            for (int i = 0; i < WindowLength / 2 + 1; ++i) {
                int numerator = i;
                int denominator = Factor;
                int numeratorReminder = numerator % (denominator*2);
                if (numerator == 0) {
                    mCoeffs[1 + center + i] = 1.0f;
                } else if (numerator % denominator == 0) {
                    // sinc(180 deg) == 0, sinc(360 deg) == 0, ...
                    mCoeffs[1 + center + i] = 0.0f;
                } else {
                    mCoeffs[1 + center + i] = Math.Sin(Math.PI * numeratorReminder / denominator)
                        / (Math.PI * numerator / denominator)
                        * window[center + i];
                }

                // Factor分の１する。
                mCoeffs[1 + center + i] /= Factor;

                // 左右鏡像なので。
                mCoeffs[1 + center - i] = mCoeffs[1 + center + i];
            }
        }

        public override long NumOfSamplesNeeded() {
            if (mFirst) {
                //     最初のサンプル
                return (WindowLength + 1) / 2 + PROCESS_SLICE * Factor;
            } else {
                return PROCESS_SLICE * Factor;
            }
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength == NumOfSamplesNeeded());
            var inPcm = inPcmLA.ToArray();

            int inputSamples = (WindowLength + 1) + PROCESS_SLICE * Factor;

            if (mFirst) {
                var silence = new double[(WindowLength+1) / 2];
                mInputDelay.AddRange(silence);
            }

            mInputDelay.AddRange(inPcm);
            if (inputSamples < mInputDelay.Count) {
                int count = mInputDelay.Count - inputSamples;
                mInputDelay.RemoveRange(0, count);
            }

            var fromPcm = mInputDelay.ToArray();
            var toPcm = new double[PROCESS_SLICE];

#if false
            for (int i=0; i<PROCESS_SLICE; ++i) {
#else
            Parallel.For(0, PROCESS_SLICE, i => {
#endif
                double sampleValue = 0;
                for (int offs = 0; offs < mCoeffs.Length; ++offs) {
                    double input = mInputDelay[offs + i*Factor];
                    if (input != 0.0) {
                        sampleValue += mCoeffs[offs] * input;
                    }
                }
                toPcm[i] = sampleValue;
#if false
            }
#else
            });
#endif

            mFirst = false;
            return new PcmDataLib.LargeArray<double>(toPcm);
        }
    }
}
