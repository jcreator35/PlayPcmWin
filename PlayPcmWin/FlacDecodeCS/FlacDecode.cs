using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Pipes;
using System.Globalization;

namespace FlacDecodeCS {
    public enum DecodeResultType {
        /// ヘッダの取得やデータの取得に成功。
        Success = 0,

        /// ファイルの最後まで行き、デコードを完了した。もうデータはない。
        Completed = 1,

        // 以下、FLACデコードエラー。
        DataNotReady = -2,
        WriteOpenFailed = -3,
        FlacStreamDecoderNewFailed = -4,

        FlacStreamDecoderInitFailed = -5,
        FlacStreamDecorderProcessFailed = -6,
        LostSync = -7,
        BadHeader = -8,
        FrameCrcMismatch = -9,

        Unparseable = -10,
        NumFrameIsNotAligned = -11,
        RecvBufferSizeInsufficient = -12,
        OtherError = -13,
        FileOpenReadError = -14,
    };

    internal static class NativeMethods {
        [DllImport("FlacDecodeDLL.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int FlacDecodeDLL_DecodeStart(string path, long skipFrames);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        void FlacDecodeDLL_DecodeEnd(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetNumOfChannels(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetBitsPerSample(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetSampleRate(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        long FlacDecodeDLL_GetNumFrames(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetNumFramesPerBlock(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetLastResult(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetNextPcmData(int id, int numFrame, byte[] buff);

        [DllImport("FlacDecodeDLL.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal extern static
        bool FlacDecodeDLL_GetTitleStr(int id, System.Text.StringBuilder name, int nameBytes);

        [DllImport("FlacDecodeDLL.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal extern static
        bool FlacDecodeDLL_GetAlbumStr(int id, System.Text.StringBuilder name, int nameBytes);

        [DllImport("FlacDecodeDLL.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal extern static
        bool FlacDecodeDLL_GetArtistStr(int id, System.Text.StringBuilder name, int nameBytes);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetPictureBytes(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetPictureData(int id, int offs, int pictureBytes, byte[] buff);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetMD5Sum(int id, byte[] buff);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(int id);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(int id, int trackId);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        long FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(int id, int trackId);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetEmbeddedCuesheetTrackNumOfIndices(int id, int trackId);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        int FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexNumber(int id, int trackId, int indexId);

        [DllImport("FlacDecodeDLL.dll")]
        internal extern static
        long FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexOffsetSamples(int id, int trackId, int indexId);
    }

    public sealed class FlacDecode {

        private FlacDecode() { }

        enum OperationType {
            DecodeAll,
            DecodeHeaderOnly
        }

        private const int MD5_BYTES = 16;

        private static string Base64Decode(string s) {
            byte[] bytes = Convert.FromBase64String(s);
            char[] chars = new char[bytes.Length / 2];

            int count = 0;
            for (int i = 0; i < chars.Length; ++i) {
                int c0 = bytes[count++];
                int c1 = bytes[count++];
                chars[i] = (char)(c0 + (c1 << 8));
            }

            return new string(chars);
        }

#if DEBUG == true
        static private System.IO.StreamWriter m_logFile;
#endif

        static private void LogOpen() {
#if DEBUG == true
            m_logFile = new System.IO.StreamWriter(
                string.Format("logDecodeCS{0}.txt",
                    System.Diagnostics.Process.GetCurrentProcess().Id));
#endif
        }
        static private void LogClose() {
#if DEBUG == true
            m_logFile.Close();
            m_logFile = null;
#endif
        }

        static private void LogWriteLine(string s) {
#if DEBUG == true
            m_logFile.WriteLine(s);
            LogFlush();
#endif
        }

        static private void LogFlush() {
#if DEBUG == true
            m_logFile.Flush();
#endif
        }

        private static int DecodeOne(BinaryWriter bw) {
            string operationStr = System.Console.ReadLine();
            if (null == operationStr) {
                LogWriteLine("stdinの1行目には、ヘッダーのみ抽出の場合H、内容も抽出する場合Aを入力してください。");
                return -2;
            }

            OperationType operationType = OperationType.DecodeAll;
            switch (operationStr[0]) {
            case 'H':
                operationType = OperationType.DecodeHeaderOnly;
                break;
            case 'A':
                operationType = OperationType.DecodeAll;
                break;
            default:
                LogWriteLine("stdinの1行目には、ヘッダーのみ抽出の場合H、内容も抽出する場合Aを入力してください。");
                return -3;
            }

            string sbase64 = System.Console.ReadLine();
            if (null == sbase64) {
                LogWriteLine("stdinの2行目には、FLACファイルのパスを入力してください。");
                return -4;
            }

            string path = Base64Decode(sbase64);

            long skipFrames = 0;
            long wantFrames = 0;
            if (operationType == OperationType.DecodeAll) {
                // 内容抽出の場合
                // 3行目にスキップサンプル数。
                // 4行目に取得サンプル数。
                string skipFramesStr = System.Console.ReadLine();
                if (null == skipFramesStr || !Int64.TryParse(skipFramesStr, out skipFrames) || skipFrames < 0) {
                    LogWriteLine("内容抽出の場合、stdinの3行目にスキップフレーム数を入力してください。");
                    return -3;
                }

                string wantFramesStr = System.Console.ReadLine();
                if (null == wantFramesStr || !Int64.TryParse(wantFramesStr, out wantFrames)) {
                    LogWriteLine("内容抽出の場合、stdinの4行目に読み込むフレーム数を入力してください。");
                    return -3;
                }
            }
            if (operationType == OperationType.DecodeHeaderOnly) {
                skipFrames = -1;
            }

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne operationType={0} path={1} skipF={2} wantF={3}",
                operationType, path, skipFrames, wantFrames));

            /* パイプ出力の内容
             * オフセット サイズ(バイト) 内容
             * 0          4              リターンコード (0以外の場合 以降のデータは無い)
             * 4          4              チャンネル数   nChannels
             * 8          4              サンプルレート sampleRate
             * 12         4              量子化ビット数 bitsPerSample
             * 16         8              総フレーム数   numFrames (残りフレーム数ではない)
             * 24         4              NumFramesPerBlock
             * 28         titleLen       タイトル文字列
             * 28+titleLen albumLen      アルバム文字列
             * 28+t+al     artistLen      アーティスト文字列
             * 
             * 28+t+al+ar  1             MD5利用可能の時1 利用不可の時0
             * 29+t+al+ar  16            PCMデータ全体のMD5
             * 
             * 45+t+al+ar  4             画像データバイト数
             * 49+t+al+ar  pictureBytes   画像データ
             * 49+t+al+ar+pic   4         CUEシートトラック数 nct
             * 53+t+al+ar+pic+tOffs  4    CUEシートトラック0のトラック番号
             * 57+t+al+ar+pic+tOffs  8    CUEシートトラック0のオフセット
             * 65+t+al+ar+pic+tOffs  4    CUEシートトラック0のインデックス数nIdx0
             * 69+t+al+ar+pic+tOffs  8    CUEシートトラック0インデックス0のインデックス番号
             * …
             * frameOffs    4             frameCount1 (ヘッダのみの場合なし)
             * frameOffs+4  ※1           PCMデータ1(リトルエンディアン、LRLRLR…) (ヘッダのみの場合なし)
             * ※2          4             frameCount2
             * ※2+4        ※3           PCMデータ2
             * 
             * ※1…frameCount1 * nChannels * (bitsPerSample/8)
             * ※2…32+t+al+ar+pic+tOffs+※1
             */

            int rv = NativeMethods.FlacDecodeDLL_DecodeStart(path, skipFrames);
            bw.Write(rv);
            if (rv < 0) {
                LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FLACデコード開始エラー。{0}", rv));
                // NativeMethods.FlacDecodeDLL_DecodeEnd(-1);
                return rv;
            }

            int id = rv;

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne id={0}", id));

            int nChannels     = NativeMethods.FlacDecodeDLL_GetNumOfChannels(id);
            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne GetNumChannels() {0}", nChannels));

            int bitsPerSample = NativeMethods.FlacDecodeDLL_GetBitsPerSample(id);
            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne GetBitsPerSample() {0}", bitsPerSample));

            int sampleRate    = NativeMethods.FlacDecodeDLL_GetSampleRate(id);
            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne GetSampleRate() {0}", sampleRate));

            long numFrames   = NativeMethods.FlacDecodeDLL_GetNumFrames(id);
            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne GetNumFrames() {0}", numFrames));

            int numFramesPerBlock = NativeMethods.FlacDecodeDLL_GetNumFramesPerBlock(id);
            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne GetNumFramesPerBlock() {0}", numFramesPerBlock));

            StringBuilder buf = new StringBuilder(256);
            NativeMethods.FlacDecodeDLL_GetTitleStr(id, buf, buf.Capacity * 2);
            string titleStr = buf.ToString();

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne titleStr {0}", titleStr));

            NativeMethods.FlacDecodeDLL_GetAlbumStr(id, buf, buf.Capacity * 2);
            string albumStr = buf.ToString();

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne albumStr {0}", albumStr));

            NativeMethods.FlacDecodeDLL_GetArtistStr(id, buf, buf.Capacity * 2);
            string artistStr = buf.ToString();

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne artistStr {0}", artistStr));

            int pictureBytes = NativeMethods.FlacDecodeDLL_GetPictureBytes(id);

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne pictureBytes {0}", pictureBytes));

            byte[] pictureData = null;
            if (0 < pictureBytes) {
                pictureData = new byte[pictureBytes];
                rv = NativeMethods.FlacDecodeDLL_GetPictureData(id, 0, pictureBytes, pictureData);
                if (rv < 0) {
                    LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne FlacDecodeDLL_GetPictureData rv={0}", rv));
                    pictureBytes = 0;
                    pictureData = null;
                }
            }

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne output start"));

            byte [] md5sum = new byte[MD5_BYTES];
            rv = NativeMethods.FlacDecodeDLL_GetMD5Sum(id, md5sum);
            byte md5Available = (rv != 0) ? (byte)1 : (byte)0;

            bw.Write(nChannels);
            bw.Write(bitsPerSample);
            bw.Write(sampleRate);
            bw.Write(numFrames);
            bw.Write(numFramesPerBlock);

            bw.Write(titleStr);
            bw.Write(albumStr);
            bw.Write(artistStr);

            bw.Write(md5Available);
            bw.Write(md5sum);

            bw.Write(pictureBytes);
            if (0 < pictureBytes) {
                bw.Write(pictureData);
            }

            {
                // Cuesheets
                int numCuesheetTracks = NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(id);
                LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne numOfCuesheetTracks {0}", numCuesheetTracks));
                bw.Write(numCuesheetTracks);

                for (int trackId=0; trackId < numCuesheetTracks; ++trackId) {
                    bw.Write(NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(id, trackId));
                    bw.Write(NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(id, trackId));

                    int numCuesheetTrackIndices = NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetTrackNumOfIndices(id, trackId);
                    LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecodeCS DecodeOne CuesheetTrackNumOfIndices track={0} nIdx={1}", trackId, numCuesheetTrackIndices));
                    bw.Write(numCuesheetTrackIndices);

                    for (int indexId=0; indexId < numCuesheetTrackIndices; ++indexId) {
                        bw.Write(NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexNumber(id, trackId, indexId));
                        bw.Write(NativeMethods.FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexOffsetSamples(id, trackId, indexId));
                    }
                }
            }

            int ercd = 0;

            if (operationType == OperationType.DecodeAll && wantFrames != 0) {
                // デコードしたデータを全部パイプに出力する。

                if (wantFrames < 0) {
                    // wantFramesが負の値の時、最後まで読み出す。
                    wantFrames = numFrames - skipFrames;
                }

                int frameBytes = nChannels * bitsPerSample / 8;
                long readFrames = 0;

                const int numFramePerCall = 1024 * 1024;
                byte[] buff = new byte[numFramePerCall * frameBytes];

                while (true) {
                    LogWriteLine("NativeMethods.FlacDecodeDLL_GetNextPcmData 呼び出し");
                    rv = NativeMethods.FlacDecodeDLL_GetNextPcmData(id, numFramePerCall, buff);
                    ercd = NativeMethods.FlacDecodeDLL_GetLastResult(id);
                    LogWriteLine(string.Format(CultureInfo.InvariantCulture, "NativeMethods.FlacDecodeDLL_GetNextPcmData rv={0} ercd={1}", rv, ercd));

                    if (0 < rv) {
                        bw.Write(rv);
                        bw.Write(buff, 0, rv * frameBytes);

                        readFrames += rv;
                    }

                    if (rv <= 0 || ercd == 1 || wantFrames <= readFrames) {
                        // これでおしまい。
                        int v0 = 0;
                        bw.Write(v0);
                        LogWriteLine(string.Format(CultureInfo.InvariantCulture, "NativeMethods.FlacDecodeDLL_GetNextPcmData 終了。rv={0} ercd={1}", rv, ercd));
                        if (0 <= rv && ercd == 1) {
                            ercd = 0;
                        }
                        break;
                    }
                }
            }

            LogWriteLine("NativeMethods.FlacDecodeDLL_DecodeEnd 呼び出し");
            NativeMethods.FlacDecodeDLL_DecodeEnd(id);
            return ercd;
        }

        private static int Run(string pipeHandleAsString) {
            int exitCode = -1;
            using (PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeHandleAsString)) {
                using (BinaryWriter bw = new BinaryWriter(pipeClient)) {
                    try {
                        exitCode = DecodeOne(bw);
                    } catch (IOException ex) {
                        LogWriteLine(string.Format(CultureInfo.InvariantCulture, "E: {0}", ex));
                        exitCode = -5;
                    } catch (ArgumentException ex) {
                        LogWriteLine(string.Format(CultureInfo.InvariantCulture, "E: {0}", ex));
                        exitCode = -5;
                    } catch (UnauthorizedAccessException ex) {
                        LogWriteLine(string.Format(CultureInfo.InvariantCulture, "E: {0}", ex));
                        exitCode = -5;
                    }
                }
            }
            return exitCode;
        }

        static int Main1(string[] args) {
            if (1 != args.Length) {
                LogWriteLine(string.Format(CultureInfo.InvariantCulture, "E: FlacDecode.cs args[0] must be pipeHandleAsStream"));
                return (int)DecodeResultType.OtherError;
            }

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecode.cs Main1 開始 args[0]={0}", args[0]));

            int exitCode = FlacDecode.Run(args[0]);

            LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecode.cs Main1 終了 exitCode={0}", exitCode));
            return exitCode;
        }

        static void Main(string[] args) {
            LogOpen();
            LogWriteLine("started");

            int exitCode = (int)DecodeResultType.OtherError;
            try {
                exitCode = Main1(args);
            } catch (Exception ex) {
                LogWriteLine(string.Format(CultureInfo.InvariantCulture, "FlacDecode.cs Main {0}", ex));
            }
            LogClose();

            System.Environment.ExitCode = exitCode;
        }
    }
}