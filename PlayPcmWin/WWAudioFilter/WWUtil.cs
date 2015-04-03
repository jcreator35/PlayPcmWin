using System.Text;

namespace WWAudioFilter {
    public class WWUtil {
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

    }
}
