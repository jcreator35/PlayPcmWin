using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace WWMFReaderCs {
    public class WWMFReader {

        public struct Metadata {
            public int sampleRate;
            public int numChannels;
            public int bitsPerSample;
            public int bitRate;

            public int dwChannelMask;

            /// おおよその値が戻る。
            public long numApproxFrames;

            public long numExactFrames;

            public string title;
            public string artist;
            public string album;
            public string composer;

            public byte[] picture;

            public long CalcApproxDataBytes() {
                return numApproxFrames * numChannels * bitsPerSample / 8;
            }
        };

        private static byte[] TrimJpeg(byte[] p) {
            int pos = 0;

            do {
                pos = Array.IndexOf(p, (byte)(0xff), pos);
                if (0 <= pos && pos + 4 < p.Length
                        && p[pos + 1] == 0xd8
                        && p[pos + 2] == 0xff
                        && (p[pos + 3] == 0xdb || p[pos + 3] == 0xe0 || p[pos + 3] == 0xe1)) {
                    return p.Skip(pos).ToArray();
                }
                pos += 1;

            } while (0 <= pos && pos + 4 < p.Length);

            return new byte [0];
        }

        private static byte[] TrimPng(byte[] p) {
            int pos = 0;

            do {
                pos = Array.IndexOf(p, (byte)(0x89), pos);
                if (0 <= pos && pos + 4 < p.Length && p[pos + 1] == 0x50 && p[pos + 2] == 0x4e && p[pos + 3] == 0x47) {
                    return p.Skip(pos).ToArray();
                }
                pos += 1;

            } while (0 <= pos && pos + 4 < p.Length);

            return new byte[0];
        }

        public static int ReadHeader(
               string wszSourceFile,
               out Metadata meta_return) {
            meta_return = new Metadata();

            NativeMethods.NativeMetadata nativeMeta;
            int hr = NativeMethods.WWMFReaderReadHeader(wszSourceFile, out nativeMeta);
            if (hr < 0) {
                return hr;
            }

            meta_return.sampleRate = nativeMeta.sampleRate;
            meta_return.numChannels = nativeMeta.numChannels;
            meta_return.bitsPerSample = nativeMeta.bitsPerSample;
            meta_return.bitRate = nativeMeta.bitRate;
            meta_return.dwChannelMask = nativeMeta.dwChannelMask;

            meta_return.numApproxFrames = nativeMeta.numApproxFrames;

            meta_return.title = nativeMeta.title;
            meta_return.artist = nativeMeta.artist;
            meta_return.album = nativeMeta.album;
            meta_return.composer = nativeMeta.composer;

            if (0 < nativeMeta.pictureBytes) {
                var picture = new byte[nativeMeta.pictureBytes];
                long pictureBytes = nativeMeta.pictureBytes;
                hr = NativeMethods.WWMFReaderGetCoverart(wszSourceFile, picture, ref pictureBytes);
                if (0 <= hr) {
                    // ゴミが最初についているので取る。
                    var picJpeg = TrimJpeg(picture);
                    if (0 < picJpeg.Length) {
                        picture = picJpeg;
                    } else {
                        var picPng = TrimPng(picture);
                        picture = picPng;
                        // JPEGでもPNGでもないときはここで画像サイズが0になる。
                    }
                    meta_return.picture = picture;
                }
            } else {
                meta_return.picture = new byte[0];
            }

            return hr;
        }

        public static int ReadHeaderAndData(
                string wszSourceFile,
                out Metadata meta_return,
                out byte[] data) {

            ReadHeader(wszSourceFile, out meta_return);

            long dataBytes = meta_return.CalcApproxDataBytes();
            data = new byte[dataBytes];

            int hr = NativeMethods.WWMFReaderReadData(
                    wszSourceFile, data, ref dataBytes);
            if (hr < 0) {
                return hr;
            }

            meta_return.numExactFrames = dataBytes / (meta_return.numChannels * meta_return.bitsPerSample / 8);

            return hr;
        }
    
        #region Native Stuff
        internal static class NativeMethods {
            public const int TEXT_STRSZ = 256;

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeMetadata {
                public int sampleRate;
                public int numChannels;
                public int bitsPerSample;
                public int bitRate;

                public int dwChannelMask;
                public int dummy0;

                /// おおよその値が戻る。
                public long numApproxFrames;
                public long numExactFrames;

                public long pictureBytes;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string title;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string artist;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string album;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string composer;
            };

#if vs2017
            [DllImport("WWMFReaderCpp2017.dll", CharSet = CharSet.Unicode)]
#else
            [DllImport("WWMFReader.dll", CharSet = CharSet.Unicode)]
#endif
            internal extern static int WWMFReaderReadHeader(
                string wszSourceFile,
                out NativeMetadata meta_return);

#if vs2017
            [DllImport("WWMFReaderCpp2017.dll", CharSet = CharSet.Unicode)]
#else
            [DllImport("WWMFReader.dll", CharSet = CharSet.Unicode)]
#endif
            internal extern static int WWMFReaderGetCoverart(
                    string wszSourceFile,
                    byte[] data_return,
                    ref long dataBytes_inout);

#if vs2017
            [DllImport("WWMFReaderCpp2017.dll", CharSet = CharSet.Unicode)]
#else
            [DllImport("WWMFReader.dll", CharSet = CharSet.Unicode)]
#endif
            internal extern static int WWMFReaderReadData(
                    string wszSourceFile,
                    byte[] data_return,
                    ref long dataBytes_inout);
        };
        #endregion
    };
};
