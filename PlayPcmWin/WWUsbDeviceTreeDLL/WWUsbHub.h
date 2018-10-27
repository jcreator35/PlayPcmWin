// 日本語。

#pragma once

#include "WWUsbCommon.h"

struct WWHub
{
    int idx;
    std::wstring name;
    int  numPorts;
    BOOL isBusPowered; //< TRUE: Bus powered, FALSE: Self powered
    BOOL isRoot;
    WWUsbDeviceBusSpeed hubType;
    WWUsbDeviceBusSpeed speed;
};

extern std::vector<WWHub> mHubs;

HRESULT
WWEnumHub(std::wstring hubName);
