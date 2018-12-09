// 日本語。

#include "WWUsbHostController.h"
#include "WWUsbHub.h"

#include <SetupAPI.h>
#include <usbioctl.h>
#include <usb.h>
#include <usbuser.h>
#include <devioctl.h>
#include <strsafe.h>
#include <usbiodef.h>
#include <string>
#include <vector>
#include <assert.h>
#include "WWUtil.h"
#include "WWUsbHub.h"
#include "WWUsbVendorIdToStr.h"

std::vector<WWHostController> mHCs;

void
WWHostControllersClear(void)
{
    mHCs.clear();
}

static HRESULT
GetHostControllerDriverKeyName(HANDLE hcd, std::wstring &guid_r)
{
    PUSB_HCD_DRIVERKEY_NAME puhd = nullptr;
    USB_HCD_DRIVERKEY_NAME  uhd;
    ULONG                   bytes = 0;
    HRESULT                 hr = E_FAIL;

    memset(&uhd, 0, sizeof uhd);
    BHRG(DeviceIoControl(hcd, IOCTL_GET_HCD_DRIVERKEY_NAME, &uhd, sizeof uhd, &uhd, sizeof uhd, &bytes, nullptr));

    bytes = uhd.ActualLength;
    if (bytes <= sizeof uhd) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    // 念の為終端文字の領域を余分に確保。
    bytes += 2;
    ALLOC_MEM(puhd, PUSB_HCD_DRIVERKEY_NAME, bytes);
    BHRG(DeviceIoControl(hcd, IOCTL_GET_HCD_DRIVERKEY_NAME, puhd, bytes, puhd, bytes, &bytes, nullptr));

    guid_r = std::wstring(puhd->DriverKeyName);
    hr = S_OK;
end:

    free(puhd);
    puhd = nullptr;

    return hr;
}

static HRESULT
GetRootHubName(HANDLE hDev, std::wstring &name_r)
{
    PUSB_ROOT_HUB_NAME  prhn = nullptr;
    USB_ROOT_HUB_NAME   rhn;
    ULONG               bytes = 0;
    HRESULT             hr = E_FAIL;

    memset(&rhn, 0, sizeof rhn);
    BHRG(DeviceIoControl(hDev, IOCTL_USB_GET_ROOT_HUB_NAME, 0, 0, &rhn, sizeof rhn, &bytes, nullptr));

    bytes = rhn.ActualLength;
    if (bytes <= sizeof rhn) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    // 念の為終端文字の領域を余分に確保。
    bytes += 2;
    ALLOC_MEM(prhn, PUSB_ROOT_HUB_NAME, bytes);
    BHRG(DeviceIoControl(hDev, IOCTL_USB_GET_ROOT_HUB_NAME, nullptr, 0, prhn, bytes, &bytes, nullptr));

    name_r = std::wstring(prhn->RootHubName);
    hr = S_OK;
end:

    free(prhn);
    prhn = nullptr;

    return hr;
}

static HRESULT
GetControllerInf0(HANDLE h, USB_CONTROLLER_INFO_0 &ci0_r)
{
    HRESULT hr = E_FAIL;
    DWORD bytes = 0;
    USBUSER_CONTROLLER_INFO_0 p;

    memset(&p, 0, sizeof p);
    p.Header.UsbUserRequest = USBUSER_GET_CONTROLLER_INFO_0;
    p.Header.RequestBufferLength = sizeof p;
    BHRG(DeviceIoControl(h, IOCTL_USB_USER_REQUEST, &p, sizeof p, &p, sizeof p, &bytes, nullptr));
    ci0_r = p.Info0;
    hr = S_OK;
end:
    return hr;
}

static HRESULT
GetBandwidthInf(HANDLE h, USB_BANDWIDTH_INFO &bir_r)
{
    HRESULT hr = E_FAIL;
    DWORD bytes = 0;
    USBUSER_BANDWIDTH_INFO_REQUEST p;

    memset(&p, 0, sizeof p);
    p.Header.UsbUserRequest = USBUSER_GET_BANDWIDTH_INFORMATION;
    p.Header.RequestBufferLength = sizeof p;
    BHRG(DeviceIoControl(h, IOCTL_USB_USER_REQUEST, &p,
            sizeof p, &p, sizeof p, &bytes, nullptr));
    bir_r = p.BandwidthInformation;
    hr = S_OK;
end:
    return hr;
}

static HRESULT
GetBusStatistics0(HANDLE h, USB_BUS_STATISTICS_0 &bs0_r)
{
    HRESULT hr = E_FAIL;
    DWORD bytes = 0;
    USBUSER_BUS_STATISTICS_0_REQUEST p;

    memset(&p, 0, sizeof p);
    p.Header.UsbUserRequest = USBUSER_GET_BUS_STATISTICS_0;
    p.Header.RequestBufferLength = sizeof p;
    BHRG(DeviceIoControl(h, IOCTL_USB_USER_REQUEST, &p, sizeof p, &p, sizeof p, &bytes, nullptr));
    bs0_r = p.BusStatistics0;
    hr = S_OK;
end:
    return hr;
}

static HRESULT
GetPowerInf(HANDLE h, USB_POWER_INFO &pir_r)
{
    HRESULT hr = E_FAIL;
    DWORD bytes = 0;
    USBUSER_POWER_INFO_REQUEST p;

    memset(&p, 0, sizeof p);
    p.Header.UsbUserRequest = USBUSER_GET_POWER_STATE_MAP;
    p.Header.RequestBufferLength = sizeof p;
    BHRG(DeviceIoControl(h, IOCTL_USB_USER_REQUEST, &p, sizeof p, &p, sizeof p, &bytes, nullptr));
    pir_r = p.PowerInformation;
    hr = S_OK;
end:
    return hr;
}

static HRESULT
GetDriverVersionInf(HANDLE h, USB_DRIVER_VERSION_PARAMETERS &dvp_r)
{
    HRESULT hr = E_FAIL;
    USBUSER_GET_DRIVER_VERSION p;
    DWORD bytes = 0;

    memset(&p, 0, sizeof p);
    p.Header.UsbUserRequest = USBUSER_GET_USB_DRIVER_VERSION;
    p.Header.RequestBufferLength = sizeof p;
    BHRG(DeviceIoControl(h, IOCTL_USB_USER_REQUEST, &p, sizeof p, &p, sizeof p, &bytes, nullptr));
    dvp_r = p.Parameters;
    hr = S_OK;
end:

    return hr;
}

HRESULT
WWGetHostControllerInf(
        HANDLE hDev,
        HDEVINFO devInf,
        SP_DEVINFO_DATA &sdd)
{
    HRESULT            hr = E_FAIL;
    WWHostController   hc;
    ULONG              devFunc = 0;
    WWUsbDeviceStrings uds;
    std::wstring       rootHubName;

    HRG(GetHostControllerDriverKeyName(hDev, hc.driverKey));
    if (hc.driverKey.length() == 0) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        return E_FAIL;
    }

    for (size_t i = 0; i < mHCs.size(); ++i) {
        if (0 == wcscmp(mHCs[i].driverKey.c_str(), hc.driverKey.c_str())) {
            // Already on the list.
            return S_OK;
        }
    }

    HRG(WWDriverKeyNameToDeviceStrings(hc.driverKey, uds));
    hc.devStr = uds;
    if (swscanf_s(uds.deviceId.c_str(), L"PCI\\VEN_%x&DEV_%x&SUBSYS_%x&REV_%x",
        &hc.vendorID, &hc.deviceID, &hc.subSysID, &hc.revision) != 4) {
        hr = E_FAIL;
        goto end;
    }

    BHRG(SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, SPDRP_BUSNUMBER, nullptr, (PBYTE)&hc.busNumber, sizeof hc.busNumber, nullptr));
    BHRG(SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, SPDRP_ADDRESS, nullptr, (PBYTE)&devFunc, sizeof devFunc, nullptr));
    hc.busDevice = devFunc >> 16;
    hc.busFunction = devFunc & 0xffff;

    HRG(GetControllerInf0(hDev, hc.ci0));
    HRG(GetBandwidthInf(hDev, hc.bir));
    HRG(GetBusStatistics0(hDev, hc.bs0));
    HRG(GetPowerInf(hDev, hc.pir));
    HRG(GetDriverVersionInf(hDev, hc.dvp));

    hc.idx = WWUsbIdGenerate();

    // hc.ci0.Info0.NumberOfRootPortsはHighSpeed以下のポートの総数が入る。
    printf("#%d %S %S %S curUsbFrames=%u\n",
        hc.idx, hc.devStr.friendlyName.c_str(),
        WWUsbVendorIdToStr(hc.vendorID),
        hc.devStr.deviceDesc.c_str(),
        hc.bs0.CurrentUsbFrame);
    mHCs.push_back(hc);

    HRG(GetRootHubName(hDev, rootHubName));
    if (0 < rootHubName.length()) {
        WWGetHubInf(1, hc.idx, rootHubName);
    }

    hr = S_OK;
end:

    return hr;
}

