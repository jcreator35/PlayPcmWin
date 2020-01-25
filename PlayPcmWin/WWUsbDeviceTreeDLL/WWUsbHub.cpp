// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include "WWUsbHub.h"
#include "WWUsbHubPorts.h"
#include "WWUsbCommon.h"

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

std::vector<WWHub> mHubs;

void
WWHubsClear(void)
{
    mHubs.clear();
}

static std::string
DeviceRemovableFlagToStr(int nPorts, int flag)
{
    char s[256];
    memset(s, 0, sizeof s);

    if (15 < nPorts) {
        nPorts = 15;
    }

    for (int i = 0; i < nPorts; ++i) {
        // flag 1 : non-removable
        // flag 0 : removable
        if (flag & (1 << (i + 1))) {
            s[i] = 'N';
        } else {
            s[i] = 'R';
        }
    }
    return std::string(s);
}

static std::string
HubCharacteristicsToStr(int a)
{
    std::string s;

    // 00
    // 01
    // 1x
    switch (a & 3) {
    case 0:
        s.append("PowerPortSwitchingGanged ");
        break;
    case 1:
        s.append("PowerPortSwitchingIndividual ");
        break;
    default:
        s.append("PowerPortSwitchingReserved ");
        break;
    }

    if (a & 0x4) {
        s.append("HubIsPartOfCompondDevice ");
    } else {
        s.append("HubIsNotPartOfCompondDevice ");
    }

    switch ((a >> 3) & 3) {
    case 0:
        s.append("GlobalOverCurrentProtection ");
        break;
    case 1:
        s.append("IndividualPortOverCurrentProtection ");
        break;
    default:
        s.append("NoOverCurrentProtection ");
        break;
    }

    return s;
}

HRESULT
WWGetHubInf(int level, int parentIdx, std::wstring hubName)
{
    HRESULT hr = E_FAIL;
    HANDLE hHub = INVALID_HANDLE_VALUE;
    USB_NODE_INFORMATION ni;
    USB_HUB_INFORMATION_EX hi;
    USB_HUB_CAPABILITIES_EX hc;
    ULONG bytes = 0;
    WWHub hub;


    std::wstring header(L"\\\\.\\");
    std::wstring deviceName = header + hubName;

    hHub = CreateFileW(deviceName.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);
    if (hHub == INVALID_HANDLE_VALUE) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    memset(&ni, 0, sizeof ni);
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_INFORMATION, &ni, sizeof ni, &ni, sizeof ni, &bytes, nullptr));

    memset(&hi, 0, sizeof hi);
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_HUB_INFORMATION_EX, &hi, sizeof hi, &hi, sizeof hi, &bytes, nullptr));

    memset(&hc, 0, sizeof hc);
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_HUB_CAPABILITIES_EX, &hc, sizeof hc, &hc, sizeof hc, &bytes, nullptr));


    hub.name = hubName;
    hub.numPorts = ni.u.HubInformation.HubDescriptor.bNumberOfPorts;
    hub.hubType = WWUsbHubTypeToWWUsbDeviceBusSpeed(hi.HubType);
    hub.isBusPowered = ni.u.HubInformation.HubIsBusPowered;
    hub.isRoot = hc.CapabilityFlags.HubIsRoot;
    hub.speed = WWUDB_RootHub;
    hub.idx = WWUsbIdGenerate();
    hub.parentIdx = parentIdx;
    hub.ni = ni;
    hub.hi = hi;
    hub.hc = hc;
    mHubs.push_back(hub);

    WWPrintIndentSpace(level);
    printf("#%d UsbHub : %d ports %S %S", hub.idx, hub.numPorts,
        hub.isBusPowered ? L"BusPowered" : L"SelfPowered",
        WWUsbDeviceBusSpeedToStr(hub.hubType));
    switch (hub.hi.HubType) {
    case Usb20Hub:
        printf("  %s PowerOnDelay=%dms HubControlCurrent=%dmA",
            HubCharacteristicsToStr(hub.hi.u.UsbHubDescriptor.wHubCharacteristics).c_str(),
            2 * (int)hub.hi.u.UsbHubDescriptor.bPowerOnToPowerGood,
            (int)hub.hi.u.UsbHubDescriptor.bHubControlCurrent);
        break;
    case Usb30Hub:
        printf("  %s PowerOnDelay=%dms HubControlCurrent=%dmA PacketHeaderDecodeLatency=%4.2fus HubDelay=%dns DeviceRemovableFlags=%s",
            HubCharacteristicsToStr(hub.hi.u.Usb30HubDescriptor.wHubCharacteristics).c_str(),
            2 * (int)hub.hi.u.Usb30HubDescriptor.bPowerOnToPowerGood,
            (int)hub.hi.u.Usb30HubDescriptor.bHubControlCurrent,
            0.1 * (int)hub.hi.u.Usb30HubDescriptor.bHubHdrDecLat,

            (int)hub.hi.u.Usb30HubDescriptor.wHubDelay,
            DeviceRemovableFlagToStr(hub.numPorts, hub.hi.u.Usb30HubDescriptor.DeviceRemovable).c_str());
        break;
    default:
        break;
    }

    printf("\n");

    HRG(WWEnumHubPorts(level+1, hub.idx, hHub, hub.idx, hub.numPorts));

    hr = S_OK;
end:
    if (hHub != INVALID_HANDLE_VALUE) {
        CloseHandle(hHub);
        hHub = INVALID_HANDLE_VALUE;
    }

    return hr;
}
