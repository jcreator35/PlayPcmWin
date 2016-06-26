using System.Runtime.InteropServices;

namespace WWFlacRWCS {
    public class Metadata {
        public int sampleRate;
        public int channels;
        public int bitsPerSample;
        public int pictureBytes;
        public long totalSamples;

        public string titleStr = string.Empty;
        public string artistStr = string.Empty;
        public string albumStr = string.Empty;
        public string albumArtistStr = string.Empty;
        public string genreStr = string.Empty;

        public string dateStr = string.Empty;
        public string trackNumberStr = string.Empty;
        public string discNumberStr = string.Empty;
        public string pictureMimeTypeStr = string.Empty;
        public string pictureDescriptionStr = string.Empty;

        public byte[] md5sum = new byte[NativeMethods.WWFLAC_MD5SUM_BYTES];

        public Metadata() {
        }

        /// <summary>
        /// PCMデータのバイト数。
        /// </summary>
        public long PcmBytes {
            get { return totalSamples * channels * bitsPerSample / 8; }
        }

        private void SafeCopy(string from, ref string to) {
            if (from != null && from != string.Empty) {
                to = string.Copy(from);
            }
        }

        public Metadata(Metadata rhs) {
            sampleRate = rhs.sampleRate;
            channels = rhs.channels;
            bitsPerSample = rhs.bitsPerSample;
            pictureBytes = rhs.pictureBytes;
            totalSamples = rhs.totalSamples;

            SafeCopy(rhs.titleStr, ref titleStr);
            SafeCopy(rhs.artistStr, ref artistStr);
            SafeCopy(rhs.albumStr, ref albumStr);
            SafeCopy(rhs.albumArtistStr, ref albumArtistStr);
            SafeCopy(rhs.genreStr, ref genreStr);

            SafeCopy(rhs.dateStr, ref dateStr);
            SafeCopy(rhs.trackNumberStr, ref trackNumberStr);
            SafeCopy(rhs.discNumberStr, ref discNumberStr);
            SafeCopy(rhs.pictureMimeTypeStr, ref pictureMimeTypeStr);
            SafeCopy(rhs.pictureDescriptionStr, ref pictureDescriptionStr);

            if (rhs.md5sum != null && rhs.md5sum.Length != 0) {
                md5sum = new byte[rhs.md5sum.Length];
                System.Array.Copy(rhs.md5sum, md5sum, md5sum.Length);
            } else {
                md5sum = new byte[NativeMethods.WWFLAC_MD5SUM_BYTES];
            }
        }
    };

    public enum FlacErrorCode {
        OK = 0,
        DataNotReady = -2,
        WriteOpenFailed = -3,
        StreamDecoderNewFailed = -4,
        StreamDecoderInitFailed = -5,
        DecoderProcessFailed = -6,
        LostSync = -7,
        BadHeader = -8,
        FrameCrcMismatch = -9,
        Unparseable = -10,
        NumFrameIsNotAligned = -11,
        RecvBufferSizeInsufficient = -12,
        Other = -13,
        FileReadOpen = -14,
        BufferSizeMismatch = -15,
        MemoryExhausted = -16,
        Encoder = -17,
        InvalidNumberOfChannels = -18,
        InvalidBitsPerSample = -19,
        InvalidSampleRate = -20,
        InvalidMetadata = -21,
        BadParams = -22,
        IdNotFound = -23,
        EncoderProcessFailed = -24,
        OutputFileTooLarge = -25
    };

    public class FlacRW {
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
            case (int)WWFlacRWCS.FlacErrorCode.FileReadOpen:
                return Properties.Resources.FlacErrorFileReadOpen;
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
            default:
                return Properties.Resources.FlacErrorOther;
            }
        }

        private int mId = (int)FlacErrorCode.IdNotFound;

        public int DecodeAll(string path) {
            mId = NativeMethods.WWFlacRW_DecodeAll(path);
            return mId;
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

        public int GetDecodedPicture(out byte[] pictureReturn, int pictureBytes) {
            pictureReturn = new byte[pictureBytes];
            return NativeMethods.WWFlacRW_GetDecodedPicture(mId, pictureReturn, pictureReturn.Length);
        }

        public int GetDecodedPcmBytes(int channel, long startBytes, out byte[] pcmReturn, int pcmBytes) {
            pcmReturn = new byte[pcmBytes];
            return NativeMethods.WWFlacRW_GetDecodedPcmBytes(mId, channel, startBytes, pcmReturn, pcmReturn.Length);
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
            nMeta.genreStr = meta.genreStr;
            nMeta.dateStr = meta.dateStr;
            nMeta.trackNumberStr = meta.trackNumberStr;
            nMeta.discNumberStr = meta.discNumberStr;
            nMeta.pictureMimeTypeStr = meta.pictureMimeTypeStr;
            nMeta.pictureDescriptionStr = meta.pictureDescriptionStr;
            nMeta.md5sum = meta.md5sum;
            mId = NativeMethods.WWFlacRW_EncodeInit(nMeta);
            return mId;
        }

        public int EncodeSetPicture(byte[] pictureData) {
            if (pictureData == null || pictureData.Length == 0) {
                return 0;
            }

            return NativeMethods.WWFlacRW_EncodeSetPicture(mId, pictureData, pictureData.Length);
        }

        public int EncodeAddPcm(int channel, PcmDataLib.LargeArray<byte> pcmData) {
            long pos = 0;
            for (long remain = pcmData.LongLength; 0 < remain;) {
                int fragmentBytes = PcmDataLib.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM;
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
    }

    internal static class NativeMethods {
        public const int WWFLAC_TEXT_STRSZ = 256;
        public const int WWFLAC_MD5SUM_BYTES = 16;

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

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_DecodeAll(string path);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedMetadata(int id, out Metadata metaReturn);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedPicture(int id, byte[] pictureReturn, int pictureBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_GetDecodedPcmBytes(int id, int channel, long startBytes, byte[] pcmReturn, int pcmBytes);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_DecodeEnd(int id);

        [DllImport("WWFlacRW.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFlacRW_EncodeInit(Metadata meta);

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
    }
}
