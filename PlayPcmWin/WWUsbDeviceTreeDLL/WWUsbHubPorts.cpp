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
#include "WWPrintStructs.h"

std::vector<WWHubPort> mHPs;

void
WWHubPortsClear(void)
{
    for (int i = 0; i < (int)mHPs.size(); ++i) {
        auto & hp = mHPs[i];

        free(hp.cie);
        hp.cie = nullptr;

        free(hp.pcp);
        hp.pcp = nullptr;

        free(hp.pcr);
        hp.pcr = nullptr;
        hp.confDesc = nullptr;

        free(hp.pbr);
        hp.pbr = nullptr;
        hp.bosDesc = nullptr;
    }

    mHPs.clear();
}

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

// @return pdr_r free()で開放して下さい。
static HRESULT
GetConfigDescriptor(
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

static HRESULT
GetBOSDescriptor(
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

static bool
IsStringDescAvailable(
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
                comm->bLength != WW_SIZEOF_USB_INTERFACE_DESCRIPTOR2) {
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
    if (!DeviceIoControl(hHub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, dr, bytes, dr, bytes, &bytes, nullptr)) {
        hr = E_FAIL;
        goto end;
    }
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

static HRESULT
GetAllStringDescs(
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
    hr = GetStringDesc(hHub, connIdx, 0, 0, sdLang);
    if (FAILED(hr)) {
        goto end;
    }
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
                && commDesc->bLength != WW_SIZEOF_USB_INTERFACE_DESCRIPTOR2) {
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

static HRESULT
GetDriverKeyName(
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

/// @return E_FAIL ポートに何もつながっていない。
static HRESULT
GetHubPortInf(int level, int parentIdx, HANDLE hHub, int hubIdx, int connIdx, WWHubPort & hp_r)
{
    BOOL brv = FALSE;
    HRESULT hr = E_FAIL;
    ULONG bytes = 0;
    WWUsbDeviceStrings uds;
    USB_PORT_CONNECTOR_PROPERTIES cp;
    USB_NODE_CONNECTION_INFORMATION_EX_V2 ci2;
    PUSB_PORT_CONNECTOR_PROPERTIES pcp = nullptr;
    PUSB_NODE_CONNECTION_INFORMATION_EX   cie = nullptr;
    PUSB_NODE_CONNECTION_INFORMATION      ci = nullptr;
    PUSB_DESCRIPTOR_REQUEST pcr = nullptr;
    PUSB_DESCRIPTOR_REQUEST pbr = nullptr;
    std::wstring extHubName;

    // Get PPCP
    bytes = sizeof cp;
    memset(&cp, 0, sizeof cp);
    cp.ConnectionIndex = connIdx;
    BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES, &cp, sizeof cp, &cp, sizeof cp, &bytes, nullptr));
    if (bytes == sizeof cp) {
        ALLOC_MEM(pcp, PUSB_PORT_CONNECTOR_PROPERTIES, cp.ActualLength);
        pcp->ConnectionIndex = connIdx;
        BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES, pcp, cp.ActualLength, pcp, cp.ActualLength, &bytes, nullptr));
        if (bytes < cp.ActualLength) {
            free(pcp);
            pcp = nullptr;
        }
    }
    hp_r.pcp = pcp;

    // Get ci2, may fail : all zero
    memset(&ci2, 0, sizeof ci2);
    ci2.ConnectionIndex = connIdx;
    ci2.Length = sizeof ci2;
    ci2.SupportedUsbProtocols.Usb110 = 1;
    ci2.SupportedUsbProtocols.Usb200 = 1;
    ci2.SupportedUsbProtocols.Usb300 = 1;
    brv = DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2, &ci2, sizeof ci2, &ci2, sizeof ci2, &bytes, nullptr);
    if (!brv) {
        // all zero == failed to get
        memset(&ci2, 0, sizeof ci2);
    }
    
    // Get pcie, may fail : nullptr
    bytes = sizeof(USB_NODE_CONNECTION_INFORMATION_EX) + (sizeof(USB_PIPE_INFO) * 30);
    ALLOC_MEM(cie, PUSB_NODE_CONNECTION_INFORMATION_EX, bytes);
    cie->ConnectionIndex = connIdx;
    brv = DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, cie, bytes, cie, bytes, &bytes, nullptr);
    if (!brv) {
        // FAIL!
        // try to get pci and copy inf to pcie

        bytes = sizeof(USB_NODE_CONNECTION_INFORMATION) + sizeof(USB_PIPE_INFO) * 30;
        ALLOC_MEM(ci, PUSB_NODE_CONNECTION_INFORMATION, bytes);
        ci->ConnectionIndex = connIdx;

        BHRG(DeviceIoControl(hHub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION, ci, bytes, ci, bytes, &bytes, nullptr));
        Copy(ci, cie);
        // speedを決定する。
        hp_r.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)cie->Speed);
    } else {
        // pcieの取得成功。

        // speedを決定する。
        if (ci2.ConnectionIndex != 0 && cie->Speed == UsbHighSpeed) {
            if (ci2.Flags.DeviceIsOperatingAtSuperSpeedPlusOrHigher) {
                hp_r.speed = WWUDB_SuperSpeedPlus;
            } else if (ci2.Flags.DeviceIsOperatingAtSuperSpeedOrHigher) {
                hp_r.speed = WWUDB_SuperSpeed;
            } else {
                hp_r.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)cie->Speed);
            }
        } else {
            hp_r.speed = WWUsbDeviceSpeedToWWUsbDeviceBusSpeed((USB_DEVICE_SPEED)cie->Speed);
        }
    }
    hp_r.cie = cie;

    hp_r.connIdx = (int)connIdx;
    hp_r.devDesc = cie->DeviceDescriptor;
    hp_r.deviceIsHub = cie->DeviceIsHub;
    hp_r.deviceAddress = cie->DeviceAddress;
    hp_r.numOfOpenPipes = cie->NumberOfOpenPipes;
    hp_r.connStat = cie->ConnectionStatus;
    hp_r.ci2 = ci2;

    if (cie->ConnectionStatus != NoDeviceConnected) {
        HRG(GetDriverKeyName(hHub, connIdx, hp_r.driverKey));
        HRG(WWDriverKeyNameToDeviceStrings(hp_r.driverKey, uds));
        hp_r.devStr = uds;
    }

    if (cie->ConnectionStatus == DeviceConnected) {
        HRG(GetConfigDescriptor(hHub, connIdx, 0, &pcr));
        PUSB_CONFIGURATION_DESCRIPTOR pcd = (PUSB_CONFIGURATION_DESCRIPTOR)(pcr + 1);
        hp_r.confDesc = pcd;
        hp_r.pcr = pcr;
    } else {
        hp_r.confDesc = nullptr;
        hp_r.pcr = nullptr;
    }

    if (pcr != nullptr && cie->DeviceDescriptor.bcdUSB > 0x0200) {
        HRG(GetBOSDescriptor(hHub, connIdx, &pbr));
        PUSB_BOS_DESCRIPTOR pbd = (PUSB_BOS_DESCRIPTOR)(pcr + 1);
        hp_r.bosDesc = pbd;
        hp_r.pbr = pbr;
    } else {
        hp_r.bosDesc = nullptr;
        hp_r.pbr = nullptr;
    }

    if (pcr != nullptr && IsStringDescAvailable(&cie->DeviceDescriptor, (PUSB_CONFIGURATION_DESCRIPTOR)(pcr + 1))) {
        GetAllStringDescs(hHub, connIdx, &cie->DeviceDescriptor, (PUSB_CONFIGURATION_DESCRIPTOR)(pcr + 1), hp_r.sds);
    }
    
    if (hp_r.deviceIsHub) {
        HRG(GetExternalHubName(hHub, connIdx, hp_r.extHubName));
    }

    if (cie->ConnectionStatus == DeviceConnected) {
        // 使用されているポート。
        // リストに追加する。
        hp_r.idx = WWUsbIdGenerate();
        hp_r.parentIdx = parentIdx;

        WWPrintIndentSpace(level);
        printf("#%d %S %S %S %04x ", hp_r.idx,
            hp_r.pcp->UsbPortProperties.PortConnectorIsTypeC ? L"TypeC" : L"TypeA",
            WWUsbDeviceBusSpeedToStr(hp_r.speed), hp_r.devStr.deviceDesc.c_str(), hp_r.devDesc.idVendor);
        if (hp_r.confDesc->bmAttributes & USB_CONFIG_BUS_POWERED) {
            printf("MaxPower=%dmW ", hp_r.confDesc->MaxPower * 2);
        }
        for (int i = 0; i < hp_r.sds.size(); ++i) {
            printf("%S ", hp_r.sds[i].s.c_str());
        }
        printf("\n");

        WWPrintConfDesc(level+1, (int)WWUDB_SuperSpeed <= (int)hp_r.speed, hp_r.confDesc, hp_r.sds);

        hr = S_OK;
    } else {
        // ポート情報は取得成功したが、
        // 何もつながっていないので不要。
        hr = E_FAIL;
    }

end:
    free(ci);
    ci = nullptr;
    if (FAILED(hr)) {
        free(cie);
        cie = nullptr;
        free(pcp);
        pcp = nullptr;
        free(pcr);
        pcr = nullptr;
        free(pbr);
        pbr = nullptr;
    }

    return hr;
}

HRESULT
WWEnumHubPorts(int level, int parentIdx, HANDLE hHub, int hubIdx, int numPorts)
{
    HRESULT hr = E_FAIL;

    // Port indices are 1 based!!
    for (int connIdx = 1; connIdx <= numPorts; ++connIdx) {
        WWHubPort hp;

        hr = GetHubPortInf(level, parentIdx, hHub, hubIdx, connIdx, hp);
        if (FAILED(hr)) {
            continue;
        }

        mHPs.push_back(hp);

        if (hp.deviceIsHub) {
            WWGetHubInf(level + 1, hp.idx, hp.extHubName);
        }
    }

    return S_OK;
}
