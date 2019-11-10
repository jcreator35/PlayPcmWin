// 日本語。

using System;
using System.Runtime.InteropServices;

namespace WWMFResamplerCs {
    public class WWMFResampler : IDisposable {
        private int mInstanceId = -1;

        private WWPcmFormat mInFmt;
        private WWPcmFormat mOutFmt;

        public void Term() {
            if (mInstanceId < 0) {
                return;
            }

            NativeMethods.WWMFResamplerTerm(mInstanceId);
            mInstanceId = -1;
        }

        /// <summary>
        /// サンプルレート変換開始。
        /// </summary>
        /// <param name="inFmt">入力PCMデータのフォーマット</param>
        /// <param name="outFmt">出力PCMデータのフォーマット</param>
        /// <param name="halfFilterLength">変換品質。1(最低)～60(最高)</param>
        /// <returns>0のとき成功。HRESULT</returns>
        public int Init(WWPcmFormat inFmt, WWPcmFormat outFmt, int halfFilterLength) {
            mInFmt = inFmt;
            mOutFmt = outFmt;

            NativeMethods.NativePcmFormat inN;
            inN.sampleFormat = (int)inFmt.sampleFormat;
            inN.nChannels = inFmt.nChannels;
            inN.bits = inFmt.bits;
            inN.sampleRate = inFmt.sampleRate;
            inN.dwChannelMask = inFmt.dwChannelMask;
            inN.validBitsPerSample = inFmt.validBitsPerSample;

            NativeMethods.NativePcmFormat outN;
            outN.sampleFormat = (int)outFmt.sampleFormat;
            outN.nChannels = outFmt.nChannels;
            outN.bits = outFmt.bits;
            outN.sampleRate = outFmt.sampleRate;
            outN.dwChannelMask = outFmt.dwChannelMask;
            outN.validBitsPerSample = outFmt.validBitsPerSample;

            int hr = NativeMethods.WWMFResamplerInit(inN, outN, halfFilterLength);
            if (hr < 0) {
                mInstanceId = -1;
                return hr;
            }

            mInstanceId = hr;
            return 0;
        }

        /// <summary>
        /// PCMデータをリサンプルする。
        /// </summary>
        /// <param name="inPcm">入力PCMデータ</param>
        /// <param name="outPcm">出力PCMデータ</param>
        /// <returns>0のとき成功。HRESULT</returns>
        public int Resample(byte[] inPcm, out byte[] outPcm) {
            outPcm = new byte[0];

            if (mInstanceId < 0) {
                return -1;
            }

            if (inPcm.Length == 0) {
                return 0;
            }

            double convRatio = (double)mOutFmt.BitRate / mInFmt.BitRate;

            var tmp = new byte[(int)(inPcm.Length * convRatio + 256)];
            int outLength = tmp.Length;
            int hr = NativeMethods.WWMFResamplerResample(mInstanceId, inPcm, inPcm.Length, tmp, ref outLength);
            if (hr < 0) {
                return hr;
            }

            outPcm = new byte[outLength];
            Array.Copy(tmp, outPcm, outLength);

            return 0;
        }

        /// <summary>
        /// 最後のPCMデータをResample()した後に１回呼ぶ。
        /// パイプラインの中の入力データ待ち状態でペンディングになっていたPCMデータが押し出されて出てくる。
        /// </summary>
        /// <param name="outPcm">出力PCMデータ。</param>
        /// <returns>0のとき成功。HRESULT</returns>
        public int Drain(out byte[] outPcm) {
            outPcm = new byte[0];

            if (mInstanceId < 0) {
                return -1;
            }

            var tmp = new byte[4096];
            int outLength = tmp.Length;
            int hr = NativeMethods.WWMFResamplerDrain(mInstanceId, tmp, ref outLength);
            if (hr < 0) {
                return hr;
            }

            outPcm = new byte[outLength];
            Array.Copy(tmp, outPcm, outLength);

            return 0;
        }

        internal static class NativeMethods {
            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativePcmFormat {
                public int sampleFormat;       ///< WWMFBitFormatType of WWMFResampler.h
                public int nChannels;          ///< PCMデータのチャンネル数。
                public int bits;               ///< PCMデータ1サンプルあたりのビット数。パッド含む。
                public int sampleRate;         ///< 44100等。
                public int dwChannelMask;      ///< 2チャンネルステレオのとき3
                public int validBitsPerSample; ///< PCMの量子化ビット数。
            };

            [DllImport("WWMFResamplerCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWMFResamplerInit(
                NativePcmFormat inFmt,
                NativePcmFormat outFmt,
                int halfFilterLength);

            [DllImport("WWMFResamplerCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWMFResamplerResample(
                int instanceId,
                byte[] inPcm,
                int inBytes,
                byte[] outPcm,
                ref int outBytes_inout);

            [DllImport("WWMFResamplerCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWMFResamplerDrain(
                int instanceId,
                byte[] outPcm,
                ref int outBytes_inout);

            [DllImport("WWMFResamplerCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWMFResamplerTerm(
                int instanceId);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                // ここでunmanaged resourcesを消す。
                Term();
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
