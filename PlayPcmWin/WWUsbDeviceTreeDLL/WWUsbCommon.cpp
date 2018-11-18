// 日本語。

#include "WWUsbCommon.h"
#include "WWUtil.h"

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
    GetDeviceProperty(devInf, sdd, SPDRP_FRIENDLYNAME, uds_r.friendlyName);
    GetDeviceProperty(devInf, sdd, SPDRP_MFG, uds_r.manufacturer);
    hr = S_OK;
end:

    if (devInf != INVALID_HANDLE_VALUE) {
        SetupDiDestroyDeviceInfoList(devInf);
        devInf = INVALID_HANDLE_VALUE;
    }

    return hr;
}

HRESULT
WWGetTransportCharacteristics(HANDLE h, USB_TRANSPORT_CHARACTERISTICS & tc_r)
{
    HRESULT hr = E_FAIL;
    BOOL brv = FALSE;
    DWORD bytes = 0;

    memset(&tc_r, 0, sizeof tc_r);
    tc_r.Version = USB_TRANSPORT_CHARACTERISTICS_VERSION_1;
    brv = DeviceIoControl(h, IOCTL_USB_GET_TRANSPORT_CHARACTERISTICS,
        &tc_r, sizeof tc_r, &tc_r, sizeof tc_r, &bytes, nullptr);
    if (!brv) {
        printf("Error: Get Transport Characteristics failed\n");

    } else {
        printf("rt_letency=%lldms potentialBandwidth=%lld\n",
            tc_r.CurrentRoundtripLatencyInMilliSeconds, tc_r.MaxPotentialBandwidth);
        hr = S_OK;
    }

    return hr;
}

HRESULT
WWGetDeviceCharacteristics(HANDLE h, USB_DEVICE_CHARACTERISTICS &dc_r)
{
    HRESULT hr = E_FAIL;
    BOOL brv = FALSE;
    DWORD bytes = 0;

    memset(&dc_r, 0, sizeof dc_r);
    dc_r.Version = USB_DEVICE_CHARACTERISTICS_VERSION_1;
    brv = DeviceIoControl(h, IOCTL_USB_GET_DEVICE_CHARACTERISTICS,
        &dc_r, sizeof dc_r, &dc_r, sizeof dc_r, &bytes, nullptr);
    if (!brv) {
        printf("Error: Get Device Characteristics failed\n");

    } else {
        printf("dc MaxSendPathDelay=%ums MaxCompPathDelay=%ums\n",
            dc_r.MaximumSendPathDelayInMilliSeconds,
            dc_r.MaximumCompletionPathDelayInMilliSeconds);
        hr = S_OK;
    }

    return hr;
}

void
WWPrintIndentSpace(int level)
{
    for (int i = 0; i < level; ++i) {
        printf("  ");
    }
}

static int gNextId = -1;

void
WWUsbIdGeneratorReset(void)
{
    gNextId = -1;
}

int
WWUsbIdGenerate(void)
{
    ++gNextId;
    return gNextId;
}


const wchar_t *
WWStringDescFindString(std::vector<WWStringDesc> &sds, int idx)
{
    for (int i = 0; i < sds.size(); ++i) {
        if (sds[i].descIdx == idx) {
            return sds[i].s.c_str();
        }
    }
    return L"";
}
