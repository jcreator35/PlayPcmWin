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

enum WWUsbDeviceType {
    WWUD_HostController,
    WWUD_RootHub,
    WWUD_ExternalHub,
    WWUD_Device
};

enum WWUsbDeviceBusSpeed {
    WWUDB_RootHub, //< RootHubは別格の扱い。
    WWUDB_LowSpeed,
    WWUDB_FullSpeed,
    WWUDB_HighSpeed,
    WWUDB_SuperSpeed,
    WWUDB_SuperSpeedPlus,
};

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
};

struct WWStringDesc {
    int descIdx;
    int langId;
    int descType;
    std::wstring s;
};

HRESULT
WWDriverKeyNameToDeviceStrings(std::wstring driverName, WWUsbDeviceStrings &uds_r);

HRESULT
WWGetTransportCharacteristics(HANDLE h, USB_TRANSPORT_CHARACTERISTICS & tc_r);

HRESULT
WWGetDeviceCharacteristics(HANDLE h, USB_DEVICE_CHARACTERISTICS &dc_r);

void
WWPrintIndentSpace(int level);
