using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WWCompareTwoImages
{
    // from: https://stackoverflow.com/questions/13533754/code-example-for-wcsgetdefaultcolorprofile
    public class WWMonitorProfile
    {
        internal static class NativeMethods {
            [Flags()]
            public enum DisplayDeviceStateFlags : UInt32
            {
                // from: http://www.pinvoke.net/default.aspx/Enums/DisplayDeviceStateFlags.html
                // equvalent to defines from: wingdi.h (c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\wingdi.h)
                //#define DISPLAY_DEVICE_ATTACHED_TO_DESKTOP      0x00000001
                //#define DISPLAY_DEVICE_MULTI_DRIVER             0x00000002
                //#define DISPLAY_DEVICE_PRIMARY_DEVICE           0x00000004
                //#define DISPLAY_DEVICE_MIRRORING_DRIVER         0x00000008
                //#define DISPLAY_DEVICE_VGA_COMPATIBLE           0x00000010
                //#if (_WIN32_WINNT >= _WIN32_WINNT_WIN2K)
                //#define DISPLAY_DEVICE_REMOVABLE                0x00000020
                //#endif // (_WIN32_WINNT >= _WIN32_WINNT_WIN2K)
                //#if (_WIN32_WINNT >= _WIN32_WINNT_WIN8)
                //#define DISPLAY_DEVICE_ACC_DRIVER               0x00000040
                //#endif
                //#define DISPLAY_DEVICE_MODESPRUNED              0x08000000
                //#if (_WIN32_WINNT >= _WIN32_WINNT_WIN2K)
                //#define DISPLAY_DEVICE_REMOTE                   0x04000000
                //#define DISPLAY_DEVICE_DISCONNECT               0x02000000
                //#endif
                //#define DISPLAY_DEVICE_TS_COMPATIBLE            0x00200000
                //#if (_WIN32_WINNT >= _WIN32_WINNT_LONGHORN)
                //#define DISPLAY_DEVICE_UNSAFE_MODES_ON          0x00080000
                //#endif

                ///* Child device state */
                //#if (_WIN32_WINNT >= _WIN32_WINNT_WIN2K)
                //#define DISPLAY_DEVICE_ACTIVE              0x00000001
                //#define DISPLAY_DEVICE_ATTACHED            0x00000002
                //#endif // (_WIN32_WINNT >= _WIN32_WINNT_WIN2K)
                /// <summary>The device is part of the desktop.</summary>
                AttachedToDesktop = 0x1,
                MultiDriver = 0x2,
                /// <summary>The device is part of the desktop.</summary>
                PrimaryDevice = 0x4,
                /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
                MirroringDriver = 0x8,
                /// <summary>The device is VGA compatible.</summary>
                VGACompatible = 0x10,
                /// <summary>The device is removable; it cannot be the primary display.</summary>
                Removable = 0x20,
                /// <summary>The device has more display modes than its output devices support.</summary>
                ModesPruned = 0x8000000,
                Remote = 0x4000000,
                Disconnect = 0x2000000,

                /// <summary>Child device state: DISPLAY_DEVICE_ACTIVE</summary>
                Active = 0x1,
                /// <summary>Child device state: DISPLAY_DEVICE_ATTACHED</summary>
                Attached = 0x2
            }

            public enum DeviceClassFlags : UInt32
            {
                // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
                /// <summary>
                ///#define CLASS_MONITOR           'mntr' 
                /// </summary>
                CLASS_MONITOR = 0x6d6e7472,

                /// <summary>
                /// #define CLASS_PRINTER           'prtr'
                /// </summary>
                CLASS_PRINTER = 0x70727472,

                /// <summary>
                /// #define CLASS_SCANNER           'scnr'
                /// </summary>
                CLASS_SCANNER = 0x73636e72
            }

            public enum WCS_PROFILE_MANAGEMENT_SCOPE : UInt32
            {
                // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
                WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE,
                WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER
            }

            public enum COLORPROFILETYPE : UInt32
            {
                // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
                CPT_ICC,
                CPT_DMP,
                CPT_CAMP,
                CPT_GMMP
            }

            public enum COLORPROFILESUBTYPE : UInt32
            {
                // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
                // intent
                CPST_PERCEPTUAL = 0,
                CPST_RELATIVE_COLORIMETRIC = 1,
                CPST_SATURATION = 2,
                CPST_ABSOLUTE_COLORIMETRIC = 3,

                // working space
                CPST_NONE,
                CPST_RGB_WORKING_SPACE,
                CPST_CUSTOM_WORKING_SPACE,
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct DISPLAY_DEVICE
            {
                // from: http://www.pinvoke.net/default.aspx/Structures/DISPLAY_DEVICE.html
                [MarshalAs(UnmanagedType.U4)]
                public int cb;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string DeviceName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceString;
                [MarshalAs(UnmanagedType.U4)]
                public DisplayDeviceStateFlags StateFlags;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceID;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceKey;
            }

            public const UInt32 EDD_GET_DEVICE_INTERFACE_NAME = 0x1;

            //BOOL EnumDisplayDevices(
            //  _In_ LPCTSTR         lpDevice,
            //  _In_ DWORD           iDevNum,
            //  _Out_ PDISPLAY_DEVICE lpDisplayDevice,
            //  _In_ DWORD           dwFlags
            //);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern UInt32 EnumDisplayDevices(string s, UInt32 iDevNum, ref DISPLAY_DEVICE displayDevice, UInt32 dwFlags);

            // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
            //BOOL WINAPI WcsGetUsePerUserProfiles(
            //  _In_ LPCWSTR pDeviceName,
            //  _In_ DWORD dwDeviceClass,
            //  _Out_ BOOL *pUsePerUserProfiles
            //);
            /// <summary>
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd372253(v=vs.85).aspx
            /// </summary>
            /// <returns>0, if failed</returns>
            [DllImport("Mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern UInt32 WcsGetUsePerUserProfiles(string deviceName, DeviceClassFlags deviceClass, out UInt32 usePerUserProfiles);

            // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
            //BOOL WINAPI WcsGetDefaultColorProfileSize(
            //  _In_ WCS_PROFILE_MANAGEMENT_SCOPE profileManagementScope,
            //  _In_opt_ PCWSTR pDeviceName,
            //  _In_ COLORPROFILETYPE cptColorProfileType,
            //  _In_ COLORPROFILESUBTYPE cpstColorProfileSubType,
            //  _In_ DWORD dwProfileID,
            //  _Out_ PDWORD pcbProfileName
            //);
            /// <summary>
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd372249(v=vs.85).aspx
            /// </summary>
            /// <param name="cbProfileName">Size in bytes! String length is /2</param>
            /// <returns>0, if failed</returns>
            [DllImport("Mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern UInt32 WcsGetDefaultColorProfileSize(WCS_PROFILE_MANAGEMENT_SCOPE scope,
                string deviceName,
                COLORPROFILETYPE colorProfileType,
                COLORPROFILESUBTYPE colorProfileSubType,
                UInt32 dwProfileID,
                out UInt32 cbProfileName
            );

            // from: c:\Program Files (x86)\Windows Kits\10\Include\10.0.10240.0\um\Icm.h
            //BOOL WINAPI WcsGetDefaultColorProfile(
            //  _In_ WCS_PROFILE_MANAGEMENT_SCOPE profileManagementScope,
            //  _In_opt_ PCWSTR pDeviceName,
            //  _In_ COLORPROFILETYPE cptColorProfileType,
            //  _In_ COLORPROFILESUBTYPE cpstColorProfileSubType,
            //  _In_ DWORD dwProfileID,
            //  _In_ DWORD cbProfileName,
            //  _Out_ LPWSTR pProfileName
            //);
            /// <summary>
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd372247(v=vs.85).aspx
            /// </summary>
            /// <returns>0, if failed</returns>
            [DllImport("Mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern UInt32 WcsGetDefaultColorProfile(WCS_PROFILE_MANAGEMENT_SCOPE scope,
                string deviceName,
                COLORPROFILETYPE colorProfileType,
                COLORPROFILESUBTYPE colorProfileSubType,
                UInt32 dwProfileID,
                UInt32 cbProfileName,
                StringBuilder profileName
            );

            [DllImport("Mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern bool GetColorDirectory(IntPtr pMachineName, StringBuilder pBuffer, ref uint pdwSize);
        };

        public static string GetMonitorProfile()
        {
            // c++ recommendation: http://stackoverflow.com/questions/13533754/code-example-for-wcsgetdefaultcolorprofile

            var displayDevice = new NativeMethods.DISPLAY_DEVICE();
            displayDevice.cb = Marshal.SizeOf(displayDevice);

            // First, find the primary adaptor
            string adaptorName = null;
            UInt32 deviceIndex = 0;

            while (NativeMethods.EnumDisplayDevices(null, deviceIndex++, ref displayDevice, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME) != 0) {
                if ((displayDevice.StateFlags & NativeMethods.DisplayDeviceStateFlags.AttachedToDesktop) != 0 &&
                    (displayDevice.StateFlags & NativeMethods.DisplayDeviceStateFlags.PrimaryDevice) != 0) {
                    adaptorName = displayDevice.DeviceName;
                    break;
                }
            }

            // Second, find the first active (and attached) monitor
            string deviceName = null;
            deviceIndex = 0;
            while (NativeMethods.EnumDisplayDevices(adaptorName, deviceIndex++, ref displayDevice, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME) != 0) {
                if ((displayDevice.StateFlags & NativeMethods.DisplayDeviceStateFlags.Active) != 0 &&
                    (displayDevice.StateFlags & NativeMethods.DisplayDeviceStateFlags.Attached) != 0) {
                    deviceName = displayDevice.DeviceKey;
                    break;
                }
            }

            // Third, find out whether to use the global or user profile
            UInt32 usePerUserProfiles = 0;
            UInt32 res = NativeMethods.WcsGetUsePerUserProfiles(deviceName, NativeMethods.DeviceClassFlags.CLASS_MONITOR, out usePerUserProfiles);
            if (res == 0) {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            // Finally, get the profile name
            NativeMethods.WCS_PROFILE_MANAGEMENT_SCOPE scope = (usePerUserProfiles != 0) ?
                NativeMethods.WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER :
                NativeMethods.WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE;

            UInt32 cbProfileName = 0;   // in bytes
            res = NativeMethods.WcsGetDefaultColorProfileSize(scope,
                deviceName,
                NativeMethods.COLORPROFILETYPE.CPT_ICC,
                NativeMethods.COLORPROFILESUBTYPE.CPST_RGB_WORKING_SPACE,
                0,
                out cbProfileName);
            if (res == 0) {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            int nLengthProfileName = (int)cbProfileName / 2;    // WcsGetDefaultColor... is using LPWSTR, i.e. 2 bytes/char
            StringBuilder profileName = new StringBuilder(nLengthProfileName);
            res = NativeMethods.WcsGetDefaultColorProfile(scope,
                deviceName,
                NativeMethods.COLORPROFILETYPE.CPT_ICC,
                NativeMethods.COLORPROFILESUBTYPE.CPST_RGB_WORKING_SPACE,
                0,
                cbProfileName,
                profileName);
            if (res == 0) {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return profileName.ToString();
        }

        public static string GetColorDirectory()
        {
            // s. http://stackoverflow.com/questions/14792764/is-there-an-equivalent-to-winapi-getcolordirectory-in-net
            uint pdwSize = 260;  // MAX_PATH 
            StringBuilder sb = new StringBuilder((int)pdwSize);
            bool b = NativeMethods.GetColorDirectory(IntPtr.Zero, sb, ref pdwSize);
            int hr = Marshal.GetLastWin32Error();

            if (b) {
                return sb.ToString();
            } else {
                throw new System.ComponentModel.Win32Exception(hr);
            }
        }

    }
}
