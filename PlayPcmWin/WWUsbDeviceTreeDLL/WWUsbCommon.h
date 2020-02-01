// 日本語。

#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>
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

#define WW_SIZEOF_USB_INTERFACE_DESCRIPTOR2 (11)

enum WWUsbDeviceBusSpeed {
    WWUDB_Unknown = -1,

    WWUDB_RootHub, //< RootHubは別格の扱い。
    WWUDB_LowSpeed,
    WWUDB_FullSpeed,
    WWUDB_HighSpeed,
    WWUDB_SuperSpeed,

    WWUDB_SuperSpeedPlus10,
    WWUDB_SuperSpeedPlus20,
};

enum WWUsbPortConnectorType {
    WWUPC_TypeA,
    WWUPC_TypeC,
};

int
WWUsbIdGenerate(void);

void
WWUsbIdGeneratorReset(void);

const wchar_t *
WWUsbDeviceBusSpeedToStr(WWUsbDeviceBusSpeed t);

WWUsbDeviceBusSpeed
WWUsbHubTypeToWWUsbDeviceBusSpeed(USB_HUB_TYPE t);

WWUsbDeviceBusSpeed
WWUsbDeviceSpeedToWWUsbDeviceBusSpeed(USB_DEVICE_SPEED ds);

struct WWUsbDeviceStrings {
    std::wstring deviceId;
    std::wstring deviceDesc;
    std::wstring hwId;
    std::wstring service;
    std::wstring deviceClass;
    std::wstring friendlyName;
    std::wstring manufacturer;
};

struct WWStringDesc {
    int descIdx;
    int langId;
    int descType;
    std::wstring s;
};

const wchar_t *
WWStringDescFindString(std::vector<WWStringDesc> &sds, int idx);

HRESULT
WWDriverKeyNameToDeviceStrings(std::wstring driverName, WWUsbDeviceStrings &uds_r);

HRESULT
WWGetTransportCharacteristics(HANDLE h, USB_TRANSPORT_CHARACTERISTICS & tc_r);

HRESULT
WWGetDeviceCharacteristics(HANDLE h, USB_DEVICE_CHARACTERISTICS &dc_r);

void
WWPrintIndentSpace(int level);

/// @param firstD very first descriptor
/// @param totalBytes descriptor total bytes
/// @param startD current descriptor
/// @param descType wanted descriptor type. -1 means any type
PUSB_COMMON_DESCRIPTOR
WWGetNextDescriptor(
    PUSB_COMMON_DESCRIPTOR firstD,
    ULONG totalBytes,
    PUSB_COMMON_DESCRIPTOR startD,
    long descType);