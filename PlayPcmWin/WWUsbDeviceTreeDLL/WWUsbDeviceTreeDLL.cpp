// 日本語。

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include "WWUsbDeviceTreeDLL.h"
#include "WWUsbHub.h"
#include "WWUsbHostController.h"
#include "WWUsbBuildDeviceTree.h"
#include "WWUsbHubPorts.h"
#include "WWUsbVendorIdToStr.h"

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


}; // extern "C"
