using System.IO;
using System.Runtime.InteropServices;

namespace WWMFVideoReaderCs
{
    public class WWMFVideoReader
    {
        const int E_UNEXPECTED = -2147418113; // 0x8000FFFFL;

        internal static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct WWImageWH
            {
                public int w;
                public int h;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct WWImageXYWH
            {
                public int x;
                public int y;
                public int w;
                public int h;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct WWRational32
            {
                public int numer;
                public int denom;
            };

            const uint WW_MF_VIDEO_IMAGE_FMT_TopDown = 1;
            const uint WW_MF_VIDEO_IMAGE_FMT_CAN_SEEK = 2;
            const uint WW_MF_VIDEO_IMAGE_FMT_SLOW_SEEK = 4;
            const uint WW_MF_VIDEO_IMAGE_FMT_LIMITED_RANGE_16_to_235 = 8;

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct WWMFVideoFormat
            {
                public WWImageWH pixelWH; //< 若干大きめのサイズが入っている。aperture.whが表示画像のサイズ。
                public WWImageWH aspectStretchedWH;
                public WWImageXYWH aperture; //< Geometric Aperture。単位はpixel.
                public WWRational32 aspectRatio;
                public WWRational32 frameRate;
                public long duration;
                public long timeStamp;
                public uint flags; //< WW_MF_VIDEO_IMAGE_FMT_???
            };

            [DllImport("WWMFVideoReaderCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static
            int WWMFVReaderIFStaticInit();

            [DllImport("WWMFVideoReaderCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static
            void WWMFVReaderIFStaticTerm();

            /// インスタンスを作成し、1つのファイルを読む。
            /// @return instanceIdが戻る。
            /// @retval 負の値 読み出し時の失敗のHRESULT
            [DllImport("WWMFVideoReaderCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static
            int WWMFVReaderIFReadStart(string path);

            /// 作ったインスタンスを消す。
            /// @retval S_OK インスタンスが見つかって、削除成功。
            /// @retval E_INVALIDARG インスタンスがない。
            [DllImport("WWMFVideoReaderCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static
            int WWMFVReaderIFReadEnd(int instanceId);

            /// @param posToSeek シークする位置。負のときシークしないで次のフレームを取得。
            /// @param pImg_io 画像が入る領域。
            /// @param imgBytes_io 画像のバイト数が戻る。4 * vf.aperture.w * vf.aperture.h
            /// @param vf_return ビデオフォーマットが戻る。
            [DllImport("WWMFVideoReaderCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static
            int WWMFVReaderIFReadImage(
            int instanceId, long posToSeek, byte[] pImg_io,
            ref int imgBytes_io, ref WWMFVideoFormat vf_return);
        };

        private int mInstanceId = -1;

        /// <summary>
        /// プログラム起動後に1回呼ぶ。
        /// MFStartup()が中で呼び出される。
        /// </summary>
        public static int StaticInit()
        {
            return NativeMethods.WWMFVReaderIFStaticInit();
        }

        /// <summary>
        /// プログラム終了時に1回呼ぶ。
        /// MFShutdown()が中で呼び出される。
        /// </summary>
        public static void StaticTerm()
        {
            NativeMethods.WWMFVReaderIFStaticTerm();
        }

        public bool Reading {
            get { return 0 <= mInstanceId; }
        }

        /// <summary>
        /// ファイルを読み出し開始。
        /// </summary>
        /// <param name="path">ファイル名。</param>
        /// <returns>負のときエラーコード(HRESULT)。0以上のとき成功。</returns>
        public int ReadStart(string path)
        {
            if (0 <= mInstanceId) {
                // 既にファイルを開いて読んでいる。
                System.Diagnostics.Debug.Assert(false);
                return E_UNEXPECTED;
            }

            int hr = NativeMethods.WWMFVReaderIFReadStart(path);
            if (hr < 0) {
                return hr;
            }

            mInstanceId = hr;
            return hr;
        }

        /// <summary>
        /// ファイルを閉じる。
        /// </summary>
        /// <returns>負のときエラーコード(HRESULT)。0以上のとき成功。</returns>
        public int ReadEnd()
        {
            if (mInstanceId < 0) {
                // ファイルを開いていないのに終了処理が呼び出された。
                return 0;
            }

            int hr = NativeMethods.WWMFVReaderIFReadEnd(mInstanceId);

            mInstanceId = -1;

            return hr;
        }

        public class VideoImage
        {
            public int w;
            public int h;
            public long duration;
            public long timeStamp;
            public byte[] img;
        };

        private static void SaveImgToFile(VideoImage vi, string path)
        {
            using (var bw = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write))) {
                bw.Write(vi.img, 0, vi.img.Length);
            }
        }

        public int ReadImage(long posToSeek, out VideoImage vi)
        {
            NativeMethods.WWMFVideoFormat vf = new NativeMethods.WWMFVideoFormat();
            vi = new VideoImage();

            int imgBytes = 3840 * 2160 * 4;
            var img = new byte[imgBytes];

            int hr = NativeMethods.WWMFVReaderIFReadImage(mInstanceId, posToSeek, img, ref imgBytes, ref vf);
            if (0 <= hr) {
                vi.w = vf.aperture.w;
                vi.h = vf.aperture.h;
                vi.img = img;
                vi.duration = vf.duration;
                vi.timeStamp = vf.timeStamp;
            }

            //SaveImgToFile(vi, string.Format("{0}_{1}.data", mInstanceId, vi.timeStamp));

            return hr;
        }
    }
}
