using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WWShowAudioStatus
{
    class WWShowAudioStatusCs : IDisposable {
        private int mInstanceId = -1;

        public WWShowAudioStatusCs() {
            mInstanceId = NativeMethods.WWSASInit();
            if (mInstanceId < 0) {
                throw new IndexOutOfRangeException();
            }
        }

        public enum WWDataFlow {
            Render,
            Capture,
            All,
        };

        public int CreateDeviceList(WWDataFlow df) {
            return NativeMethods.WWSASCreateDeviceList(mInstanceId, (int)df);
        }

        public int DestroyDeviceList() {
            return NativeMethods.WWSASDestroyDeviceList(mInstanceId);
        }

        public int GetDeviceCount() {
            return NativeMethods.WWSASGetDeviceCount(mInstanceId);
        }

        public class DeviceParams {
            public int id;
            public bool defaultDevice;
            public string name;
        }

        public DeviceParams GetDeviceParams(int idx) {
            var nadp = new NativeMethods.NativeAudioDeviceParams();
            int hr = NativeMethods.WWSASGetDeviceParams(mInstanceId, idx, ref nadp);
            if (hr < 0) {
                throw new IndexOutOfRangeException();
            }

            var adp = new DeviceParams();
            adp.id = nadp.id;
            adp.defaultDevice = nadp.isDefaultDevice != 0;
            adp.name = nadp.name;

            return adp;
        }

        public class MixFormat {
            public int samplerate;
            public int numChannels;
            public int dwChannelMask;
            public bool offloadCapable;
            public long hnsDevicePeriod;
            public long hnsMinDevicePeriod;
            public long hnsEventMin;
            public long hnsEventMax;
            public long hnsTimerMin;
            public long hnsTimerMax;
        }

        public MixFormat GetMixFormat(int idx) {
            var nmf = new NativeMethods.NativeMixFormat();
            NativeMethods.WWSASGetMixFormat(mInstanceId, idx, ref nmf);

            var r = new MixFormat();
            r.samplerate = nmf.sampleRate;
            r.numChannels = nmf.numChannels;
            r.dwChannelMask = nmf.dwChannelMask;
            r.offloadCapable = nmf.offloadCapable !=0;
            r.hnsDevicePeriod = nmf.hnsDevicePeriod;
            r.hnsMinDevicePeriod = nmf.hnsMinDevicePeriod;
            r.hnsEventMin = nmf.hnsEventMin;
            r.hnsEventMax = nmf.hnsEventMax;
            r.hnsTimerMin = nmf.hnsTimerMin;
            r.hnsTimerMax = nmf.hnsTimerMax;
            return r;
        }

        public class SpatialAudioParams {
            public int maxDynamicObjectCount;
            public int virtualSpeakerMask;
            public int sampleRate;
            public int maxFrameCount;
        }

        public SpatialAudioParams GetSpatialAudioParams(int idx) {
            var n = new NativeMethods.NativeSpatialAudioParams();
            NativeMethods.WWSASGetSpatialAudioParams(mInstanceId, idx, ref n);

            var r = new SpatialAudioParams();
            r.maxDynamicObjectCount = n.maxDynamicObjectCount;
            r.virtualSpeakerMask = n.virtualSpeakerMask;
            r.sampleRate = n.sampleRate;
            r.maxFrameCount = n.maxFrameCount;
            return r;
        }

        /// <summary>
        /// デバイスが消えたとかのイベント。
        /// </summary>
        /// <param name="idStr">デバイスのID。</param>
        /// <param name="dwNewState">WasapiDeviceState型の値のOR</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate void StateChangedCallback(StringBuilder idStr, int dwNewState);


        internal static class NativeMethods {
            public const int TEXT_STRSZ = 256;

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeAudioDeviceParams {
                public int id;

                public int isDefaultDevice;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string name;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativePcmFormat {
                public int sampleFormat;       ///< WWMFBitFormatType of WWMFResampler.h
                public int nChannels;          ///< PCMデータのチャンネル数。
                public int bits;               ///< PCMデータ1サンプルあたりのビット数。パッド含む。
                public int sampleRate;         ///< 44100等。
                public int dwChannelMask;      ///< 2チャンネルステレオのとき3
                public int validBitsPerSample; ///< PCMの量子化ビット数。
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeMixFormat {
                public int sampleRate;
                public int numChannels;
                public int dwChannelMask;
                public int offloadCapable;
                public long hnsDevicePeriod;
                public long hnsMinDevicePeriod;
                public long hnsEventMin;
                public long hnsEventMax;
                public long hnsTimerMin;
                public long hnsTimerMax;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeSpatialAudioParams {
                public int maxDynamicObjectCount;
                public int virtualSpeakerMask;
                public int sampleRate;
                public int maxFrameCount;
            };


            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASInit();

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASTerm(
                int instanceId);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASRegisterStateChangedCallback(int instanceId, StateChangedCallback callback);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASCreateDeviceList(int instanceId, int dataFlow);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASDestroyDeviceList(int instanceId);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetDeviceCount(
                int instanceId);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetDeviceParams(
                int instanceId,
                int idx,
                ref NativeAudioDeviceParams adp);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetMixFormat(
                int instanceId,
                int idx,
                ref NativeMixFormat nativesaf_return);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetSpatialAudioParams(
                int instanceId,
                int idx,
                ref NativeSpatialAudioParams nativesap_return);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeDeviceNodeIF {
                public ulong self;
                public ulong parent;
                public int type;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASCreateDeviceNodeList(
                int instanceId,
                int idx);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetDeviceNodeNum(
                int instanceId);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetDeviceNodeNth(
                int instanceId,
                int idx,
                ref NativeDeviceNodeIF dn_return);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASClearDeviceNodeList(
                int instanceId);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWAudioMuteIF {
                public int bEnabled;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetAudioMuteParams(
                int instanceId,
                int idx,
                ref WWAudioMuteIF param_return);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWAudioVolumeLevelIF {
                public int nChannels;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
                public float [] volumeLevels;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetAudioVolumeLevelParams(
                int instanceId,
                int idx,
                ref WWAudioVolumeLevelIF param_return);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWKsJackDescriptionIF {
                public int ChannelMapping;
                public int Color;
                public int ConnectionType;
                public int GeoLocation;
                public int GenLocation;
                public int PortConnection;
                public int IsConnected;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWKsJackDescriptionsIF {
                public int nChannels;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
                public WWKsJackDescriptionIF [] desc;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetKsJackDescriptionsParams(
                int instanceId,
                int idx,
                ref WWKsJackDescriptionsIF param_return);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWAudioInputSelectorIF {
                public int id;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetAudioInputSelectorParams(
                int instanceId,
                int idx,
                ref WWAudioInputSelectorIF param_return);


            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativePartIF {
                public int partType;
                public uint localId;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string name;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string gid;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetPartParams(
                int instanceId,
                int idx,
                ref NativePartIF param_return);

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeControlInterfaceIF {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string name;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public string iid;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetControlInterfaceParams(
                int instanceId,
                int idx,
                ref NativeControlInterfaceIF param_return);


            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct NativeKsFormat {
                public int sampleRate;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
                public int [] ch;
                public int containerBitsPerSample;
                public int validBitsPerSample;
                public int bFloat;
            };

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetKsFormatSupportedFmtNum(
                int instanceId,
                int idx);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetKsFormatpreferredFmt(
                int instanceId,
                int idx,
                ref NativeKsFormat param_return);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetKsFormatSupportedFmtNth(
                int instanceId,
                int idx,
                int nth,
                ref NativeKsFormat param_return);

            [DllImport("WWShowAudioStatusCpp.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSASGetAudioChannelConfig(
                int instanceId,
                int idx);

        }

        public int CreateDeviceNodeList(int deviceId) {
            return NativeMethods.WWSASCreateDeviceNodeList(mInstanceId, deviceId);
        }

        public int GetDeviceNodeNum() {
            return NativeMethods.WWSASGetDeviceNodeNum(mInstanceId);
        }

        public enum DeviceNodeType {
            IDeviceTopology,
            IConnector,
            IPart,
            ISubunit,
            IAudioMute,

            IAudioVolumeLevel,
            IAudioPeakMeter,
            IAudioAutoGainControl,
            IAudioBass,
            IAudioChannelConfig,

            IAudioInputSelector,
            IAudioLoudness,
            IAudioMidrange,
            IAudioOutputSelector,
            IAudioTreble,

            IKsJackDescription,
            IKsFormatSupport,
            IControlInterface,
            T_Pointer
        };

        public struct DeviceNode {
            public ulong self;
            public ulong parent;
            public DeviceNodeType type;
        }

        public DeviceNode GetDeviceNodeNth(int nth) {
            var ndn = new NativeMethods.NativeDeviceNodeIF();
            int hr = NativeMethods.WWSASGetDeviceNodeNth(mInstanceId, nth, ref ndn);
            if (hr < 0) {
                throw new ArgumentException();
            }

            var dn = new DeviceNode();
            dn.self = ndn.self;
            dn.parent = ndn.parent;
            dn.type = (DeviceNodeType)ndn.type;
            return dn;
        }

        public int ClearDeviceNodeList() {
            return NativeMethods.WWSASClearDeviceNodeList(mInstanceId);
        }

        public struct AudioMuteParams {
            public bool bEnabled;
        }

        public AudioMuteParams GetAudioMuteParams(int nth) {
            var np = new NativeMethods.WWAudioMuteIF();
            int hr = NativeMethods.WWSASGetAudioMuteParams(mInstanceId, nth, ref np);
            var p = new AudioMuteParams();
            p.bEnabled = np.bEnabled != 0;
            return p;
        }

        public struct AudioVolumeLevelParams {
            public float[] volumeLevels;
        }

        public AudioVolumeLevelParams GetAudioVolumeLevelParams(int nth) {
            var np = new NativeMethods.WWAudioVolumeLevelIF();
            int hr = NativeMethods.WWSASGetAudioVolumeLevelParams(mInstanceId, nth, ref np);
            var p = new AudioVolumeLevelParams();
            p.volumeLevels = new float[np.nChannels];
            for (int i=0; i<p.volumeLevels.Length;++i) {
                p.volumeLevels[i] = np.volumeLevels[i];
            }
            return p;
        }

        public enum ConnectionType {
            Unknown_Type,
            _3point5mmJack,
            Quarter,
            AtapiInternal,
            RCA,
            Optical,
            OtherDigital,
            OtherAnalog,
            MultichannelAnalogDIN,
            XlrProfessional,
            RJ11Modem,
            Combination
        };

        public enum ConnectorLocationType {
            Rear = 0x1,
            Front,
            Left,
            Right,
            Top,
            Bottom,
            RearPanel,
            Riser,
            InsideMobileLid,
            Drivebay,
            HDMI,
            OutsideMobileLid,
            ATAPI,
            NotApplicable,
            Reserved6,
        };

        public enum GeneralLocationType {
            PrimaryBox = 0,
            Internal,
            Separate,
            Other,
        };

        public enum PortConnectionType {
            Jack = 0,
            IntegratedDevice,
            BothIntegratedAndJack,
            Unknown
        };

        public struct KsJackDescription {
            public uint ChannelMapping;
            public uint Color;
            public ConnectionType ConnectionType;
            public ConnectorLocationType GeoLocation;
            public GeneralLocationType GenLocation;
            public PortConnectionType PortConnection;
            public bool IsConnected;
        }

        public struct KsJackDescriptionsParams {
            public KsJackDescription[] descs;
        }

        public KsJackDescriptionsParams GetKsJackDescriptionParams(int idx) {
            var np = new NativeMethods.WWKsJackDescriptionsIF();
            int hr = NativeMethods.WWSASGetKsJackDescriptionsParams(mInstanceId, idx, ref np);
            var p = new KsJackDescriptionsParams();
            p.descs = new KsJackDescription[np.nChannels];
            for (int i = 0; i < p.descs.Length; ++i) {
                var d = new KsJackDescription();
                var f = np.desc[i];
                d.ChannelMapping = (uint)f.ChannelMapping;
                d.Color = (uint)f.Color;
                d.ConnectionType = (ConnectionType)f.ConnectionType;
                d.GeoLocation = (ConnectorLocationType)f.GeoLocation;
                d.GenLocation = (GeneralLocationType)f.GenLocation;
                d.PortConnection = (PortConnectionType)f.PortConnection;
                d.IsConnected = f.IsConnected != 0;
                p.descs[i] = d;
            }
            return p;
        }

        public struct AudioInputSelectorParams {
            public int id;
        }

        public AudioInputSelectorParams GetAudioInputSelectorParams(int idx) {
            var np = new NativeMethods.WWAudioInputSelectorIF();
            int hr = NativeMethods.WWSASGetAudioInputSelectorParams(mInstanceId, idx, ref np);
            var p = new AudioInputSelectorParams();
            p.id = np.id;
            return p;
        }

        public enum PartType {
            Connector,
            Subunit,
        }

        public struct WWPart {
            public PartType partType;
            public uint localId;
            public string name;
            public string gid;
        };


        public WWPart GetPartParams(int idx) {
            var np = new NativeMethods.NativePartIF();
            int hr = NativeMethods.WWSASGetPartParams(mInstanceId, idx, ref np);
            var p = new WWPart();
            p.partType = (PartType)np.partType;
            p.name = np.name;
            p.gid = np.gid;
            p.localId = np.localId;
            return p;
        }

        public struct WWControlInterface {
            public string name;
            public string iid;
        };


        public WWControlInterface GetControlInterfaceParams(int idx) {
            var np = new NativeMethods.NativeControlInterfaceIF();
            int hr = NativeMethods.WWSASGetControlInterfaceParams(mInstanceId, idx, ref np);
            var p = new WWControlInterface();
            p.name = np.name;
            p.iid = np.iid;
            return p;
        }

        public struct WWKsFormat {
            public int sampleRate;
            public List<int> numChannels;
            public int containerBitsPerSample;
            public int validBitsPerSample;
            public bool bFloat;
        };

        private int [] mChIdxToNumChannels = new int [] {
            1, 2, 4, 6, 8,
        };

        public WWKsFormat GetKsFormatpreferredFmt(int idx) {
            var np = new NativeMethods.NativeKsFormat();
            int hr = NativeMethods.WWSASGetKsFormatpreferredFmt(mInstanceId, idx, ref np);
            var p = new WWKsFormat();
            p.bFloat = np.bFloat != 0;
            p.containerBitsPerSample = np.containerBitsPerSample;
            p.numChannels = new List<int>();
            for (int i = 0; i < mChIdxToNumChannels.Length; ++i) {
                if (np.ch[i] != 0) {
                    int ch = mChIdxToNumChannels[i];
                    p.numChannels.Add(ch);
                }
            }
            p.sampleRate = np.sampleRate;
            p.validBitsPerSample = np.validBitsPerSample;
            return p;
        }
        public WWKsFormat GetKsFormatSupportedFmt(int idx, int nth) {
            var np = new NativeMethods.NativeKsFormat();
            int hr = NativeMethods.WWSASGetKsFormatSupportedFmtNth(mInstanceId, idx, nth, ref np);
            var p = new WWKsFormat();
            p.bFloat = np.bFloat != 0;
            p.containerBitsPerSample = np.containerBitsPerSample;
            p.numChannels = new List<int>();
            for (int i = 0; i < mChIdxToNumChannels.Length; ++i) {
                if (np.ch[i] != 0) {
                    int ch = mChIdxToNumChannels[i];
                    p.numChannels.Add(ch);
                }
            }
            p.sampleRate = np.sampleRate;
            p.validBitsPerSample = np.validBitsPerSample;
            return p;
        }

        public int GetKsFormatSupportedFmtNum(int idx) {
            return NativeMethods.WWSASGetKsFormatSupportedFmtNum(mInstanceId, idx);
        }

        public int GetAudioChannelConfig(int idx) {
            return NativeMethods.WWSASGetAudioChannelConfig(mInstanceId, idx);
        }

        public void RegisterStateChangedCallback(StateChangedCallback callback) {
            NativeMethods. WWSASRegisterStateChangedCallback(mInstanceId, callback);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                if (0 <= mInstanceId) {
                    NativeMethods.WWSASTerm(mInstanceId);
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
