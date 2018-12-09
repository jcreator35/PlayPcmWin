using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WWShowUSBDeviceTree {
    public class UsbDeviceTreeCs {
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
            case BusSpeed.SuperSpeed: return "USB 3.0/USB3.1 Gen1";
            case BusSpeed.SuperSpeedPlus: return "USB 3.1 Gen2 or higher";
            default: return "Unknown";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbHostControllerCs {
            public int idx;
            public int numberOfRootPorts;

            public uint deviceCount;
            public uint currentUsbFrame;

            public uint bulkBytes;
            public uint isoBytes;
            public uint interruptBytes;
            public uint controlDataBytes;

            public uint pciInterruptCount;
            public uint hardResetCount;

            public ulong totalBusBandwidth;     //< bits/sec
            public ulong total32secBandwidth;   //< bits/32sec
            public ulong allocedBulkAndControl; //< bits/32sec
            public ulong allocedIso;            //< bits/32sec
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string desc;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string vendor;
        };

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

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbHubPortNative {
            public int idx;
            public int parentIdx;
            public int deviceIsHub;
            public int bmAttributes; //< USB config descriptor bmAttributes
            public int powerMilliW;

            public int speed; //< WWUsbDeviceBusSpeed
            public int usbVersion; //< WWUsbDeviceBusSpeed
            public int portConnectorType; //< PortConnectorType
            public int confDescBytes;
            public int numStringDesc;

            public IntPtr confDesc;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string product;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string vendor;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct WWUsbStringDescCs {
            public int descIdx;
            public int langId;
            public int descType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
        };


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

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetHostControllerInf(int nth, out WWUsbHostControllerCs hub_r);

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetNumOfHubs();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
            private extern static int
        WWUsbDeviceTreeDLL_GetHubInf(int nth, out WWUsbHubCs hub_r);

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetNumOfHubPorts();

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetHubPortInf(int nth, out WWUsbHubPortNative hp_r);

        [DllImport("WWUsbDeviceTreeDLL.dll")]
        private extern static int
        WWUsbDeviceTreeDLL_GetStringDesc(int nth, int idx, out WWUsbStringDescCs sd_r);

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        #endregion

        public class WWUsbHubPortCs {
            public int idx;
            public int parentIdx;
            public int deviceIsHub;
            public int bmAttributes; //< USB config descriptor bmAttributes
            public int powerMilliW;
            public int speed; //< WWUsbDeviceBusSpeed
            public int usbVersion; //< WWUsbDeviceBusSpeed
            public int portConnectorType; //< PortConnectorType
            public string name;
            public string product;
            public string vendor;
            public byte[] confDesc;
            public int numStringDesc;

            public List<WWUsbStringDescCs> stringDescList = new List<WWUsbStringDescCs>();

            public WWUsbHubPortCs(WWUsbHubPortNative n) {
                idx = n.idx;
                parentIdx = n.parentIdx;
                deviceIsHub = n.deviceIsHub;
                bmAttributes = n.bmAttributes;
                powerMilliW = n.powerMilliW;
                speed = n.speed;
                usbVersion = n.usbVersion;
                portConnectorType = n.portConnectorType;
                name = n.name.Trim();
                product = n.product.Trim();
                vendor = n.vendor.Trim();
                confDesc = new byte[n.confDescBytes];
                Marshal.Copy(n.confDesc, confDesc, 0, n.confDescBytes);
                numStringDesc = n.numStringDesc;
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


        public const int USB_CONFIG_POWERED_MASK = 0xC0;
        public const int USB_CONFIG_BUS_POWERED = 0x80;
        public const int USB_CONFIG_SELF_POWERED = 0x40;
        public const int USB_CONFIG_REMOTE_WAKEUP = 0x20;
        public const int USB_CONFIG_RESERVED = 0x1F;
        public List<WWUsbHubCs> Hubs { get; } = new List<WWUsbHubCs>();
        public List<WWUsbHostControllerCs> HCs { get; } = new List<WWUsbHostControllerCs>();
        public List<WWUsbHubPortCs> HPs { get; } = new List<WWUsbHubPortCs>();

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

            HCs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHostControllers();
            for (int i=0; i<n; ++i) {
                WWUsbHostControllerCs hc;
                rv = WWUsbDeviceTreeDLL_GetHostControllerInf(i, out hc);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HC failed {0}", rv);
                    return rv;
                }

                HCs.Add(hc);
            }

            Hubs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHubs();
            for (int i = 0; i < n; ++i) {
                WWUsbHubCs hub;
                rv = WWUsbDeviceTreeDLL_GetHubInf(i, out hub);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HUB failed {0}", rv);
                    return rv;
                }
                Hubs.Add(hub);
            }

            HPs.Clear();
            n = WWUsbDeviceTreeDLL_GetNumOfHubPorts();
            for (int i = 0; i < n; ++i) {
                WWUsbHubPortNative hpn;
                rv = WWUsbDeviceTreeDLL_GetHubPortInf(i, out hpn);
                if (rv < 0) {
                    Console.WriteLine("Error: WWUsbDeviceTreeCs::Update HubPort failed {0}", rv);
                    return rv;
                }

                WWUsbHubPortCs hp = new WWUsbHubPortCs(hpn);
                for (int j=0; j<hpn.numStringDesc; ++j) {
                    WWUsbStringDescCs sdc;
                    WWUsbDeviceTreeDLL_GetStringDesc(i, j, out sdc);
                    hp.stringDescList.Add(sdc);
                }

                HPs.Add(hp);
            }

            return 0;
        }
    }
}
