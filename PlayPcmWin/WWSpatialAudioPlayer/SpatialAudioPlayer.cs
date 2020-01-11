// 日本語。

using System;
using System.Collections.Generic;
using WWMFReaderCs;
using WWMFResamplerCs;
using WWSpatialAudioUserCs;
using WWUtil;

namespace WWSpatialAudioPlayer  {
    public class SpatialAudioPlayer : IDisposable {
        /// 1 to 60
        private const int RESAMPLE_QUALITY = 60;

        private WWSpatialAudioUser mSAudio = new WWSpatialAudioUser();
        public WWSpatialAudioUser SpatialAudio {  get { return mSAudio; } }

        private WWMFReader.Metadata mMetadata;
        public WWMFReader.Metadata Metadata {  get { return mMetadata; } }

        private LargeArray<byte> mPcmData;
        public LargeArray<byte> PcmData { get { return mPcmData; } }

        private List<LargeArray<float>> mResampledPcmByChannel = new List<LargeArray<float>>();

        public LargeArray<float> Pcm(int ch) { return mResampledPcmByChannel[ch]; }
        public int NumChannels { get { return mResampledPcmByChannel.Count; } }

        public int DwChannelMask {
            get { return mMetadata.dwChannelMask; }
        }

        public int AudioObjectTypeMask {
            get { return WWSpatialAudioUser.DwChannelMaskToAudioObjectTypeMask(DwChannelMask); }
        }

        public bool IsChannelSupported(int ch) {
            switch (ch) {
            case 1:
            case 2:
            case 4:
            case 6:
            case 8:
            case 12:
                return true;
            default:
                return false;
            }
        }

        private int GetDefaultDwChannelMask(int ch) {
            int mask = 0;
            switch (ch) {
            case 1:
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontCenter);
                break;
            case 2:
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.FrontRight);
                break;
            case 4:
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.FrontRight
                    | WWSpatialAudioUser.DwChannelMaskType.BackLeft
                    | WWSpatialAudioUser.DwChannelMaskType.BackRight);
                break;
            case 6:
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.FrontRight
                    | WWSpatialAudioUser.DwChannelMaskType.FrontCenter
                    | WWSpatialAudioUser.DwChannelMaskType.LowFrequency
                    | WWSpatialAudioUser.DwChannelMaskType.BackLeft
                    | WWSpatialAudioUser.DwChannelMaskType.BackRight);
                break;
            case 8:
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.FrontRight
                    | WWSpatialAudioUser.DwChannelMaskType.FrontCenter
                    | WWSpatialAudioUser.DwChannelMaskType.LowFrequency
                    | WWSpatialAudioUser.DwChannelMaskType.BackLeft
                    | WWSpatialAudioUser.DwChannelMaskType.BackRight
                    | WWSpatialAudioUser.DwChannelMaskType.SideLeft
                    | WWSpatialAudioUser.DwChannelMaskType.SideRight);
                break;
            case 12: //< 7.1.4ch
                mask = (int)(WWSpatialAudioUser.DwChannelMaskType.FrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.FrontRight
                    | WWSpatialAudioUser.DwChannelMaskType.FrontCenter
                    | WWSpatialAudioUser.DwChannelMaskType.LowFrequency
                    | WWSpatialAudioUser.DwChannelMaskType.BackLeft
                    | WWSpatialAudioUser.DwChannelMaskType.BackRight
                    | WWSpatialAudioUser.DwChannelMaskType.SideLeft
                    | WWSpatialAudioUser.DwChannelMaskType.SideRight
                    | WWSpatialAudioUser.DwChannelMaskType.TopFrontLeft
                    | WWSpatialAudioUser.DwChannelMaskType.TopFrontRight
                    | WWSpatialAudioUser.DwChannelMaskType.TopBackLeft
                    | WWSpatialAudioUser.DwChannelMaskType.TopBackRight);
                break;
            default:
                mask = 0;
                break;
            }

            return mask;
        }

        private WWSpatialAudioUser.AudioObjectType DwChannelMaskChannelGetAudioObjectTypeof(int ch) {
            // DwChannelMaskからAudioObjectTypeのリストを作成。
            int aoMask = AudioObjectTypeMask;
            var aoList = WWSpatialAudioUser.AudioObjectTypeMaskToList(aoMask);

            if (ch < 0 || aoList.Count <= ch) {
                // Dynamicにしても良い？
                return WWSpatialAudioUser.AudioObjectType.None;
            }

            return aoList[ch];
        }


        public int ReadAudioFile(string path) {
            int hr = WWMFReader.ReadHeaderAndData(path, out mMetadata, out mPcmData);
            if (hr < 0) {
                return hr;
            }

            // update DwChannelMask
            if (0 == mMetadata.dwChannelMask) {
                mMetadata.dwChannelMask = GetDefaultDwChannelMask(mMetadata.numChannels);
            }

            return hr;
        }

        public int PlaySampleRate {
            get { return 48000; }
        }

        /// <summary>
        /// サンプルフォーマットがmMetadataのmPcmDataを
        /// Spatial Audio用のフォーマットにリサンプルして
        /// mResampledPcmByChannelに入れる。
        /// </summary>
        public int Resample() {
            int hr = 0;

            // 48000Hz 32bit floatに変換。
            var fromFmt = new WWPcmFormat(WWPcmFormat.SampleFormat.SF_Int, mMetadata.numChannels,
                mMetadata.bitsPerSample, mMetadata.sampleRate, DwChannelMask,
                mMetadata.bitsPerSample);

            var toFmt = new WWPcmFormat(WWPcmFormat.SampleFormat.SF_Float, mMetadata.numChannels,
                32, PlaySampleRate, DwChannelMask,
                32);

            var toBufList = new List<byte[]>();

            using (var resampler = new WWMFResampler()) { 
                hr = resampler.Init(fromFmt, toFmt, RESAMPLE_QUALITY);
                if (hr < 0) {
                    return hr;
                }

                long inPos = 0;
                int inBytes = 1024 * 1024 * fromFmt.FrameBytes;
                var inBuf = new byte[inBytes];
                do
                {
                    // 少しずつ変換。
                    if (mPcmData.LongLength < inPos + inBytes) {
                        inBytes = (int)(mPcmData.LongLength - inPos);
                        inBuf = new byte[inBytes];
                    }

                    mPcmData.CopyTo(inPos, ref inBuf, 0, inBytes);
                    inPos += inBytes;

                    byte[] outBuf;
                    hr = resampler.Resample(inBuf, out outBuf);
                    if (hr < 0) {
                        return hr;
                    }

                    if (0 < outBuf.Length) {
                        toBufList.Add(outBuf);
                    }
                    outBuf = null;
                } while (inPos < mPcmData.LongLength);
                inBuf = null;

                {
                    byte[] outBuf;
                    hr = resampler.Drain(out outBuf);
                    if (hr < 0) {
                        return hr;
                    }

                    if (0 < outBuf.Length) {
                        toBufList.Add(outBuf);
                    }
                    outBuf = null;
                }
            }

            // 整列する。
            long totalBytes = 0;
            foreach (var item in toBufList) {
                totalBytes += item.Length;
            }

            var fullPcm = WWUtil.ListUtils<byte>.GetLargeArrayFragment(toBufList, 0, totalBytes);
            toBufList.Clear();
            toBufList = null;

            // fullPcmからチャンネルごとのfloatデータを取得し、
            // mResampledPcmByChannelに入れる。
            mResampledPcmByChannel.Clear();

            int sampleBytes = toFmt.bits / 8;
            int nCh = toFmt.nChannels;
            long numFrames = totalBytes / (sampleBytes * nCh);
            for (int ch = 0; ch < nCh; ++ch) {
                var buf = new LargeArray<float>(numFrames);
                mResampledPcmByChannel.Add(buf);
            }

            { 
                var buf4 = new byte[4];
                var f1 = new float[1];
                for (long b = 0; b < numFrames; ++b) {
                    for (int ch = 0; ch < nCh; ++ch) {
                        fullPcm.CopyTo(sampleBytes * (nCh * b + ch), ref buf4, 0, 4);
                        var bufCh = mResampledPcmByChannel[ch];
                        f1[0] = BitConverter.ToSingle(buf4, 0);
                        bufCh.CopyFrom(f1, 0, b, 1);
                    }
                }
            }
            return hr;
        }

        /// <summary>
        /// mResampledPcmByChannelのPCMデータをNativeバッファーに入れる。
        /// </summary>
        public int StoreSamplesToNativeBuffer() {
            if (mResampledPcmByChannel.Count == 0) {
                Console.WriteLine("Error: WWSpatialAudioPlayer::StoreSamplesToNativeBuffer() PCM sample not found\n");
                return -1;
            }

            int hr = 0;

            mSAudio.ClearAllPcm();
            for (int ch = 0; ch < mResampledPcmByChannel.Count; ++ch) {
                var from = mResampledPcmByChannel[ch];

                mSAudio.SetPcmBegin(ch, from.LongLength);

                int COPY_COUNT = 16 * 1024 * 1024;
                var buf = new float[COPY_COUNT];

                for (long pos=0; pos < from.LongLength; pos += COPY_COUNT) {
                    int copyCount = COPY_COUNT;
                    if (from.LongLength < pos + COPY_COUNT) {
                        copyCount = (int)(from.LongLength - pos);
                        buf = new float[copyCount];
                    }

                    from.CopyTo(pos, ref buf, 0, copyCount);
                    mSAudio.SetPcmFragment(ch, pos, buf);
                }

                // チャンネル番号ch のDwChannekMaskType
                var cmt = WWSpatialAudioUser.DwChannelMaskToList(DwChannelMask)[ch];
                var aot = WWSpatialAudioUser.DwChannelMaskTypeToAudioObjectType(cmt);
                if (aot == WWSpatialAudioUser.AudioObjectType.None)
                {
                    Console.WriteLine("D: channel {0} {1} does not map to audio object...", ch, cmt);
                } else { 
                    mSAudio.SetPcmEnd(ch, aot);
                }
            }

            return hr;
        }

        public int Start() {
            int hr = mSAudio.Start();
            return hr;
        }
        public int Stop() {
            int hr = mSAudio.Stop();
            return hr;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        public virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    mSAudio.Dispose();
                    mSAudio = null;
                }

                // free unmanaged resources here.

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code!
            Dispose(true);
        }
        #endregion
    }
}
