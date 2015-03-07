
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System;
using System.Globalization;
namespace PlayPcmWin {
    /// <summary>
    /// WWAudioFilterTypeと同じ順番で並べる
    /// </summary>
    public enum PreferenceAudioFilterType {
        PolarityInvert,
        MonauralMix,

        NUM
    };

    public class PreferenceAudioFilter {
        private const int FILTER_FILE_VERSION = 1;
        public PreferenceAudioFilterType FilterType { get; set; }


        public static List<PreferenceAudioFilter> LoadFiltersFromStream(Stream stream) {
            try {
                var filters = new List<PreferenceAudioFilter>();

                using (StreamReader r = new StreamReader(stream)) {
                    int filterNum = 0;

                    {
                        // ヘッダ部分。バージョン番号とフィルタの個数が入っている。
                        var s = r.ReadLine();
                        s = s.Trim();
                        var tokens = s.Split(null);
                        if (tokens.Length != 2) {
                            MessageBox.Show("Audio filter preference read failed");
                            return null;
                        }
                        int version;
                        if (!Int32.TryParse(tokens[0], out version) || version != FILTER_FILE_VERSION) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorFilterFileVersionMismatch,
                                    FILTER_FILE_VERSION, tokens[0]));
                            return null;
                        }

                        if (!Int32.TryParse(tokens[1], out filterNum) || filterNum < 0) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. bad filter count {0}",
                                    tokens[1]));
                            return null;
                        }
                    }

                    for (int i = 0; i < filterNum; ++i) {
                        var s = r.ReadLine();
                        s = s.Trim();
                        var f = Create(s);
                        if (null == f) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. line={0}, {1}",
                                    i + 2, s));
                        }
                        filters.Add(f);
                    }
                }

                return filters;
            } catch (IOException ex) {
                MessageBox.Show(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show(ex.Message);
            }

            return null;
        }

        public static bool SaveFilteresToStream(List<PreferenceAudioFilter> filters, Stream stream) {
            try {
                using (StreamWriter w = new StreamWriter(stream)) {
                    w.WriteLine("{0} {1}", FILTER_FILE_VERSION, filters.Count);
                    foreach (var f in filters) {
                        w.WriteLine("{0} {1}", f.FilterType, f.ToSaveText());
                    }
                }

                return true;
            } catch (IOException ex) {
                MessageBox.Show(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        enum State {
            Start,
            InToken,
            InEscapedToken,
            SkipWhiteSpace,
        };

        private static string[] Split(string s) {
            if (null == s) {
                return null;
            }

            s.Trim();
            if (s.Length == 0) {
                return null;
            }

            var result = new List<string>();

            var sb = new StringBuilder();
            var state = State.Start;

            for (int i=0; i<s.Length; ++i) {
                switch (state) {
                case State.Start:
                    if (s[i] == '\"') {
                        state = State.InEscapedToken;
                    } else {
                        state = State.InToken;
                        sb.Append(s[i]);
                    }
                    break;
                case State.InToken:
                    if (s[i] == ' ') {
                        if (0 < sb.Length) {
                            result.Add(sb.ToString());
                            sb.Clear();
                        }
                        state = State.SkipWhiteSpace;
                    } else {
                        sb.Append(s[i]);
                    }
                    break;
                case State.InEscapedToken:
                    if (s[i] == '\\') {
                        ++i;
                        if (i < s.Length) {
                            sb.Append(s[i]);
                        }
                    } else if (s[i] == '\"') {
                        if (0 < sb.Length) {
                            result.Add(sb.ToString());
                            sb.Clear();
                        }
                        state = State.SkipWhiteSpace;
                    } else {
                        sb.Append(s[i]);
                    }
                    break;
                case State.SkipWhiteSpace:
                    if (s[i] == '\"') {
                        state = State.InEscapedToken;
                    } else if (s[i] != ' ') {
                        state = State.InToken;
                        sb.Append(s[i]);
                    } else {
                        // do nothing
                    }
                    break;
                }
            }

            if (state == State.InToken) {
                if (0 < sb.Length) {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }

            return result.ToArray();
        }

        public static PreferenceAudioFilter Create(string s) {
            var tokens = Split(s);
            if (tokens == null || tokens.Length < 1) {
                return null;
            }

            switch (tokens[0]) {
            case "PolarityInvert":
                return new PreferenceAudioFilter(PreferenceAudioFilterType.PolarityInvert);
            case "MonauralMix":
                return new PreferenceAudioFilter(PreferenceAudioFilterType.MonauralMix);
            default:
                return null;
            }
        }

        public PreferenceAudioFilter(PreferenceAudioFilterType t) {
            FilterType = t;
        }

        public string ToDescriptionText() {
            switch (FilterType) {
            case PreferenceAudioFilterType.PolarityInvert:
                return Properties.Resources.AudioFilterPolarityInvert;
            case PreferenceAudioFilterType.MonauralMix:
                return Properties.Resources.AudioFilterMonauralMix;
            default:
                System.Diagnostics.Debug.Assert(false);
                return "Unknown";
            }
        }

        public string ToSaveText() {
            // 個別フィルターパラメータの保存をする。
            return FilterType.ToString();
        }

        public PreferenceAudioFilter Copy() {
            var p = new PreferenceAudioFilter(FilterType);

            // 個別フィルターパラメーターのコピーをすること。
            
            return p;
        }

        // ここに個別フィルターパラメーターを並べる。
    }
}

