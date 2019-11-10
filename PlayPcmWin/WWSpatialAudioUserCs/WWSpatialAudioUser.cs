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

        public class DeviceProperty {
            public int id;
            public string name;

            public DeviceProperty(int id, string name) {
                this.id = id;
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

                var dev = new DeviceProperty(i, sadp.name);
                mDevicePropertyList.Add(dev);
            }
            mState = StateEnum.Ready;
            return 0;
        }

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

