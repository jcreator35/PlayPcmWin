using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using WavRWLib2;
using PcmDataLib;

namespace PlayPcmWin {
    class PcmHeaderReader {
        /// <summary>
        /// アルバムのカバーアート画像のファイル名
        /// </summary>
        private readonly string[] ALBUM_IMAGE_FILENAMES = {
            "folder.jpg",
            "cover.jpg",
        };

        public enum ReadHeaderMode {
            ReadAll,
            OnlyConcreteFile,
            OnlyMetaFile,
        }

        private List<string> mErrorMessageList = new List<string>();
        private Encoding mEncoding;
        private bool mSortFolderItem;
        public delegate void AddPcmDataDelegate(PcmData pcmData, bool readSeparatorAfter, bool bReadFromPpwPlaylist);
        AddPcmDataDelegate mAddPcmData;

        PlaylistTrackInfo mPlaylistTrackMeta;
        PlaylistItemSave  mPlis;

        public PcmHeaderReader(Encoding enc, bool sortFolderItem, AddPcmDataDelegate addPcmData) {
            mEncoding = enc;
            mSortFolderItem = sortFolderItem;
            mAddPcmData = addPcmData;
        }

        public List<string> ErrorMessageList() {
            return mErrorMessageList;
        }

        private void LoadErrorMessageAdd(string s) {
            mErrorMessageList.Add(s);
        }

        private void HandleFileReadException(string path, Exception ex) {
            LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + ": {1}{3}{2}{3}",
                    "WAV", path, ex, Environment.NewLine));
        }

        /// <summary>
        /// ファイルを最初から最後まで全部読む。
        /// </summary>
        private static byte[] ReadWholeFile(string path) {
            var result = new byte[0];

            if (System.IO.File.Exists(path)) {
                // ファイルが存在する。
                using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                    if (br.BaseStream.Length <= 0x7fffffff) {
                        result = br.ReadBytes((int)br.BaseStream.Length);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// カバーアート画像を追加する。
        /// </summary>
        /// <returns>true: カバーアート画像が付いている。false: カバーアート画像がついていない。</returns>
        private bool AddCoverart(string path, PcmDataLib.PcmData pcmData) {
            if (0 < pcmData.PictureBytes) {
                // 既に追加されている。
                return true;
            }

            try {
                var dirPath = System.IO.Path.GetDirectoryName(path);

                var pictureData = ReadWholeFile(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}.jpg", dirPath,
                        System.IO.Path.GetFileNameWithoutExtension(path)));
                if (0 < pictureData.Length) {
                    // ファイル名.jpgが存在。
                    pcmData.SetPicture(pictureData.Length, pictureData);
                    return true;
                }

                foreach (string albumImageFilename in ALBUM_IMAGE_FILENAMES) {
                    pictureData = ReadWholeFile(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", dirPath, albumImageFilename));
                    if (0 < pictureData.Length) {
                        // アルバムのカバーアート画像(folder.jpg等)が存在。
                        pcmData.SetPicture(pictureData.Length, pictureData);
                        return true;
                    }
                }
            } catch (Exception ex) {
                // エラーが起きたら読まない。
                LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, "W: coverart image read failed: {0}", ex));
            }

            return false;
        }

        /// <summary>
        /// メタ情報更新。PcmData読み込み成功後に行う。
        /// FLACとWAVとAIFFで共通。
        /// </summary>
        private bool CheckAddPcmData(string path, PcmDataLib.PcmData pcmData, bool bUsePlaylistTrackInfo) {
            if (31 < pcmData.NumChannels) {
                LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, "{0}: {1} {2}ch{3}",
                        Properties.Resources.TooManyChannels, path, pcmData.NumChannels, Environment.NewLine));
                return false;
            }

            if (pcmData.BitsPerSample != 16
                    && pcmData.BitsPerSample != 24
                    && pcmData.BitsPerSample != 32) {
                LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, "{0}: {1} {2}bit{3}",
                        Properties.Resources.NotSupportedQuantizationBitRate, path, pcmData.BitsPerSample, Environment.NewLine));
                return false;
            }

            pcmData.FullPath = path;
            pcmData.FileName = System.IO.Path.GetFileName(path);
            // PCMファイルにタイトル名が埋め込まれていない時、ファイル名をタイトル名にする。
            if (pcmData.DisplayName == null || pcmData.DisplayName.Length == 0) {
                pcmData.DisplayName = pcmData.FileName;
            }

            if (!bUsePlaylistTrackInfo || null == mPlaylistTrackMeta) {
                // startTickとendTickは、既にセットされていることもあるので、ここではセットしない。
                // pcmData.StartTick     = 0;
                // pcmData.EndTick       = -1;
                // pcmData.CueSheetIndex = 1;
            } else {
                pcmData.StartTick     = mPlaylistTrackMeta.startTick;
                pcmData.EndTick       = mPlaylistTrackMeta.endTick;
                pcmData.CueSheetIndex = mPlaylistTrackMeta.indexId;

                // 再生リストにタイトル名情報がある時は、再生リストのタイトル名をタイトル名にする。
                if (mPlaylistTrackMeta.title != null && 0 < mPlaylistTrackMeta.title.Length) {
                    pcmData.DisplayName = mPlaylistTrackMeta.title;
                }

                if (mPlaylistTrackMeta.performer != null && 0 < mPlaylistTrackMeta.performer.Length) {
                    pcmData.ArtistName = mPlaylistTrackMeta.performer;
                }
                if (mPlaylistTrackMeta.albumTitle != null && 0 < mPlaylistTrackMeta.albumTitle.Length) {
                    pcmData.AlbumTitle = mPlaylistTrackMeta.albumTitle;
                }
            }

            bool readSeparatorAfter = false;
            if (mPlis != null) {
                // PPWプレイリストの情報で上書きする
                pcmData.DisplayName = mPlis.Title;
                pcmData.AlbumTitle = mPlis.AlbumName;
                pcmData.ArtistName = mPlis.ArtistName;
                pcmData.StartTick = mPlis.StartTick;
                pcmData.EndTick = mPlis.EndTick;
                pcmData.CueSheetIndex = mPlis.CueSheetIndex;
                readSeparatorAfter = mPlis.ReadSeparaterAfter;
            }

            // カバーアート画像を追加する
            AddCoverart(path, pcmData);

            mAddPcmData(pcmData, readSeparatorAfter, mPlis != null);
            return true;
        }

        /// <summary>
        /// WAVファイルのヘッダ部分を読み込む。
        /// </summary>
        /// <returns>読めたらtrue</returns>
        private bool ReadWavFileHeader(string path) {
            bool result = false;
            var pd = new PcmDataLib.PcmData();

            var wavR = new WavReader();
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                if (wavR.ReadHeader(br)) {
                    // WAVヘッダ読み込み成功。PcmDataを作って再生リストに足す。

                    pd.SetFormat(wavR.NumChannels, wavR.BitsPerSample, wavR.ValidBitsPerSample,
                            (int)wavR.SampleRate, wavR.SampleValueRepresentationType, wavR.NumFrames);
                    if ("RIFFINFO_INAM".Equals(wavR.Title) &&
                            "RIFFINFO_IART".Equals(wavR.ArtistName)) {
                        // Issue 79 workaround
                    } else {
                        if (wavR.Title != null) {
                            pd.DisplayName = wavR.Title;
                        }
                        if (wavR.AlbumName != null) {
                            pd.AlbumTitle = wavR.AlbumName;
                        }
                        if (wavR.ArtistName != null) {
                            pd.ArtistName = wavR.ArtistName;
                        }
                    }
                    pd.SetPicture(wavR.PictureBytes, wavR.PictureData);
                    result = CheckAddPcmData(path, pd, true);
                } else {
                    LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + ": {1} : {2}{3}",
                            "WAV", path, wavR.ErrorReason, Environment.NewLine));
                }
            }

            return result;
        }

        /// <summary>
        /// AIFFファイルのヘッダ部分を読み込む。
        /// </summary>
        /// <returns>読めたらtrue</returns>
        private bool ReadAiffFileHeader(string path) {
            bool result = false;

            var ar = new AiffReader();
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                PcmDataLib.PcmData pd;
                var aiffResult = ar.ReadHeader(br, out pd);
                if (aiffResult == AiffReader.ResultType.Success) {
                    if (CheckAddPcmData(path, pd, true)) {
                        result = true;
                    }
                } else {
                    LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + " {1}: {2}{3}",
                        "AIFF", aiffResult, path, Environment.NewLine));
                }
            }

            return result;
        }

        /// <summary>
        /// DSFファイルのヘッダ部分を読み込む。
        /// </summary>
        /// <returns>読めたらtrue</returns>
        private bool ReadDsfFileHeader(string path) {
            bool result = false;

            var r = new DsfReader();
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                PcmDataLib.PcmData pd;
                var rv = r.ReadHeader(br, out pd);
                if (rv == DsfReader.ResultType.Success) {
                    if (CheckAddPcmData(path, pd, true)) {
                        result = true;
                    }
                } else {
                    LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + " {1}: {2}{3}",
                            "DSF", rv, path, Environment.NewLine));
                }
            }

            return result;
        }

        /// <summary>
        /// DSDIFFファイルのヘッダ部分を読み込む。
        /// </summary>
        /// <returns>読めたらtrue</returns>
        private bool ReadDsdiffFileHeader(string path) {
            bool result = false;

            var r = new DsdiffReader();
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                PcmDataLib.PcmData pd;
                var rv = r.ReadHeader(br, out pd);
                if (rv == DsdiffReader.ResultType.Success) {
                    if (CheckAddPcmData(path, pd, true)) {
                        result = true;
                    }
                } else {
                    LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + " {1}: {2}{3}",
                            "DSDIFF", rv, path, Environment.NewLine));
                }
            }

            return result;
        }

        /// <summary>
        /// FLACファイルのヘッダ部分を読み込む。
        /// </summary>
        /// <returns>読めたらtrue</returns>
        private bool ReadFlacFileHeader(string path, ReadHeaderMode mode) {
            PcmDataLib.PcmData pcmData;
            List<FlacDecodeIF.FlacCuesheetTrackInfo> ctiList;

            var fdif = new FlacDecodeIF();
            int flacErcd = 0;
            flacErcd = fdif.ReadHeader(path, out pcmData, out ctiList);
            if (flacErcd != 0) {
                // FLACヘッダ部分読み込み失敗。
                LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + " {2}: {1}{3}",
                        "FLAC", path, FlacDecodeIF.ErrorCodeToStr(flacErcd), Environment.NewLine));
                return false;
            }

            // FLACヘッダ部分読み込み成功。
            if (ctiList.Count == 0 || mode == ReadHeaderMode.OnlyConcreteFile) {
                // FLAC埋め込みCUEシート情報を読まない。

                CheckAddPcmData(path, pcmData, true);
            } else {
                // FLAC埋め込みCUEシート情報を読む。

                PcmData pcmTrack = null;
                for (int trackId=0; trackId < ctiList.Count; ++trackId) {
                    var cti = ctiList[trackId];

                    if (cti.indices.Count == 0) {
                        // インデックスが1つもないトラック。lead-outトラックの場合等。
                        if (pcmTrack != null) {
                            pcmTrack.EndTick = (int)((cti.offsetSamples * 75) / pcmTrack.SampleRate);
                            CheckAddPcmData(path, pcmTrack, false);
                            pcmTrack = null;
                        }
                    } else {
                        for (int indexId=0; indexId < cti.indices.Count; ++indexId) {
                            var indexInfo = cti.indices[indexId];

                            if (pcmTrack != null) {
                                pcmTrack.EndTick = (int)(((cti.offsetSamples + indexInfo.offsetSamples) * 75) / pcmTrack.SampleRate);
                                CheckAddPcmData(path, pcmTrack, false);
                                pcmTrack = null;
                            }

                            pcmTrack = new PcmData();
                            pcmTrack.CopyFrom(pcmData);
                            if (pcmTrack.DisplayName.Length == 0) {
                                pcmTrack.DisplayName = System.IO.Path.GetFileName(path);
                            }
                            pcmTrack.DisplayName = string.Format(CultureInfo.InvariantCulture,
                                    "{0} (Track {1}, Index {2})", pcmTrack.DisplayName, cti.trackNr, indexInfo.indexNr);
                            pcmTrack.CueSheetIndex = indexInfo.indexNr;
                            pcmTrack.StartTick = (int)(((cti.offsetSamples + indexInfo.offsetSamples) * 75) / pcmTrack.SampleRate);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// CUEシートを読み込む。
        /// </summary>
        /// <returns>読めたファイルの数を戻す</returns>
        private int ReadCueSheet(string path) {
            PlaylistReader plr = null;
            switch (Path.GetExtension(path).ToUpperInvariant()) {
            case ".CUE":
                plr = new CueSheetReader(mEncoding);
                break;
            case ".M3U":
            case ".M3U8":
                plr = new M3uReader();
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            bool result = plr.ReadFromFile(path);
            if (!result) {
                LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadFileFailed + ": {1}{2}",
                        Path.GetExtension(path), path, Environment.NewLine));
                return 0;
            }

            int readCount = 0;
            for (int i = 0; i < plr.GetTrackInfoCount(); ++i) {
                var plti = plr.GetTrackInfo(i);
                readCount += ReadFileHeader1(plti.path, ReadHeaderMode.OnlyConcreteFile, plti, null);
            }
            return readCount;
        }

        /// <summary>
        /// 保存してあった再生リストを読んでm_pcmDataListとm_playListItemsに足す。
        /// UpdateUIは行わない。
        /// </summary>
        /// <param name="path">string.Emptyのとき: IsolatedStorageに保存された再生リストを読む。</param>
        /// <returns>読み込まれたファイルの数。</returns>
        private int ReadPpwPlaylist(string path) {
            int count = 0;

            PlaylistSave pl;
            if (path.Length == 0) {
                pl = PpwPlaylistRW.Load();
            } else {
                pl = PpwPlaylistRW.LoadFrom(path);
            }
            foreach (var p in pl.Items) {
                int rv = ReadFileHeader1(p.PathName, ReadHeaderMode.OnlyConcreteFile, null, p);
                count += rv;
            }

            return count;
        }

        /// <summary>
        /// N.B. PcmReader.StreamBeginも参照(へぼい)。
        /// MenuItemFileOpen_Clickも参照。
        /// </summary>
        /// <returns>読めたファイルの数を戻す</returns>
        private int ReadFileHeader1(string path, ReadHeaderMode mode, PlaylistTrackInfo plti, PlaylistItemSave plis) {
            mPlaylistTrackMeta = plti;
            mPlis = plis;

            int result = 0;
            var ext = System.IO.Path.GetExtension(path).ToUpperInvariant();

            try {
                switch (ext) {
                case ".PPWPL":
                    if (mode != ReadHeaderMode.OnlyConcreteFile) {
                        // PPWプレイリストを読み込み
                        result += ReadPpwPlaylist(path);
                    }
                    break;
                case ".CUE":
                case ".M3U":
                case ".M3U8":
                    if (mode != ReadHeaderMode.OnlyConcreteFile) {
                        // CUEシートかM3U8再生リストを読み込み。
                        result += ReadCueSheet(path);
                    }
                    break;
                case ".FLAC":
                    if (mode != ReadHeaderMode.OnlyMetaFile) {
                        result += ReadFlacFileHeader(path, mode) ? 1 : 0;
                    }
                    break;
                case ".AIF":
                case ".AIFF":
                case ".AIFC":
                case ".AIFFC":
                    if (mode != ReadHeaderMode.OnlyMetaFile) {
                        result += ReadAiffFileHeader(path) ? 1 : 0;
                    }
                    break;
                case ".WAV":
                case ".WAVE":
                    if (mode != ReadHeaderMode.OnlyMetaFile) {
                        result += ReadWavFileHeader(path) ? 1 : 0;
                    }
                    break;
                case ".DSF":
                    if (mode != ReadHeaderMode.OnlyMetaFile) {
                        result += ReadDsfFileHeader(path) ? 1 : 0;
                    }
                    break;
                case ".DFF":
                    if (mode != ReadHeaderMode.OnlyMetaFile) {
                        result += ReadDsdiffFileHeader(path) ? 1 : 0;
                    }
                    break;
                case ".JPG":
                case ".JPEG":
                case ".PNG":
                case ".BMP":
                    // 読まないで無視する。
                    break;
                default:
                    LoadErrorMessageAdd(string.Format(CultureInfo.InvariantCulture, "{0}: {1}{2}",
                            Properties.Resources.NotSupportedFileFormat, path, Environment.NewLine));
                    break;
                }
            } catch (IOException ex) {
                HandleFileReadException(path, ex);
            } catch (ArgumentException ex) {
                HandleFileReadException(path, ex);
            } catch (UnauthorizedAccessException ex) {
                HandleFileReadException(path, ex);
            } catch (Exception ex) {
                // 未知のエラー。
                HandleFileReadException(path, ex);
            }

            return result;
        }

        public int ReadFileHeader(string path, ReadHeaderMode mode, PlaylistTrackInfo plti) {

            int result = 0;

            if (System.IO.Directory.Exists(path)) {
                // pathはディレクトリである。直下のファイル一覧を作って足す。再帰的にはたぐらない。
                var files = System.IO.Directory.GetFiles(path);

                if (mSortFolderItem) {
                    files = (from s in files orderby s select s).ToArray();
                }

                foreach (var file in files) {
                    result += ReadFileHeader1(file, mode, plti, null);
                }
            } else {
                // pathはファイル。
                result += ReadFileHeader1(path, mode, plti, null);
            }
            return result;
        }

    }
}
