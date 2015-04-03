﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilter {
    class InsertZeroesUpsampler : FilterBase {
        public int Factor { get; set; }
        private const int BATCH_PROCESS_SAMPLES = 4096;

        public InsertZeroesUpsampler(int factor)
                : base(FilterType.InsertZeroesUpsampler) {
            if (factor <= 1 || !IsPowerOfTwo(factor)) {
                throw new ArgumentException("factor must be power of two integer and larger than 1");
            }

            Factor = factor;
        }

        public override FilterBase CreateCopy() {
            return new InsertZeroesUpsampler(Factor);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterInsertZeroesDesc, Factor);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Factor);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int factor;
            if (!Int32.TryParse(tokens[1], out factor) || factor <= 1 || !IsPowerOfTwo(factor)) {
                return null;
            }

            return new InsertZeroesUpsampler(factor);
        }

        public override long NumOfSamplesNeeded() {
            return BATCH_PROCESS_SAMPLES;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            return r;
        }

        public override double[] FilterDo(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.Length == NumOfSamplesNeeded());

            double [] outPcm = new double[inPcm.Length * Factor];
            for (int i = 0; i < inPcm.Length; ++i) {
                outPcm[i*Factor] = inPcm[i];
            }
            return outPcm;
        }
    }
}
