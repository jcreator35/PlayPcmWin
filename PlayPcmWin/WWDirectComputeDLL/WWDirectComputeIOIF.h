#pragma once

#include <Windows.h>

/////////////////////////////////////////////////////////////////////////////
// �A�b�v�T���v�� GPU����

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

/////////////////////////////////////////////////////////////////////////////
// CPU����

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

