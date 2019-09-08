// 日本語。
using System.Text;
using System.IO;

namespace WWAudioFilterCore {
    public class WWAFUtil {
        /// <summary>
        /// 保存ファイルフォーマットの種類。
        /// </summary>
        public enum FileFormatType {
            Unknown = -1,
            FLAC,
            WAVE,
            DSF,
        }

        public enum AFSampleFormat {
            Auto,
            PcmInt16,
            PcmInt24,
            PcmInt32,
            PcmFloat32,
            PcmInt64,
            PcmFloat64,
        };

        public static string EscapeString(string s) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; ++i) {
                if (s[i] == '\\') {
                    sb.Append('\\');
                    sb.Append('\\');
                } else if (s[i] == '\"') {
                    sb.Append('\\');
                    sb.Append('\"');
                } else {
                    sb.Append(s[i]);
                }
            }
            return "\"" + sb.ToString() + "\"";
        }

        public static double[] Crossfade(double[] first, double[] second) {
            System.Diagnostics.Debug.Assert(first.Length == second.Length);

            var result = new double[first.Length];

            for (int i = 0; i < first.Length; ++i) {
                double secondGain = (double)i / first.Length;
                double firstGain = 1.0 - secondGain;
                result[i] = firstGain * first[i] + secondGain * second[i];
            }

            return result;
        }

        public static double[] Mul(double[] first, double[] second) {
            System.Diagnostics.Debug.Assert(first.Length == second.Length);

            var result = new double[first.Length];

            for (int i = 0; i < first.Length; ++i) {
                result[i] = first[i] * second[i];
            }

            return result;
        }

        public static FileFormatType FileNameToFileFormatType(string fileName) {
            var fileFormat = FileFormatType.Unknown;
            if (0 == string.CompareOrdinal(Path.GetExtension(fileName).ToUpperInvariant(), ".FLAC")) {
                fileFormat = FileFormatType.FLAC;
            } else if (0 == string.CompareOrdinal(Path.GetExtension(fileName).ToUpperInvariant(), ".DSF")) {
                fileFormat = FileFormatType.DSF;
            } else if (0 == string.CompareOrdinal(Path.GetExtension(fileName).ToUpperInvariant(), ".WAV")) {
                fileFormat = FileFormatType.WAVE;
            }
            return fileFormat;
        }
    }
}
