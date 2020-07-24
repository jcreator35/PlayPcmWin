// 日本語。

#pragma once

#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

class WWResampleGpu {
public:

    HRESULT Init(void);

    WWDirectCompute12User& GetDC(void)
    {
        return mDC;
    }

    HRESULT EnumGpuAdapters(void)
    {
        return mDC.EnumGpuAdapters();
    }

    HRESULT NumOfAdapters(void) const
    {
        return mDC.NumOfAdapters();
    }

    HRESULT GetNthAdapterInf(int nth, WWDirectCompute12AdapterInf& adap_out)
    {
        return mDC.GetNthAdapterInf(nth, adap_out);
    }

    HRESULT ChooseAdapter(int nth);

    /// @brief sampleFromメモリを割り当てる。
    /// @param sampleTotalFrom メモリの要素数(floatの個数)。
    /// @return 割り当てたメモリの先頭アドレス。
    float* AllocSampleFromMem(int sampleTotalFrom);

    HRESULT Setup(
        int convolutionN,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        bool highPrecision);

    HRESULT Dispatch(
        int startPos,
        int count);

    float* GetResultPtr(void) const {
        return mSampleTo;
    }

    HRESULT ResultCopyGpuMemoryToCpuMemory(void);

    void Unsetup(void);

    void Term(void);

private:
    float* mSampleFrom = nullptr;
    float* mSampleTo = nullptr;
    int mConvolutionN = 0;
    int mSampleTotalFrom = 0;
    int mSampleRateFrom = 0;
    int mSampleRateTo = 0;
    int mSampleTotalTo = 0;

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
