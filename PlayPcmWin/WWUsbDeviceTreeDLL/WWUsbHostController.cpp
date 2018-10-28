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
FillUUCI(
        HANDLE hHCDev,
        WWHostController &hc)
{
    HRESULT hr = E_FAIL;
    DWORD dwBytes = 0;

    memset(&hc.uuci, 0, sizeof hc.uuci);
    hc.uuci.Header.UsbUserRequest = USBUSER_GET_CONTROLLER_INFO_0;
    hc.uuci.Header.RequestBufferLength = sizeof hc.uuci;

    BHRG(DeviceIoControl(hHCDev, IOCTL_USB_USER_REQUEST, &hc.uuci, sizeof hc.uuci,
        &hc.uuci, sizeof hc.uuci, &dwBytes, nullptr));
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

    hc.deviceType = WWUD_HostController;
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

    HRG(FillUUCI(hDev, hc));

    hc.idx = (int)mHCs.size();

    // hc.uuci.Info0.NumberOfRootPortsはHighSpeed以下のポートの総数が入る。
    printf("Host Controller #%d %S %S\n", hc.idx, WWUsbVendorIdToStr(hc.vendorID), hc.devStr.deviceDesc.c_str());
    mHCs.push_back(hc);

    HRG(GetRootHubName(hDev, rootHubName));
    if (0 < rootHubName.length()) {
        WWGetHubInf(1, rootHubName);
    }

    hr = S_OK;
end:

    return hr;
}

