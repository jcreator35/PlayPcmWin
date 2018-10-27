// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

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
#include "WWUsbHub.h"

static void
Copy(PUSB_NODE_CONNECTION_INFORMATION pci, PUSB_NODE_CONNECTION_INFORMATION_EX pcie)
{
    pcie->ConnectionIndex = pci->ConnectionIndex;
    pcie->DeviceDescriptor = pci->DeviceDescriptor;
    pcie->CurrentConfigurationValue = pci->CurrentConfigurationValue;
    pcie->Speed = pci->LowSpeed ? UsbLowSpeed : UsbFullSpeed;
    pcie->DeviceIsHub = pci->DeviceIsHub;
    pcie->DeviceAddress = pci->DeviceAddress;
    pcie->NumberOfOpenPipes = pci->NumberOfOpenPipes;
    pcie->ConnectionStatus = pci->ConnectionStatus;

    memcpy(&pcie->PipeList[0], &pci->PipeList[0], sizeof(USB_PIPE_INFO) * 30);
}

static HRESULT
WWGetDriverKeyName(
        HANDLE  hHub,
        ULONG   cIdx,
        std::wstring & driverName_r)
{
    HRESULT hr = E_FAIL;
    DWORD bytes = 0;
    USB_NODE_CONNECTION_DRIVERKEY_NAME  cdn;
    PUSB_NODE_CONNECTION_DRIVERKEY_NAME pcdn = nullptr;

    driverName_r = std::wstring(L"");

    memset(&cdn, 0, sizeof cdn);
    cdn.ConnectionIndex = cIdx;

    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, &cdn, sizeof cdn, &cdn, sizeof cdn, &bytes, nullptr));

    if (cdn.ActualLength <= sizeof cdn) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        hr = E_FAIL;
        goto end;
    }

    bytes = cdn.ActualLength + 2;
    ALLOC_MEM(pcdn, PUSB_NODE_CONNECTION_DRIVERKEY_NAME, bytes);
    pcdn->ConnectionIndex = cIdx;

    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, pcdn, bytes, pcdn, bytes, &bytes, nullptr));

    driverName_r = std::wstring(pcdn->DriverKeyName);
    hr = S_OK;
end:
    free(pcdn);
    pcdn = nullptr;

    return hr;
}

static HRESULT
GetExternalHubName(
        HANDLE hHub,
        int connIdx,
        std::wstring &name_r)
{
    HRESULT hr = E_FAIL;
    DWORD                       bytes = 0;
    USB_NODE_CONNECTION_NAME    nc;
    PUSB_NODE_CONNECTION_NAME   pnc = nullptr;

    // サイズ取得⇒nc.ActualLength
    memset(&nc, 0, sizeof nc);
    nc.ConnectionIndex = connIdx;
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_NAME, &nc, sizeof nc, &nc, sizeof nc, &bytes, nullptr));
    bytes = nc.ActualLength;
    if (bytes <= sizeof nc) {
        printf("Error: %s:%d\n", __FILE__, __LINE__);
        goto end;
    }

    ALLOC_MEM(pnc, PUSB_NODE_CONNECTION_NAME, bytes);
    pnc->ConnectionIndex = connIdx;
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_NAME, pnc, bytes, pnc, bytes, &bytes, nullptr));

    name_r = std::wstring(pnc->NodeName);
    hr = S_OK;
end:

    free(pnc);
    pnc = nullptr;

    return hr;
}

static HRESULT
EnumHubPort(HANDLE hHub, int hubIdx, int connIdx)
{
    WWUsbDevice ud((int)mDevices.size());
    BOOL brv = FALSE;
    HRESULT hr = E_FAIL;
    ULONG bytes = 0;
    WWUsbDeviceStrings uds;
    USB_PORT_CONNECTOR_PROPERTIES pcp;
    USB_NODE_CONNECTION_INFORMATION_EX_V2 ci2;
    PUSB_PORT_CONNECTOR_PROPERTIES ppcp = nullptr;
    PUSB_NODE_CONNECTION_INFORMATION_EX   pcie = nullptr;
    PUSB_NODE_CONNECTION_INFORMATION      pci = nullptr;
    PUSB_DESCRIPTOR_REQUEST pdr = nullptr;
    PUSB_DESCRIPTOR_REQUEST pbr = nullptr;
    std::wstring extHubName;

    // Get PPCP
    bytes = sizeof pcp;
    memset(&pcp, 0, sizeof pcp);
    pcp.ConnectionIndex = connIdx;
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES, &pcp, sizeof pcp, &pcp, sizeof pcp, &bytes, nullptr));
    if (bytes == sizeof pcp) {
        ALLOC_MEM(ppcp, PUSB_PORT_CONNECTOR_PROPERTIES, pcp.ActualLength);
        ppcp->ConnectionIndex = connIdx;
        BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES, ppcp, pcp.ActualLength, ppcp, pcp.ActualLength, &bytes, nullptr));
        if (bytes < pcp.ActualLength) {
            free(ppcp);
            ppcp = nullptr;
        }
    }

    // Get ci2, may fail : all zero
    memset(&ci2, 0, sizeof ci2);
    ci2.ConnectionIndex = connIdx;
    ci2.Length = sizeof ci2;
    ci2.SupportedUsbProtocols.Usb300 = 1;
    brv = DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2, &ci2, sizeof ci2, &ci2, sizeof ci2, &bytes, nullptr);
    if (!brv) {
        // all zero == failed to get
        memset(&ci2, 0, sizeof ci2);
    }
    
    // Get pcie, may fail : nullptr
    bytes = sizeof(USB_NODE_CONNECTION_INFORMATION_EX) + (sizeof(USB_PIPE_INFO) * 30);
    ALLOC_MEM(pcie, PUSB_NODE_CONNECTION_INFORMATION_EX, bytes);
    pcie->ConnectionIndex = connIdx;
    brv = DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, pcie, bytes, pcie, bytes, &bytes, nullptr);
    if (!brv) {
        // FAIL!
        // try to get pci and copy inf to pcie

        bytes = sizeof(USB_NODE_CONNECTION_INFORMATION) + sizeof(USB_PIPE_INFO) * 30;
        ALLOC_MEM(pci, PUSB_NODE_CONNECTION_INFORMATION, bytes);
        pci->ConnectionIndex = connIdx;

        BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION, pci, bytes, pci, bytes, &bytes, nullptr));
        Copy(pci, pcie);
        ud.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)pcie->Speed);
    } else {
        // pcieの取得成功。

        // ud.speedを決定する。
        if (ci2.ConnectionIndex != 0 && pcie->Speed == UsbHighSpeed) {
            if (ci2.Flags.DeviceIsOperatingAtSuperSpeedPlusOrHigher) {
                ud.speed = WWUDB_SuperSpeedPlus;
            } else if (ci2.Flags.DeviceIsOperatingAtSuperSpeedOrHigher) {
                ud.speed = WWUDB_SuperSpeed;
            } else {
                ud.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)pcie->Speed);
            }
        } else {
            ud.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)pcie->Speed);
        }
    }

    ud.connIdx = connIdx;
    ud.devDesc = pcie->DeviceDescriptor;
    ud.currentConfigurationValue = pcie->CurrentConfigurationValue;
    ud.deviceIsHub = pcie->DeviceIsHub;
    ud.deviceAddress = pcie->DeviceAddress;
    ud.numOfOpenPipes = pcie->NumberOfOpenPipes;
    ud.connStat = pcie->ConnectionStatus;

    if (pcie->ConnectionStatus != NoDeviceConnected) {
        HRG(WWGetDriverKeyName(hHub, connIdx, ud.driverKeyName));
        HRG(WWDriverKeyNameToDeviceStrings(ud.driverKeyName, uds));
        ud.devStr = uds;
    }

    if (pcie->ConnectionStatus == DeviceConnected) {
        HRG(WWGetConfigDescriptor(hHub, connIdx, 0, &pdr));
        PUSB_CONFIGURATION_DESCRIPTOR pcd = (PUSB_CONFIGURATION_DESCRIPTOR)(pdr + 1);
        ud.confDesc = *pcd;
    }

    if (pdr != nullptr && pcie->DeviceDescriptor.bcdUSB > 0x0200) {
        HRG(WWGetBOSDescriptor(hHub, connIdx, &pbr));
        PUSB_BOS_DESCRIPTOR pbd = (PUSB_BOS_DESCRIPTOR)(pdr + 1);
        ud.bosDesc = *pbd;
    }

    if (pdr != nullptr && WWStringDescAvailable(&pcie->DeviceDescriptor, (PUSB_CONFIGURATION_DESCRIPTOR)(pdr + 1))) {
        WWGetAllStringDescs(hHub, connIdx, &pcie->DeviceDescriptor, (PUSB_CONFIGURATION_DESCRIPTOR)(pdr + 1), ud.sds);
    }

    if (pcie->ConnectionStatus == DeviceConnected) {
        printf("%S %S ", ud.devStr.deviceDesc.c_str(), WWUsbDeviceBusSpeedToStr(ud.speed));
        if (ud.confDesc.bmAttributes & 0x80) {
            printf("MaxPower=%dmW ", ud.confDesc.MaxPower * 2);
        }
        for (int i = 0; i < ud.sds.size(); ++i) {
            printf("%S ", ud.sds[i].s.c_str());
        }
        printf("\n");
        mDevices.push_back(ud);
    }

    if (ud.deviceIsHub) {
        HRG(GetExternalHubName(hHub, connIdx, extHubName));
        WWEnumHub(extHubName);
    }

    hr = S_OK;

end:
    free(ppcp);
    ppcp = nullptr;
    free(pcie);
    pcie = nullptr;
    free(pci);
    pci = nullptr;
    free(pdr);
    pdr = nullptr;
    free(pbr);
    pbr = nullptr;

    return hr;
}

HRESULT
WWEnumHubPorts(HANDLE hHub, int hubIdx, int numPorts)
{
    // Port indices are 1 based!!
    for (int connIdx = 1; connIdx <= numPorts; ++connIdx) {
        EnumHubPort(hHub, hubIdx, connIdx);
    }

    return S_OK;
}
