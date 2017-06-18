// 日本語 UTF-8
// CUEシートを読む。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace PlayPcmWin {

    /// <summary>
    /// 1曲の情報
    /// </summary>
    class CueSheetTrackInfo {
        public string path;
        public string title;
        public int    trackId;   // TRACK 10 ==> 10
        public int    startTick; // *75 seconds
        public int    endTick;   // -1: till the end of file
 
        public int    indexId;   // INDEX 00 ==> 0, INDEX 01 ==> 1
        public string performer;
        public bool readSeparatorAfter;

        // 複数アルバムが単一のCUEシートに入っている場合、
        // アルバム情報が全体で1個というわけにはいかない。曲情報として扱う。
        public string albumTitle;

        public void Clear() {
            path = "";
            title = string.Empty;
            trackId = 0;
            startTick = 0;
            endTick = -1;

            indexId = -1;
            performer = string.Empty;
            albumTitle = string.Empty;
            readSeparatorAfter = false;
        }

        public void CopyFrom(CueSheetTrackInfo rhs) {
            path      = rhs.path;
            title     = rhs.title;
            trackId   = rhs.trackId;
            startTick = rhs.startTick;
            endTick   = rhs.endTick;

            indexId    = rhs.indexId;
            performer  = rhs.performer;
            albumTitle = rhs.albumTitle;
            readSeparatorAfter = rhs.readSeparatorAfter;
        }

        public PlaylistTrackInfo ConvertToPlaylistTrackInfo() {
            PlaylistTrackInfo pti = new PlaylistTrackInfo();
            pti.path      = path;
            pti.title     = title;
            pti.trackId   = trackId;
            pti.startTick = startTick;
            pti.endTick   = endTick;

            pti.indexId    = indexId;
            pti.performer  = performer;
            pti.albumTitle = albumTitle;
            pti.readSeparatorAfter = readSeparatorAfter;

            return pti;
        }

        private static bool TimeStrToInt(string timeStr, out int timeInt) {
            string s = String.Copy(timeStr);
            if (2 <= s.Length && s[0] == '0') {
                s = s.Substring(1);
            }
            return int.TryParse(s, out timeInt);
        }

        public static int TickStrToInt(string tickStr) {
            string[] msf = tickStr.Split(':');
            if (msf.Length != 3) {
                return -1;
            }

            int m;
            if (!TimeStrToInt(msf[0], out m)) {
                return -1;
            }
            int s;
            if (!TimeStrToInt(msf[1], out s)) {
                return -1;
            }
            int f;
            if (!TimeStrToInt(msf[2], out f)) {
                return -1;
            }

            return f + s * 75 + m * 75 * 60;
        }

        public static string TickIntToStr(int tick) {
            if (tick < 0) {
                return "--:--:--";
            }

            int f = tick % 75;
            int s =((tick - f) / 75) % 60;
            int m = (tick - s * 75 - f)/75/60;

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", m, s, f);
        }

        public void Debug() {
            Console.WriteLine("    path={0}\n    Track={1} Index={2} start={3}({4}) end={5}({6}) performer={7} albumTitle={8} rsa={9}\n    title={10}",
                path, trackId, indexId, startTick, TickIntToStr(startTick),
                endTick, TickIntToStr(endTick), performer, albumTitle, readSeparatorAfter, title);
        }
    };

    /// <summary>
    /// CUEシートを書き込むクラス。
    /// </summary>
    class CueSheetWriter {
        private List<CueSheetTrackInfo> m_trackInfoList = new List<CueSheetTrackInfo>();
        private string m_albumTitle     = string.Empty;
        private string m_albumPerformer = string.Empty;

        public void AddTrackInfo(CueSheetTrackInfo a) {
            m_trackInfoList.Add(a);
        }

        public void SetAlbumTitle(string s) {
            m_albumTitle = s;
        }

        public void SetAlbumPerformer(string s) {
            m_albumPerformer = s;
        }

        /// <summary>
        /// throws IOException, ArgumentException, UnauthorizedException
        /// </summary>
        public bool WriteToFile(string path) {
            if (m_trackInfoList.Count() == 0) {
                return false;
            }

            using (StreamWriter sw = new StreamWriter(path, false, Encoding.Default)) {
                // アルバムタイトル
                if (null != m_albumTitle && 0 < m_albumTitle.Length) {
                    sw.WriteLine(
                        string.Format(CultureInfo.InvariantCulture, "TITLE \"{0}\"", m_albumTitle));
                } else {
                    sw.WriteLine(
                        string.Format(CultureInfo.InvariantCulture, "TITLE \"\""));
                }

                // アルバム演奏者。
                if (null != m_albumPerformer && 0 < m_albumPerformer.Length) {
                    sw.WriteLine(
                        string.Format(CultureInfo.InvariantCulture, "PERFORMER \"{0}\"", m_albumPerformer));
                }

                // 曲情報出力
                int trackCount = 1;
                for (int i = 0; i < m_trackInfoList.Count(); ++i) {
                    CueSheetTrackInfo cti = m_trackInfoList[i];

                    if (0 == string.CompareOrdinal(Path.GetDirectoryName(path),
                            Path.GetDirectoryName(cti.path))) {
                        sw.WriteLine("FILE \"{0}\" WAVE", Path.GetFileName(cti.path));
                    } else {
                        sw.WriteLine("FILE \"{0}\" WAVE", cti.path);
                    }

                    sw.WriteLine("  TRACK {0:D2} AUDIO", trackCount++);

                    sw.WriteLine("    TITLE \"{0}\"", cti.title);

                    if (null != cti.performer && 0 < cti.performer.Length) {
                        sw.WriteLine("    PERFORMER \"{0}\"", cti.performer);
                    }

                    // INDEX ?? で曲情報が確定するので、その前にREM KOKOMADEを入れる。
                    if (!(0 <= cti.endTick &&
                        (i == m_trackInfoList.Count() - 1)) &&
                        cti.readSeparatorAfter) {
                        sw.WriteLine("    REM KOKOMADE");
                    }

                    sw.WriteLine("    INDEX {0} {1}",
                        cti.indexId,
                        CueSheetTrackInfo.TickIntToStr(cti.startTick));

                    if (0 <= cti.endTick
                            && ((i == m_trackInfoList.Count() -1)
                            || (0 == string.CompareOrdinal(m_trackInfoList[i + 1].path, m_trackInfoList[i].path)
                                && m_trackInfoList[i+1].startTick != m_trackInfoList[i].endTick))) {
                        sw.WriteLine("  TRACK {0:D2} AUDIO", trackCount++);
                        sw.WriteLine("    TITLE \" gap \"");
                        sw.WriteLine("    INDEX 00 {0}",
                            CueSheetTrackInfo.TickIntToStr(cti.endTick));
                    }
                }
            }
            return true;
        }
    };

    /// <summary>
    /// CUEシートを読むクラス
    /// </summary>
    class CueSheetReader : PlaylistReader {
        private List<CueSheetTrackInfo> mTrackInfoList;
        private CueSheetTrackInfo mCurrentTrackInfo;
        private string mDirPath;

        private string mAlbumTitle;
        private string mAlbumPerformer;

        private bool mIsAlbumInfoParsing;

        private Encoding encoding;

        public CueSheetReader(Encoding encoding) {
            this.encoding = encoding;
        }

        public int GetTrackInfoCount() {
            return mTrackInfoList.Count;
        }

        public PlaylistTrackInfo GetTrackInfo(int nth) {
            var pti = mTrackInfoList[nth].ConvertToPlaylistTrackInfo();

            if (0 == pti.albumTitle.Length) {
                pti.albumTitle = mAlbumTitle;
            }
            if (0 == pti.performer.Length) {
                pti.performer = mAlbumPerformer;
            }

            return pti;
        }

        /// <summary>
        /// if file read is failed IOException or ArgumentException or UnauthrizedAccessException occurs
        /// </summary>
        public bool ReadFromFile(string path) {
            // 2パス処理する。
            // パス1…ファイルから読み込んでm_trackInfoListに骨格を作る。
            // パス2…m_trackInfoListを走査して、前後関係によって判明する情報を埋める。
            //          現在のtrackInfoと1個後のトラックが同一ファイル名で
            //          cur.endTickが埋まっていない場合
            //          cur.endTick←next.startTickする。

            mTrackInfoList = new List<CueSheetTrackInfo>();
            mDirPath = System.IO.Path.GetDirectoryName(path) + "\\";

            mCurrentTrackInfo = new CueSheetTrackInfo();
            mCurrentTrackInfo.Clear();

            mAlbumTitle     = string.Empty;

            mIsAlbumInfoParsing = true;

            // Pass 1の処理
            bool result = false;
            
            using (StreamReader sr = new StreamReader(path, encoding)) {
                string line;
                int lineno = 0;
                while ((line = sr.ReadLine()) != null) {
                    ++lineno;
                    result = ParseOneLine(line, lineno);
                    if (!result) {
                        break;
                    }
                }
            }
            if (!result) {
                return false;
            }

            /*
            Console.WriteLine("after Pass1 =================================");
            Console.WriteLine("trackInfoList.Count={0}", m_trackInfoList.Count);
            for (int i = 0; i < m_trackInfoList.Count; ++i) {
                Console.WriteLine("trackInfo {0}", i);
                m_trackInfoList[i].Debug();
            }
            */

            // Pass 2の処理
            for (int i = 0; i < mTrackInfoList.Count-1; ++i) {
                CueSheetTrackInfo cur = mTrackInfoList[i];
                CueSheetTrackInfo next = mTrackInfoList[i+1];

                if (0 == string.CompareOrdinal(cur.path, next.path) &&
                    cur.endTick < 0) {
                    cur.endTick = next.startTick;
                }

                if (0 <= cur.endTick &&
                    0 <= cur.startTick &&
                    cur.endTick < cur.startTick) {
                    Console.WriteLine("track {0}: startTick{1} points newer time than endTick{2}",
                        cur.trackId, cur.startTick, cur.endTick);
                    return false;
                }
            }

            /*
            Console.WriteLine("after Pass2 =================================");
            Console.WriteLine("trackInfoList.Count={0}", m_trackInfoList.Count);
            for (int i = 0; i < m_trackInfoList.Count; ++i) {
                Console.WriteLine("trackInfo {0}", i);
                m_trackInfoList[i].Debug();
            }
            */

            return true;
        }

        private static List<string> Tokenize(string line) {
            int quoteStartPos = -1;
            int lastWhiteSpacePos = -1;
            List<string> tokenList = new List<string>();

            // 最後の文字がホワイトスペースではない場合の処理が面倒なので、
            // 最後にホワイトスペースをつける。
            line = line + " ";

            for (int i = 0; i < line.Length; ++i) {
                if (0 <= quoteStartPos) {
                    // ダブルクォートの中の場合。
                    // 次にダブルクォートが出てきたらトークンが完成。クォーティングモードを終わる。
                    if (line[i] == '\"') {
                        string token = line.Substring(quoteStartPos, i - quoteStartPos);
                        tokenList.Add(token);
                        quoteStartPos = -1;
                        lastWhiteSpacePos = i;
                    }
                } else {
                    // ダブルクォートの中ではない場合。
                    if (line[i] == '\"') {
                        // トークン開始。
                        quoteStartPos = i + 1;
                    } else {
                        // ダブルクォートの中でなく、ダブルクォートでない場合。

                        // トークンは、ホワイトスペース的な物で区切られる。
                        if (line[i] == ' ' || line[i] == '\t' || line[i] == '\r' || line[i] == '\n') {
                            if (lastWhiteSpacePos + 1 == i) {
                                // 左隣の文字がホワイトスペース。
                                lastWhiteSpacePos = i;
                            } else {
                                // 左隣の文字がホワイトスペースではなかった。
                                // トークンが完成。
                                string token = line.Substring(lastWhiteSpacePos + 1, i - (lastWhiteSpacePos + 1));
                                tokenList.Add(token);
                                lastWhiteSpacePos = i;
                            }
                        }
                    }
                }
            }

            /*
            Console.WriteLine("tokenList.Count={0}", tokenList.Count);
            for (int i = 0; i < tokenList.Count; ++i) {
                Console.WriteLine("{0} : \"{1}\"", i, tokenList[i]);
            }
            */

            return tokenList;
        }

        private bool ParseOneLine(string line, int lineno) {
            line = line.Trim();

            List<string> tokenList = Tokenize(line);
            if (tokenList.Count == 0) {
                return true;
            }

            switch (tokenList[0].ToUpperInvariant()) {
            case "PERFORMER":
                mCurrentTrackInfo.performer = string.Empty;
                if (2 <= tokenList.Count && 0 < tokenList[1].Trim().Length) {
                    if (mIsAlbumInfoParsing) {
                        mAlbumPerformer = tokenList[1];
                    } else {
                        mCurrentTrackInfo.performer = tokenList[1];
                    }
                }

                break;
            case "TITLE":
                mCurrentTrackInfo.title = string.Empty;
                if (2 <= tokenList.Count && 0 < tokenList[1].Trim().Length) {
                    if (mIsAlbumInfoParsing) {
                        mAlbumTitle = tokenList[1];
                    } else {
                        mCurrentTrackInfo.title = tokenList[1];
                    }
                }
                break;
            case "REM":
                if (2 <= tokenList.Count
                        && 0 == string.Compare(tokenList[1], "KOKOMADE", StringComparison.OrdinalIgnoreCase)) {
                    mCurrentTrackInfo.readSeparatorAfter = true;
                }
                break;
            case "FILE":
                if (tokenList.Count < 2) {
                    Console.WriteLine("Error on line {0}: FILE directive error: filename is not specified", lineno);
                    return true;
                }
                if (3 <= tokenList[1].Length &&
                    ((tokenList[1][0] == '\\' && tokenList[1][1] == '\\') ||
                    ((tokenList[1][1] == ':')))) {
                    // フルパス。
                    mCurrentTrackInfo.path = tokenList[1];
                } else {
                    // 相対パス。
                    string fileName = tokenList[1];
                    mCurrentTrackInfo.path = mDirPath + fileName;
                }

                {
                    string dirName = Path.GetDirectoryName(mCurrentTrackInfo.path);
                    string fileName = Path.GetFileName(mCurrentTrackInfo.path);
                    if (fileName.Contains('?')) {
                        // ファイル名に'?'
                        // search matched file
                        var paths = Directory.GetFiles(dirName, fileName, SearchOption.TopDirectoryOnly);
                        if (paths != null && 0 < paths.Length) {
                            mCurrentTrackInfo.path = paths[0];
                        }
                    }
                }

                // file tag has come; End album info.
                mIsAlbumInfoParsing = false;
                mCurrentTrackInfo.Debug();

                break;
            case "TRACK":
                if (tokenList.Count < 2) {
                    Console.WriteLine("Error on line {0}: track number is not specified", lineno);
                    return true;
                }
                if (!int.TryParse(tokenList[1], out mCurrentTrackInfo.trackId)) {
                    Console.WriteLine("Error on line {0}: track number TryParse failed", lineno);
                    return true;
                }
                mCurrentTrackInfo.Debug();
                break;
            case "INDEX":
                if (tokenList.Count < 3) {
                    Console.WriteLine("Error on line {0}: index number tick format err", lineno);
                    return true;
                }
                if (!int.TryParse(tokenList[1], out mCurrentTrackInfo.indexId)) {
                    mCurrentTrackInfo.indexId = 1;
                }

                mCurrentTrackInfo.startTick = CueSheetTrackInfo.TickStrToInt(tokenList[2]);
                if (mCurrentTrackInfo.startTick < 0) {
                    Console.WriteLine("Error on line {0}: index {1} time format error ({2})",
                        lineno, mCurrentTrackInfo.indexId, tokenList[2]);
                    return true;
                }

                if (mCurrentTrackInfo.indexId == 0 ||
                    mCurrentTrackInfo.indexId == 1) {
                    CueSheetTrackInfo newTrackInfo = new CueSheetTrackInfo();
                    newTrackInfo.CopyFrom(mCurrentTrackInfo);
                    mTrackInfoList.Add(newTrackInfo);
                    mCurrentTrackInfo.Debug();

                    // 揮発要素はここでリセットする。
                    mCurrentTrackInfo.startTick = -1;
                    mCurrentTrackInfo.readSeparatorAfter = false;
                    mCurrentTrackInfo.performer = string.Empty;
                    mCurrentTrackInfo.albumTitle = string.Empty;
                }
                break;

            default:
                Console.WriteLine("D: skipped {0}", tokenList[0]);
                break;
            }

            return true;
        }
    }
}
