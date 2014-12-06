// 日本語UTF-8

using Wasapi;
using WasapiPcmUtil;

namespace PlayPcmWin {
    /// <summary>
    /// デバイスのセットアップ情報
    /// </summary>
    struct DeviceSetupParams {
        bool setuped;
        int samplingRate;
        WasapiCS.SampleFormatType sampleFormat;
        int latencyMillisec;
        int zeroFlushMillisec;
        WasapiDataFeedModeType dfm;
        WasapiSharedOrExclusiveType shareMode;
        RenderThreadTaskType threadTaskType;

        public int SampleRate { get { return samplingRate; } }
        public WasapiCS.SampleFormatType SampleFormat { get { return sampleFormat; } }
        public int NumChannels { get; set; }
        public int LatencyMillisec { get { return latencyMillisec; } }
        public int ZeroFlushMillisec { get { return zeroFlushMillisec; } }
        public WasapiDataFeedModeType DataFeedMode { get { return dfm; } }
        public WasapiSharedOrExclusiveType SharedOrExclusive { get { return shareMode; } }
        public RenderThreadTaskType ThreadTaskType { get { return threadTaskType; } }
        public int ResamplerConversionQuality { get; set; }
        public WasapiCS.StreamType StreamType { get; set; }

        /// <summary>
        /// 1フレーム(1サンプル全ch)のデータがメモリ上を占める領域(バイト)
        /// </summary>
        public int UseBytesPerFrame {
            get {
                return NumChannels * WasapiCS.SampleFormatTypeToUseBitsPerSample(sampleFormat) / 8;
            }
        }

        public bool Is(
                    int samplingRate,
                    WasapiCS.SampleFormatType fmt,
                    int numChannels,
                    int latencyMillisec,
                    int zeroFlushMillisec,
                    WasapiDataFeedModeType dfm,
                    WasapiSharedOrExclusiveType shareMode,
                    RenderThreadTaskType threadTaskType,
                    int resamplerConversionQuality,
                    WasapiCS.StreamType streamType) {
            return (this.setuped
                && this.samplingRate == samplingRate
                && this.sampleFormat == fmt
                && this.NumChannels == numChannels
                && this.latencyMillisec == latencyMillisec
                && this.zeroFlushMillisec == zeroFlushMillisec
                && this.dfm == dfm
                && this.shareMode == shareMode
                && this.threadTaskType == threadTaskType
                && this.ResamplerConversionQuality == resamplerConversionQuality
                && this.StreamType == streamType);
        }

        public bool CompatibleTo(
                    int samplingRate,
                    WasapiCS.SampleFormatType fmt,
                    int numChannels,
                    int latencyMillisec,
                    int zeroFlushMillisec,
                    WasapiDataFeedModeType dfm,
                    WasapiSharedOrExclusiveType shareMode,
                    RenderThreadTaskType threadTaskType,
                    int resamplerConversionQuality,
                    WasapiCS.StreamType streamType) {
            return (this.setuped
                && this.samplingRate == samplingRate
                && SampleFormatIsCompatible(this.sampleFormat, fmt)
                && this.NumChannels == numChannels
                && this.latencyMillisec == latencyMillisec
                && this.ZeroFlushMillisec == zeroFlushMillisec
                && this.dfm == dfm
                && this.shareMode == shareMode
                && this.threadTaskType == threadTaskType
                && this.ResamplerConversionQuality == resamplerConversionQuality
                && this.StreamType == streamType);
        }

        private static bool SampleFormatIsCompatible(
                WasapiCS.SampleFormatType lhs,
                WasapiCS.SampleFormatType rhs) {
            switch (lhs) {
            case WasapiCS.SampleFormatType.Sint24:
            case WasapiCS.SampleFormatType.Sint32V24:
                return rhs == WasapiCS.SampleFormatType.Sint24 ||
                    rhs == WasapiCS.SampleFormatType.Sint32V24;
            default:
                return lhs == rhs;
            }
        }

        public void Set(int samplingRate,
                WasapiCS.SampleFormatType fmt,
                int numChannels,
                int latencyMillisec,
                int zeroFlushMillisec,
                WasapiDataFeedModeType dfm,
                WasapiSharedOrExclusiveType shareMode,
                RenderThreadTaskType threadTaskType,
                int resamplerConversionQuality,
                WasapiCS.StreamType streamType) {
            this.setuped = true;
            this.samplingRate = samplingRate;
            this.sampleFormat = fmt;
            this.NumChannels = numChannels;
            this.latencyMillisec = latencyMillisec;
            this.zeroFlushMillisec = zeroFlushMillisec;
            this.dfm = dfm;
            this.shareMode = shareMode;
            this.threadTaskType = threadTaskType;
            this.ResamplerConversionQuality = resamplerConversionQuality;
            this.StreamType = streamType;
        }

        /// <summary>
        /// wasapi.Unsetup()された場合に呼ぶ。
        /// </summary>
        public void Unsetuped() {
            setuped = false;
        }

        /// <summary>
        /// Setup状態か？
        /// </summary>
        /// <returns>true: Setup状態。false: Setupされていない。</returns>
        public bool IsSetuped() {
            return setuped;
        }
    }
}
