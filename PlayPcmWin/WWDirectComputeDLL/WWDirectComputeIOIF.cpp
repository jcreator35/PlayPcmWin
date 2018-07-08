//　日本語
#include "WWDirectComputeIOIF.h"
#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWWave1DGpu.h"
#include "WWUtil.h"
#include <assert.h>

static WWUpsampleGpu gUpsample;
static WWWave1DGpu   gWave1D;

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// 共通初期化処理。

#define GET_CU(x)               \
    switch (t) {                \
    case WWDCT_Upsample:        \
        x = &gUpsample.GetCU(); \
        break;                  \
    case WWDCT_Wave1D:          \
        x = &gWave1D.GetCU();   \
        break;                  \
    default:                    \
        break;                  \
    }

extern "C" __declspec(dllexport)
int __stdcall
WWDC_EnumAdapter(WWDirectComputeType t)
{
    WWDirectComputeUser *pCU = nullptr;
    GET_CU(pCU);
    assert(pCU);

    int hr = pCU->EnumAdapters();
    if (hr < 0) {
        return hr;
    }

    return pCU->GetNumOfAdapters();
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC_GetAdapterDesc(WWDirectComputeType t, int idx, WWDirectComputeAdapterDesc *desc)
{
    WWDirectComputeUser *pCU = nullptr;
    GET_CU(pCU);
    assert(pCU);

    int hr =  pCU->GetAdapterDesc(idx, desc->name, sizeof desc->name);
    if (hr < 0) {
        return hr;
    }
    return pCU->GetAdapterVideoMemoryBytes(idx, &desc->videoMemoryBytes);
}

extern "C" __declspec(dllexport)
int __stdcall
WWDC_ChooseAdapter(WWDirectComputeType t, int idx)
{
    WWDirectComputeUser *pCU = nullptr;
    GET_CU(pCU);
    assert(pCU);

    return pCU->ChooseAdapter(idx);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// アップサンプル。

extern "C" __declspec(dllexport)
void __stdcall
WWDCUpsample_Init(void)
{
    gUpsample.Init();
}

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo)
{
    return gUpsample.Setup(
        convolutionN, sampleFrom, sampleTotalFrom,
        sampleRateFrom, sampleRateTo, sampleTotalTo);
}

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
        double *fractionArray)
{
    return gUpsample.Setup(
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
    return gUpsample.Dispatch(startPos, count);
}

/// @result HRESULT
extern "C" __declspec(dllexport)
int __stdcall
WWDCUpsample_GetResultFromGpuMemory(
        float * outputTo,
        int outputToElemNum)
{
    int hr = gUpsample.GetResultFromGpuMemory(outputTo, outputToElemNum);
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
    gUpsample.Unsetup();
    gUpsample.Term();
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Wave1D
extern "C" __declspec(dllexport)
void __stdcall
WWDCWave1D_Init(void)
{
    gWave1D.Init();
}

extern "C" __declspec(dllexport)
void __stdcall
WWDCWave1D_Term(void)
{
    gWave1D.Unsetup();
    gWave1D.Term();
}

extern "C" __declspec(dllexport)
int __stdcall
WWDCWave1D_Setup(const WWWave1DParams &p, float *loss, float *roh, float *cr)
{
    return gWave1D.Setup(p, loss, roh, cr);
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
