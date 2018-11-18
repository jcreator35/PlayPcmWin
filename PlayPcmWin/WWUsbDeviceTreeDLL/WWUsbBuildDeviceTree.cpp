// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include "WWUsbBuildDeviceTree.h"
#include "WWUsbHostController.h"
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

HRESULT
WWBuildUsbDeviceTree(void)
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

    // ホストコントローラーを列挙する。
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

        WWGetHostControllerInf(hDev, devInf, sdd);

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

