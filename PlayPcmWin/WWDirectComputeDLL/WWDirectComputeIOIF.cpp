#include "WWDirectComputeIOIF.h"
#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWUtil.h"
#include <assert.h>

static WWUpsampleGpu g_upsampleGpu;

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Init(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo)
{
    g_upsampleGpu.Init();
    return g_upsampleGpu.Setup(
        convolutionN, sampleFrom, sampleTotalFrom,
        sampleRateFrom, sampleRateTo, sampleTotalTo);
}

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
        double *fractionArray)
{
    g_upsampleGpu.Init();
    return g_upsampleGpu.Setup(
        convolutionN, sampleFrom, sampleTotalFrom,
        sampleRateFrom, sampleRateTo, sampleTotalTo,
        resamplePosArray, fractionArray);
}

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Dispatch(
        int startPos,
        int count)
{
    return g_upsampleGpu.Dispatch(startPos, count);
}

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_GetResultFromGpuMemory(
        float * outputTo,
        int outputToElemNum)
{
    int hr = g_upsampleGpu.GetResultFromGpuMemory(outputTo, outputToElemNum);
    if (hr < 0) {
        return hr;
    }

    // 何倍にスケールしたかわからなくなるので別の関数に分けた。
    //WWUpsampleGpu::LimitSampleData(outputTo, outputToElemNum);

    return hr;
}

extern "C" __declspec(dllexport)
void __stdcall
WWDCUpsample_Term(void)
{
    g_upsampleGpu.Unsetup();
    g_upsampleGpu.Term();
}
