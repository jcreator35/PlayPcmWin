// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include "WWUsbDeviceTreeDLL.h"
#include "WWUsbCommon.h"
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


static std::vector<WWHostController> mHCs;

static HRESULT
GetPdid(HDEVINFO devInf, SP_DEVINFO_DATA &sdd, int idx, PSP_DEVICE_INTERFACE_DETAIL_DATA *pdid_r)
{
    HRESULT hr = E_FAIL;
    ULONG bytes = 0;
    PSP_DEVICE_INTERFACE_DETAIL_DATA pdid = nullptr;
    SP_DEVICE_INTERFACE_DATA sdid;
    memset(&sdid, 0, sizeof sdid);
    sdid.cbSize = sizeof sdid;

    *pdid_r = nullptr;

    BHRG(SetupDiEnumDeviceInterfaces(devInf, 0, &GUID_DEVINTERFACE_USB_HOST_CONTROLLER, idx, &sdid));

    SetupDiGetDeviceInterfaceDetailW(devInf, &sdid, nullptr, 0, &bytes, nullptr);
    if (bytes == 0) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    // 念の為終端文字の領域を余分に確保。
    bytes += 2;
    ALLOC_MEM(pdid, PSP_DEVICE_INTERFACE_DETAIL_DATA, bytes);
    // cbSize should be size of static part of struct according to the manual !!
    // https://docs.microsoft.com/en-us/windows/desktop/api/setupapi/nf-setupapi-setupdigetdeviceinterfacedetaila
    pdid->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);
    BHRG(SetupDiGetDeviceInterfaceDetailW(devInf, &sdid, pdid, bytes, &bytes, nullptr));

    *pdid_r = pdid;
    hr = S_OK;
end:

    return hr;
}

static HRESULT
GetHCDDriverGuid(HANDLE hcd, std::wstring &guid_r)
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


static int
EnumHostController(
        HTREEITEM hParent,
        HANDLE hDev,
        HDEVINFO devInf,
        SP_DEVINFO_DATA &sdd)
{
    HRESULT            hr = E_FAIL;
    WWHostController   hc;
    ULONG              devFunc = 0;
    WWUsbDeviceStrings uds;
    WWUsbDevice    node((int)mDevices.size());
    std::wstring       rootHubName;

    node.deviceType = WWUD_HostController;
    HRG(GetHCDDriverGuid(hDev, node.name));
    if (node.name.length() == 0) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        return E_FAIL;
    }

    for (size_t i = 0; i < mDevices.size(); ++i) {
        if (0 == wcscmp(mDevices[i].name.c_str(), node.name.c_str())) {
            // Already on the list.
            return S_OK;
        }
    }

    mDevices.push_back(node);

    hc.deviceType = WWUD_HostController;
    HRG(WWDriverKeyNameToDeviceStrings(node.name, uds));
    hc.devStr = uds;
    hc.driverKey = node.name;
    if (swscanf_s(uds.deviceId.c_str(), L"PCI\\VEN_%x&DEV_%x&SUBSYS_%x&REV_%x",
            &hc.vendorID, &hc.deviceID, &hc.subSysID, &hc.revision) != 4) {
        hr = E_FAIL;
        goto end;
    }

    BHRG(SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, SPDRP_BUSNUMBER, nullptr, (PBYTE)&hc.busNumber, sizeof hc.busNumber, nullptr));
    BHRG(SetupDiGetDeviceRegistryPropertyW(devInf, &sdd, SPDRP_ADDRESS,   nullptr, (PBYTE)&devFunc,      sizeof devFunc,      nullptr));
    hc.busDevice = devFunc >> 16;
    hc.busFunction = devFunc & 0xffff;

    HRG(FillUUCI(hDev, hc));

    hc.idx = (int)mHCs.size();
    printf("Host Controller %d\n", hc.idx);
    mHCs.push_back(hc);

    HRG(GetRootHubName(hDev, rootHubName));
    if (0 < rootHubName.length()) {
        WWEnumHub(rootHubName);
    }

    hr = S_OK;
end:

    return hr;
}

static HRESULT
BuildUsbDeviceTree(HTREEITEM hParent)
{
    HRESULT hr = E_FAIL;
    PSP_DEVICE_INTERFACE_DETAIL_DATA pdid = nullptr;
    SP_DEVINFO_DATA sdd;
    HANDLE hDev = INVALID_HANDLE_VALUE;
    HDEVINFO devInf = INVALID_HANDLE_VALUE;
    
    devInf = SetupDiGetClassDevsW(&GUID_DEVINTERFACE_USB_HOST_CONTROLLER, nullptr, nullptr, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
    if (devInf == INVALID_HANDLE_VALUE) {
        hr = E_FAIL;
        goto end;
    }

    for (int i = 0; ; ++i) {
        memset(&sdd, 0, sizeof sdd);
        sdd.cbSize = sizeof sdd;
        if (!SetupDiEnumDeviceInfo(devInf, i, &sdd)) {
            // successfully finished.
            break;
        }
        
        HRG(GetPdid(devInf, sdd, i, &pdid));

        hDev = CreateFileW(pdid->DevicePath, GENERIC_WRITE, FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);
        if (hDev == INVALID_HANDLE_VALUE) {
            printf("Error: %s:%d\n", __FILE__, __LINE__);
            goto end;
        }

        EnumHostController(hParent, hDev, devInf, sdd);
        CloseHandle(hDev);
        hDev = INVALID_HANDLE_VALUE;

        free(pdid);
        pdid = nullptr;
    }

    hr = S_OK;
end:

    free(pdid);
    pdid = nullptr;

    return hr;
}

extern "C" {

__declspec(dllexport)
    int __stdcall
    WWUsbDeviceTreeDLL_Init(void)
{
    return 0;
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_Refresh(void)
{
    mDevices.clear();
    mHCs.clear();

    //WWUsbDeviceNode root(0, std::wstring(L"PC"));
    //mDevices.push_back(root);

   return BuildUsbDeviceTree(nullptr);
}


__declspec(dllexport)
void __stdcall
WWUsbDeviceTreeDLL_Term(void)
{
    mDevices.clear();
    mHCs.clear();
}


}; // extern "C"
