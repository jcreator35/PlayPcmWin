// 日本語。

#include "framework.h"

#pragma once

enum WWDC12FuncType {
    WWDC12T_Resample,
};

#pragma pack(push, 4)
struct WWDirectComputeAdapterDesc {
    int32_t videoMemoryMiB;
    int32_t systemMemoryMiB;
    int32_t sharedMemoryMiB;
    int32_t featureSupported;

    int32_t dxgiAdapterFlags;
    int32_t pad0;
    int32_t pad1;
    int32_t pad2;

    wchar_t name[128];
};
#pragma pack(pop)

extern "C" __declspec(dllexport)
void __stdcall
WWDC12_Term(void);

/// アダプターの個数が戻る。
/// 0の時一つも無い。負の時エラーコード HRESULT。
extern "C" __declspec(dllexport)
int __stdcall
WWDC12_EnumAdapter(void);

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_GetAdapterDesc(int idx, WWDirectComputeAdapterDesc & desc);

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_ChooseAdapter(int idx);

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_Setup(
    int convolutionN,
    float* sampleFrom,
    int sampleTotalFrom,
    int sampleRateFrom,
    int sampleRateTo,
    int sampleTotalTo);

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_Dispatch(
    int startPos,
    int count);

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_ResultGetFromGpuMemory(
    float* outputTo,
    int outputToElemNum);
