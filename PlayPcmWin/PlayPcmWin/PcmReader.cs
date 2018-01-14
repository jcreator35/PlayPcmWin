// このファイルは改善の余地あり。
// ファイルフォーマットごとのReaderを汎化するスーパークラスを作って抽象化するとここで行われているswitch-case処理は消えてなくなるであろう。

using WavRWLib2;
using PcmDataLib;
using System;
using System.IO;
using System.Globalization;
using WWMFReaderCs;
using System.Linq;

namespace PlayPcmWin {
    class PcmReader : IDisposable {
        private PcmData mPcmData;
        private FlacDecodeIF mFlacR;
        private AiffReader mAiffR;
        private DsfReader mDsfR;
        private DsdiffReader mDsdiffR;
        private WavReader mWaveR;
        private BinaryReader mBr;
        private Mp3Reader mMp3Reader;

        public long NumFrames { get; set; }

        public enum Format {
            FLAC,
            AIFF,
            WAVE,
            DSF,
            DSDIFF,
            MP3,
            Unknown
        };
        Format m_format;

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                mBr.Dispose();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        public static bool IsTheFormatParallelizable(Format fmt) {
            switch (fmt) {
            case Format.FLAC:
                return true;
            case Format.MP3:
            case Format.AIFF:
            case Format.WAVE:
            case Format.DSF:
            case Format.DSDIFF:
                return false;
            default:
                System.Diagnostics.Debug.Assert(false);
                return false;
            }
        }

        public static Format GuessFileFormatFromFilePath(string path) {
            string ext = System.IO.Path.GetExtension(path);
            switch (ext.ToUpperInvariant()) {
            case ".FLAC":
                return Format.FLAC;
            case ".AIF":
            case ".AIFF":
            case ".AIFC":
            case ".AIFFC":
                return Format.AIFF;
            case ".WAV":
            case ".WAVE":
                return Format.WAVE;
            case ".DSF":
                return Format.DSF;
            case ".DFF":
                return Format.DSDIFF;
            case ".MP3":
                return Format.MP3;
            default:
                return Format.Unknown;
            }
        }

        /// <summary>
        /// StreamBegin()を呼んだら、成功しても失敗してもStreamEnd()を呼んでください。
        /// </summary>
        /// <param name="path">ファイルパス。</param>
        /// <param name="startFrame">読み出し開始フレーム</param>
        /// <param name="wantFrames">取得したいフレーム数。負の数: 最後まで。0: 取得しない。</param>
        /// <returns>0以上: 成功。負: 失敗。</returns>
        public int StreamBegin(string path, long startFrame, long wantFrames, int typicalReadFrames) {
            var fmt = GuessFileFormatFromFilePath(path);
            try {
                switch (fmt) {
                case Format.FLAC:
                    m_format = Format.FLAC;
                    return StreamBeginFlac(path, startFrame);
                case Format.AIFF:
                    m_format = Format.AIFF;
                    return StreamBeginAiff(path, startFrame);
                case Format.WAVE:
                    m_format = Format.WAVE;
                    return StreamBeginWave(path, startFrame);
                case Format.DSF:
                    m_format = Format.DSF;
                    return StreamBeginDsf(path, startFrame);
                case Format.DSDIFF:
                    m_format = Format.DSDIFF;
                    return StreamBeginDsdiff(path, startFrame);
                case Format.MP3:
                    m_format = Format.MP3;
                    return StreamBeginMp3(path, (int)startFrame);
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return -1;
                }
            } catch (IOException ex) {
                Console.WriteLine("E: StreamBegin {0}" + ex);
                return -1;
            } catch (ArgumentException ex) {
                Console.WriteLine("E: StreamBegin {0}" + ex);
                return -1;
            } catch (UnauthorizedAccessException ex) {
                Console.WriteLine("E: StreamBegin {0}" + ex);
                return -1;
            }
        }

        /// <summary>
        /// PCMデータを読み出す。
        /// </summary>
        /// <param name="preferredFrames">読み込みたいフレーム数。1Mフレームぐらいにすると良い。(このフレーム数のデータが戻るとは限らない)</param>
        /// <returns>PCMデータが詰まったバイト列。0要素の配列の場合、もう終わり。</returns>
        public byte[] StreamReadOne(int preferredFrames, out int ercd) {
            ercd = 0;

            // FLACのデコーダーはエラーコードを戻すことがある。
            // 他のデコーダーは、データ領域に構造がないので読み出しエラーは特にない。System.IOExceptionが起きることはある。

            byte[] result;
            switch (m_format) {
            case Format.FLAC:
                result = mFlacR.ReadStreamReadOne(preferredFrames, out ercd);
                break;
            case Format.AIFF:
                result = mAiffR.ReadStreamReadOne(mBr, preferredFrames);
                break;
            case Format.WAVE:
                result = mWaveR.ReadStreamReadOne(mBr, preferredFrames);
                break;
            case Format.DSF:
                result = mDsfR.ReadStreamReadOne(mBr, preferredFrames);
                break;
            case Format.DSDIFF:
                result = mDsdiffR.ReadStreamReadOne(mBr, preferredFrames);
                break;
            case Format.MP3:
                result = mMp3Reader.data;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                result = new byte[0];
                break;
            }
            return result;
        }

        public void StreamAbort() {
            switch (m_format) {
            case Format.FLAC:
                mFlacR.ReadStreamAbort();
                break;
            case Format.AIFF:
                mAiffR.ReadStreamEnd();
                break;
            case Format.WAVE:
                mWaveR.ReadStreamEnd();
                break;
            case Format.DSF:
                mDsfR.ReadStreamEnd();
                break;
            case Format.DSDIFF:
                mDsdiffR.ReadStreamEnd();
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            if (null != mBr) {
                mBr.Close();
                mBr = null;
            }
            mPcmData = null;
            mFlacR = null;
            mAiffR = null;
            mWaveR = null;
            mDsfR = null;
        }

        /// <summary>
        /// 読み込み処理を通常終了する。
        /// </summary>
        /// <returns>Error code</returns>
        public int StreamEnd() {
            int rv = 0;

            mMD5SumInMetadata = null;
            mMD5SumOfPcm = null;

            switch (m_format) {
            case Format.FLAC:
                rv = mFlacR.ReadStreamEnd();
                mMD5SumInMetadata = mFlacR.MD5SumInMetadata;
                mMD5SumOfPcm = mFlacR.MD5SumOfPcm;
                break;
            case Format.AIFF:
                mAiffR.ReadStreamEnd();
                break;
            case Format.WAVE:
                mWaveR.ReadStreamEnd();
                break;
            case Format.DSF:
                mDsfR.ReadStreamEnd();
                break;
            case Format.DSDIFF:
                mDsdiffR.ReadStreamEnd();
                break;
            case Format.MP3:
                mMp3Reader.ReadStreamEnd();
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            if (null != mBr) {
                mBr.Close();
                mBr = null;
            }
            mPcmData = null;
            mFlacR = null;
            mAiffR = null;
            mWaveR = null;
            mDsfR = null;

            return rv;
        }

        private byte [] mMD5SumOfPcm;
        private byte [] mMD5SumInMetadata;

        public byte[] MD5SumOfPcm { get { return mMD5SumOfPcm; } }
        public byte[] MD5SumInMetadata { get { return mMD5SumInMetadata; } }

        public static bool CalcMD5SumIfAvailable { get; set; }

        private int StreamBeginFlac(string path, long startFrame)
        {
            // m_pcmData = new PcmDataLib.PcmData();
            mFlacR = new FlacDecodeIF();
            mFlacR.CalcMD5 = CalcMD5SumIfAvailable;
            int ercd = mFlacR.ReadStreamBegin(path, startFrame, out mPcmData);
            if (ercd < 0) {
                return ercd;
            }

            NumFrames = mFlacR.NumFrames;
            return ercd;
        }

        private int StreamBeginAiff(string path, long startFrame)
        {
            int ercd = -1;

            mAiffR = new AiffReader();
            mBr = new BinaryReader(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            AiffReader.ResultType result = mAiffR.ReadStreamBegin(mBr, out mPcmData);
            if (result == AiffReader.ResultType.Success) {

                NumFrames = mAiffR.NumFrames;

                mAiffR.ReadStreamSkip(mBr, startFrame);
                ercd = 0;
            }

            return ercd;
        }

        private int StreamBeginDsf(string path, long startFrame) {
            int ercd = -1;

            mDsfR = new DsfReader();
            mBr = new BinaryReader(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            DsfReader.ResultType result = mDsfR.ReadStreamBegin(mBr, out mPcmData);
            if (result == DsfReader.ResultType.Success) {

                NumFrames = mDsfR.OutputFrames;

                mDsfR.ReadStreamSkip(mBr, startFrame);
                ercd = 0;
            }

            return ercd;
        }

        private int StreamBeginDsdiff(string path, long startFrame) {
            int ercd = -1;

            mDsdiffR = new DsdiffReader();
            mBr = new BinaryReader(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            DsdiffReader.ResultType result = mDsdiffR.ReadStreamBegin(mBr, out mPcmData);
            if (result == DsdiffReader.ResultType.Success) {

                NumFrames = mDsdiffR.OutputFrames;

                mDsdiffR.ReadStreamSkip(mBr, startFrame);
                ercd = 0;
            }

            return ercd;
        }

        private int StreamBeginMp3(string path, int startFrame) {
            mMp3Reader = new Mp3Reader();
            int hr = mMp3Reader.Read(path);
            if (0 <= hr && 0 < startFrame) {
                if (startFrame < mMp3Reader.data.Length) {
                    mMp3Reader.data = mMp3Reader.data.Skip(startFrame).ToArray();
                }
            }

            return hr;
        }
        
        private int StreamBeginWave(string path, long startFrame) {
            int ercd = -1;

            mWaveR = new WavReader();
            mBr = new BinaryReader(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            bool readSuccess = mWaveR.ReadStreamBegin(mBr, out mPcmData);
            if (readSuccess) {

                NumFrames = mWaveR.NumFrames;

                if (mWaveR.ReadStreamSkip(mBr, startFrame)) {
                    ercd = 0;
                }
            }
            return ercd;
        }
    }
}
