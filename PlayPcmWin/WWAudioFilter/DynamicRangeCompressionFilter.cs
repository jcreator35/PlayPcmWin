using System;
using System.Globalization;

namespace WWAudioFilter {
    public class DynamicRangeCompressionFilter : FilterBase {

        public double LsbScalingDb { get; set; }

        private const int FFT_LENGTH = 4096;
        private const double LSB_DECIBEL = -144.0;
        private const double GAIN_COEFF = 1.5;

        WWRadix2Fft mFft;
        double[] mOverlapInputSamples;
        double[] mOverlapOutputSamples;

        public DynamicRangeCompressionFilter(double lsbScalingDb)
                : base(FilterType.DynamicRangeCompression) {
            LsbScalingDb = lsbScalingDb;
        }

        public override long NumOfSamplesNeeded() {
            if (mOverlapInputSamples == null) {
                return FFT_LENGTH;
            } else {
                return FFT_LENGTH/2;
            }
        }

        public override FilterBase CreateCopy() {
            return new DynamicRangeCompressionFilter(LsbScalingDb);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterDynamicRangeCompressionDesc,
                LsbScalingDb);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", LsbScalingDb);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double lsbScalingDb;
            if (!Double.TryParse(tokens[1], out lsbScalingDb)) {
                return null;
            }

            return new DynamicRangeCompressionFilter(lsbScalingDb);
        }

        public override void FilterStart() {
            base.FilterStart();

            mFft = new WWRadix2Fft(FFT_LENGTH);
            mOverlapInputSamples = null;
            mOverlapOutputSamples = null;
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mFft = null;
            mOverlapInputSamples = null;
            mOverlapOutputSamples = null;
        }

        private double[] FilterCore(double[] inPcm) {
            double scaleLsb = Math.Pow(10, LsbScalingDb / 20.0);

            var inPcmT = new WWComplex[FFT_LENGTH];
            for (int i = 0; i < inPcmT.Length; ++i) {
                inPcmT[i] = new WWComplex(inPcm[i], 0);
            }

            var pcmF = mFft.ForwardFft(inPcmT);
            inPcmT = null;

            double maxMagnitude = FFT_LENGTH / 2;

            for (int i = 0; i < pcmF.Length; ++i) {
                /*   -144 dB 以下: そのまま
                 *   -144 dB: scaleLsb倍
                 *   -72 dB: scaleLsb/2倍
                 *   0 dB: 1倍
                 * になるようなスケーリングをする。
                 * 出力データは音量が増えるので、後段にノーマライズ処理を追加すると良い。
                 */
                pcmF[i].Mul(1.0 / maxMagnitude);

                double magnitude = pcmF[i].Magnitude();

                double db = float.Epsilon;
                if (float.Epsilon < magnitude) {
                    db = 20.0 * Math.Log10(magnitude);
                }

                double scale = 1.0;
                if (db < LSB_DECIBEL) {
                    scale = 1.0;
                } else if (0 <= db) {
                    scale = 1.0;
                } else {
                    scale = 1.0 + db * (scaleLsb - 1) / LSB_DECIBEL;
                }

                pcmF[i].Mul(scale * maxMagnitude);
            }

            var pcmT = mFft.InverseFft(pcmF);
            pcmF = null;

            var outPcm = new double[FFT_LENGTH];

            for (int i = 0; i < outPcm.Length; ++i) {
                outPcm[i] = pcmT[i].real;
            }

            pcmT = null;

            return outPcm;
        }

        private double [] FilterDoFirstTime(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.Length == FFT_LENGTH);
            
            var outPcm = FilterCore(inPcm);
            
            // store last half part for later processing
            mOverlapInputSamples = new double[FFT_LENGTH / 2];
            Array.Copy(inPcm, FFT_LENGTH / 2, mOverlapInputSamples, 0, FFT_LENGTH / 2);

            mOverlapOutputSamples = new double[FFT_LENGTH / 2];
            Array.Copy(outPcm, FFT_LENGTH / 2, mOverlapOutputSamples, 0, FFT_LENGTH / 2);

            // returns first half part
            var result = new double[FFT_LENGTH / 2];
            Array.Copy(outPcm, 0, mOverlapOutputSamples, 0, FFT_LENGTH / 2);
            return result;
        }

        private double[] Crossfade(double[] first, double[] second) {
            System.Diagnostics.Debug.Assert(first.Length == second.Length);

            var result = new double[first.Length];

            for (int i = 0; i < first.Length; ++i) {
                double secondGain = (double)i / first.Length;
                double firstGain = 1.0 - secondGain;
                result[i] = firstGain * first[i] + secondGain * second[i];
            }

            return result;
        }

        private double [] FilterDoOther(double[] inPcmArg) {
            System.Diagnostics.Debug.Assert(inPcmArg.Length == FFT_LENGTH/2);

            var inPcm = new double[FFT_LENGTH];
            Array.Copy(mOverlapInputSamples, 0, inPcm, 0, FFT_LENGTH / 2);
            Array.Copy(inPcmArg, 0, inPcm, FFT_LENGTH/2, FFT_LENGTH / 2);

            var outPcm = FilterCore(inPcm);

            // store last half part of inPcm for later processing
            mOverlapInputSamples = new double[FFT_LENGTH / 2];
            Array.Copy(inPcmArg, 0, mOverlapInputSamples, 0, FFT_LENGTH / 2);

            var outPcmFirstHalf = new double[FFT_LENGTH / 2];
            Array.Copy(outPcm, 0, outPcmFirstHalf, 0, FFT_LENGTH / 2);

            // returns first half part mixed with last overlap
            var result = Crossfade(mOverlapOutputSamples, outPcmFirstHalf);

            // store last half part of outPcm for later processing
            mOverlapOutputSamples = new double[FFT_LENGTH / 2];
            Array.Copy(outPcm, FFT_LENGTH / 2, mOverlapOutputSamples, 0, FFT_LENGTH / 2);

            return result;
        }

        public override double[] FilterDo(double[] inPcm) {
            if (mOverlapInputSamples == null) {
                return FilterDoFirstTime(inPcm);
            } else {
                return FilterDoOther(inPcm);
            }
        }
    }
}
