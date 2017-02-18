using System;

namespace WWOfflineResampler {
    class SampleValueStatistics {
        public double MaxValue;
        public double MinValue;
        public long TotalSamples;

        public double MaxAbsValue() {
            double v = Math.Abs(MaxValue);
            if (v < Math.Abs(MinValue)) {
                v = Math.Abs(MinValue);
            }
            return v;
        }

        public SampleValueStatistics() {
            Reset();
        }

        public void Reset() {
            MaxValue = double.NegativeInfinity;
            MinValue = double.PositiveInfinity;
            TotalSamples = 0;
        }

        public void Add(double sample) {
            ++TotalSamples;
            if (MaxValue < sample) {
                MaxValue = sample;
            }
            if (sample < MinValue) {
                MinValue = sample;
            }
        }
    }
}
