//　日本語
#pragma once

#include <Windows.h>
#include "WWWave1DGpu.h"

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// アップサンプル GPU処理

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Init(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo);

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_InitWithResamplePosArray(
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

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_EnumAdapter(void);

struct WWDirectComputeAdapterDesc {
    wchar_t name[256];
    int64_t videoMemoryBytes;
};

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_GetAdapterDesc(int idx, WWDirectComputeAdapterDesc *desc);

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_ChooseAdapter(int idx);

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Setup(int dataCount, float deltaT, float sc, float c0, float *loss, float *roh, float *cr);

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

