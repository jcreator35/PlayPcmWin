// 日本語。

namespace WWMFResamplerCs {
    public class WWPcmFormat {
        public enum SampleFormat {
            SF_Int,
            SF_Float,
        };

        public WWPcmFormat(SampleFormat aSF, int aNChannels, int aBits, int aSampleRate, int aDwChannelMask, int aValidBitsPerSample) {
            sampleFormat = aSF;
            nChannels = aNChannels;
            bits = aBits;
            sampleRate = aSampleRate;
            dwChannelMask = aDwChannelMask;
            validBitsPerSample = aValidBitsPerSample;
        }

        public long BitRate {
            get { return (long)nChannels * bits * sampleRate; }
        }

        public int FrameBytes {
            get { return nChannels * bits / 8; }
        }

        public SampleFormat sampleFormat;       ///< WWMFBitFormatType of WWMFResampler.h
        public int nChannels;          ///< PCMデータのチャンネル数。
        public int bits;               ///< PCMデータ1サンプルあたりのビット数。パッド含む。
        public int sampleRate;         ///< 44100等。
        public int dwChannelMask;      ///< 2チャンネルステレオのとき3
        public int validBitsPerSample; ///< PCMの量子化ビット数。
    }
}
