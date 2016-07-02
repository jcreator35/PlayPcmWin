using System.Text;
using System.Runtime.InteropServices;
using System;

namespace Wasapi {
    public class WasapiCS {
        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_Init(ref int instanceIdReturn);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_Term(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_EnumerateDevices(int instanceId, int deviceType);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_GetDeviceCount(int instanceId);

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet=CharSet.Unicode)]
        internal struct WasapiIoDeviceAttributes {
            public int    deviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String deviceIdString;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_GetDeviceAttributes(int instanceId, int deviceId, out WasapiIoDeviceAttributes attr);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct MixFormatArgs {
            public int sampleRate;
            public int sampleFormat;    ///< WWPcmDataSampleFormatType
            public int numChannels;
            public int dwChannelMask;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_GetMixFormat(int instanceId, int deviceId, out MixFormatArgs mixFormat);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct InspectArgs {
            public int deviceType;      ///< DeviceType
            public int sampleRate;
            public int sampleFormat;    ///< WWPcmDataSampleFormatType
            public int numChannels;
            public int dwChannelMask;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_InspectDevice(int instanceId, int deviceId, ref InspectArgs args);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct SetupArgs {
            public int deviceType;
            public int streamType;
            public int sampleRate;
            public int sampleFormat;
            public int numChannels;

            public int dwChannelMask;
            public int shareMode;
            public int mmcssCall; ///< 0: disable, 1: enable, 2: do not call DwmEnableMMCSS()
            public int mmThreadPriority; ///< 0: None, 1: Low, 2: Normal, 3: High, 4: Critical
            public int schedulerTask;

            public int dataFeedMode;
            public int latencyMillisec;
            public int timePeriodHandledNanosec;
            public int zeroFlushMillisec;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_Setup(int instanceId, int deviceId, ref SetupArgs args);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_Unsetup(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_AddPlayPcmDataStart(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_AddPlayPcmData(int instanceId, int pcmId, byte[] data, long bytes);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_AddPlayPcmDataSetPcmFragment(int instanceId, int pcmId, long posBytes, byte[] data, long bytes);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_ResampleIfNeeded(int instanceId, int conversionQuality);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_AddPlayPcmDataEnd(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static double
        WasapiIO_ScanPcmMaxAbsAmplitude(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_ScalePcmAmplitude(int instanceId, double scale);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_ClearPlayList(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_RemovePlayPcmDataAt(int instanceId, int pcmId);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_SetPlayRepeat(int instanceId, bool repeat);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_ConnectPcmDataNext(int instanceId, int fromIdx, int toIdx);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_GetPcmDataId(int instanceId, int usageType);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_SetNowPlayingPcmDataId(int instanceId, int pcmId);

        [DllImport("WasapiIODLL.dll")]
        private extern static long
        WasapiIO_GetCaptureGlitchCount(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_StartPlayback(int instanceId, int wavDataId);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_StartRecording(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_Run(int instanceId, int millisec);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_Stop(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_Pause(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_Unpause(int instanceId);

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_SetPosFrame(int instanceId, long v);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct WasapiIoSessionStatus {
            public int streamType;
            public int pcmDataSampleRate;
            public int deviceSampleRate;
            public int deviceSampleFormat;
            public int deviceBytesPerFrame;
            public int deviceNumChannels;
            public int timePeriodHandledNanosec;
            public int bufferFrameNum;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_GetSessionStatus(int instanceId, out WasapiIoSessionStatus a);

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct WasapiIoCursorLocation {
            public long posFrame;
            public long totalFrameNum;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static bool
        WasapiIO_GetPlayCursorPosition(int instanceId, int usageType, out WasapiIoCursorLocation a);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate void StateChangedCallback(StringBuilder idStr);

        [DllImport("WasapiIODLL.dll")]
        private static extern void WasapiIO_RegisterStateChangedCallback(int instanceId, StateChangedCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void NativeCaptureCallback(IntPtr data, int bytes);

        public delegate void CaptureCallback(byte[] data);

        [DllImport("WasapiIODLL.dll")]
        private static extern void WasapiIO_RegisterCaptureCallback(int instanceId, NativeCaptureCallback callback);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct WasapiIoWorkerThreadSetupResult {
            public int dwmEnableMMCSSResult;
            public int avSetMmThreadCharacteristicsResult;
            public int avSetMmThreadPriorityResult;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_GetWorkerThreadSetupResult(int instanceId, out WasapiIoWorkerThreadSetupResult result);

        [DllImport("WasapiIODLL.dll", CharSet = CharSet.Unicode)]
        private extern static void
        WasapiIO_AppendAudioFilter(int instanceId, int audioFilterType, string args);

        [DllImport("WasapiIODLL.dll")]
        private extern static void
        WasapiIO_ClearAudioFilter(int instanceId);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct WasapiIoVolumeParams {
            public float levelMinDB;
            public float levelMaxDB;
            public float volumeIncrementDB;
            public float defaultLevel;
            /// ENDPOINT_HARDWARE_SUPPORT_VOLUME ==1
            /// ENDPOINT_HARDWARE_SUPPORT_MUTE   ==2
            /// ENDPOINT_HARDWARE_SUPPORT_METER  ==4
            public int hardwareSupport;
        };

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_GetVolumeParams(int instanceId, out WasapiIoVolumeParams args);

        [DllImport("WasapiIODLL.dll")]
        private extern static int
        WasapiIO_SetMasterVolumeInDb(int instanceId, float db);

        public enum MMCSSCallType {
            Disable,
            Enable,
            DoNotCall
        };

        public enum MMThreadPriorityType {
            None,
            Low,
            Normal,
            High,
            Critical
        };

        public enum SchedulerTaskType {
            None,
            Audio,
            ProAudio,
            Playback
        };

        public enum ShareMode {
            Shared,
            Exclusive
        };

        public enum DataFeedMode {
            EventDriven,
            TimerDriven,
        };

        public enum DeviceType {
            Play,
            Rec
        };

        /// <summary>
        /// enum項目はPcmData.ValueRepresentationTypeと同じ順番で並べる。
        /// WasapiPcmUtilのVrtToBftも参照。
        /// </summary>
        public enum BitFormatType {
            SInt,
            SFloat
        };

        public enum SampleFormatType {
            Unknown = -1,
            Sint16,
            Sint24,
            Sint32V24,
            Sint32,
            Sfloat,
            Sdouble, //< WASAPIはサポートしないが便宜上用意する
        };

        public enum StreamType {
            PCM,
            DoP,
        };

        /// <summary>
        /// WWAudioFilterType.hと同じ順番で並べる
        /// </summary>
        public enum WWAudioFilterType {
            PolarityInvert,
            Monaural,
            ChannelMapping,
            MuteChannel,
            SoloChannel,

            ZohNosdacCompensation,
            Delay
        };

        /// <summary>
        /// サンプルフォーマットタイプ→メモリ上に占めるビット数(1サンプル1chあたり)
        /// </summary>
        /// <param name="t">サンプルフォーマットタイプ</param>
        /// <returns>メモリ上に占めるビット数(1サンプル1chあたり)</returns>
        public static int SampleFormatTypeToUseBitsPerSample(SampleFormatType t) {
            switch (t) {
            case SampleFormatType.Sint16:
                return 16;
            case SampleFormatType.Sint24:
                return 24;
            case SampleFormatType.Sint32V24:
                return 32;
            case SampleFormatType.Sint32:
                return 32;
            case SampleFormatType.Sfloat:
                return 32;
            case SampleFormatType.Sdouble:
                return 64;
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }
        }

        public static SampleFormatType BitAndFormatToSampleFormatType(int bitsPerSample, int validBitsPerSample, BitFormatType bitFormat) {
            if (bitFormat == BitFormatType.SInt) {
                // int
                switch (bitsPerSample) {
                case 16:
                    return SampleFormatType.Sint16;
                case 24:
                    return SampleFormatType.Sint24;
                case 32:
                    switch (validBitsPerSample) {
                    case 24:
                        return SampleFormatType.Sint32V24;
                    case 32:
                        return SampleFormatType.Sint32;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        return SampleFormatType.Unknown;
                    }
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return SampleFormatType.Unknown;
                }
            } else {
                // float
                switch (bitsPerSample) {
                case 32:
                    return SampleFormatType.Sfloat;
                case 64:
                    return SampleFormatType.Sdouble;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return SampleFormatType.Unknown;
                }
            }
        }

        /// <summary>
        ///  サンプルフォーマットタイプ→有効ビット数(1サンプル1chあたり。バイト数ではなくビット数)
        /// </summary>
        /// <param name="t">サンプルフォーマットタイプ</param>
        /// <returns>有効ビット数(1サンプル1chあたり。バイト数ではなくビット数)</returns>
        public static int SampleFormatTypeToValidBitsPerSample(SampleFormatType t) {
            switch (t) {
            case SampleFormatType.Sint16:
                return 16;
            case SampleFormatType.Sint24:
                return 24;
            case SampleFormatType.Sint32V24:
                return 24;
            case SampleFormatType.Sint32:
                return 32;
            case SampleFormatType.Sfloat:
                return 32;
            case SampleFormatType.Sdouble:
                return 64;
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }
        }

        public static BitFormatType SampleFormatTypeToBitFormatType(SampleFormatType t) {
            switch (t) {
            case SampleFormatType.Sint16:
            case SampleFormatType.Sint24:
            case SampleFormatType.Sint32V24:
            case SampleFormatType.Sint32:
                return BitFormatType.SInt;
            case WasapiCS.SampleFormatType.Sfloat:
                return BitFormatType.SFloat;
            default:
                System.Diagnostics.Debug.Assert(false);
                return BitFormatType.SInt;
            }
        }

        private int mId =  -1;

        private NativeCaptureCallback mNativeCaptureCallback;
        private CaptureCallback mCaptureCallback;

        public int Init() {
            return WasapiIO_Init(ref mId);
        }

        public void Term() {
            WasapiIO_Term(mId);
            mNativeCaptureCallback = null;
            mCaptureCallback = null;
        }

        public void RegisterStateChangedCallback(StateChangedCallback callback) {
            WasapiIO_RegisterStateChangedCallback(mId, callback);
        }
        
        private void NativeCaptureCallbackImpl(IntPtr ptr, int bytes) {
            var data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
            mCaptureCallback(data);
        }

        public void RegisterCaptureCallback(CaptureCallback cb) {
            if (cb == null) {
                mNativeCaptureCallback = null;
                mCaptureCallback = null;
                WasapiIO_RegisterCaptureCallback(mId, null);
                return;
            }

            mNativeCaptureCallback = new NativeCaptureCallback(NativeCaptureCallbackImpl);
            mCaptureCallback = cb;
            WasapiIO_RegisterCaptureCallback(mId, mNativeCaptureCallback);
        }

        public int EnumerateDevices(DeviceType t) {
            return WasapiIO_EnumerateDevices(mId, (int)t);
        }

        public int GetDeviceCount() {
            return WasapiIO_GetDeviceCount(mId);
        }

        public class DeviceAttributes {
            /// <summary>
            /// device id. numbered from 0
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// device friendly name to display
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// device id string to identify
            /// </summary>
            public string DeviceIdString { get; set; }

            public DeviceAttributes(int id, string name, string deviceIdString) {
                Id = id;
                Name = name;
                DeviceIdString = deviceIdString;
            }
        };

        public DeviceAttributes GetDeviceAttributes(int deviceId) {
            var a = new WasapiIoDeviceAttributes();
            if (!WasapiIO_GetDeviceAttributes(mId, deviceId, out a)) {
                return null;
            }
            return new DeviceAttributes(a.deviceId, a.name, a.deviceIdString);
        }

        public class MixFormat {
            public int sampleRate;
            public SampleFormatType sampleFormat;    ///< WWPcmDataSampleFormatType
            public int numChannels;
            public int dwChannelMask;
            public MixFormat(int sampleRate, SampleFormatType sampleFormat, int numChannels, int dwChannelMask) {
                this.sampleRate = sampleRate;
                this.sampleFormat = sampleFormat;
                this.numChannels = numChannels;
                this.dwChannelMask = dwChannelMask;
            }
        };

        public MixFormat GetMixFormat(int deviceId) {
            MixFormatArgs args;
            if (WasapiIO_GetMixFormat(mId, deviceId, out args) < 0) {
                return null;
            }
            return new MixFormat(args.sampleRate, (SampleFormatType)args.sampleFormat, args.numChannels, args.dwChannelMask);
        }

        public int InspectDevice(int deviceId, DeviceType dt, int sampleRate, SampleFormatType format, int numChannels, int dwChannelMask) {
            var args = new InspectArgs();
            args.deviceType = (int)dt;
            args.sampleRate = sampleRate;
            args.numChannels = numChannels;
            args.sampleFormat = (int)format;
            args.dwChannelMask = dwChannelMask;
            return WasapiIO_InspectDevice(mId, deviceId, ref args);
        }

        public int Setup(int deviceId, DeviceType t, StreamType streamType,
                int sampleRate, SampleFormatType format, int numChannels,
                int dwChannelMask,
                MMCSSCallType mmcssCall, MMThreadPriorityType threadPriority,
                SchedulerTaskType schedulerTask, ShareMode shareMode, DataFeedMode dataFeedMode,
                int latencyMillisec, int zeroFlushMillisec, int timePeriodHandredNanosec) {
            var args = new SetupArgs();
            args.deviceType = (int)t;
            args.streamType = (int)streamType;
            args.sampleRate = sampleRate;
            args.sampleFormat = (int)format;
            args.numChannels = numChannels;
            args.dwChannelMask = dwChannelMask;
            args.mmcssCall = (int)mmcssCall;
            args.mmThreadPriority = (int)threadPriority;
            args.schedulerTask = (int)schedulerTask;
            args.shareMode = (int)shareMode;
            args.dataFeedMode = (int)dataFeedMode;
            args.latencyMillisec = latencyMillisec;
            args.timePeriodHandledNanosec = timePeriodHandredNanosec;
            args.zeroFlushMillisec = zeroFlushMillisec;
            return WasapiIO_Setup(mId, deviceId, ref args);
        }

        public void Unsetup() {
            WasapiIO_Unsetup(mId);
        }

        public bool AddPlayPcmDataStart() {
            return WasapiIO_AddPlayPcmDataStart(mId);
        }

        public bool AddPlayPcmData(int pcmId, byte[] data) {
            return WasapiIO_AddPlayPcmData(mId, pcmId, data, data.LongLength);
        }

        public bool AddPlayPcmDataAllocateMemory(int pcmId, long bytes) {
            return WasapiIO_AddPlayPcmData(mId, pcmId, null, bytes);
        }

        public bool AddPlayPcmDataSetPcmFragment(int pcmId, long posBytes, byte[] data) {
            return WasapiIO_AddPlayPcmDataSetPcmFragment(mId, pcmId, posBytes, data, data.Length);
        }

        /// <summary>
        /// perform resample on shared mode. blocking call.
        /// </summary>
        /// <param name="conversionQuality">1(minimum quality) to 60(maximum quality)</param>
        /// <returns>HRESULT</returns>
        public int ResampleIfNeeded(int conversionQuality) {
            return WasapiIO_ResampleIfNeeded(mId, conversionQuality);
        }

        public double ScanPcmMaxAbsAmplitude() {
            return WasapiIO_ScanPcmMaxAbsAmplitude(mId);
        }

        public void ScalePcmAmplitude(double scale) {
            WasapiIO_ScalePcmAmplitude(mId, scale);
        }

        public bool AddPlayPcmDataEnd() {
            return WasapiIO_AddPlayPcmDataEnd(mId);
        }

        public void RemovePlayPcmDataAt(int pcmId) {
            WasapiIO_RemovePlayPcmDataAt(mId, pcmId);
        }

        public void ClearPlayList() {
            WasapiIO_ClearPlayList(mId);
        }

        public void SetPlayRepeat(bool repeat) {
            WasapiIO_SetPlayRepeat(mId, repeat);
        }

        public bool ConnectPcmDataNext(int fromPcmId, int toPcmId) {
            return WasapiIO_ConnectPcmDataNext(mId, fromPcmId, toPcmId);
        }

        public enum PcmDataUsageType {
            NowPlaying,
            PauseResumeToPlay,
            SpliceNext,
            Capture,
            Splice,
        };

        public int GetPcmDataId(PcmDataUsageType t) {
            return WasapiIO_GetPcmDataId(mId, (int)t);
        }

        /// <summary>
        /// 再生中の曲変更。
        /// idのグループが読み込まれている必要がある。
        /// 再生中に呼ぶ必要がある。再生中でない場合、空振りする。
        /// 
        /// </summary>
        /// <param name="id">曲番号。id==-1を指定すると再生終了時無音に曲変更する(その後再生するものが無くなって再生停止する)。</param>
        public void UpdatePlayPcmDataById(int pcmId) {
            WasapiIO_SetNowPlayingPcmDataId(mId, pcmId);
        }

        public long GetCaptureGlitchCount() {
            return WasapiIO_GetCaptureGlitchCount(mId);
        }

        public int StartPlayback(int wavDataId) {
            return WasapiIO_StartPlayback(mId, wavDataId);
        }

        public int StartRecording() {
            return WasapiIO_StartRecording(mId);
        }

        public bool Run(int millisec) {
            return WasapiIO_Run(mId, millisec);
        }

        public void Stop() {
            WasapiIO_Stop(mId);
        }

        public int Pause() {
            return WasapiIO_Pause(mId);
        }

        public int Unpause() {
            return WasapiIO_Unpause(mId);
        }

        public bool SetPosFrame(long v) {
            return WasapiIO_SetPosFrame(mId, v);
        }

        public class SessionStatus {
            public StreamType StreamType { get; set; }
            public int PcmDataSampleRate { get; set; }
            public int DeviceSampleRate { get; set; }
            public SampleFormatType DeviceSampleFormat { get; set; }
            public int DeviceBytesPerFrame { get; set; }
            public int DeviceNumChannels { get; set; }
            public int TimePeriodHandledNanosec { get; set; }
            public int EndpointBufferFrameNum { get; set; }

            public SessionStatus(StreamType streamType, int pcmDataSampleRate, int deviceSampleRate, SampleFormatType deviceSampleFormat,
                    int deviceBytesPerFrame, int deviceNumChannels, int timePeriodHandledNanosec, int bufferFrameNum) {
                StreamType = streamType;
                PcmDataSampleRate = pcmDataSampleRate;
                DeviceSampleRate = deviceSampleRate;
                DeviceSampleFormat = deviceSampleFormat;
                DeviceBytesPerFrame = deviceBytesPerFrame;
                DeviceNumChannels = deviceNumChannels;
                TimePeriodHandledNanosec = timePeriodHandledNanosec;
                EndpointBufferFrameNum = bufferFrameNum;
            }
        };

        public SessionStatus GetSessionStatus() {
            var s = new WasapiIoSessionStatus();
            if (!WasapiIO_GetSessionStatus(mId, out s)) {
                return null;
            }
            return new SessionStatus((StreamType)s.streamType, s.pcmDataSampleRate, s.deviceSampleRate, (SampleFormatType)s.deviceSampleFormat,
                    s.deviceBytesPerFrame, s.deviceNumChannels, s.timePeriodHandledNanosec, s.bufferFrameNum);
        }

        public class CursorLocation {
            public long PosFrame { get; set; }
            public long TotalFrameNum { get; set; }
            public CursorLocation(long posFrame, long totalFrameNum) {
                PosFrame = posFrame;
                TotalFrameNum = totalFrameNum;
            }
        };

        public CursorLocation GetPlayCursorPosition(PcmDataUsageType usageType) {
            var p = new WasapiIoCursorLocation();
            if (!WasapiIO_GetPlayCursorPosition(mId, (int)usageType, out p)) {
                return null;
            }
            return new CursorLocation(p.posFrame, p.totalFrameNum);
        }

        public class WorkerThreadSetupResult {
            public int DwmEnableMMCSSResult { get; set; }
            public bool AvSetMmThreadCharacteristicsResult { get; set; }
            public bool AvSetMmThreadPriorityResult { get; set; }
            public WorkerThreadSetupResult(int dwm, bool av, bool tp) {
                DwmEnableMMCSSResult = dwm;
                AvSetMmThreadCharacteristicsResult = av;
                AvSetMmThreadPriorityResult = tp;
            }
        }

        public WorkerThreadSetupResult GetWorkerThreadSetupResult() {
            var p = new WasapiIoWorkerThreadSetupResult();
            WasapiIO_GetWorkerThreadSetupResult(mId, out p);
            return new WorkerThreadSetupResult(p.dwmEnableMMCSSResult, p.avSetMmThreadCharacteristicsResult!=0,
                p.avSetMmThreadPriorityResult!=0);
        }

        public void ClearAudioFilter() {
            WasapiIO_ClearAudioFilter(mId);
        }

        public void AppendAudioFilter(WWAudioFilterType aft, string args) {
            WasapiIO_AppendAudioFilter(mId, (int)aft, args);
        }

        public int SetMasterVolumeInDb(float db) {
            return WasapiIO_SetMasterVolumeInDb(mId, db);
        }

        public class VolumeParams {
            public float levelMinDB;
            public float levelMaxDB;
            public float volumeIncrementDB;
            public float defaultLevel;
            /// ENDPOINT_HARDWARE_SUPPORT_VOLUME ==1
            /// ENDPOINT_HARDWARE_SUPPORT_MUTE   ==2
            /// ENDPOINT_HARDWARE_SUPPORT_METER  ==4
            public int hardwareSupport;
            public VolumeParams(float min, float max, float increment, float aDefault, int hs) {
                levelMinDB = min;
                levelMaxDB = max;
                volumeIncrementDB = increment;
                defaultLevel = aDefault;
                hardwareSupport = hs;
            }
        };

        public int GetVolumeParams(out VolumeParams volumeParams) {
            WasapiIoVolumeParams vp = new WasapiIoVolumeParams();
            int hr = WasapiIO_GetVolumeParams(mId, out vp);
            volumeParams = new VolumeParams(vp.levelMinDB, vp.levelMaxDB, vp.volumeIncrementDB, vp.defaultLevel, vp.hardwareSupport);
            return hr;
        }
    }
}
