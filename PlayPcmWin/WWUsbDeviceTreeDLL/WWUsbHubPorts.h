// 日本語。

#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include <usb.h>
#include <usbuser.h>

HRESULT WWEnumHubPorts(HANDLE hHub, int hubIdx, int numPorts);
