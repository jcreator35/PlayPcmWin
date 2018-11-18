// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include "WWUsbDeviceTreeDLL.h"
#include "WWUsbCommon.h"
#include "WWUsbHub.h"
#include "WWUsbHostController.h"
#include "WWUsbBuildDeviceTree.h"
#include "WWUsbHubPorts.h"
#include "WWUsbVendorIdToStr.h"
#include <string.h>

extern "C" {

__declspec(dllexport)
    int __stdcall
    WWUsbDeviceTreeDLL_Init(void)
{
    WWUsbVendorIdToStrInit();

    return 0;
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_Refresh(void)
{
    WWUsbIdGeneratorReset();
    WWHubsClear();
    WWHubPortsClear();
    WWHostControllersClear();

    return WWBuildUsbDeviceTree();
}


__declspec(dllexport)
void __stdcall
WWUsbDeviceTreeDLL_Term(void)
{
    WWHubsClear();
    WWHubPortsClear();
    WWHostControllersClear();
    WWUsbVendorIdToStrTerm();
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetNumOfHostControllers(void)
{
    return (int)mHCs.size();
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetNumOfHubs(void)
{
    return (int)mHubs.size();
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetNumOfHubPorts(void)
{
    return (int)mHPs.size();
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetHostControllerInf(int nth, WWUsbHostControllerCs &hc_r)
{
    if (nth < 0 || mHCs.size() <= nth) {
        return E_INVALIDARG;
    }

    auto &hc = mHCs[nth];
    hc_r.idx = hc.idx;
    memset(hc_r.name, 0, sizeof hc_r.name);
    wcsncpy_s(hc_r.name, hc.devStr.friendlyName.c_str(), WWUSB_STRING_COUNT - 1);

    memset(hc_r.desc, 0, sizeof hc_r.desc);
    wcsncpy_s(hc_r.desc, hc.devStr.deviceDesc.c_str(), WWUSB_STRING_COUNT - 1);

    memset(hc_r.vendor, 0, sizeof hc_r.vendor);
    wcsncpy_s(hc_r.vendor, WWUsbVendorIdToStr(hc.vendorID), WWUSB_STRING_COUNT - 1);
    return S_OK;
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetHubInf(int nth, WWUsbHubCs &hub_r)
{
    if (nth < 0 || mHubs.size() <= nth) {
        return E_INVALIDARG;
    }

    auto &h = mHubs[nth];
    hub_r.idx = h.idx;
    hub_r.isBusPowered = h.isBusPowered;
    hub_r.isRoot = h.isRoot;
    memset(hub_r.name, 0, sizeof hub_r.name);
    wcsncpy_s(hub_r.name, h.name.c_str(), WWUSB_STRING_COUNT - 1);
    hub_r.numPorts = h.numPorts;
    hub_r.parentIdx = h.parentIdx;
    hub_r.speed = h.hubType;
    return S_OK;
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetHubPortInf(int nth, WWUsbHubPortCs &hp_r)
{
    if (nth < 0 || mHPs.size() <= nth) {
        return E_INVALIDARG;
    }

    auto &hp = mHPs[nth];
    hp_r.idx = hp.idx;
    hp_r.parentIdx = hp.parentIdx;
    hp_r.deviceIsHub = hp.deviceIsHub;
    hp_r.bmAttributes = hp.confDesc->bmAttributes;
    hp_r.powerMilliW = hp.confDesc->MaxPower * 2;
    hp_r.speed = hp.speed;

    if (hp.ci2.Flags.DeviceIsSuperSpeedPlusCapableOrHigher) {
        hp_r.usbVersion = WWUDB_SuperSpeedPlus;
    } else if (hp.ci2.Flags.DeviceIsSuperSpeedCapableOrHigher) {
        hp_r.usbVersion = WWUDB_SuperSpeed;
    } else if (hp.ci2.SupportedUsbProtocols.Usb200 && 0x200 <= hp.cie->DeviceDescriptor.bcdUSB) {
        hp_r.usbVersion = WWUDB_HighSpeed;
    } else {
        hp_r.usbVersion = WWUDB_FullSpeed;
    }

    hp_r.portConnectorType = WWUPC_TypeA;
    if (hp.pcp->UsbPortProperties.PortConnectorIsTypeC) {
        hp_r.portConnectorType = WWUPC_TypeC;
    }

    memset(hp_r.name, 0, sizeof hp_r.name);
    wcsncpy_s(hp_r.name, hp.devStr.deviceDesc.c_str(), WWUSB_STRING_COUNT - 1);

    memset(hp_r.product, 0, sizeof hp_r.product);
    if (hp.cie->DeviceDescriptor.iProduct != 0
        && WWStringDescFindString(hp.sds, hp.cie->DeviceDescriptor.iProduct)[0] != 0) {
        const wchar_t *p = WWStringDescFindString(hp.sds, hp.cie->DeviceDescriptor.iProduct);
        wcsncpy_s(hp_r.product, p, WWUSB_STRING_COUNT - 1);
    } else {
        wcsncpy_s(hp_r.product, hp.devStr.deviceDesc.c_str(), WWUSB_STRING_COUNT - 1);
    }

    memset(hp_r.vendor, 0, sizeof hp_r.vendor);
    if (hp.cie->DeviceDescriptor.iManufacturer != 0
        && WWStringDescFindString(hp.sds, hp.cie->DeviceDescriptor.iManufacturer)[0] != 0) {
        const wchar_t *p = WWStringDescFindString(hp.sds, hp.cie->DeviceDescriptor.iManufacturer);
        wcsncpy_s(hp_r.vendor, p, WWUSB_STRING_COUNT - 1);
    } else {
        wcsncpy_s(hp_r.vendor, WWUsbVendorIdToStr(hp.devDesc.idVendor), WWUSB_STRING_COUNT - 1);
    }

    hp_r.confDesc = (UCHAR*)hp.confDesc;
    hp_r.confDescBytes = hp.confDesc->wTotalLength;

    hp_r.numStringDesc = (int)hp.sds.size();
    return S_OK;
}

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_GetStringDesc(int nth, int idx, WWUsbStringDescCs &sd_r)
{
    if (nth < 0 || mHPs.size() <= nth) {
        return E_INVALIDARG;
    }

    auto &hp = mHPs[nth];

    if (idx < 0 || hp.sds.size() <= idx) {
        return E_INVALIDARG;
    }

    auto &sd = hp.sds[idx];

    sd_r.descIdx = sd.descIdx;
    sd_r.descType = sd.descType;
    sd_r.langId = sd.langId;
    memset(sd_r.name, 0, sizeof sd_r.name);
    wcsncpy_s(sd_r.name, sd.s.c_str(), WWUSB_STRING_COUNT - 1);

    return 0;
}


}; // extern "C"
