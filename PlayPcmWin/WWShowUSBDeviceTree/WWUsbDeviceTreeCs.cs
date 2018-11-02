using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace WWShowUSBDeviceTree {
    public class WWUsbDeviceTreeCs {
        public enum BusSpeed {
            RootHub, //< RootHubは別格の扱い。
            LowSpeed,
            FullSpeed,
            HighSpeed,
            SuperSpeed,
            SuperSpeedPlus,
        };

        public enum PortConnectorType {
            TypeA,
            TypeC,
        }

        public static string WWUsbDeviceBusSpeedToStr(BusSpeed t) {
            switch (t) {
            case BusSpeed.RootHub: return "RootHub";
            case BusSpeed.LowSpeed: return "LowSpeed(0.19MB/s)";
            case BusSpeed.FullSpeed: return "FullSpeed(1.5MB/s)";
            case BusSpeed.HighSpeed: return "HighSpeed(60MB/s)";
            case BusSpeed.SuperSpeed: return "SuperSpeed(625MB/s)";
            case BusSpeed.SuperSpeedPlus: return "SuperSpeed+(1.25GB/s～)";
            default: return "Unknown";
            }
        }

        public static string WWUsbDeviceBusSpeedToUsbVersionStr(BusSpeed t) {
            switch (t) {
            case BusSpeed.RootHub: return "RootHub";
            case BusSpeed.LowSpeed: return "USB 1.x";
            case BusSpeed.FullSpeed: return "USB 1.x";
            case BusSpeed.HighSpeed: return "USB 2.0";
            case BusSpeed.SuperSpeed: return "USB 3.0";
            case BusSpeed.SuperSpeedPlus: return "USB 3.1 or higher";
            default: return "Unknown";
            }
        }


        #region native methods
        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_Init();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_Refresh();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static void
        WWUsbDeviceTreeDLL_Term();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetNumOfHostControllers();

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbHostControllerCs {
            public int idx;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string vendor;
        };

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetHostControllerInf(int nth, out WWUsbHostControllerCs hub_r);

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetNumOfHubs();

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbHubCs {
            public int idx;
            public int parentIdx;
            public int numPorts;
            public int isBusPowered; //< TRUE: Bus powered, FALSE: Self powered
            public int isRoot;
            public int speed; //< WWUsbDeviceBusSpeed
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
        };

        [DllImport("WWUsbDeviceTreeDLL.dll")]
            private extern static int
        WWUsbDeviceTreeDLL_GetHubInf(int nth, out WWUsbHubCs hub_r);

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbHubPortCs {
            public int idx;
            public int parentIdx;
            public int deviceIsHub;
            public int bmAttributes; //< USB config descriptor bmAttributes
            public int powerMilliW;
            public int speed; //< WWUsbDeviceBusSpeed
            public int usbVersion; //< WWUsbDeviceBusSpeed
            public int portConnectorType; //< PortConnectorType
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string vendor;

            public string PowerStr() {
                string power = "Bus powered";

                switch (bmAttributes & USB_CONFIG_POWERED_MASK) {
                case USB_CONFIG_BUS_POWERED:
                    power = "Bus powered";
                    break;
                case USB_CONFIG_SELF_POWERED:
                    power = "Self powered";
                    break;
                case USB_CONFIG_BUS_POWERED | USB_CONFIG_SELF_POWERED:
                    power = "Bus powered or Self powered";
                    break;
                }
                return string.Format("{0}\nMax {1} mW", power, powerMilliW);
            }

            public string VersionStr() {
                var versionEnum = (BusSpeed)usbVersion;
                return WWUsbDeviceBusSpeedToUsbVersionStr(versionEnum);
            }

            public string SpeedStr() {
                var speedEnum = (BusSpeed)speed;
                return WWUsbDeviceBusSpeedToStr(speedEnum);
            }

            public string ConnectorTypeStr() {
                var t = (PortConnectorType)portConnectorType;
                return t.ToString();
            }
        };

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetNumOfHubPorts();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetHubPortInf(int nth, out WWUsbHubPortCs hp_r);


        #endregion

        public const int USB_CONFIG_POWERED_MASK = 0xC0;
        public const int USB_CONFIG_BUS_POWERED = 0x80;
        public const int USB_CONFIG_SELF_POWERED = 0x40;
        public const int USB_CONFIG_REMOTE_WAKEUP = 0x20;
        public const int USB_CONFIG_RESERVED = 0x1F;

        private List<WWUsbHubCs> mHubs = new List<WWUsbHubCs>();
        public List<WWUsbHubCs> Hubs { get { return mHubs; } }

        private List<WWUsbHostControllerCs> mHCs = new List<WWUsbHostControllerCs>();
        public List<WWUsbHostControllerCs> HCs { get { return mHCs; } }

        private List<WWUsbHubPortCs> mHPs = new List<WWUsbHubPortCs>();
        public List<WWUsbHubPortCs> HPs { get { return mHPs; } }

        public int Init() {
            return WWUsbDeviceTreeDLL_Init();
        }

        public void Term() {
            WWUsbDeviceTreeDLL_Term();
        }

        public int Refresh() {
            int rv;
            int n;

            rv = WWUsbDeviceTreeDLL_Refresh();
            if (rv < 0) {
                Console.WriteLine("Error: WWUsbDeviceTreeCs::Refresh() failed {0}", rv);
                return rv;
            }

            mHCs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHostControllers();
            for (int i=0; i<n; ++i) {
                WWUsbHostControllerCs hc;
                rv = WWUsbDeviceTreeDLL_GetHostControllerInf(i, out hc);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HC failed {0}", rv);
                    return rv;
                }

                mHCs.Add(hc);
            }

            mHubs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHubs();
            for (int i = 0; i < n; ++i) {
                WWUsbHubCs hub;
                rv = WWUsbDeviceTreeDLL_GetHubInf(i, out hub);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HUB failed {0}", rv);
                    return rv;
                }
                mHubs.Add(hub);
            }

            mHPs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHubPorts();
            for (int i = 0; i < n; ++i) {
                WWUsbHubPortCs hp;
                rv = WWUsbDeviceTreeDLL_GetHubPortInf(i, out hp);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HubPort failed {0}", rv);
                    return rv;
                }
                mHPs.Add(hp);
            }

            return 0;
        }
    }
}
