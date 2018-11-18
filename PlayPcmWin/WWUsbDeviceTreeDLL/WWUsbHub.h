// 日本語。

#pragma once

#include "WWUsbCommon.h"

void WWHubsClear(void);

struct WWHub
{
    int idx;
    int parentIdx;
    std::wstring name;
    int  numPorts;
    BOOL isBusPowered; //< TRUE: Bus powered, FALSE: Self powered
    BOOL isRoot;
    WWUsbDeviceBusSpeed hubType;
    WWUsbDeviceBusSpeed speed;
    USB_NODE_INFORMATION ni;
    USB_HUB_INFORMATION_EX hi;
    USB_HUB_CAPABILITIES_EX hc;
};

extern std::vector<WWHub> mHubs;

HRESULT
WWGetHubInf(int level, int parentIdx, std::wstring hubName);
