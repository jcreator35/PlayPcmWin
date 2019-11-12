// 日本語。

using System;
using System.Collections.Generic;
using WWMFReaderCs;
using WWMFResamplerCs;
using WWSpatialAudioUserCs;

namespace WWSpatialAudioPlayer  {
    public class SpatialAudioPlayer : IDisposable {
        private WWSpatialAudioUser mSAudio = new WWSpatialAudioUser();
        private WWMFReader.Metadata mMetadata;
        private byte[] mPcmData;

        public WWSpatialAudioUser SpatialAudio {  get { return mSAudio; } }

        public WWMFReader.Metadata Metadata {  get { return mMetadata; } }

        public byte[] OriginalPcmData { get { return mPcmData; } }

        private List<byte[]> mPcmByChannel = new List<byte[]>();

        public int NumChannels {  get { return mPcmByChannel.Count; } }
        public byte[] Pcm(int ch) { return mPcmByChannel[ch]; }

        public int DwChannelMask {
            get { return mMetadata.dwChannelMask; }
        }

        private int GetDefaultDwChannelMask(int ch) {
            int mask = 0;
            switch (ch) {
            case 1:
                mask = 0;
                break;
            case 2:
                mask = 3;
                break;
            case 4:
                mask = 0x33;
                break;
            case 6:
                mask = 0x3f;
                break;
            case 8:
                mask = 0xff;
                break;
            case 12:
                mask = 0x2d63f; //< 7.1.4ch
                break;
            default:
                mask = 0;
                break;
            }

            return mask;
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

        public int Resample() {
            int hr = 0;

            // 48000Hz 32bit floatに変換。
            var fromFmt = new WWPcmFormat(WWPcmFormat.SampleFormat.SF_Int, mMetadata.numChannels,
                mMetadata.bitsPerSample, mMetadata.sampleRate, DwChannelMask,
                mMetadata.bitsPerSample);

            var toFmt = new WWPcmFormat(WWPcmFormat.SampleFormat.SF_Float, mMetadata.numChannels,
                32, 48000, DwChannelMask,
                32);

            var toBufList = new List<byte[]>();

            using (var resampler = new WWMFResampler()) { 
                hr = resampler.Init(fromFmt, toFmt, 60);
                if (hr < 0) {
                    return hr;
                }

                int inPos = 0;
                do {
                    // 少しずつ変換。
                    int inBytes = 256 * fromFmt.FrameBytes;
                    if (mPcmData.Length < inPos + inBytes) {
                        inBytes = mPcmData.Length - inPos;
                    }

                    var inBuf = new byte[inBytes];
                    Array.Copy(mPcmData, inPos, inBuf, 0, inBytes);
                    inPos += inBytes;

                    byte[] outBuf;
                    hr = resampler.Resample(inBuf, out outBuf);
                    if (hr < 0) {
                        return hr;
                    }

                    if (0 < outBuf.Length) {
                        toBufList.Add(outBuf);
                    }
                } while (inPos < mPcmData.Length);

                {
                    byte[] outBuf;
                    hr = resampler.Drain(out outBuf);
                    if (hr < 0) {
                        return hr;
                    }

                    if (0 < outBuf.Length) {
                        toBufList.Add(outBuf);
                    }
                }
            }

            // 整列する。
            int totalBytes = 0;
            foreach (var item in toBufList) {
                totalBytes += item.Length;
            }
            var toBuf = new byte[totalBytes];
            int toPos = 0;
            foreach (var item in toBufList) {
                Array.Copy(item, 0, toBuf, toPos, item.Length);
                toPos += item.Length;
            }

            mPcmByChannel = new List<byte[]>();

            int sampleBytes = toFmt.bits / 8;
            int nCh = toFmt.nChannels;
            int numFrames = totalBytes / (sampleBytes * nCh);
            for (int ch=0; ch<nCh; ++ch) {
                var buf = new byte[numFrames * sampleBytes];
                for (int i=0; i<numFrames; ++i) {
                    for (int b=0; b<sampleBytes; ++b) {
                        buf[i * sampleBytes + b] = toBuf[sampleBytes * ( nCh * i + ch) + b];
                    }
                }
                //Console.WriteLine("Rearranged {0}/{1}", ch+1, nCh);

                mPcmByChannel.Add(buf);
            }

            return hr;
        }

        public bool Play() {
            return true;
        }
        public bool Stop() {
            return true;
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
