using System.Text;

namespace WWAudioFilterCore {
    public class WWAFUtil {
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
    }
}
