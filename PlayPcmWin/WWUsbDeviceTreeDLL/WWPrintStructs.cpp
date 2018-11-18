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
static void
PrintEndpointDescriptor(int level, PUSB_ENDPOINT_DESCRIPTOR cd,
    PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR secd,
    PUSB_SUPERSPEEDPLUS_ISOCH_ENDPOINT_COMPANION_DESCRIPTOR siec)
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
ProcInterfaceDesc(int level, bool isSS, PUSB_CONFIGURATION_DESCRIPTOR cd, PUSB_COMMON_DESCRIPTOR commD)
{
    if (commD->bLength != sizeof(USB_INTERFACE_DESCRIPTOR)) {
        return;
    }

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
ProcConfDesc(int level, PUSB_COMMON_DESCRIPTOR commD, std::vector<WWStringDesc> &sds)
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
            ProcConfDesc(level, commD, sds);
            break;
        case USB_INTERFACE_DESCRIPTOR_TYPE:
            ProcInterfaceDesc(level, isSS, cd, commD);
            break;
        }

        commD = GetNextDescriptor((PUSB_COMMON_DESCRIPTOR)cd, cd->wTotalLength, commD, -1);
    } while (commD != nullptr);
}

