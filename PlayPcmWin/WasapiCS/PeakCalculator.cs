using System;
using WWUtil;

namespace Wasapi {
    class PeakCalculator {
        public double PeakDb { get; set; }
        public double PeakHoldDb { get; set; }

        public Delay delay;
        public Delay delayPeakHoldDb;

        public PeakCalculator(int peakHoldDelayCount) {
            PeakDb = Double.NegativeInfinity;
            PeakHoldDb = Double.NegativeInfinity;

            delay = new Delay(7);

            if (0 < peakHoldDelayCount) {
                delayPeakHoldDb = new Delay(peakHoldDelayCount);
                delayPeakHoldDb.Fill(Double.NegativeInfinity);
            } else {
                delayPeakHoldDb = null;
            }
        }

        public void UpdateBegin() {
            PeakDb = double.NegativeInfinity;
        }

        public void NextSample(double newValue) {
            delay.Filter(newValue);

            // Create Analytic signal and calculate absolute value "levelMagnitude"

            double levelReal = delay.GetNthDelayedSampleValue(3);

            // Discrete Hilbert transform
            double levelImaginary =
                (delay.GetNthDelayedSampleValue(2) - delay.GetNthDelayedSampleValue(4)) * 2.0 / 1.0 / Math.PI +
                (delay.GetNthDelayedSampleValue(0) - delay.GetNthDelayedSampleValue(6)) * 2.0 / 3.0 / Math.PI;
            double levelMagnitude = Math.Sqrt(levelReal * levelReal + levelImaginary * levelImaginary);

            double db = Double.NegativeInfinity;
            if (Double.Epsilon < levelMagnitude) {
                db = 20.0 * Math.Log10(levelMagnitude);
            }

            if (PeakDb < db) {
                PeakDb = db;
            }
        }

        public void PeakHoldReset() {
            PeakDb = Double.NegativeInfinity;
            PeakHoldDb = Double.NegativeInfinity;

            if (delayPeakHoldDb != null) {
                delayPeakHoldDb.Fill(Double.NegativeInfinity);
            }
        }

        public void UpdateEnd() {
            if (delayPeakHoldDb == null) {
                // ピークホールドがずっと持続
                if (PeakHoldDb < PeakDb) {
                    PeakHoldDb = PeakDb;
                }
                return;
            }

            delayPeakHoldDb.Filter(PeakDb);

            PeakHoldDb = Double.NegativeInfinity;
            for (int i = 0; i < delayPeakHoldDb.DelaySamples; ++i) {
                if (PeakHoldDb < delayPeakHoldDb.GetNthDelayedSampleValue(i)) {
                    PeakHoldDb = delayPeakHoldDb.GetNthDelayedSampleValue(i);
                }
            }
        }

        public void UpdatePeakDbTo(double v) {
            PeakDb = v;
            PeakHoldDb = v;
        }
    };
}
