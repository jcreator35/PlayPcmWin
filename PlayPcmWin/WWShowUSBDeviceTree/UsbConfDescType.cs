using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWShowUSBDeviceTree
{
    public enum UsbConfDescType {
        Unknown = -1,
        Device = 1,
        Configuration = 2,
        String = 3,
        Interface = 4,
        Endpoint = 5,
        DeviceQualifier = 6,
        OtherSpeedConfiguration = 7,
        InterfacePower = 8,
        OTG = 9,
        Debug = 0xa,
        InterfaceAssociation = 0xb,
        BOS = 0xf,
        DeviceCapability = 0x10,
        SuperspeedEndpointCompanion = 0x30,
        SuperspeedPlusIsochEndpointCompanion = 0x31,
    }
}
