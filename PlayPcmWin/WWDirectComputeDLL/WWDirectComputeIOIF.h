//　日本語
#pragma once

#include <Windows.h>
#include "WWWave1DGpu.h"

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// 共通デバイス選択処理。

enum WWDirectComputeType {
    WWDCT_Upsample,
    WWDCT_Wave1D,
};

struct WWDirectComputeAdapterDesc {
    wchar_t name[256];
    int64_t videoMemoryBytes;
};

/// アダプターの個数が戻る。
/// 0の時一つも無い。負の時エラーコード HRESULT。
extern "C" __declspec(dllexport)
int __stdcall
WWDC_EnumAdapter(WWDirectComputeType t);

extern "C" __declspec(dllexport)
int __stdcall
WWDC_GetAdapterDesc(WWDirectComputeType t, int idx, WWDirectComputeAdapterDesc *desc);

extern "C" __declspec(dllexport)
int __stdcall
WWDC_ChooseAdapter(WWDirectComputeType t, int idx);

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// アップサンプル GPU処理

/* 使い方。
    Init()
    WWDC_EnumAdapter()
    WWDC_GetAdapterDesc()
    WWDC_GetAdapterVideoMemoryBytes()
    WWDC_ChooseAdaper()
    Setup() or SetupWithResamplePosArray()
    Dispatch(), GetResultFromGpuMemory()
    Dispatch(), GetResultFromGpuMemory()
    ...
    Term()
*/

/// @result HRESULT
extern "C" __declspec(dllexport)
void __stdcall
WWDCUpsample_Init(void);

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo);

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_SetupWithResamplePosArray(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int * resamplePosArray,
        double *fractionArray);

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Dispatch(
        int startPos,
        int count);

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_GetResultFromGpuMemory(
        float * outputTo,
        int outputToElemNum);

extern "C" __declspec(dllexport)
void __stdcall
WWDCUpsample_Term(void);

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Wave1D GPU

/*
    WWWave1DGpu 使い方
    Init()
    WWDC_EnumAdapter()
    WWDC_GetAdapterDesc()
    WWDC_GetAdapterVideoMemoryBytes()
    WWDC_ChooseAdaper()
    Setup()
    Run(), GetResult()
    Run(), GetResult()
    ...
    Term()
*/

extern "C" __declspec(dllexport)
void __stdcall
WWDCWave1D_Init(void);

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Setup(const WWWave1DParams &p, float *loss, float *roh, float *cr);

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Run(int cRepeat, int stimNum, WWWave1DStim *stim, float *v, float *p);

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_GetResult(
        int outputToElemNum,
        float * outputVTo,
        float * outputPTo);

extern "C" __declspec(dllexport)
void __stdcall
WWDCWave1D_Term(void);


// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// CPU処理

extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_UpsampleCpuSetup(
        int convolutionN,
        float * sampleData,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo);

extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_UpsampleCpuSetupWithResamplePosArray(
        int convolutionN,
        float * sampleData,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int *resamplePosArray,
        double *fractionArray);

extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_UpsampleCpuDo(
        int startPos,
        int count,
        float * outputTo);

extern "C" __declspec(dllexport)
void __stdcall
WWDCUpsample_UpsampleCpuUnsetup(void);

