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

struct WWUsbDevice {
    int idx;
    std::vector<int> children;
    std::wstring name;

    WWUsbDeviceType deviceType;
    std::wstring driverKeyName;
    ULONG  vendorID;
    ULONG  deviceID;
    ULONG  subSysID;
    ULONG  revision;
    BOOL   busDeviceFunctionValid;
    ULONG  busNumber;
    USHORT busDevice;
    USHORT busFunction;
    
    // connection index
    int connIdx;

    // USB_NODE_CONNECTION_INFORMATION_EX
    WWUsbDeviceBusSpeed speed;
    UCHAR currentConfigurationValue;
    BOOL deviceIsHub;
    USHORT deviceAddress;
    ULONG numOfOpenPipes;
    USB_CONNECTION_STATUS connStat;

    WWUsbDeviceStrings devStr;
    std::vector<WWStringDesc> sds;
    USB_DEVICE_DESCRIPTOR devDesc;
    USB_CONFIGURATION_DESCRIPTOR        confDesc;
    USB_BOS_DESCRIPTOR                  bosDesc;

    WWUsbDevice(int aIdx, std::wstring aName) {
        idx = aIdx;
        name = aName;
    }

    WWUsbDevice(int aIdx) {
        idx = aIdx;
    }
    ~WWUsbDevice(void) {
    }
};

extern std::vector<WWUsbDevice>  mDevices;

struct WWHostController
{
    int idx;
    std::vector<int> children;

    WWUsbDeviceType deviceType;
    std::wstring                        driverKey;
    ULONG                               vendorID;
    ULONG                               deviceID;
    ULONG                               subSysID;
    ULONG                               revision;
    ULONG                               busNumber;
    USHORT                              busDevice;
    USHORT                              busFunction;
    WWUsbDeviceStrings                  devStr;
    USBUSER_CONTROLLER_INFO_0           uuci;
};



struct WWHubPort
{
    int idx;
    std::wstring name;
    WWUsbDeviceBusSpeed speed;
};


// @return pdr_r free()で開放して下さい。
HRESULT
WWGetConfigDescriptor(
    HANDLE  hHub,
    ULONG   connIdx,
    UCHAR   descIdx,
    PUSB_DESCRIPTOR_REQUEST *pdr_r);

HRESULT
WWGetBOSDescriptor(
    HANDLE  hHub,
    ULONG   connIdx,
    PUSB_DESCRIPTOR_REQUEST *pdr_r);

bool
WWStringDescAvailable(
    PUSB_DEVICE_DESCRIPTOR          dd,
    PUSB_CONFIGURATION_DESCRIPTOR   cd);

HRESULT
WWGetAllStringDescs(
    HANDLE                          hHub,
    ULONG                           connIdx,
    PUSB_DEVICE_DESCRIPTOR          dd,
    PUSB_CONFIGURATION_DESCRIPTOR   cd,
    std::vector<WWStringDesc> &desc_r);

