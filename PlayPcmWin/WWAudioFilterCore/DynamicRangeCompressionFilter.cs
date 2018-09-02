using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class DynamicRangeCompressionFilter : FilterBase {

        public double LsbScalingDb { get; set; }

        private const int    FFT_LENGTH  = 4096;
        private const double LSB_DECIBEL = -144.0;

        OverlappedFft mOverlappedFft = null;

        public DynamicRangeCompressionFilter(double lsbScalingDb)
                : base(FilterType.DynamicRangeCompression) {
            LsbScalingDb = lsbScalingDb;
        }

        public override long NumOfSamplesNeeded() {
            return mOverlappedFft.NumOfSamplesNeeded();
        }

        public override FilterBase CreateCopy() {
            return new DynamicRangeCompressionFilter(LsbScalingDb);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.FilterDynamicRangeCompressionDesc,
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
            mOverlappedFft = new OverlappedFft(FFT_LENGTH);
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mOverlappedFft.Clear();
        }


        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            var pcmF = mOverlappedFft.ForwardFft(inPcm);

            double scaleLsb = Math.Pow(10, LsbScalingDb / 20.0);

            double maxMagnitude = FFT_LENGTH / 2;

            for (int i = 0; i < pcmF.Length; ++i) {
                /*   -144 dBより小さい: そのまま
                 *   -144 dB: scaleLsb倍
                 *   -72 dB: scaleLsb/2倍
                 *    0 dB: 1倍
                 * になるようなスケーリングをする。
                 * 出力データは音量が増えるので、後段にノーマライズ処理を追加すると良い。
                 */

                // magnitudeは0.0～1.0の範囲の値。
                double magnitude = pcmF[i].Magnitude() / maxMagnitude;

                double db = float.MinValue;
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

                pcmF[i].Mul(scale);
            }

            return new WWUtil.LargeArray<double>(mOverlappedFft.InverseFft(pcmF));
        }
    }
}
