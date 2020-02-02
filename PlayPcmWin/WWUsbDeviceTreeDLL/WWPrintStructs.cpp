// 日本語。

#include "WWPrintStructs.h"
#include "WWUsbCommon.h"

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

// USB Device Class Definition for Human Interface Devices. Appendix E.4
#define WW_USB_HID_DESCRIPTOR_TYPE             0x21

// USB Device Class Definition for Human Interface Devices. p.68
#define WW_USB_REPORT_DESCRIPTOR_TYPE          0x22

// USB Device Class Definition for Human Interface Devices. 6.2.1 HID Descriptor
struct WW_USB_HID_DESCRIPTOR
{
    UCHAR   bLength;
    UCHAR   bDescriptorType;
    USHORT  bcdHID;
    UCHAR   bCountryCode;
    UCHAR   bNumDescriptors;
    struct
    {
        UCHAR   bDescriptorType;
        USHORT  wDescriptorLength;
    } OptionalDescriptors[1];
};

typedef WW_USB_HID_DESCRIPTOR *PWW_USB_HID_DESCRIPTOR;

// USB Device Class Definition for Human Interface Devices. 
// p.23
static std::string
CountryCodeToStr(int c)
{
    switch (c) {
    case 0: return "";
    case 1: return "Arabic";
    case 2: return "Belgian";
    case 3: return "Canadian-Bilingual";
    case 4: return "Canadian-French";

    case 5: return "Czech Republic";
    case 6: return "Danish";
    case 7: return "Finnish";
    case 8: return "French";
    case 9: return "German";

    case 10: return "Greek";
    case 11: return "Hebrew";
    case 12: return "Hungary";
    case 13: return "International(ISO)";
    case 14: return "Italian";

    case 15: return "Japan(Katakana)";
    case 16: return "Korean";
    case 17: return "Latin American";
    case 18: return "Netherlands/Dutch";
    case 19: return "Norwegian";

    case 20: return "Persian(Farsi)";
    case 21: return "Poland";
    case 22: return "Portuguese";
    case 23: return "Russia";
    case 24: return "Slovakia";

    case 25: return "Spanish";
    case 26: return "Swedish";
    case 27: return "Swiss/French";
    case 28: return "Swiss/German";
    case 29: return "Switzerland";

    case 30: return "Taiwan";
    case 31: return "Turkish-Q";
    case 32: return "UK";
    case 33: return "US";
    case 34: return "Yugoslavia";

    case 35: return "Turkish-F";
    default:
    {
        char s[256];
        sprintf_s(s, "Reserved(%d)", c);
        return std::string(s);
    }
    }
}

static std::string
DeviceClassToStr(int c)
{
    switch (c) {

    case USB_DEVICE_CLASS_RESERVED: return "Reserved";
    case USB_DEVICE_CLASS_AUDIO: return "Audio";
    case USB_DEVICE_CLASS_COMMUNICATIONS: return "Communications";
    case USB_DEVICE_CLASS_HUMAN_INTERFACE: return "HumanInterface";
    case USB_DEVICE_CLASS_MONITOR: return "Monitor";
    case USB_DEVICE_CLASS_PHYSICAL_INTERFACE: return "PhysicalInterface";
    case USB_DEVICE_CLASS_POWER: return "Power or Image";
    case USB_DEVICE_CLASS_PRINTER: return "Printer";
    case USB_DEVICE_CLASS_STORAGE: return "Storage";
    case USB_DEVICE_CLASS_HUB: return "Hub";
    case USB_DEVICE_CLASS_CDC_DATA: return "CDC_DATA";
    case USB_DEVICE_CLASS_SMART_CARD: return "SmartCard";
    case USB_DEVICE_CLASS_CONTENT_SECURITY: return "ContentSecurity";
    case USB_DEVICE_CLASS_VIDEO: return "Video";
    case USB_DEVICE_CLASS_PERSONAL_HEALTHCARE: return "PersonalHealthCare";
    case USB_DEVICE_CLASS_AUDIO_VIDEO: return "AudioVideo";
    case USB_DEVICE_CLASS_BILLBOARD: return "Billboard";
    case USB_DEVICE_CLASS_DIAGNOSTIC_DEVICE: return "DiagnosticDevice";
    case USB_DEVICE_CLASS_WIRELESS_CONTROLLER: return "WirelessController";
    case USB_DEVICE_CLASS_MISCELLANEOUS: return "Miscellaneous";
    case USB_DEVICE_CLASS_APPLICATION_SPECIFIC: return "ApplicationSpecific";
    case 0xff:
        return "VendorSpecific";

    default:
    {
        char s[256];
        sprintf_s(s, "UnknownDeviceClass(%d)", c);
        return std::string(s);
    }
    }
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

enum WWEndPointDirection {
    WWED_Unknown = -1,
    WWED_In,
    WWED_Out,
};

static const wchar_t *
WWEndPointDirectionToStr(WWEndPointDirection d)
{
    switch (d) {
    case WWED_Unknown:
    default:
        return L"Unknown";
    case WWED_In:
        return L"In";
    case WWED_Out:
        return L"Out";
    }
}

static WWEndPointDirection
EndPointAddressToDir(UCHAR a)
{
    if (USB_ENDPOINT_DIRECTION_OUT(a)) {
        return WWED_Out;
    }
    if (USB_ENDPOINT_DIRECTION_IN(a)) {
        return WWED_In;
    }
    return WWED_Unknown;
}

enum WWIsocEndpointUsage {
    WWIEU_Unknown = -1,
    WWIEU_Data,
    WWIEU_Feedback,
    WWIEU_ImplicitFeedback,
};

static const wchar_t *
WWIsocEndpointUsageToStr(WWIsocEndpointUsage u)
{
    switch (u) {
    case WWIEU_Data:
        return L"Data";
    case WWIEU_Feedback:
        return L"Feedback";
    case WWIEU_ImplicitFeedback:
        return L"ImplicitFeedback";
    default:
        return L"Unknown";
    }
}

static WWIsocEndpointUsage
EndPointAttrToUsage(UCHAR attr)
{
    switch (USB_ENDPOINT_TYPE_ISOCHRONOUS_USAGE(attr)) {
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_USAGE_DATA_ENDOINT: //< ヘッダーがtypoしている！
        return WWIEU_Data;
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_USAGE_FEEDBACK_ENDPOINT:
        return WWIEU_Feedback;
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_USAGE_IMPLICIT_FEEDBACK_DATA_ENDPOINT:
        return WWIEU_ImplicitFeedback;
    default:
        return WWIEU_Unknown;
    }
}

static const wchar_t *
SyncTypeToStr(UCHAR sync)
{
    switch (sync) {
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_NO_SYNCHRONIZATION:
        return L"NoSync";
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ASYNCHRONOUS:
        return L"Async";
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ADAPTIVE:
        return L"Adaptive";
    case USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_SYNCHRONOUS:
        return L"Sync";
    default:
        return L"Unknown";
    }
}

/// @return endpointType (USB_ENDPOINT_TYPE_ISOCHRONOUS, USB_ENDPOINT_TYPE_BULK etc)
static int
PrintEndpointDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
    PUSB_ENDPOINT_DESCRIPTOR ed = (PUSB_ENDPOINT_DESCRIPTOR)d;
    WWEndPointDirection dir = EndPointAddressToDir(ed->bEndpointAddress);
    int epAddr = ed->bEndpointAddress & USB_ENDPOINT_ADDRESS_MASK;
    UCHAR epType = ed->bmAttributes & USB_ENDPOINT_TYPE_MASK;
    UCHAR sync = USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION(ed->bmAttributes);
    WWIsocEndpointUsage usage = EndPointAttrToUsage(ed->bmAttributes);

    switch (epType) {
    case USB_ENDPOINT_TYPE_ISOCHRONOUS:
        WWPrintIndentSpace(level);
        printf("Addr=%2d Isochronous %S %S %S Endpoint. MaxPacketSize=%d\n",
            epAddr,
            SyncTypeToStr(sync), WWIsocEndpointUsageToStr(usage), WWEndPointDirectionToStr(dir),
            ed->wMaxPacketSize);
            break;
    case USB_ENDPOINT_TYPE_BULK:
        WWPrintIndentSpace(level);
        printf("Addr=%2d Bulk transfer %S Endpoint. MaxPacketSize=%d\n",
            epAddr, WWEndPointDirectionToStr(dir), ed->wMaxPacketSize);
        break;
    default:
        // 表示しない。
        break;
    }

    return epType;
}

static const wchar_t *
InterfaceClassToStr(int c)
{
    switch (c) {
    case USB_DEVICE_CLASS_AUDIO: return L"Audio";
    case USB_DEVICE_CLASS_COMMUNICATIONS: return L"Communications";
    case USB_DEVICE_CLASS_HUMAN_INTERFACE: return L"HumanInterface";
    case USB_DEVICE_CLASS_MONITOR: return L"Monitor";
    case USB_DEVICE_CLASS_PHYSICAL_INTERFACE: return L"PhysicalInterface";
    case USB_DEVICE_CLASS_POWER: return L"Power";
    case USB_DEVICE_CLASS_PRINTER: return L"Printer";
    case USB_DEVICE_CLASS_STORAGE: return L"Storage";
    case USB_DEVICE_CLASS_HUB: return L"Hub";
    case USB_DEVICE_CLASS_CDC_DATA: return L"CdcData";
    case USB_DEVICE_CLASS_SMART_CARD: return L"SmartCard";
    case USB_DEVICE_CLASS_CONTENT_SECURITY: return L"ContentSecurity";
    case USB_DEVICE_CLASS_VIDEO: return L"Video";
    case USB_DEVICE_CLASS_PERSONAL_HEALTHCARE: return L"PersonalHealthcare";
    case USB_DEVICE_CLASS_AUDIO_VIDEO: return L"AudioVideo";
    case USB_DEVICE_CLASS_BILLBOARD: return L"Billboard";
    case USB_DEVICE_CLASS_DIAGNOSTIC_DEVICE: return L"DiagnosticDevice";
    case USB_DEVICE_CLASS_WIRELESS_CONTROLLER: return L"WirelessController";
    case USB_DEVICE_CLASS_MISCELLANEOUS: return L"Miscellaneous";
    case USB_DEVICE_CLASS_APPLICATION_SPECIFIC: return L"ApplicationSpecific";
    case USB_DEVICE_CLASS_VENDOR_SPECIFIC: return L"VendorSpecific";
    default: return L"Unknown";
    }
}

static void
PrintInterfaceDesc(int level, PUSB_COMMON_DESCRIPTOR commD)
{
    assert(commD->bLength == sizeof(USB_INTERFACE_DESCRIPTOR));

    PUSB_INTERFACE_DESCRIPTOR id = (PUSB_INTERFACE_DESCRIPTOR)commD;
    
    WWPrintIndentSpace(level);
    printf("Interface #%d alt#%d endpoint#%d %S\n", id->bInterfaceNumber, id->bAlternateSetting,
        id->bNumEndpoints, InterfaceClassToStr(id->bInterfaceClass));
}

static const std::string
DescriptorTypeToStr(int c)
{
    switch (c) {
    case USB_DEVICE_DESCRIPTOR_TYPE: return "Device";
    case USB_CONFIGURATION_DESCRIPTOR_TYPE: return "Configuration";
    case USB_STRING_DESCRIPTOR_TYPE: return "String"; 
    case USB_INTERFACE_DESCRIPTOR_TYPE: return "Interface";
    case USB_ENDPOINT_DESCRIPTOR_TYPE: return "Endpoint";         

    case USB_DEVICE_QUALIFIER_DESCRIPTOR_TYPE: return "DeviceQualifier";              
    case USB_OTHER_SPEED_CONFIGURATION_DESCRIPTOR_TYPE: return "OtherSpeedConfiguration";    
    case USB_INTERFACE_POWER_DESCRIPTOR_TYPE: return "InterfacePower";        
    case USB_OTG_DESCRIPTOR_TYPE: return "OTG";                               
    case USB_DEBUG_DESCRIPTOR_TYPE: return "Debug";                              

    case USB_INTERFACE_ASSOCIATION_DESCRIPTOR_TYPE: return "InterfaceAssociation";           
    case USB_BOS_DESCRIPTOR_TYPE: return "BOS";                  
    case USB_DEVICE_CAPABILITY_DESCRIPTOR_TYPE: return "DeviceCapability";                  
    case USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR_TYPE: return "SuperspeedEndpointCompanion";        
    case USB_SUPERSPEEDPLUS_ISOCH_ENDPOINT_COMPANION_DESCRIPTOR_TYPE : return "SuperspeedPlusIsochEndpointCompanion";

    case WW_USB_HID_DESCRIPTOR_TYPE: return "HID";
    case WW_USB_REPORT_DESCRIPTOR_TYPE: return "Report";
    default:
        {
            char s[256];
            memset(s, 0, sizeof s);
            sprintf_s(s, "Unknown(0x%x)", c);
            return std::string(s);
        }
    }
}


static void
PrintConfDesc(int level, PUSB_COMMON_DESCRIPTOR d, std::vector<WWStringDesc> &sds)
{
    if (d->bLength < sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
        return;
    }

    PUSB_CONFIGURATION_DESCRIPTOR p = (PUSB_CONFIGURATION_DESCRIPTOR)d;
    WWPrintIndentSpace(level);
    printf("Configuration #%d %S nInterfaces=%d\n",
        p->bConfigurationValue, WWStringDescFindString(sds, p->iConfiguration), p->bNumInterfaces);
}

static void
PrintHidDesc(int level, PUSB_COMMON_DESCRIPTOR cd)
{

    PWW_USB_HID_DESCRIPTOR d = (PWW_USB_HID_DESCRIPTOR)cd;
    WWPrintIndentSpace(level);
    printf("HID bcdHID=%x %s\n", d->bcdHID, CountryCodeToStr(d->bCountryCode).c_str());
    for (int i = 0; i < d->bNumDescriptors; ++i) {
        WWPrintIndentSpace(level);
        printf("%s bytes=%d\n", DescriptorTypeToStr(d->OptionalDescriptors[i].bDescriptorType).c_str(), d->OptionalDescriptors[i].wDescriptorLength);
    }
}

static void
PrintOtherDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
    WWPrintIndentSpace(level);
    printf("Other %s\n", DescriptorTypeToStr(d->bDescriptorType).c_str());
}

static void
PrintDeviceDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
    PUSB_DEVICE_DESCRIPTOR p = (PUSB_DEVICE_DESCRIPTOR)d;

    WWPrintIndentSpace(level);
    printf("Device %0x %s\n", p->bcdUSB, DeviceClassToStr(p->bDeviceClass).c_str());
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// BOS desc

// https://github.com/microsoft/Windows-driver-samples/blob/master/usb/usbview/display.c
// USB 3.2 specification Revision 1.0 9.6.2.2


static void
PrintUsb20ExtensionCapabilityDesc(int level, PUSB_DEVICE_CAPABILITY_USB20_EXTENSION_DESCRIPTOR d)
{
    WWPrintIndentSpace(level);
    printf("USB2.0Extension LinkPowerManagementSupported=%d\n",
        0!=(d->bmAttributes.LPMCapable));
}

static std::string
SpeedsSupportedToStr(int a)
{
    std::string s;
    if (a & 1) {
        s.append("LowSpeed,");
    }
    if (a & 2) {
        s.append("FullSpeed,");
    }
    if (a & 4) {
        s.append("HighSpeed,");
    }
    if (a & 8) {
        s.append("Gen1Speed,");
    }
    return s;
}

static const char *
FunctionalitySupportToStr(int a)
{
    switch (a) {
    case 0:
        return "LowSpeed";
    case 1:
        return "FullSpeed";
    case 2:
        return "HighSpeed";
    case 3:
        return "Gen1Speed";
    default:
        return "Unknown";
    }
}

static void
PrintSuperSpeedCapabilityDesc(int level, PUSB_DEVICE_CAPABILITY_SUPERSPEED_USB_DESCRIPTOR d)
{
    WWPrintIndentSpace(level);
    printf("SuperSpeedUSB LinkPowerManagementSupported=%d SpeedsSupported=%s LowestSpeed=%s DeviceExitLatency U1=%dus U2=%dus\n",
        0 != (d->bmAttributes &2),
        SpeedsSupportedToStr(d->wSpeedsSupported).c_str(),
        FunctionalitySupportToStr(d->bFunctionalitySupport),
        d->bU1DevExitLat,
        d->wU2DevExitLat);
}

static const char *
LinkProtocolToStr(int a)
{
    switch (a) {
    case 0:
        return "SuperSpeed";
    case 1:
        return "SuperSpeedPlus";
    default:
        return "Unknown";
    }
}

static std::string
LaneSpeedToStr(USB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_SPEED &a)
{
    char buf[256];
    memset(buf, 0, sizeof buf);

    switch (a.LaneSpeedExponent) {
    case 0:
        sprintf_s(buf, "%dbps", a.LaneSpeedMantissa);
        break;
    case 1:
        sprintf_s(buf, "%dKbps", a.LaneSpeedMantissa);
        break;
    case 2:
        sprintf_s(buf, "%dMbps", a.LaneSpeedMantissa);
        break;
    case 3:
        sprintf_s(buf, "%dGbps", a.LaneSpeedMantissa);
        break;
    }
    return std::string(buf);
}

static std::string
SubLinkTypeToStr(USB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_SPEED &a)
{
    std::string s;

    switch (a.SublinkTypeDir) {
    case 0:
        s.append("Direction=Rx,");
        break;
    case 1:
        s.append("Direction=Tx,");
        break;
    }

    switch (a.SublinkTypeMode) {
    case 0:
        s.append("Symmetric");
        break;
    case 1:
        s.append("Asymmetric");
        break;
    }

    return s;
}


static void
PrintSuperSpeedPlusCapabilityDesc(int level, PUSB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB_DESCRIPTOR d)
{
    int ssaCount = d->bmAttributes.SublinkSpeedAttrCount;
    int ssiCount = d->bmAttributes.SublinkSpeedIDCount;
    int minRxLanes = d->wFunctionalitySupport.MinRxLaneCount;
    int minTxLanes = d->wFunctionalitySupport.MinTxLaneCount;
    int minLaneSpeed = d->wFunctionalitySupport.SublinkSpeedAttrID;

    WWPrintIndentSpace(level);
    printf("SuperSpeedPlusUSB ");
    for (int i = 0; i < ssaCount; ++i) {
        auto & a = d->bmSublinkSpeedAttr[i];
        printf("(ID=%d LinkProtocol=%s LaneSpeed=%s SubLinkType=%s) ",
            a.SublinkSpeedAttrID,
            LinkProtocolToStr(a.LinkProtocol),
            LaneSpeedToStr(a).c_str(),
            SubLinkTypeToStr(a).c_str());
    }

    printf("\n");
}

/// @return bMaxBurst
static int
PrintSuperSpeedEndpointCompanionDesc(int level, int endpointType, PUSB_COMMON_DESCRIPTOR cd)
{
    PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR d = (PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR)cd;

    WWPrintIndentSpace(level);
    printf("SuperSpeedEndopintCompanion MaxBurst=%dpackets bmAttributes=0x%02x ",
        d->bMaxBurst + 1,
        d->wBytesPerInterval);

    switch (endpointType) {
    case USB_ENDPOINT_TYPE_BULK:
        printf("MaxStreams=%d bytesPerInterval=%d", d->bmAttributes.Bulk.MaxStreams, d->wBytesPerInterval);
        break;
    case USB_ENDPOINT_TYPE_CONTROL:
    case USB_ENDPOINT_TYPE_INTERRUPT:
        printf("bytesPerInterval=%d", d->wBytesPerInterval);
        break;
    case USB_ENDPOINT_TYPE_ISOCHRONOUS:
        if (!d->bmAttributes.Isochronous.SspCompanion) {
            printf("MaxNumberOfPacketsOnServiceStream=%d", (d->bMaxBurst + 1) * (d->bmAttributes.Isochronous.Mult + 1));
        }
        break;
    }

    printf("\n");

    return d->bMaxBurst;
}

static std::string
DeviceCapabilityTypeToStr(int t)
{
	switch (t) {
	case USB_DEVICE_CAPABILITY_WIRELESS_USB: return "Wireless USB";
	case USB_DEVICE_CAPABILITY_USB20_EXTENSION: return "USB20 Extension";
	case USB_DEVICE_CAPABILITY_SUPERSPEED_USB: return "SuperSpeed USB";
	case USB_DEVICE_CAPABILITY_CONTAINER_ID: return "Container ID";
	case USB_DEVICE_CAPABILITY_PLATFORM: return "Platform";
	case USB_DEVICE_CAPABILITY_POWER_DELIVERY: return "Power Delivery";
	case USB_DEVICE_CAPABILITY_BATTERY_INFO: return "Battery Info";
	case USB_DEVICE_CAPABILITY_PD_CONSUMER_PORT: return "PD Consumer Port";
	case USB_DEVICE_CAPABILITY_PD_PROVIDER_PORT: return "PD Provider Port";
	case USB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB: return "SuperSpeedPlus USB";
	case USB_DEVICE_CAPABILITY_PRECISION_TIME_MEASUREMENT: return "Precision Timer Measurement";
	case USB_DEVICE_CAPABILITY_BILLBOARD: return "Billboard";
	case USB_DEVICE_CAPABILITY_FIRMWARE_STATUS: return "Firmware Status";
	default:
		{
			char s[256];
			sprintf_s(s, "UnknownCapabilityType %d", t);
			return std::string(s);
		}
	}
}


static void
PrintCapabilityDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
    PUSB_DEVICE_CAPABILITY_DESCRIPTOR capD = (PUSB_DEVICE_CAPABILITY_DESCRIPTOR)d;
    switch (capD->bDevCapabilityType) {
    case USB_DEVICE_CAPABILITY_USB20_EXTENSION:
        PrintUsb20ExtensionCapabilityDesc(level, (PUSB_DEVICE_CAPABILITY_USB20_EXTENSION_DESCRIPTOR)d);
        break;
    case USB_DEVICE_CAPABILITY_SUPERSPEED_USB:
        PrintSuperSpeedCapabilityDesc(level, (PUSB_DEVICE_CAPABILITY_SUPERSPEED_USB_DESCRIPTOR)d);
        break;
    case USB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB:
        PrintSuperSpeedPlusCapabilityDesc(level, (PUSB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB_DESCRIPTOR)d);
        break;
	default:
        WWPrintIndentSpace(level);
        printf("%s\n",
            DeviceCapabilityTypeToStr(capD->bDevCapabilityType).c_str());
        break;
    }
}

static void
PrintOtgDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
    WWPrintIndentSpace(level);
    printf("OTG\n");
}

static void
PrintBosDesc(int level, PUSB_COMMON_DESCRIPTOR d)
{
	PUSB_BOS_DESCRIPTOR bos = (PUSB_BOS_DESCRIPTOR)d;

	WWPrintIndentSpace(level);
	printf("BOS nCaps=%d totalLen=%d\n", bos->bNumDeviceCaps, bos->wTotalLength);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

class DescriptorPrinter {
private:
    int endpointType = 0;
public:
    void PrintDesc(int level, PUSB_COMMON_DESCRIPTOR dFirst, int bytes, std::vector<WWStringDesc> &sds) {
        PUSB_COMMON_DESCRIPTOR d = dFirst;

        do {
            switch (d->bDescriptorType) {
            case USB_CONFIGURATION_DESCRIPTOR_TYPE:
                PrintConfDesc(level, d, sds);
                break;
            case USB_INTERFACE_DESCRIPTOR_TYPE:
                PrintInterfaceDesc(level+1, d);
                break;
            case USB_DEVICE_DESCRIPTOR_TYPE:
                PrintDeviceDesc(level, d);
                break;
            case USB_DEVICE_CAPABILITY_DESCRIPTOR_TYPE:
                PrintCapabilityDesc(level+1, d);
                break;
            case USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR_TYPE:
                PrintSuperSpeedEndpointCompanionDesc(level+1, endpointType, d);
                break;
            case USB_ENDPOINT_DESCRIPTOR_TYPE:
                endpointType = PrintEndpointDesc(level+1, d);
                break;
            case WW_USB_HID_DESCRIPTOR_TYPE:
                PrintHidDesc(level+1, d);
                break;
            case USB_OTG_DESCRIPTOR_TYPE:
                PrintOtgDesc(level + 1, d);
                break;
			case USB_BOS_DESCRIPTOR_TYPE:
				PrintBosDesc(level + 0, d);
				break;
            default:
                PrintOtherDesc(level + 1, d);
                break;
            }

            d = WWGetNextDescriptor((PUSB_COMMON_DESCRIPTOR)dFirst, bytes, d, -1);
        } while (d != nullptr);
    }
};

void
WWPrintConfDesc(int level, PUSB_CONFIGURATION_DESCRIPTOR cd, std::vector<WWStringDesc> &sds)
{
    PUSB_COMMON_DESCRIPTOR commD = (PUSB_COMMON_DESCRIPTOR)cd;

    DescriptorPrinter dp;
    dp.PrintDesc(level, commD, cd->wTotalLength, sds);
}

void
WWPrintBosDesc(int level, PUSB_BOS_DESCRIPTOR pbd, std::vector<WWStringDesc> &sds)
{
    PUSB_COMMON_DESCRIPTOR d = (PUSB_COMMON_DESCRIPTOR)pbd;

    DescriptorPrinter dp;
    dp.PrintDesc(level, d, pbd->wTotalLength, sds);
}

