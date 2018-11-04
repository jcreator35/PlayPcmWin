using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WWShowUSBDeviceTree.UsbDeviceTreeCs;

namespace WWShowUSBDeviceTree
{
    public class UsbConfDescReader
    {
        WWUsbHubPortCs mHp;
        StringBuilder mSB = new StringBuilder();
        List<WWUsbStringDescCs> mSds;
        UsbDeviceTreeCs.BusSpeed mSpeed;

        public string Read(WWUsbHubPortCs hp) {
            mHp = hp;
            var buff = hp.confDesc;
            mSds = hp.stringDescList;
            mSpeed = (UsbDeviceTreeCs.BusSpeed)hp.speed;

            mSB.Clear();

            int offs = 0;

            for (; ; ) {
                if (buff == null || buff.Length - offs < 2) {
                    return mSB.ToString();
                }

                var commD = new UsbCommonDescriptor(buff, offs);
                switch (commD.descType) {
                case UsbConfDescType.Configuration:
                    ReadConfDesc(buff, offs);
                    break;
                }

                offs += commD.bytes;
            }
        }

        private string FindString(int idx) {
            foreach (var v in mSds) {
                if (v.descIdx == idx) {
                    return v.name;
                }
            }

            return "";
        }

        private const int USB_CONFIG_BUS_POWERED = 0x80;
        private const int USB_CONFIG_SELF_POWERED = 0x40;

        private void ReadConfDesc(byte [] buff, int offs) {
            /*
            0 UCHAR bLength;
            1 UCHAR bDescriptorType;
            2 USHORT wTotalLength;
            4 UCHAR bNumInterfaces;
            5 UCHAR bConfigurationValue;
            6 UCHAR iConfiguration;
            7 UCHAR bmAttributes;
            8 UCHAR MaxPower;
            */
            if (buff[0] < 9) {
                return;
            }

            int iConfiguration = buff[6];

            int bmAttributes = buff[7];

            var s = FindString(iConfiguration);

            // maxPowerAを計算。
            int maxPower = buff[8];
            int maxPowerA;
            if (BusSpeed.SuperSpeed <= mSpeed) {
                maxPowerA = maxPower * 8;
            } else {
                maxPowerA = maxPower * 2;
            }

            // バスパワーかどうか。
            string power = "";
            if (mSpeed <= BusSpeed.FullSpeed) {
                if (0 != (USB_CONFIG_BUS_POWERED & bmAttributes)) {
                    power = "Bus powered,";
                }
                if (0 != (USB_CONFIG_SELF_POWERED & bmAttributes)) {
                    power = "Self powered,";
                }
            } else {
                if (0 != (USB_CONFIG_SELF_POWERED & bmAttributes)) {
                    power = "Self powered,";
                } else {
                    power = "Bus powered,";
                }
            }

            mSB.AppendFormat("  Configuration {0}\n    {1} Max power={2}mA", s, power, maxPowerA);
        }
    }
}
