// 日本語。

#include "WWPrintStructs.h"
#include "WWUsbCommon.h"

PUSB_COMMON_DESCRIPTOR
NextDescriptor(PUSB_COMMON_DESCRIPTOR cd)
{
    if (cd->bLength == 0) {
        return nullptr;
    }
    return (PUSB_COMMON_DESCRIPTOR)((PUCHAR)cd + cd->bLength);
}

/// @param firstD very first descriptor
/// @param totalBytes descriptor total bytes
/// @param startD current descriptor
/// @param descType wanted descriptor type. -1 means any type
PUSB_COMMON_DESCRIPTOR
GetNextDescriptor(
    PUSB_COMMON_DESCRIPTOR firstD,
    ULONG totalBytes,
    PUSB_COMMON_DESCRIPTOR startD,
    long descType)
{
    PUSB_COMMON_DESCRIPTOR curD = nullptr;
    PUSB_COMMON_DESCRIPTOR endD = nullptr;

    endD = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)firstD + totalBytes);

    if (endD <= startD || endD <= NextDescriptor(startD)) {
        return nullptr;
    }

    if (descType == -1) {
        return NextDescriptor(startD);
    }

    curD = startD;
    while (((curD = NextDescriptor(curD)) < endD)
        && curD != nullptr) {
        if (curD->bDescriptorType == (UCHAR)descType) {
            return curD;
        }
    }
    return nullptr;
}

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
PrintEndpointDescriptor(int level, PUSB_ENDPOINT_DESCRIPTOR cd)
{
    WWEndPointDirection dir = EndPointAddressToDir(cd->bEndpointAddress);
    int epAddr = cd->bEndpointAddress & USB_ENDPOINT_ADDRESS_MASK;
    UCHAR epType = cd->bmAttributes & USB_ENDPOINT_TYPE_MASK;
    UCHAR sync = USB_ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION(cd->bmAttributes);
    WWIsocEndpointUsage usage = EndPointAttrToUsage(cd->bmAttributes);

    switch (epType) {
    case USB_ENDPOINT_TYPE_ISOCHRONOUS:
        WWPrintIndentSpace(level);
        printf("Addr=%2d Isochronous %S %S %S Endpoint. MaxPacketSize=%d\n",
            epAddr,
            SyncTypeToStr(sync), WWIsocEndpointUsageToStr(usage), WWEndPointDirectionToStr(dir),
            cd->wMaxPacketSize);
            break;
    case USB_ENDPOINT_TYPE_BULK:
        WWPrintIndentSpace(level);
        printf("Addr=%2d Bulk transfer %S Endpoint. MaxPacketSize=%d\n",
            epAddr, WWEndPointDirectionToStr(dir), cd->wMaxPacketSize);
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

static const wchar_t *
DescriptorTypeToStr(int c)
{
    switch (c) {
    case USB_DEVICE_DESCRIPTOR_TYPE: return L"Device";
    case USB_CONFIGURATION_DESCRIPTOR_TYPE: return L"Configuration";
    case USB_STRING_DESCRIPTOR_TYPE: return L"String"; 
    case USB_INTERFACE_DESCRIPTOR_TYPE: return L"Interface";
    case USB_ENDPOINT_DESCRIPTOR_TYPE: return L"Endpoint";         
    case USB_DEVICE_QUALIFIER_DESCRIPTOR_TYPE: return L"DeviceQualifier";              
    case USB_OTHER_SPEED_CONFIGURATION_DESCRIPTOR_TYPE: return L"OtherSpeedConfiguration";    
    case USB_INTERFACE_POWER_DESCRIPTOR_TYPE: return L"InterfacePower";        
    case USB_OTG_DESCRIPTOR_TYPE: return L"OTG";                               
    case USB_DEBUG_DESCRIPTOR_TYPE: return L"Debug";                              
    case USB_INTERFACE_ASSOCIATION_DESCRIPTOR_TYPE: return L"InterfaceAssociation";           
    case USB_BOS_DESCRIPTOR_TYPE: return L"BOS";                  
    case USB_DEVICE_CAPABILITY_DESCRIPTOR_TYPE: return L"DeviceCapability";                  
    case USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR_TYPE: return L"SuperspeedEndpointCompanion";        
    case USB_SUPERSPEEDPLUS_ISOCH_ENDPOINT_COMPANION_DESCRIPTOR_TYPE : return L"SuperspeedPlusIsochEndpointCompanion";
    default:
        return L"Unknown";
    }
}


static void
PrintConfDesc(int level, PUSB_COMMON_DESCRIPTOR commD, std::vector<WWStringDesc> &sds)
{
    if (commD->bLength < sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
        return;
    }

    PUSB_CONFIGURATION_DESCRIPTOR p = (PUSB_CONFIGURATION_DESCRIPTOR)commD;
    WWPrintIndentSpace(level);
    printf("Configuration #%d %S nInterfaces=%d\n",
        p->bConfigurationValue, WWStringDescFindString(sds, p->iConfiguration), p->bNumInterfaces);
}

void
WWPrintConfDesc(int level, bool isSS, PUSB_CONFIGURATION_DESCRIPTOR cd, std::vector<WWStringDesc> &sds)
{
    PUSB_COMMON_DESCRIPTOR commD = (PUSB_COMMON_DESCRIPTOR)cd;

    do {
        switch (commD->bDescriptorType) {
        case USB_CONFIGURATION_DESCRIPTOR_TYPE:
            PrintConfDesc(level, commD, sds);
            break;
        case USB_INTERFACE_DESCRIPTOR_TYPE:
            PrintInterfaceDesc(level, commD);
            break;
        }

        commD = GetNextDescriptor((PUSB_COMMON_DESCRIPTOR)cd, cd->wTotalLength, commD, -1);
    } while (commD != nullptr);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// BOS desc

// https://github.com/microsoft/Windows-driver-samples/blob/master/usb/usbview/display.c
// USB 3.2 specification Revision 1.0 9.6.2.2

static PUSB_COMMON_DESCRIPTOR
NextDescriptor(
        PUSB_COMMON_DESCRIPTOR firstD,
        int totalLength,
        PUSB_COMMON_DESCRIPTOR startD)
{
    PUSB_COMMON_DESCRIPTOR curD = nullptr;
    PUSB_COMMON_DESCRIPTOR endD = nullptr;

    endD = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)firstD + totalLength);

    if (startD >= endD ||
            NextDescriptor(startD) >= endD) {
        return nullptr;
    }

    return NextDescriptor(startD);
}

static void
PrintUsb20ExtensionCapabilityDescriptor(int level, PUSB_DEVICE_CAPABILITY_USB20_EXTENSION_DESCRIPTOR d)
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
PrintSuperSpeedCapabilityDescriptor(int level, PUSB_DEVICE_CAPABILITY_SUPERSPEED_USB_DESCRIPTOR d)
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
PrintSuperSpeedPlusCapabilityDescriptor(int level, PUSB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB_DESCRIPTOR d)
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
PrintSuperSpeedEndpointCompanionDescriptor(int level, int endpointType, PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR d)
{
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

void
WWPrintBosDesc(int level, PUSB_BOS_DESCRIPTOR pbd)
{
    PUSB_COMMON_DESCRIPTOR            d    = (PUSB_COMMON_DESCRIPTOR)pbd;
    PUSB_DEVICE_CAPABILITY_DESCRIPTOR capD = nullptr;

    WWPrintIndentSpace(level);
    printf("BOS bLength=%d bDescriptorType=%d wTotalLength=%d bNumDeviceCaps=%d\n",
        pbd->bLength, pbd->bDescriptorType, pbd->wTotalLength, pbd->bNumDeviceCaps);

    while ((d = NextDescriptor((PUSB_COMMON_DESCRIPTOR)pbd, pbd->wTotalLength, d)) != nullptr) {
        int endpointType = 0;

        switch (d->bDescriptorType) {
        case USB_DEVICE_CAPABILITY_DESCRIPTOR_TYPE:
            capD = (PUSB_DEVICE_CAPABILITY_DESCRIPTOR)d;
            switch (capD->bDevCapabilityType) {
            case USB_DEVICE_CAPABILITY_USB20_EXTENSION:
                PrintUsb20ExtensionCapabilityDescriptor(level+1, (PUSB_DEVICE_CAPABILITY_USB20_EXTENSION_DESCRIPTOR)d);
                break;
            case USB_DEVICE_CAPABILITY_SUPERSPEED_USB:
                PrintSuperSpeedCapabilityDescriptor(level + 1, (PUSB_DEVICE_CAPABILITY_SUPERSPEED_USB_DESCRIPTOR)d);
                break;
            case USB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB:
                PrintSuperSpeedPlusCapabilityDescriptor(level + 1, (PUSB_DEVICE_CAPABILITY_SUPERSPEEDPLUS_USB_DESCRIPTOR)d);
                break;
            }
            break;
        case USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR_TYPE:
            PrintSuperSpeedEndpointCompanionDescriptor(level + 1, endpointType, (PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR)d);
            break;
        case USB_INTERFACE_DESCRIPTOR_TYPE:
            PrintInterfaceDesc(level + 1, d);
            break;
        case USB_ENDPOINT_DESCRIPTOR_TYPE:
            endpointType = PrintEndpointDescriptor(level + 1, (PUSB_ENDPOINT_DESCRIPTOR)d);
            break;
        default:
            //printf("SkipDesc=%S ", DescriptorTypeToStr(d->bDescriptorType));
            break;
        }
    }
}

