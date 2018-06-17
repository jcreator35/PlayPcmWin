//　日本語
#include "WWDirectComputeIOIF.h"
#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWWave1DGpu.h"
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

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static WWWave1DGpu gWave1D;

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Init(int dataCount, float deltaT, float sc, float c0, float *loss, float *roh, float *cr)
{
    return gWave1D.Init(dataCount, deltaT, sc, c0, loss, roh, cr);
}

extern "C" __declspec(dllexport)
void __stdcall
WWDCWave1D_Term(void)
{
    gWave1D.Term();
}

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Run(int cRepeat, int stimNum, WWWave1DStim *stim, float *v, float *p)
{
    return gWave1D.Run(cRepeat, stimNum, stim, v, p);
}

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_GetResult(
        int outputToElemNum,
        float * outputVTo,
        float * outputPTo)
{
    gWave1D.CopyResultV(outputVTo, outputToElemNum);
    return gWave1D.CopyResultP(outputPTo, outputToElemNum);
}
