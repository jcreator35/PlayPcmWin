// 日本語。

#include "WWDC12IOIF.h"
#include "WWResampleGpu.h"

// Resample機能のみ。
static WWResampleGpu gResample;

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Init(void)
{
    return gResample.Init();
}

extern "C" __declspec(dllexport)
void __stdcall
WWDC12_Term(void)
{
    gResample.Term();
}

/// アダプターの個数が戻る。
/// 0の時一つも無い。負の時エラーコード HRESULT。
extern "C" __declspec(dllexport)
int __stdcall
WWDC12_EnumAdapter(void)
{
    int hr = gResample.EnumGpuAdapters();
    if (hr < 0) {
        return hr;
    }

    return gResample.NumOfAdapters();
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_GetAdapterDesc(int idx, WWDirectComputeAdapterDesc & desc)
{
    WWDirectCompute12AdapterInf ai;
    int hr = gResample.GetNthAdapterInf(idx, ai);
    if (hr < 0) {
        return hr;
    }

    wcsncpy_s(desc.name, ai.name, ARRAYSIZE(desc.name) - 1);
    desc.videoMemoryMiB = ai.dedicatedVideoMemoryMiB;
    desc.systemMemoryMiB = ai.dedicatedSystemMemoryMiB;
    desc.sharedMemoryMiB = ai.sharedSystemMemoryMiB;
    desc.featureSupported = ai.supportsFeatureLv;
    desc.dxgiAdapterFlags = ai.dxgiAdapterFlags;
    desc.pad0 = 0;
    desc.pad1 = 0;
    desc.pad2 = 0;

    return S_OK;
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_ChooseAdapter(int idx)
{
    return gResample.ChooseAdapter(idx);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Resample機能。

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_PrepareFromSamplePtr(int sampleTotalFrom, float** pp_out)
{
    *pp_out = gResample.AllocSampleFromMem(sampleTotalFrom);
    return S_OK;
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_Setup(
    int convolutionN,
    int sampleRateFrom,
    int sampleRateTo,
    int sampleTotalTo)
{
    bool highPrecision = true;
    return gResample.Setup(convolutionN,
        sampleRateFrom, sampleRateTo, sampleTotalTo, highPrecision);
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_Dispatch(
    int startPos,
    int count)
{
    return gResample.Dispatch(startPos, count);
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC12_Resample_ResultGetFromGpuMemory(float **pp_out)
{
    int hr = gResample.ResultCopyGpuMemoryToCpuMemory();
    if (hr < 0) {
        return hr;
    }

    *pp_out = gResample.GetResultPtr();
    return hr;
}

extern "C" __declspec(dllexport)
void __stdcall
WWDC12_Resample_Unsetup(void)
{
    gResample.Unsetup();
}
