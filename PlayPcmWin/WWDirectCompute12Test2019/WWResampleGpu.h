// 日本語。

#pragma once

#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

class WWResampleGpu {
public:
    HRESULT Setup(
        int convolutionN,
        float* sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        bool highPrecision,
        int gpuNr = -1);

    HRESULT Dispatch(
        int startPos,
        int count);

    HRESULT ResultGetFromGpuMemory(
        float* outputTo,
        int outputToElemNum);

    void Term(void);

private:
    float* m_sampleFrom = nullptr;
    int m_convolutionN = 0;
    int m_sampleTotalFrom = 0;
    int m_sampleRateFrom = 0;
    int m_sampleRateTo = 0;
    int m_sampleTotalTo = 0;

    WWDirectCompute12User mDC;
    WWShader mCS;
    WWSrvUavHeap mSUHeap;
    WWComputeState mCState;
    WWConstantBuffer mCBuf;

    enum GpuBufType {
        GB_InputPCM,
        GB_ResamplePosBuf,
        GB_ResampleFractionBuf,
        GB_SinPrecomputeBuf,
        GB_OutPCM,
        GB_NUM
    };

    static const int NUM_CONSTS = 1;
    static const int NUM_SRV = 4;
    static const int NUM_UAV = 1;

    WWGpuBuf mGpuBuf[GB_NUM];

    WWSrv mSrv[NUM_SRV];
    WWUav mUav;
};
