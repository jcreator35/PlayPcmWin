using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WWSpatialAudioUserCs {
    public class WWSpatialAudioUser : IDisposable {
        private int mInstanceId = -1;

        public enum StateEnum {
            NoAudioDevice,
            SpatialAudioIsNotEnabled,
            Initialized,
            Ready,
        }

        private StateEnum mState = StateEnum.NoAudioDevice;
        public StateEnum State { get { return mState; } }

        /// <summary>
        /// AudioObjectType of SpatialAudioClient.h
        /// </summary>
        public enum AudioObjectType {
            None = 0,
            Dynamic = 1,
            FrontLeft = 2,
            FrontRight = 4,
            FrontCenter = 8,

            LowFrequency = 0x10,
            SideLeft = 0x20,
            SideRight = 0x40,
            BackLeft = 0x80,
            BackRight = 0x100,

            TopFrontLeft = 0x200,
            TopFrontRight = 0x400,
            TopBackLeft = 0x800,
            TopBackRight = 0x1000,
            BottomFrontLeft = 0x2000,

            BottomFrontRight = 0x4000,
            BottomBackLeft = 0x8000,
            BottomBackRight = 0x10000,
            BackCenter = 0x20000
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ksmedia/ns-ksmedia-waveformatextensible
        /// </summary>
        public enum DwChannelMaskType {
            FrontLeft = 1,
            FrontRight = 2,
            FrontCenter = 4,
            LowFrequency = 8,
            BackLeft = 0x10,

            BackRight = 0x20,
            FrontLeftOfCenter = 0x40,
            FrontRightOfCenter = 0x80,
            BackCenter = 0x100,
            SideLeft = 0x200,

            SideRight = 0x400,
            TopCenter = 0x800,
            TopFrontLeft = 0x1000,
            TopFrontCenter = 0x2000,
            TopFrontRight = 0x4000,

            TopBackLeft = 0x8000,
            TopBackCenter = 0x10000,
            TopBackRight = 0x20000,
        }

        #region DwChannelMask and AudioObjectTypeMask conversion

        public static List<DwChannelMaskType> DwChannelMaskToList(int dwChannelMask) {
            var r = new List<DwChannelMaskType>();

            for (int i=1; i<=0x20000; i*=2) {
                if (0 != (dwChannelMask & i)) {
                    r.Add((DwChannelMaskType)i);
                }
            }
            return r;
        }

        public static List<AudioObjectType> AudioObjectTypeMaskToList(int audioObjectTypeMask) {
            var r = new List<AudioObjectType>();

            for (int i = 1; i <= 0x20000; i *= 2) {
                if (0 != (audioObjectTypeMask & i)) {
                    r.Add((AudioObjectType)i);
                }
            }

            return r;
        }

        public static int DwChannelMaskToAudioObjectTypeMask(int dwChannelMask) {
            int r = 0;
            if (0 != (dwChannelMask & (int)DwChannelMaskType.FrontLeft)) {
                r |= (int)AudioObjectType.FrontLeft;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.FrontRight)) {
                r |= (int)AudioObjectType.FrontRight;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.FrontCenter)) {
                r |= (int)AudioObjectType.FrontCenter;
            }

            if (0 != (dwChannelMask & (int)DwChannelMaskType.LowFrequency)) {
                r |= (int)AudioObjectType.LowFrequency;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.SideLeft)) {
                r |= (int)AudioObjectType.SideLeft;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.SideRight)) {
                r |= (int)AudioObjectType.SideRight;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.BackLeft)) {
                r |= (int)AudioObjectType.BackLeft;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.BackRight)) {
                r |= (int)AudioObjectType.BackRight;
            }

            if (0 != (dwChannelMask & (int)DwChannelMaskType.TopFrontLeft)) {
                r |= (int)AudioObjectType.TopFrontLeft;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.TopFrontRight)) {
                r |= (int)AudioObjectType.TopFrontRight;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.TopBackLeft)) {
                r |= (int)AudioObjectType.TopBackLeft;
            }
            if (0 != (dwChannelMask & (int)DwChannelMaskType.TopBackRight)) {
                r |= (int)AudioObjectType.TopBackRight;
            }

            // bottomFrontLeft
            // bottomFrontRight
            // bottomBackLeft
            // bottomBackRight

            if (0 != (dwChannelMask & (int)DwChannelMaskType.BackCenter)) {
                r |= (int)AudioObjectType.BackCenter;
            }

            return r;
        }
        public static int AudioObjectTypeMaskToDwChannelMask(int audioObjectTypeMask) {
            int r = 0;
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.FrontLeft)) {
                r |= (int)DwChannelMaskType.FrontLeft;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.FrontRight)) {
                r |= (int)DwChannelMaskType.FrontRight;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.FrontCenter)) {
                r |= (int)DwChannelMaskType.FrontCenter;
            }

            if (0 != (audioObjectTypeMask & (int)AudioObjectType.LowFrequency)) {
                r |= (int)DwChannelMaskType.LowFrequency;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.SideLeft)) {
                r |= (int)DwChannelMaskType.SideLeft;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.SideRight)) {
                r |= (int)DwChannelMaskType.SideRight;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.BackLeft)) {
                r |= (int)DwChannelMaskType.BackLeft;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.BackRight)) {
                r |= (int)DwChannelMaskType.BackRight;
            }

            if (0 != (audioObjectTypeMask & (int)AudioObjectType.TopFrontLeft)) {
                r |= (int)DwChannelMaskType.TopFrontLeft;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.TopFrontRight)) {
                r |= (int)DwChannelMaskType.TopFrontRight;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.TopBackLeft)) {
                r |= (int)DwChannelMaskType.TopBackLeft;
            }
            if (0 != (audioObjectTypeMask & (int)AudioObjectType.TopBackRight)) {
                r |= (int)DwChannelMaskType.TopBackRight;
            }

            // bottomFrontLeft
            // bottomFrontRight
            // bottomBackLeft
            // bottomBackRight

            if (0 != (audioObjectTypeMask & (int)AudioObjectType.BackCenter)) {
                r |= (int)DwChannelMaskType.BackCenter;
            }

            return r;
        }
        #endregion

        public class DeviceProperty {
            public int id;
            public string devIdStr;
            public string name;

            public DeviceProperty(int id, string devIdStr, string name) {
                this.id = id;
                this.devIdStr = devIdStr;
                this.name = name;
            }
        };

        public List<DeviceProperty> DevicePropertyList {
            get { return mDevicePropertyList; }
            set { mDevicePropertyList = value; }
        }
        private List<DeviceProperty> mDevicePropertyList = new List<DeviceProperty>();

#region NativeStuff
        internal static class NativeMethods {
            public const int TEXT_STRSZ = 256;

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            internal struct WWSpatialAudioDeviceProperty {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public String devIdStr;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TEXT_STRSZ)]
                public String name;
            };

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserInit();

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserTerm(int instanceId);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserDoEnumeration(int instanceId);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserGetDeviceCount(int instanceId);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserGetDeviceProperty(
                int instanceId, int idx,
                ref WWSpatialAudioDeviceProperty sadp);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioUserChooseDevice(
                int instanceId, int devIdx, int maxDynObjectCount, int staticObjectTypeMask);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioClearAllPcm(int instanceId);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioSetPcmBegin(
                int instanceId, int ch, long numSamples);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioSetPcmFragment(
                int instanceId, int ch, long startSamplePos, int sampleCount, float[] samples);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioSetPcmEnd(
                int instanceId, int ch, int audioObjectType);

            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioStart(
                int instanceId);
            [DllImport("WWSpatialAudioUserCpp2017.dll", CharSet = CharSet.Unicode)]
            internal extern static int WWSpatialAudioStop(
                int instanceId);
        };
#endregion

        public WWSpatialAudioUser() {
            mInstanceId = NativeMethods.WWSpatialAudioUserInit();

            int hr = NativeMethods.WWSpatialAudioUserDoEnumeration(mInstanceId);
            if (hr < 0) {
                mState = StateEnum.NoAudioDevice;
                return;
            }

            mState = StateEnum.Initialized;
        }

        public int UpdateDeviceList() {
            mDevicePropertyList.Clear();

            int hr = NativeMethods.WWSpatialAudioUserDoEnumeration(mInstanceId);
            if (hr < 0) {
                mState = StateEnum.NoAudioDevice;
                return hr;
            }

            int nDev = NativeMethods.WWSpatialAudioUserGetDeviceCount(mInstanceId);

            for (int i=0; i<nDev; ++i) {
                var sadp = new NativeMethods.WWSpatialAudioDeviceProperty();
                NativeMethods.WWSpatialAudioUserGetDeviceProperty(mInstanceId, i, ref sadp);

                var dev = new DeviceProperty(i, sadp.devIdStr, sadp.name);
                mDevicePropertyList.Add(dev);
            }
            mState = StateEnum.Ready;
            return 0;
        }

        /// <summary>
        /// choose device to play sound
        /// </summary>
        /// <param name="deviceId">device list item index starts from 0</param>
        /// <param name="maxDynamicObjectCount"></param>
        /// <param name="staticObjectTypeMask">bitwiseOR of AudioObjectType</param>
        /// <returns></returns>
        public int ChooseDevice(int deviceId, int maxDynamicObjectCount, int staticObjectTypeMask) {
            int hr = NativeMethods.WWSpatialAudioUserChooseDevice(
                mInstanceId, deviceId, maxDynamicObjectCount, staticObjectTypeMask);
            if (0 <= hr) {
                mState = StateEnum.Ready;
            }
            return hr;
        }

        public void ClearAllPcm() {
            int hr = NativeMethods.WWSpatialAudioClearAllPcm(mInstanceId);
            System.Diagnostics.Debug.Assert(0 <= hr);
        }

        public int SetPcmBegin(int ch, long numSamples) {
            int hr = NativeMethods.WWSpatialAudioSetPcmBegin(mInstanceId, ch, numSamples);
            return hr;
        }

        /// <summary>
        /// ネイティブPCMストアーのstartSamplePosにpcmFragmentを全てコピーする。
        /// </summary>
        public int SetPcmFragment(int ch, long startSamplePos, float [] pcmFragment) {
            int hr = NativeMethods.WWSpatialAudioSetPcmFragment(mInstanceId, ch, startSamplePos, pcmFragment.Length, pcmFragment);
            return hr;
        }

        public void SetPcmEnd(int ch, AudioObjectType aot) {
            int hr = NativeMethods.WWSpatialAudioSetPcmEnd(mInstanceId, ch, (int)aot);
            System.Diagnostics.Debug.Assert(0 <= hr);
        }

        public int Start() {
            int hr = NativeMethods.WWSpatialAudioStart(mInstanceId);
            return hr;
        }

        public int Stop() {
            int hr = NativeMethods.WWSpatialAudioStop(mInstanceId);
            return hr;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void Term() {
            if (mInstanceId < 0) {
                return;
            }
            NativeMethods.WWSpatialAudioUserTerm(mInstanceId);
            mInstanceId = -1;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                // Free unmanaged resources here.
                Term();

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion



    };
};

