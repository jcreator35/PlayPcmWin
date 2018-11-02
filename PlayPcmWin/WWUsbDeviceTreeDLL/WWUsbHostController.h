// 日本語。

#pragma once

#include "WWUsbCommon.h"

void WWHostControllersClear(void);

struct WWHostController
{
    int idx;
    std::wstring                        driverKey;
    ULONG                               vendorID;
    ULONG                               deviceID;
    ULONG                               subSysID;
    ULONG                               revision;
    ULONG                               busNumber;
    USHORT                              busDevice;
    USHORT                              busFunction;
    WWUsbDeviceStrings                  devStr;
    USB_CONTROLLER_INFO_0               ci0;
    USB_BANDWIDTH_INFO                  bir;
    USB_BUS_STATISTICS_0                bs0;
    USB_POWER_INFO                      pir;
    USB_DRIVER_VERSION_PARAMETERS       dvp;
};

extern std::vector<WWHostController> mHCs;

HRESULT
WWGetHostControllerInf(
    HANDLE hDev,
    HDEVINFO devInf,
    SP_DEVINFO_DATA &sdd);
