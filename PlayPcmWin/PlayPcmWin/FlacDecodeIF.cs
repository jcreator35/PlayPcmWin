using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

namespace PlayPcmWin {
    class FlacDecodeIF {
        private int mBytesPerFrame;
        private long mNumFrames;

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
            return WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd);
        }

        WWFlacRWCS.FlacRW mFlacRW;

        private void CreatePcmData(string path, WWFlacRWCS.FlacRW flacRW,
                out PcmDataLib.PcmData pcmData_return) {
            pcmData_return = new PcmDataLib.PcmData();

            WWFlacRWCS.Metadata m;
            flacRW.GetDecodedMetadata(out m);

            pcmData_return.SetFormat(m.channels, m.bitsPerSample, m.bitsPerSample,
                    m.sampleRate, PcmDataLib.PcmData.ValueRepresentationType.SInt,
                    m.totalSamples);
            pcmData_return.SampleDataType = PcmDataLib.PcmData.DataType.PCM;

            pcmData_return.DisplayName = m.titleStr;
            pcmData_return.AlbumTitle = m.albumStr;
            pcmData_return.ArtistName = m.artistStr;
            pcmData_return.ComposerName = m.composerStr;
            pcmData_return.FullPath = path;

            mMD5SumInMetadata = new byte[MD5_BYTES];
            Array.Copy(m.md5sum, mMD5SumInMetadata, MD5_BYTES);

            bool allZero = true;
            for (int i = 0; i < MD5_BYTES; ++i) {
                if (m.md5sum[i] != 0) {
                    allZero = false;
                    break;
                }
            }
            md5MetaAvailable = !allZero;

            if (0 < m.pictureBytes) {
                byte[] picture;
                flacRW.GetDecodedPicture(out picture, m.pictureBytes);
                pcmData_return.SetPicture(picture.Length, picture);
            }
        }

        public int ReadHeader(string flacFilePath, out PcmDataLib.PcmData pcmData_return,
                out List<WWFlacRWCS.FlacCuesheetTrack> cueSheet_return) {
            pcmData_return = new PcmDataLib.PcmData();
            cueSheet_return = new List<WWFlacRWCS.FlacCuesheetTrack>();

            var flacRW = new WWFlacRWCS.FlacRW();
            int ercd = flacRW.DecodeHeader(flacFilePath);
            if (ercd < 0) {
                return ercd;
            }

            flacRW.GetDecodedCuesheet(out cueSheet_return);

            CreatePcmData(flacFilePath, flacRW, out pcmData_return);

            mBytesPerFrame = pcmData_return.BitsPerFrame / 8;
            mNumFrames = pcmData_return.NumFrames;

            return 0;
        }

        /// <summary>
        /// FLACファイルからPCMデータを取り出し開始。
        /// </summary>
        /// <param name="flacFilePath">読み込むファイルパス。</param>
        /// <param name="skipFrames">ファイルの先頭からのスキップするフレーム数。0以外の値を指定するとMD5のチェックが行われなくなる。</param>
        /// <param name="pcmData">出てきたデコード後のPCMデータ。</param>
        /// <returns>0: 成功。負: 失敗。</returns>
        public int ReadStreamBegin(string flacFilePath, long skipFrames,
                out PcmDataLib.PcmData pcmData_return) {
            pcmData_return = new PcmDataLib.PcmData();
            mFlacRW = new WWFlacRWCS.FlacRW();
            int ercd = mFlacRW.DecodeHeader(flacFilePath);
            if (ercd < 0) {
                return ercd;
            }

            CreatePcmData(flacFilePath, mFlacRW, out pcmData_return);

            mNumFrames = pcmData_return.NumFrames;
            mBytesPerFrame = pcmData_return.BitsPerFrame / 8;

            mFlacRW.DecodeStreamStart(flacFilePath);
            if (0 < skipFrames) {
                mFlacRW.DecodeStreamSkip(skipFrames);
            }

            if (CalcMD5 && skipFrames == 0) {
                md5 = new MD5CryptoServiceProvider();
                mMD5SumOfPcm = new byte[MD5_BYTES];
                mMD5TmpBuffer = new byte[WWFlacRWCS.FlacRW.PCM_BUFFER_BYTES];
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

            byte[] buff = null;
            int ercd = mFlacRW.DecodeStreamOne(out buff);
            if (ercd < 0) {
                return new byte[0];
            }

            if (md5 != null) {
                md5.TransformBlock(buff, 0, buff.Length, mMD5TmpBuffer, 0);
            }

            int frameCount = ercd / mBytesPerFrame;

            if (preferredFrames < frameCount) {
                // 欲しいフレーム数よりも多くのサンプルデータが出てきた。CUEシートの場合などで起こる。
                // データの後ろをtruncateする。
                Array.Resize(ref buff, (int)preferredFrames * mBytesPerFrame);
                frameCount = (int)preferredFrames;
            }

            return buff;
        }

        public int ReadStreamEnd()
        {
            mFlacRW.DecodeEnd();

            if (md5 != null) {
                md5.TransformFinalBlock(new byte[0], 0, 0);
                mMD5SumOfPcm = md5.Hash;
                md5.Dispose();
                md5 = null;
                mMD5TmpBuffer = null;
            }

            mBytesPerFrame = 0;

            return 0;
        }

        public void ReadStreamAbort() {
            mFlacRW.DecodeEnd();

            if (md5 != null) {
                md5.Dispose();
                md5 = null;
                mMD5TmpBuffer = null;
            }
        }

    }
}
