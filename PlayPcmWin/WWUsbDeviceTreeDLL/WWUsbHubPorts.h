// 日本語。

#pragma once

#include "WWUsbCommon.h"

void WWHubPortsClear(void);

struct WWHubPort
{
    int idx;
    int connIdx;
    WWUsbDeviceBusSpeed speed;
    USB_DEVICE_DESCRIPTOR devDesc;
    BOOLEAN deviceIsHub;
    USHORT deviceAddress;
    DWORD numOfOpenPipes;
    USB_CONNECTION_STATUS connStat;

    std::wstring driverKey;
    WWUsbDeviceStrings devStr;
    PUSB_CONFIGURATION_DESCRIPTOR confDesc;
    PUSB_BOS_DESCRIPTOR bosDesc;
    std::vector<WWStringDesc> sds;

    std::wstring extHubName;

    // freeする必要あり。
    PUSB_NODE_CONNECTION_INFORMATION_EX cie;

    // freeする必要あり。
    PUSB_PORT_CONNECTOR_PROPERTIES pcp;

    // confDescを保持するメモリ領域の先頭。ここをfreeする。
    PUSB_DESCRIPTOR_REQUEST pcr;
    // bosDescを保持するメモリ領域の先頭。ここをfreeする。
    PUSB_DESCRIPTOR_REQUEST pbr;
};

extern std::vector<WWHubPort> mHPs;

HRESULT WWEnumHubPorts(int level, HANDLE hHub, int hubIdx, int numPorts);
