using Wasapi;

namespace PlayPcmWinAlbum {
    public class DeviceFormat {
        public int NumChannels { get; set; }
        public int SampleRate { get; set; }
        public WasapiCS.SampleFormatType SampleFormat { get; set; }
        public int DwChannelMask { get; set; }
        public void Set(int numChannels, int sampleRate, WasapiCS.SampleFormatType sampleFormat, int dwChannelMask) {
            NumChannels = numChannels;
            SampleRate = sampleRate;
            SampleFormat = sampleFormat;
            DwChannelMask = dwChannelMask;
        }

        /// <summary>
        /// 1フレーム(全チャンネル1サンプル)のバイト数。
        /// </summary>
        /// <returns></returns>
        public int BytesPerFrame() {
            return NumChannels * WasapiCS.SampleFormatTypeToUseBitsPerSample(SampleFormat) / 8;
        }

        /// <summary>
        /// 1チャンネル1サンプルを格納するために使用するビット数。
        /// </summary>
        public int UseBitsPerSample() {
            return WasapiCS.SampleFormatTypeToUseBitsPerSample(SampleFormat);
        }

        /// <summary>
        /// 有効なビットデプス (bit)
        /// </summary>
        public int ValidBitsPerSample() {
            return WasapiCS.SampleFormatTypeToValidBitsPerSample(SampleFormat);
        }
    };
}
