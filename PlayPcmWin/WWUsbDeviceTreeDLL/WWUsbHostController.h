// 日本語。

#pragma once

#include "WWUsbCommon.h"

void WWHostControllersClear(void);

struct WWHostController
{
    int idx;
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

extern std::vector<WWHostController> mHCs;

HRESULT
WWGetHostControllerInf(
    HANDLE hDev,
    HDEVINFO devInf,
    SP_DEVINFO_DATA &sdd);
