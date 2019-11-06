using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace WWFlacRWCS {
    public class FlacRW {
        public const int PCM_BUFFER_BYTES = 1048576;
        private byte[] mPcmBuffer = new byte[PCM_BUFFER_BYTES];
        private int mId = (int)FlacErrorCode.IdNotFound;

        public static string ErrorCodeToStr(int ercd) {
            switch (ercd) {
            case (int)WWFlacRWCS.FlacErrorCode.OK:
                return "OK";
            case (int)WWFlacRWCS.FlacErrorCode.DataNotReady:
                return Properties.Resources.FlacErrorDataNotReady;
            case (int)WWFlacRWCS.FlacErrorCode.WriteOpenFailed:
                return Properties.Resources.FlacerrorWriteOpenFailed;
            case (int)WWFlacRWCS.FlacErrorCode.StreamDecoderNewFailed:
                return Properties.Resources.FlacErrorStreamDecoderNewFailed;
            case (int)WWFlacRWCS.FlacErrorCode.StreamDecoderInitFailed:
                return Properties.Resources.FlacErrorStreamDecoderInitFailed;
            case (int)WWFlacRWCS.FlacErrorCode.DecoderProcessFailed:
                return Properties.Resources.FlacErrorDecoderProcessFailed;
            case (int)WWFlacRWCS.FlacErrorCode.LostSync:
                return Properties.Resources.FlacErrorLostSync;
            case (int)WWFlacRWCS.FlacErrorCode.BadHeader:
                return Properties.Resources.FlacErrorBadHeader;
            case (int)WWFlacRWCS.FlacErrorCode.FrameCrcMismatch:
                return Properties.Resources.FlacErrorFrameCrcMismatch;
            case (int)WWFlacRWCS.FlacErrorCode.Unparseable:
                return Properties.Resources.FlacErrorUnparseable;
            case (int)WWFlacRWCS.FlacErrorCode.NumFrameIsNotAligned:
                return Properties.Resources.FlacErrorNumFrameIsNotAligned;
            case (int)WWFlacRWCS.FlacErrorCode.RecvBufferSizeInsufficient:
                return Properties.Resources.FlacErrorRecvBufferSizeInsufficient;
            case (int)WWFlacRWCS.FlacErrorCode.Other:
                return Properties.Resources.FlacErrorOther;
            case (int)WWFlacRWCS.FlacErrorCode.ErrorFileOpen:
                return Properties.Resources.FlacErrorFileOpen;
            case (int)WWFlacRWCS.FlacErrorCode.BufferSizeMismatch:
                return Properties.Resources.FlacErrorBufferSizeMismatch;
            case (int)WWFlacRWCS.FlacErrorCode.MemoryExhausted:
                return Properties.Resources.FlacErrorMemoryExhausted;
            case (int)WWFlacRWCS.FlacErrorCode.Encoder:
                return Properties.Resources.FlacErrorEncoder;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidNumberOfChannels:
                return Properties.Resources.FlacErrorInvalidNumberOfChannels;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidBitsPerSample:
                return Properties.Resources.FlacErrorInvalidBitsPerSample;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidSampleRate:
                return Properties.Resources.FlacErrorInvalidSampleRate;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidMetadata:
                return Properties.Resources.FlacErrorInvalidMetadata;
            case (int)WWFlacRWCS.FlacErrorCode.BadParams:
                return Properties.Resources.FlacErrorBadParams;
            case (int)WWFlacRWCS.FlacErrorCode.IdNotFound:
                return Properties.Resources.FlacErrorIdNotFound;
            case (int)WWFlacRWCS.FlacErrorCode.EncoderProcessFailed:
                return Properties.Resources.FlacErrorEncoderProcessFailed;
            case (int)WWFlacRWCS.FlacErrorCode.OutputFileTooLarge:
                return Properties.Resources.FlacErrorOutputFileTooLarge;
            case (int)WWFlacRWCS.FlacErrorCode.MD5SignatureDoesNotMatch:
                return Properties.Resources.FlacErrorMD5SignatureDoesNotMatch;
            case (int)WWFlacRWCS.FlacErrorCode.SuccessButMd5WasNotCalculated:
                return Properties.Resources.FlacErrorSuccessButMd5WasNotCalculated;
            default:
                return Properties.Resources.FlacErrorOther;
            }
        }

        public int DecodeHeader(string path) {
            mId = NativeMethods.WWFlacRW_Decode(NativeMethods.WWFLAC_FRDT_HEADER, path);
            return mId;
        }

        public int DecodeStreamStart(string path) {
            mId = NativeMethods.WWFlacRW_Decode(NativeMethods.WWFLAC_FRDT_STREAM_ONE, path);
            return mId;
        }

        public int DecodeStreamOne(out byte [] pcmReturn) {
            int ercd = NativeMethods.WWFlacRW_DecodeStreamOne(mId, mPcmBuffer, mPcmBuffer.Length);
            if (0 < ercd) {
                pcmReturn = new byte[ercd];
                Array.Copy(mPcmBuffer, 0, pcmReturn, 0, ercd);
            } else {
                pcmReturn = new byte[0];
            }

            return ercd;
        }

        public int DecodeStreamSkip(long skipFrames) {
            int ercd = NativeMethods.WWFlacRW_DecodeStreamSeekAbsolute(mId, skipFrames);
            return ercd;
        }

        public int GetDecodedMetadata(out Metadata meta) {
            NativeMethods.Metadata nMeta;
            int result = NativeMethods.WWFlacRW_GetDecodedMetadata(mId, out nMeta);
            meta = new Metadata();
            if (0 <= result) {
                meta.sampleRate = nMeta.sampleRate;
                meta.channels = nMeta.channels;
                meta.bitsPerSample = nMeta.bitsPerSample;
                meta.pictureBytes = nMeta.pictureBytes;
                meta.totalSamples = nMeta.totalSamples;
                meta.titleStr = nMeta.titleStr;
                meta.albumStr = nMeta.albumStr;
                meta.artistStr = nMeta.artistStr;
                meta.albumArtistStr = nMeta.albumArtistStr;
                meta.composerStr = nMeta.composerStr;
                meta.genreStr = nMeta.genreStr;
                meta.dateStr = nMeta.dateStr;
                meta.trackNumberStr = nMeta.trackNumberStr;
                meta.discNumberStr = nMeta.discNumberStr;
                meta.pictureMimeTypeStr = nMeta.pictureMimeTypeStr;
                meta.pictureDescriptionStr = nMeta.pictureDescriptionStr;
                meta.md5sum = nMeta.md5sum;
            }
            return result;
        }

        public int GetDecodedCuesheet(out List<FlacCuesheetTrack> cuesheet) {
            cuesheet = new List<FlacCuesheetTrack>();

            int count = NativeMethods.WWFlacRW_GetDecodedCuesheetNum(mId);
            if (count <= 0) {
                return count;
            }

            for (int i = 0; i < count; ++i) {
                NativeMethods.WWFlacCuesheetTrack wfc;
                int ercd = NativeMethods.WWFlacRW_GetDecodedCuesheetByTrackIdx(mId, i, out wfc);
                if (ercd < 0) {
                    return ercd;
                }

                var fct = new FlacCuesheetTrack();
                fct.trackNr = wfc.trackNumber;
                fct.offsetSamples = wfc.offsetSamples;

                for (int j = 0; j < wfc.trackIdxCount; ++j) {
                    var fcti = new FlacCuesheetTrackIndex();
                    fcti.indexNr = wfc.trackIdx[j].number;
                    fcti.offsetSamples = wfc.trackIdx[j].offsetSamples;
                    fct.indices.Add(fcti);
                }
                cuesheet.Add(fct);
            }

            return count;
        }

        public int GetDecodedPicture(out byte[] pictureReturn, int pictureBytes) {
            pictureReturn = new byte[pictureBytes];
            return NativeMethods.WWFlacRW_GetDecodedPicture(mId, pictureReturn, pictureReturn.Length);
        }

        public void DecodeEnd() {
            NativeMethods.WWFlacRW_DecodeEnd(mId);
            mId = (int)FlacErrorCode.IdNotFound;
        }

        public int EncodeInit(Metadata meta) {
            var nMeta = new NativeMethods.Metadata();
            nMeta.sampleRate = meta.sampleRate;
            nMeta.channels = meta.channels;
            nMeta.bitsPerSample = meta.bitsPerSample;
            nMeta.pictureBytes = meta.pictureBytes;
            nMeta.totalSamples = meta.totalSamples;
            nMeta.titleStr = meta.titleStr;
            nMeta.albumStr = meta.albumStr;
            nMeta.artistStr = meta.artistStr;
            nMeta.albumArtistStr = meta.albumArtistStr;
            nMeta.composerStr = meta.composerStr;
            nMeta.genreStr = meta.genreStr;
            nMeta.dateStr = meta.dateStr;
            nMeta.trackNumberStr = meta.trackNumberStr;
            nMeta.discNumberStr = meta.discNumberStr;
            nMeta.pictureMimeTypeStr = meta.pictureMimeTypeStr;
            nMeta.pictureDescriptionStr = meta.pictureDescriptionStr;
            nMeta.md5sum = meta.md5sum;
            mId = NativeMethods.WWFlacRW_EncodeInit(ref nMeta);
            return mId;
        }

        public int EncodeSetPicture(byte[] pictureData) {
            if (pictureData == null || pictureData.Length == 0) {
                return 0;
            }

            return NativeMethods.WWFlacRW_EncodeSetPicture(mId, pictureData, pictureData.Length);
        }

        public int EncodeAddPcm(int channel, WWUtil.LargeArray<byte> pcmData) {
            long pos = 0;
            for (long remain = pcmData.LongLength; 0 < remain;) {
                int fragmentBytes = WWUtil.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentBytes) {
                    fragmentBytes = (int)remain;
                }

                var fragment = new byte[fragmentBytes];
                pcmData.CopyTo(pos, ref fragment, 0, fragmentBytes);

                int rv = NativeMethods.WWFlacRW_EncodeSetPcmFragment(mId, channel, pos, fragment, fragmentBytes);
                if (rv < 0) {
                    return rv;
                }

                pos    += fragmentBytes;
                remain -= fragmentBytes;
            }
            return 0;
        }

        public int EncodeRun(string path) {
            return NativeMethods.WWFlacRW_EncodeRun(mId, path);
        }

        public void EncodeEnd() {
            NativeMethods.WWFlacRW_EncodeEnd(mId);
            mId = (int)FlacErrorCode.IdNotFound;
        }

        public int CheckIntegrity(string path) {
            return NativeMethods.WWFlacRW_CheckIntegrity(path);
        }
    }

    internal static class NativeMethods {
        public const int WWFLAC_TRACK_IDX_NUM = 99;
        public const int WWFLAC_TEXT_STRSZ = 256;
        public const int WWFLAC_MD5SUM_BYTES = 16;

        public const int WWFLAC_FRDT_HEADER = 1;
        public const int WWFLAC_FRDT_STREAM_ONE = 2;

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal struct Metadata {
            public int sampleRate;
            public int channels;
            public int bitsPerSample;
            public int pictureBytes;

            public long totalSamples;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string titleStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string artistStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string albumStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string albumArtistStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string composerStr;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string genreStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string dateStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string trackNumberStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string discNumberStr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string pictureMimeTypeStr;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WWFLAC_TEXT_STRSZ)]
            public string pictureDescriptionStr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] md5sum;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal struct WWFlacCuesheetTrackIdx {
            public long offsetSamples;
            public int     number;
            public int     pad; // 8バイトアラインする。
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal struct WWFlacCuesheetTrack {
            public long offsetSamples;
            public int trackNumber;
            public int numIdx;
            public int isAudio;
            public int preEmphasis;

            public int trackIdxCount;
            public int pad; // 8バイトアラインする。

            [MarshalAs(UnmanagedType.ByValArray, SizeConst=WWFLAC_TRACK_IDX_NUM)]
            public WWFlacCuesheetTrackIdx [] trackIdx;
        };

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_Decode(int frdt, string path);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_DecodeStreamOne(int id, byte[] pcmReturn, int pcmBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_DecodeStreamSeekAbsolute(int id, long skipFrames);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedMetadata(int id, out Metadata metaReturn);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedPicture(int id, byte[] pictureReturn, int pictureBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_DecodeEnd(int id);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeInit(ref Metadata meta);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeSetPicture(int id, byte[] pictureData, int pictureBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeSetPcmFragment(int id, int channel, long offs, byte[] pcmData, int copyBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeRun(int id, string path);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeEnd(int id);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_CheckIntegrity(string path);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedCuesheetNum(int id);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedCuesheetByTrackIdx(int id, int trackIdx, out WWFlacCuesheetTrack trackReturn);
    }
}
