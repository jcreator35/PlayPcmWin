﻿
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System;
using System.Globalization;
using System.ComponentModel;
namespace PlayPcmWin {
    /// <summary>
    /// WWAudioFilterTypeと同じ順番で並べる
    /// </summary>
    public enum PreferenceAudioFilterType {
        PolarityInvert,
        MonauralMix,
        ChannelRouting, //< ChannelMapping。設定ファイルで使用されるフィルター名称は互換性のためにChannelRoutingとする。
        MuteChannel,
        SoloChannel,
        ZohNosdacCompensation,
        Delay,
        NUM
    };

    public class PreferenceAudioFilter : INotifyPropertyChanged {
        private const int FILTER_FILE_VERSION = 2;
        public PreferenceAudioFilterType FilterType { get; set; }

        private string [] mArgArray = new string[0];

        public string[] ArgArray {
            get { return mArgArray; }
            set {
                mArgArray = value;
                NotifyPropertyChanged("DescriptionText");
            }
        }

        /// <summary>
        /// オプションパラメーターmArgArrayを保存する形式(1個の文字列)にする
        /// </summary>
        public string ToSaveText() {
            switch (FilterType) {
            case PreferenceAudioFilterType.ChannelRouting: {
                    // mArgArrayをwhitespaceで区切ってつなげる
                    var sb = new StringBuilder();
                    foreach (var i in mArgArray) {
                        sb.AppendFormat("{0} ", i);
                    }
                    return sb.ToString().TrimEnd();
                }
            case PreferenceAudioFilterType.MonauralMix:
            case PreferenceAudioFilterType.ZohNosdacCompensation:
                return "";
            case PreferenceAudioFilterType.PolarityInvert: // mArgArray[0]に全てのパラメータが入っている。
            case PreferenceAudioFilterType.MuteChannel: // mArgArray[0]に全てのパラメータが入っている。
            case PreferenceAudioFilterType.SoloChannel:
            case PreferenceAudioFilterType.Delay:
                return string.Format("{0}", mArgArray[0]);
            default:
                System.Diagnostics.Debug.Assert(false);
                return "";
            }
        }

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
                            return filters;
                        }
                        int version;
                        if (!Int32.TryParse(tokens[0], out version) || version != FILTER_FILE_VERSION) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorFilterFileVersionMismatch,
                                    FILTER_FILE_VERSION, tokens[0]));
                            return filters;
                        }

                        if (!Int32.TryParse(tokens[1], out filterNum) || filterNum < 0) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Audio filter preference read failed. bad filter count {0}",
                                    tokens[1]));
                            return filters;
                        }
                    }

                    for (int i = 0; i < filterNum; ++i) {
                        var s = r.ReadLine();
                        s = s.Trim();
                        var f = Create(s);
                        if (null == f) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Audio filter preference read failed. line={0}, {1}",
                                    i + 2, s));
                        } else {
                            filters.Add(f);
                        }
                    }
                }

                return filters;
            } catch (IOException ex) {
                MessageBox.Show(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show(ex.Message);
            }

            return new List<PreferenceAudioFilter>();
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

        /// <summary>
        /// フィルター設定文字列からフィルターを作成
        /// </summary>
        /// <param name="s">フィルター種類とパラメータ</param>
        private static PreferenceAudioFilter Create(string s) {
            var tokens = Split(s);
            if (tokens == null || tokens.Length < 1) {
                return null;
            }

            PreferenceAudioFilterType t = PreferenceAudioFilterType.NUM;
            for (int i = 0; i < (int)PreferenceAudioFilterType.NUM; ++i) {
                var paf = (PreferenceAudioFilterType)i;
                if (0 == paf.ToString().CompareTo(tokens[0])) {
                    t = paf;
                    break;
                }
            }
            if (t == PreferenceAudioFilterType.NUM) {
                return null;
            }

            var argArray = new string[0];
            if (2 <= tokens.Length) {
                argArray = new string[tokens.Length - 1];
                for (int i=0; i < tokens.Length - 1; ++i) {
                    argArray[i] = tokens[i + 1];
                }
            }

            // 過去バージョンPPWのファイルとの後方互換性の処理。
            switch (t) {
            case PreferenceAudioFilterType.PolarityInvert:
                if (argArray.Length == 0) {
                    // 昔は全チャンネルの極性反転だった。
                    argArray = new string[] { "-1" };
                }
                break;
            default:
                break;
            }

            return new PreferenceAudioFilter(t, argArray);
        }

        /// <summary>
        /// 設定ボタンの可視
        /// </summary>
        public Visibility SettingsButtonVisibility {
            get {
                switch (FilterType) {
                case PreferenceAudioFilterType.ChannelRouting:
                    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public PreferenceAudioFilter(PreferenceAudioFilterType t, string[] argArray) {
            FilterType = t;
            mArgArray = argArray;
        }

        public List<Tuple<int, int>> ChannelMapping() {
            return ArgArrayToChannelMapping(mArgArray);
        }

        public static List<Tuple<int, int>> ArgArrayToChannelMapping(string[] args) {
            if (args == null) {
                return null;
            }

            var rv = new List<Tuple<int, int>>();
            foreach (var i in args) {
                rv.Add(ArgToChannelMapping1(i));
            }

            return rv;
        }

        private static Tuple<int, int> ArgToChannelMapping1(string s) {
            var fromTo = s.Split('>');
            if (fromTo.Length != 2) {
                return null;
            }

            int from, to;
            if (!Int32.TryParse(fromTo[0], out from)) {
                return null;
            }
            if (!Int32.TryParse(fromTo[1], out to)) {
                return null;
            }

            return new Tuple<int, int>(from, to);
        }

        /// <summary>
        /// 表示用のチャンネル番号表示。
        /// </summary>
        private static string ChannelListToDisplayString(string channelList) {
            var sb = new StringBuilder();
            var s = channelList.Split(new char[]{','});
            foreach (var ch in s) {
                int v = Int32.Parse(ch);
                if (0 < sb.Length) {
                    // 2番目以降に現れた数字。カンマで続ける。
                    sb.Append(",");
                }

                if (v == -1) {
                    sb.Append("ALL");
                } else if (v == 0) {
                    sb.Append("1(Left)");
                } else if (v == 1) {
                    sb.Append("2(Right)");
                } else {
                    sb.AppendFormat("{0}", v + 1);
                }
            }
            return sb.ToString();
        }

        public string DescriptionText {
            get {
                switch (FilterType) {
                case PreferenceAudioFilterType.MonauralMix:
                    return Properties.Resources.AudioFilterMonauralMix;
                case PreferenceAudioFilterType.ChannelRouting: {
                        // 説明文はチャンネル番号が1から始まる。
                        StringBuilder sb = new StringBuilder(Properties.Resources.AudioFilterChannelMapping);
                        sb.AppendFormat(" ({0}ch)", mArgArray.Length);
                        foreach (var s in mArgArray) {
                            var m = ArgToChannelMapping1(s);
                            sb.AppendFormat(" {0}←{1}", m.Item1+1, m.Item2+1);
                        }
                        return sb.ToString();
                    }
                case PreferenceAudioFilterType.PolarityInvert:
                    return string.Format(Properties.Resources.AudioFilterPolarityInvertDesc, ChannelListToDisplayString(mArgArray[0]));
                case PreferenceAudioFilterType.MuteChannel:
                    return string.Format(Properties.Resources.AudioFilterMuteChannelDesc, ChannelListToDisplayString(mArgArray[0]));
                case PreferenceAudioFilterType.SoloChannel:
                    return string.Format(Properties.Resources.AudioFilterSoloChannelDesc, ChannelListToDisplayString(mArgArray[0]));
                case PreferenceAudioFilterType.ZohNosdacCompensation:
                    return Properties.Resources.AudioFilterZohNosdacCompensation;
                case PreferenceAudioFilterType.Delay: {
                    StringBuilder sb = new StringBuilder(Properties.Resources.AudioFilterDelay);
                        sb.AppendFormat(": ");
                        var delaySeconds = mArgArray[0].Split(',');
                        for (int ch = 0; ch < delaySeconds.Length; ++ch) {
                            if (0 < ch) {
                                sb.AppendFormat(", ");
                            }
                            sb.AppendFormat("Ch{0}={1}ms", ch + 1, Double.Parse(delaySeconds[ch]) * 1000);
                        }
                        return string.Format(sb.ToString());
                    }
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return "Unknown";
                }
            }
        }

        public PreferenceAudioFilter Copy() {
            var p = new PreferenceAudioFilter(FilterType, mArgArray);

            return p;
        }
    }
}

