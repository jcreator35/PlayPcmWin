// 日本語。

#include "WWUsbCommon.h"
#include "WWUtil.h"

#define SIZEOF_USB_INTERFACE_DESCRIPTOR2 (11)

std::vector<WWUsbDevice>  mDevices;

const wchar_t *
WWUsbDeviceBusSpeedToStr(WWUsbDeviceBusSpeed t)
{
    switch (t) {
    case WWUDB_RootHub: return L"RootHub";
    case WWUDB_LowSpeed: return L"LowSpeed(0.19MB/s)";
    case WWUDB_FullSpeed: return L"FullSpeed(1.5MB/s)";
    case WWUDB_HighSpeed: return L"HighSpeed(60MB/s)";
    case WWUDB_SuperSpeed: return L"SuperSpeed(625MB/s)";
    case WWUDB_SuperSpeedPlus: return L"SuperSpeed+(1.25GB/s～)";
    default:
        assert(0);
        return L"Unknown";
    }
}

WWUsbDeviceBusSpeed
WWUsbHubTypeToWWUsbDeviceBusSpeed(USB_HUB_TYPE t)
{
    switch (t) {
    case UsbRootHub:
        return WWUDB_RootHub;
    case Usb20Hub:
        return WWUDB_HighSpeed;
    case Usb30Hub:
        return WWUDB_SuperSpeed;
    default:
        assert(0);
        return WWUDB_FullSpeed;
    }
}

WWUsbDeviceBusSpeed
WWUsbDeviceSpeedToWWUsbDeviceBusSpeed(USB_DEVICE_SPEED ds)
{
    switch (ds) {
    case UsbLowSpeed:
        return WWUDB_LowSpeed;
    case UsbFullSpeed:
        return WWUDB_FullSpeed;
    case UsbHighSpeed:
        return WWUDB_HighSpeed;
    case UsbSuperSpeed:
        return WWUDB_SuperSpeed;
    default:
        assert(0);
        return WWUDB_FullSpeed;
    }
}

static HRESULT
GetDeviceProperty(
        HDEVINFO         devInf,
        SP_DEVINFO_DATA  &sdd,
        DWORD            propIdx,
        std::wstring  &s_r)
{
    HRESULT hr = E_FAIL;
    DWORD len = 0;
    int bytes = 0;
    BOOL brv = FALSE;
    PWSTR buff = nullptr;

    s_r = L"";

    brv = SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, propIdx, nullptr, nullptr, 0, &len);
    DWORD lastError = GetLastError();
    if (brv != FALSE && lastError != ERROR_INSUFFICIENT_BUFFER) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    if (len == 0) {
        hr = E_FAIL;
        goto end;
    }
    
    // lenは文字数。バイト数に変換する。
    // 終端文字の領域を念のために余計に取っておく。
    bytes = 2 * (len + 1);
    ALLOC_MEM(buff, PWSTR, bytes);

    brv = SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, propIdx, nullptr, (PBYTE)buff, len, &len);
    if (!brv) {
        printf("brv=%d, len=%d\n", brv, len);
        hr = E_FAIL;
        goto end;
    }

    s_r = std::wstring(buff);
    hr = S_OK;
end:

    free(buff);
    buff = nullptr;

    return hr;
}

static HRESULT
DriverNameToDeviceInst(
        std::wstring driverName,
        HDEVINFO *pDevInfo_r,
        SP_DEVINFO_DATA &sdd_r)
{
    HRESULT hr = E_FAIL;
    HDEVINFO         devInf = INVALID_HANDLE_VALUE;
    ULONG devIdx = 0;
    BOOL             brv = TRUE;
    SP_DEVINFO_DATA  sdd;

    if (pDevInfo_r == nullptr) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        return E_FAIL;
    }

    *pDevInfo_r = INVALID_HANDLE_VALUE;
    memset(&sdd_r, 0, sizeof sdd_r);

    devInf = SetupDiGetClassDevsW(nullptr, nullptr, nullptr, DIGCF_PRESENT | DIGCF_ALLCLASSES);
    if (devInf == INVALID_HANDLE_VALUE) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }

    for (devIdx = 0; ; ++devIdx) {
        memset(&sdd, 0, sizeof sdd);
        sdd.cbSize = sizeof(sdd);
        BHRG(SetupDiEnumDeviceInfo(devInf, devIdx, &sdd));

        std::wstring dn;
        hr = GetDeviceProperty(devInf, sdd, SPDRP_DRIVER, dn);
        if (SUCCEEDED(hr) && _wcsicmp(driverName.c_str(), dn.c_str()) == 0) {
            // SUCCESS !!
            break;
        }
    }

    // ここに来るのは成功。
    hr = S_OK;
    *pDevInfo_r = devInf;
    memcpy(&sdd_r, &sdd, sizeof(sdd));

end:
    if (FAILED(hr) && devInf != INVALID_HANDLE_VALUE) {
        SetupDiDestroyDeviceInfoList(devInf);
    }

    return hr;
}

static HRESULT
GetDeviceInstanceId(HDEVINFO devInf, SP_DEVINFO_DATA &sdd, std::wstring &s_r)
{
    s_r = L"";
    PWSTR buff = nullptr;
    DWORD len = 0;
    int bytes = 0;
    HRESULT hr = E_FAIL;
    BOOL brv = FALSE;

    brv = SetupDiGetDeviceInstanceIdW(devInf, &sdd, nullptr, 0, &len);
    DWORD lastError = GetLastError();
    if (brv != FALSE && lastError != ERROR_INSUFFICIENT_BUFFER) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    // lenは文字数。バイト数に変換する。
    // 終端文字の領域を念のために余計に取っておく。
    bytes = 2 * (len + 1);
    ALLOC_MEM(buff, PWSTR, bytes);
    BHRG(SetupDiGetDeviceInstanceIdW(devInf, &sdd, buff, len, &len));

    s_r = std::wstring(buff);
    hr = S_OK;
end:
    return hr;
}

HRESULT
WWDriverKeyNameToDeviceStrings(std::wstring driverName, WWUsbDeviceStrings &uds_r)
{
    HDEVINFO        devInf = INVALID_HANDLE_VALUE;
    SP_DEVINFO_DATA sdd = { 0 };
    HRESULT         hr = E_FAIL;

    HRG(DriverNameToDeviceInst(driverName, &devInf, sdd));

    GetDeviceInstanceId(devInf, sdd, uds_r.deviceId);
    GetDeviceProperty(devInf, sdd, SPDRP_DEVICEDESC, uds_r.deviceDesc);
    GetDeviceProperty(devInf, sdd, SPDRP_HARDWAREID, uds_r.hwId);
    GetDeviceProperty(devInf, sdd, SPDRP_SERVICE, uds_r.service);
    GetDeviceProperty(devInf, sdd, SPDRP_CLASS, uds_r.deviceClass);

    hr = S_OK;
end:

    if (devInf != INVALID_HANDLE_VALUE) {
        SetupDiDestroyDeviceInfoList(devInf);
        devInf = INVALID_HANDLE_VALUE;
    }

    return hr;
}

// @return pdr_r free()で開放して下さい。
HRESULT
WWGetConfigDescriptor(
        HANDLE  hHub,
        ULONG   connIdx,
        UCHAR   descIdx,
        PUSB_DESCRIPTOR_REQUEST *pdr_r)
{
    HRESULT hr = E_FAIL;
    BOOL    success = 0;
    DWORD   bytes = 0;
    PUSB_DESCRIPTOR_REQUEST         pdr = nullptr;
    PUSB_CONFIGURATION_DESCRIPTOR   pcd = nullptr; //< pdrの途中を指すポインター。

    // 最初はサイズを取得 ⇒ pcd->wTotalLength
    bytes = sizeof(USB_DESCRIPTOR_REQUEST) + sizeof(USB_CONFIGURATION_DESCRIPTOR);
    ALLOC_MEM(pdr, PUSB_DESCRIPTOR_REQUEST, bytes);
    pcd = (PUSB_CONFIGURATION_DESCRIPTOR)(pdr + 1);
    pdr->ConnectionIndex = connIdx;
    pdr->SetupPacket.wValue = (USB_CONFIGURATION_DESCRIPTOR_TYPE << 8) | descIdx;
    pdr->SetupPacket.wLength = (USHORT)(bytes - sizeof(USB_DESCRIPTOR_REQUEST));
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, pdr, bytes, pdr, bytes, &bytes, nullptr));
    if (bytes != sizeof(USB_DESCRIPTOR_REQUEST) + sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    if (pcd->wTotalLength < sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }

    // 領域を確保して内容取得。
    free(pdr);
    pdr = nullptr;
    bytes = sizeof(USB_DESCRIPTOR_REQUEST) + pcd->wTotalLength;
    ALLOC_MEM(pdr, PUSB_DESCRIPTOR_REQUEST, bytes);
    pcd = (PUSB_CONFIGURATION_DESCRIPTOR)(pdr + 1);
    pdr->ConnectionIndex = connIdx;
    pdr->SetupPacket.wValue = (USB_CONFIGURATION_DESCRIPTOR_TYPE << 8) | descIdx;
    pdr->SetupPacket.wLength = (USHORT)(bytes - sizeof(USB_DESCRIPTOR_REQUEST));
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, pdr, bytes, pdr, bytes, &bytes, nullptr));
    if (bytes != sizeof(USB_DESCRIPTOR_REQUEST) + pcd->wTotalLength) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    if (pcd->wTotalLength != (bytes - sizeof(USB_DESCRIPTOR_REQUEST))) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    *pdr_r = pdr;
    hr = S_OK;

end:
    if (FAILED(hr)) {
        free(pdr);
        pdr = nullptr;
        pcd = nullptr;
        *pdr_r = nullptr;
    }

    return hr;
}

HRESULT
WWGetBOSDescriptor(
        HANDLE  hHub,
        ULONG   connIdx,
        PUSB_DESCRIPTOR_REQUEST *pdr_r)
{
    HRESULT hr = E_FAIL;
    DWORD   bytes = 0;
    PUSB_DESCRIPTOR_REQUEST pdr = nullptr;
    PUSB_BOS_DESCRIPTOR     pbd = nullptr; // pdrの途中を指すポインター。

    // サイズを取得 ⇒ pbd->wTotalLength
    bytes = sizeof(USB_DESCRIPTOR_REQUEST) + sizeof(USB_BOS_DESCRIPTOR);
    ALLOC_MEM(pdr, PUSB_DESCRIPTOR_REQUEST, bytes);
    pbd = (PUSB_BOS_DESCRIPTOR)(pdr + 1);
    pdr->ConnectionIndex = connIdx;
    pdr->SetupPacket.wValue = (USB_BOS_DESCRIPTOR_TYPE << 8);
    pdr->SetupPacket.wLength = (USHORT)(bytes - sizeof(USB_DESCRIPTOR_REQUEST));
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, pdr, bytes, pdr, bytes, &bytes, nullptr));
    if (bytes != sizeof(USB_DESCRIPTOR_REQUEST) + sizeof(USB_BOS_DESCRIPTOR)) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    if (pbd->wTotalLength < sizeof(USB_BOS_DESCRIPTOR)) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }

    // 領域を確保して内容取得。
    free(pdr);
    pdr = nullptr;
    bytes = sizeof(USB_DESCRIPTOR_REQUEST) + pbd->wTotalLength;
    ALLOC_MEM(pdr, PUSB_DESCRIPTOR_REQUEST, bytes);
    pbd = (PUSB_BOS_DESCRIPTOR)(pdr + 1);
    pdr->ConnectionIndex = connIdx;
    pdr->SetupPacket.wValue = (USB_BOS_DESCRIPTOR_TYPE << 8);
    pdr->SetupPacket.wLength = (USHORT)(bytes - sizeof(USB_DESCRIPTOR_REQUEST));
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, pdr, bytes, pdr, bytes, &bytes, nullptr));
    if (bytes != sizeof(USB_DESCRIPTOR_REQUEST) + pbd->wTotalLength) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    if (pbd->wTotalLength != (bytes - sizeof(USB_DESCRIPTOR_REQUEST))) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }
    *pdr_r = pdr;
    hr = S_OK;

end:
    if (FAILED(hr)) {
        free(pdr);
        pdr = nullptr;
        pbd = nullptr;
        *pdr_r = nullptr;
    }

    return hr;
}

bool
WWStringDescAvailable(
        PUSB_DEVICE_DESCRIPTOR          dd,
        PUSB_CONFIGURATION_DESCRIPTOR   cd)
{
    PUCHAR                  descEnd = nullptr;
    PUSB_COMMON_DESCRIPTOR  comm = nullptr;

    if (dd->iManufacturer || dd->iProduct || dd->iSerialNumber) {
        return true;
    }

    descEnd = (PUCHAR)cd + cd->wTotalLength;
    comm = (PUSB_COMMON_DESCRIPTOR)cd;

    while ((PUCHAR)comm + sizeof(USB_COMMON_DESCRIPTOR) < descEnd && (PUCHAR)comm + comm->bLength <= descEnd) {
        switch (comm->bDescriptorType) {
        case USB_CONFIGURATION_DESCRIPTOR_TYPE:
        case USB_OTHER_SPEED_CONFIGURATION_DESCRIPTOR_TYPE:
            if (comm->bLength != sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
                return false;
            }
            if (((PUSB_CONFIGURATION_DESCRIPTOR)comm)->iConfiguration) {
                return true;
            }
            comm = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)comm + comm->bLength);
            continue;

        case USB_INTERFACE_DESCRIPTOR_TYPE:
            if (comm->bLength != sizeof(USB_INTERFACE_DESCRIPTOR) &&
                comm->bLength != SIZEOF_USB_INTERFACE_DESCRIPTOR2) {
                return false;
            }
            if (((PUSB_INTERFACE_DESCRIPTOR)comm)->iInterface) {
                return true;
            }
            comm = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)comm + comm->bLength);
            continue;

        default:
            comm = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)comm + comm->bLength);
            continue;
        }
        break;
    }

    return false;
}

static HRESULT
GetStringDesc(
        HANDLE  hHub,
        ULONG   connIdx,
        UCHAR   descIdx,
        USHORT  langId,
        WWStringDesc &sd_r)
{
    HRESULT hr = E_FAIL;
    DWORD   bytes = 0;

    PUSB_DESCRIPTOR_REQUEST dr = nullptr;
    PUSB_STRING_DESCRIPTOR  sd = nullptr; // drの途中を指すポインター。

    bytes = sizeof(USB_DESCRIPTOR_REQUEST) + MAXIMUM_USB_STRING_LENGTH;
    ALLOC_MEM(dr, PUSB_DESCRIPTOR_REQUEST, bytes);
    sd = (PUSB_STRING_DESCRIPTOR)(dr + 1);
    dr->ConnectionIndex = connIdx;
    dr->SetupPacket.wValue = (USB_STRING_DESCRIPTOR_TYPE << 8) | descIdx;
    dr->SetupPacket.wIndex = langId;
    dr->SetupPacket.wLength = (USHORT)(bytes - sizeof(USB_DESCRIPTOR_REQUEST));
    // ↓失敗することがある。
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, dr, bytes, dr, bytes, &bytes, nullptr));
    if (bytes < sizeof(USB_DESCRIPTOR_REQUEST) + 2
            || sd->bDescriptorType != USB_STRING_DESCRIPTOR_TYPE
            || sd->bLength != bytes - sizeof(USB_DESCRIPTOR_REQUEST)
            || sd->bLength % 2 != 0) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }

    sd_r.descType = sd->bDescriptorType;
    sd_r.descIdx = descIdx;
    sd_r.langId = langId;
    sd_r.s = std::wstring(sd->bString);
    hr = S_OK;
end:
    free(dr);
    dr = nullptr;
    sd = nullptr;

    return hr;
}

static HRESULT
GetStringDescs(
        HANDLE hHub,
        ULONG  connIdx,
        UCHAR  descIdx,
        int    numLangIds,
        USHORT *langIds,
        std::vector<WWStringDesc> &desc_io)
{
    int count = 0;
    HRESULT hr = E_FAIL;

    for (size_t i = 0; i < desc_io.size(); ++i) {
        if (desc_io[i].descIdx == descIdx) {
            // 取得済み。
            return S_OK;
        }
    }

    for (int i = 0; i < numLangIds; ++i) {
        WWStringDesc sd;
        hr = GetStringDesc(hHub,
            connIdx,
            descIdx,
            langIds[i],
            sd);
        if (SUCCEEDED(hr)) {
            desc_io.push_back(sd);
            ++count;
        }
    }

    return 0 < count ? S_OK : E_FAIL;
}

HRESULT
WWGetAllStringDescs(
        HANDLE                          hHub,
        ULONG                           connIdx,
        PUSB_DEVICE_DESCRIPTOR          dd,
        PUSB_CONFIGURATION_DESCRIPTOR   cd,
        std::vector<WWStringDesc> &desc_r)
{
    int                   numLangIds = 0;
    USHORT                  *langIds = nullptr;

    PUCHAR                  descEnd = NULL;
    PUSB_COMMON_DESCRIPTOR  commDesc = NULL;
    UCHAR                   uIndex = 1;
    UCHAR                   bInterfaceClass = 0;
    BOOL                    getMoreStrings = FALSE;
    HRESULT                 hr = S_OK;
    WWStringDesc sdLang;

    // Get the array of supported Language IDs, which is returned
    // in String Descriptor 0
    // これは失敗することがある。
    HRG(GetStringDesc(hHub, connIdx, 0, 0, sdLang));
    numLangIds = (int)sdLang.s.length();
    langIds = (USHORT*)sdLang.s.c_str();

    if (dd->iManufacturer) {
        GetStringDescs(hHub, connIdx, dd->iManufacturer, numLangIds, langIds, desc_r);
    }
    if (dd->iProduct) {
        GetStringDescs(hHub, connIdx, dd->iProduct, numLangIds, langIds, desc_r);
    }
    if (dd->iSerialNumber) {
        GetStringDescs(hHub, connIdx, dd->iSerialNumber, numLangIds, langIds, desc_r);
    }

    descEnd = (PUCHAR)cd + cd->wTotalLength;
    commDesc = (PUSB_COMMON_DESCRIPTOR)cd;
    while ((PUCHAR)commDesc + sizeof(USB_COMMON_DESCRIPTOR) < descEnd && (PUCHAR)commDesc + commDesc->bLength <= descEnd) {
        switch (commDesc->bDescriptorType) {
        case USB_CONFIGURATION_DESCRIPTOR_TYPE:
            if (commDesc->bLength != sizeof(USB_CONFIGURATION_DESCRIPTOR)) {
                printf("Error: %s:%d\n", __FILE__, __LINE__);
                hr = E_FAIL;
                goto end;
            }
            if (((PUSB_CONFIGURATION_DESCRIPTOR)commDesc)->iConfiguration) {
                GetStringDescs(hHub, connIdx, ((PUSB_CONFIGURATION_DESCRIPTOR)commDesc)->iConfiguration, numLangIds, langIds, desc_r);
            }
            commDesc = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)commDesc + commDesc->bLength);
            continue;

        case USB_INTERFACE_DESCRIPTOR_TYPE:
            if (commDesc->bLength != sizeof(USB_INTERFACE_DESCRIPTOR)
                    && commDesc->bLength != SIZEOF_USB_INTERFACE_DESCRIPTOR2) {
                printf("Error: %s:%d\n", __FILE__, __LINE__);
                hr = E_FAIL;
                goto end;
            }
            if (((PUSB_INTERFACE_DESCRIPTOR)commDesc)->iInterface) {
                GetStringDescs(hHub, connIdx, ((PUSB_INTERFACE_DESCRIPTOR)commDesc)->iInterface, numLangIds, langIds, desc_r);
            }
            commDesc = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)commDesc + commDesc->bLength);
            continue;

        default:
            commDesc = (PUSB_COMMON_DESCRIPTOR)((PUCHAR)commDesc + commDesc->bLength);
            continue;
        }
        break;
    }

    hr = S_OK;
end:

    return hr;
}
