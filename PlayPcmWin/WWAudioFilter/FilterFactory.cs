﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    class FilterFactory {
        private FilterFactory() {
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

            List<string> result = new List<string>();

            StringBuilder sb = new StringBuilder();
            State state = State.Start;

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

        public static FilterBase Create(string s) {
            var tokens = Split(s);
            if (tokens == null || tokens.Length < 1) {
                return null;
            }

            // Refer FilterType enum in FilterBase.cs

            switch (tokens[0]) {
            case "Gain":
                return GainFilter.Restore(tokens);
            case "ZohUpsampler":
                return ZeroOrderHoldUpsampler.Restore(tokens);
            case "LowPassFilter":
                return LowpassFilter.Restore(tokens);
            case "FftUpsampler":
                return FftUpsampler.Restore(tokens);
            case "Mash2":
                return MashFilter.Restore(tokens);

            case "NoiseShaping":
                return NoiseShapingFilter.Restore(tokens);
            case "NoiseShaping4th":
                return NoiseShaping4thFilter.Restore(tokens);
            case "TagEdit":
                return TagEditFilter.Restore(tokens);
            case "Downsampler":
                return Downsampler.Restore(tokens);
            case "CicFilter":
                return CicFilter.Restore(tokens);

            case "InsertZeroesUpsampler":
                return InsertZeroesUpsampler.Restore(tokens);
            case "HalfbandFilter":
                return HalfbandFilter.Restore(tokens);
            case "Crossfeed":
                return CrossfeedFilter.Restore(tokens);
            default:
                return null;
            }
        }
    }
}
