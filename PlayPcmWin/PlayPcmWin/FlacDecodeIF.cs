using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

namespace PlayPcmWin {
    class FlacDecodeIF : IDisposable {
        public struct FlacCuesheetTrackIndexInfo {
            public int indexNr;
            public long offsetSamples;
        }

        public class FlacCuesheetTrackInfo {
            public int trackNr;
            public long offsetSamples;
            public List<FlacCuesheetTrackIndexInfo> indices = new List<FlacCuesheetTrackIndexInfo>();
        };

        private Process mChildProcess;
        private BinaryReader mBinaryReader;
        private AnonymousPipeServerStream mPipeServerStream;
        private int mBytesPerFrame;
        private long mNumFrames;
        private int mPictureBytes;
        private byte[] mPictureData;

        private bool md5MetaAvailable;
        private MD5 md5;
        private byte[] mMD5SumOfPcm;
        private byte[] mMD5SumInMetadata;
        private byte[] mMD5TmpBuffer;
        private const int MD5_BYTES = 16;

        public bool CalcMD5 { get; set; }
        public byte[] MD5SumInMetadata {
            get {
                if (md5MetaAvailable) {
                    return mMD5SumInMetadata;
                }
                return null;
            }
        }
        public byte[] MD5SumOfPcm { get { return mMD5SumOfPcm; } }

        public long NumFrames {
            get { return mNumFrames; }
        }

        public static string ErrorCodeToStr(int ercd) {
            switch (ercd) {
            case (int)FlacDecodeCS.DecodeResultType.Success:
                return "Success";
            case (int)FlacDecodeCS.DecodeResultType.Completed:
                return "Completed";
            case (int)FlacDecodeCS.DecodeResultType.DataNotReady:
                return "Data not ready (internal error)";
            case (int)FlacDecodeCS.DecodeResultType.WriteOpenFailed:
                return "Could not open specified file";
            case (int)FlacDecodeCS.DecodeResultType.FlacStreamDecoderNewFailed:
                return "FlacStreamDecoder create failed";

            case (int)FlacDecodeCS.DecodeResultType.FlacStreamDecoderInitFailed:
                return "FlacStreamDecoder init failed";
            case (int)FlacDecodeCS.DecodeResultType.FlacStreamDecorderProcessFailed:
                return "FlacStreamDecoder returns fail";
            case (int)FlacDecodeCS.DecodeResultType.LostSync:
                return "Lost sync while decoding (file corrupted)";
            case (int)FlacDecodeCS.DecodeResultType.BadHeader:
                return "Bad header";
            case (int)FlacDecodeCS.DecodeResultType.FrameCrcMismatch:
                return "CRC mismatch. (File corrupted)";

            case (int)FlacDecodeCS.DecodeResultType.Unparseable:
                return "Unparsable data";
            case (int)FlacDecodeCS.DecodeResultType.NumFrameIsNotAligned:
                return "NumFrame is not aligned (internal error)";
            case (int)FlacDecodeCS.DecodeResultType.RecvBufferSizeInsufficient:
                return "Recv bufer size is insufficient (internal error)";
            case (int)FlacDecodeCS.DecodeResultType.FileOpenReadError:
                return "File open or read error";
            case (int)FlacDecodeCS.DecodeResultType.OtherError:
            default:
                return "Other error";
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                ReadStreamAbort();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void SendString(string s) {
            mChildProcess.StandardInput.WriteLine(s);
        }

        private void SendBase64(string s) {
            byte[] b = new byte[s.Length * 2];

            for (int i = 0; i < s.Length; ++i) {
                char c = s[i];
                b[i * 2 + 0] = (byte)((c >> 0) & 0xff);
                b[i * 2 + 1] = (byte)((c >> 8) & 0xff);
            }
            string sSend = Convert.ToBase64String(b);
            mChildProcess.StandardInput.WriteLine(sSend);
        }

        private void StartChildProcess() {
            System.Diagnostics.Debug.Assert(null == mChildProcess);

            mChildProcess = new Process();
            mChildProcess.StartInfo.FileName = "FlacDecodeCS.exe";

            mPipeServerStream = new AnonymousPipeServerStream(
                PipeDirection.In, HandleInheritability.Inheritable);

            mChildProcess.StartInfo.Arguments = mPipeServerStream.GetClientHandleAsString();
            mChildProcess.StartInfo.UseShellExecute = false;
            mChildProcess.StartInfo.CreateNoWindow = true;
            mChildProcess.StartInfo.RedirectStandardInput = true;
            mChildProcess.StartInfo.RedirectStandardOutput = false;
            mChildProcess.Start();

            mPipeServerStream.DisposeLocalCopyOfClientHandle();
            mBinaryReader = new BinaryReader(mPipeServerStream);
        }

        private int StopChildProcess() {
            int exitCode = (int)FlacDecodeCS.DecodeResultType.FileOpenReadError;

            if (null != mChildProcess) {
                mChildProcess.WaitForExit();
                exitCode = mChildProcess.ExitCode;
                mChildProcess.Close();
                mChildProcess = null;
            }

            if (null != mPipeServerStream) {
                mPipeServerStream.Close();
                mPipeServerStream = null;
            }

            if (null != mBinaryReader) {
                mBinaryReader.Close();
                mBinaryReader = null;
            }

            return exitCode;
        }

        enum ReadMode {
            Header,
            HeadereAndData,
        };

        private int ReadStartCommon(ReadMode mode, string flacFilePath, long skipFrames, long wantFrames,
            out PcmDataLib.PcmData pcmData_return, out List<FlacCuesheetTrackInfo> cueSheet_return) {
            pcmData_return = new PcmDataLib.PcmData();
            cueSheet_return = new List<FlacCuesheetTrackInfo>();
            
            StartChildProcess();

            switch (mode) {
            case ReadMode.Header:
                SendString("H");
                SendBase64(flacFilePath);
                break;
            case ReadMode.HeadereAndData:
                SendString("A");
                SendBase64(flacFilePath);
                SendString(skipFrames.ToString(CultureInfo.InvariantCulture));
                SendString(wantFrames.ToString(CultureInfo.InvariantCulture));
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            int rv = mBinaryReader.ReadInt32();
            if (rv != 0) {
                return rv;
            }

            int nChannels     = mBinaryReader.ReadInt32();
            int bitsPerSample = mBinaryReader.ReadInt32();
            int sampleRate    = mBinaryReader.ReadInt32();

            mNumFrames         = mBinaryReader.ReadInt64();
            int numFramesPerBlock = mBinaryReader.ReadInt32();

            string titleStr = mBinaryReader.ReadString();
            string albumStr = mBinaryReader.ReadString();
            string artistStr = mBinaryReader.ReadString();

            byte md5Available = mBinaryReader.ReadByte();
            md5MetaAvailable = md5Available != 0;

            mMD5SumInMetadata = mBinaryReader.ReadBytes(MD5_BYTES);

            mPictureBytes = mBinaryReader.ReadInt32();
            mPictureData = new byte[0];
            if (0 < mPictureBytes) {
                mPictureData = mBinaryReader.ReadBytes(mPictureBytes);
            }

            {
                int numCuesheetTracks = mBinaryReader.ReadInt32();
                for (int trackId=0; trackId < numCuesheetTracks; ++trackId) {
                    var cti = new FlacCuesheetTrackInfo();
                    cti.trackNr = mBinaryReader.ReadInt32();
                    cti.offsetSamples = mBinaryReader.ReadInt64();

                    int numCuesheetTrackIndices = mBinaryReader.ReadInt32();
                    for (int indexId=0; indexId < numCuesheetTrackIndices; ++indexId) {
                        var indexInfo = new FlacCuesheetTrackIndexInfo();
                        indexInfo.indexNr = mBinaryReader.ReadInt32();
                        indexInfo.offsetSamples = mBinaryReader.ReadInt64();
                        cti.indices.Add(indexInfo);
                    }
                    cueSheet_return.Add(cti);
                }
            }

            pcmData_return.SetFormat(
                nChannels,
                bitsPerSample,
                bitsPerSample,
                sampleRate,
                PcmDataLib.PcmData.ValueRepresentationType.SInt,
                mNumFrames);

            pcmData_return.DisplayName = titleStr;
            pcmData_return.AlbumTitle = albumStr;
            pcmData_return.ArtistName = artistStr;

            pcmData_return.SetPicture(mPictureBytes, mPictureData);
            return 0;
        }

        public int ReadHeader(string flacFilePath, out PcmDataLib.PcmData pcmData_return, out List<FlacCuesheetTrackInfo> cueSheet_return) {
            int rv = ReadStartCommon(ReadMode.Header, flacFilePath, 0, 0, out pcmData_return, out cueSheet_return);
            StopChildProcess();
            if (rv != 0) {
                return rv;
            }

            return 0;
        }

        /// <summary>
        /// FLACファイルからPCMデータを取り出し開始。
        /// </summary>
        /// <param name="flacFilePath">読み込むファイルパス。</param>
        /// <param name="skipFrames">ファイルの先頭からのスキップするフレーム数。0以外の値を指定するとMD5のチェックが行われなくなる。</param>
        /// <param name="wantFrames">取得するフレーム数。</param>
        /// <param name="pcmData">出てきたデコード後のPCMデータ。</param>
        /// <returns>0: 成功。負: 失敗。</returns>
        public int ReadStreamBegin(string flacFilePath, long skipFrames, long wantFrames, int typicalReadFrames, out PcmDataLib.PcmData pcmData_return) {
            List<FlacCuesheetTrackInfo> cti;
            int rv = ReadStartCommon(ReadMode.HeadereAndData, flacFilePath, skipFrames, wantFrames, out pcmData_return, out cti);
            if (rv != 0) {
                StopChildProcess();
                mBytesPerFrame = 0;
                return rv;
            }

            mBytesPerFrame = pcmData_return.BitsPerFrame / 8;

            if (CalcMD5 && skipFrames == 0 && wantFrames == mNumFrames) {
                md5 = new MD5CryptoServiceProvider();
                mMD5SumOfPcm = new byte[MD5_BYTES];
                mMD5TmpBuffer = new byte[mBytesPerFrame * typicalReadFrames];
            }

            return 0;
        }

        /// <summary>
        /// PCMサンプルを読み出す。
        /// </summary>
        /// <returns>読んだサンプルデータ。サイズはpreferredFramesよりも少ない場合がある。(preferredFramesよりも多くはない。)</returns>
        public byte [] ReadStreamReadOne(long preferredFrames)
        {
            System.Diagnostics.Debug.Assert(0 < mBytesPerFrame);

            int frameCount = mBinaryReader.ReadInt32();
            // System.Console.WriteLine("ReadStreamReadOne() frameCount={0}", frameCount);

            if (frameCount == 0) {
                return new byte[0];
            }

            byte [] sampleArray = mBinaryReader.ReadBytes(frameCount * mBytesPerFrame);

            if (md5 != null) {
                md5.TransformBlock(sampleArray, 0, sampleArray.Length, mMD5TmpBuffer, 0);
            }

            if (preferredFrames < frameCount) {
                // 欲しいフレーム数よりも多くのサンプルデータが出てきた。CUEシートの場合などで起こる。
                // データの後ろをtruncateする。
                Array.Resize(ref sampleArray, (int)preferredFrames * mBytesPerFrame);
                frameCount = (int)preferredFrames;
            }

            return sampleArray;
        }

        public int ReadStreamEnd()
        {
            int exitCode = StopChildProcess();

            if (md5 != null) {
                md5.TransformFinalBlock(new byte[0], 0, 0);
                mMD5SumOfPcm = md5.Hash;
                md5.Dispose();
                md5 = null;
                mMD5TmpBuffer = null;
            }

            mBytesPerFrame = 0;

            return exitCode;
        }

        public void ReadStreamAbort() {
            System.Diagnostics.Debug.Assert(null != mChildProcess);
            mPipeServerStream.Close();
            mPipeServerStream = null;

            mChildProcess.Close();
            mChildProcess = null;

            mBinaryReader.Close();
            mBinaryReader = null;

            if (md5 != null) {
                md5.Dispose();
                md5 = null;
                mMD5TmpBuffer = null;
            }
        }

    }
}
