#pragma once

extern "C" {

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_Init(void);

__declspec(dllexport)
int __stdcall
WWUsbDeviceTreeDLL_Refresh(void);

__declspec(dllexport)
void __stdcall
WWUsbDeviceTreeDLL_Term(void);



}; // Extern "C"

