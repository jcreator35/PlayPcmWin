// 日本語。

#include "WWResampleGpu.h"
#include "WWDCUtil.h"

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。
/// この数値を書き換えたらシェーダーも書き換える必要あり。
#define GROUP_THREAD_COUNT 1024

/// シェーダーに渡す定数。
struct ConstShaderParams {
    uint32_t c_convOffs;
    uint32_t c_dispatchCount;
    uint32_t c_sampleToStartPos;
    uint32_t c_reserved2;
};

float
WWResampleGpu::LimitSampleData(
    float* sampleData,
    int sampleDataCount)
{
    float minV = 0.0f;
    float maxV = 0.0f;

    for (int i = 0; i < sampleDataCount; ++i) {
        if (sampleData[i] < minV) {
            minV = sampleData[i];
        }
        if (maxV < sampleData[i]) {
            maxV = sampleData[i];
        }
    }

    float scale = 1.0f;
    if (minV < -1.0f) {
        scale = -1.0f / minV;
    }
    if (0.99999988079071044921875f < maxV) {
        float scale2 = 0.99999988079071044921875f / maxV;
        if (scale2 < scale) {
            scale = scale2;
        }
    }
    if (scale < 1.0f) {
        for (int i = 0; i < sampleDataCount; ++i) {
            sampleData[i] *= scale;
        }
    }

    return scale;
}

void
WWResampleGpu::Init(void)
{
    int m_convolutionN = 0;
    float* m_sampleFrom = nullptr;
    int m_sampleTotalFrom = 0;
    int m_sampleRateFrom = 0;
    int m_sampleRateTo = 0;
    int m_sampleTotalTo = 0;
}

void
WWResampleGpu::Term(void)
{
}

HRESULT
WWResampleGpu::Setup(
    int convolutionN,
    float* sampleFrom,
    int sampleTotalFrom,
    int sampleRateFrom,
    int sampleRateTo,
    int sampleTotalTo,
    bool highPrecision)
{
    bool    result = true;
    HRESULT hr = S_OK;
    int* resamplePosArray = nullptr;
    float* fractionArray = nullptr;
    float* sinPreComputeArray = nullptr;
    ConstShaderParams shaderParams;

    assert(0 < convolutionN);
    assert(sampleFrom);
    assert(0 < sampleTotalFrom);

    /* sampleRateを下げるときは、あらかじめオリジナル信号にローパスフィルターを通し
     * sampleRateTo/2以上の信号が無い状態にして呼んで下さい。
     * そうしないとエイリアシング雑音で聴くに堪えない騒音になります。
     */
     //assert(sampleRateFrom <= sampleRateTo);

    assert(0 < sampleTotalTo);

    m_convolutionN = convolutionN;
    m_sampleFrom = sampleFrom;
    m_sampleTotalFrom = sampleTotalFrom;
    m_sampleRateFrom = sampleRateFrom;
    m_sampleRateTo = sampleRateTo;
    m_sampleTotalTo = sampleTotalTo;

    resamplePosArray = new int[sampleTotalTo];
    assert(resamplePosArray);

    fractionArray = new float[sampleTotalTo];
    assert(fractionArray);

    sinPreComputeArray = new float[sampleTotalTo];
    assert(sinPreComputeArray);

    for (int i = 0; i < sampleTotalTo; ++i) {
        double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
#if 1
        /* -0.5 <= fraction<+0.5 の範囲になるようにする。
         * 最後のほうで範囲外を指さないようにする。
         */
        int resamplePosI = (int)(resamplePos + 0.5);
        if (sampleTotalFrom <= resamplePosI) {
            resamplePosI = sampleTotalFrom - 1;
        }
#else
        /* 0<=fraction<1になるにresamplePosIを選ぶ。
         * これは1に近い値が頻出するのでよくない。
         */
        int resamplePosI = (int)(resamplePos + 0.5);
        assert(resamplePosI < sampleTotalFrom);
#endif
        double fraction = resamplePos - resamplePosI;

        resamplePosArray[i] = resamplePosI;
        fractionArray[i] = (float)fraction;
        sinPreComputeArray[i] = (float)sin(-PI_D * fraction);
    }

    HRG(mDC.Init(0));

    {   // コンピュートシェーダーをコンパイルする。
        // HLSLの中の#defineの値を決めます。
        char      convStartStr[32];
        sprintf_s(convStartStr, "%d", -convolutionN);
        char      convEndStr[32];
        sprintf_s(convEndStr, "%d", convolutionN);
        char      convCountStr[32];
        sprintf_s(convCountStr, "%d", convolutionN * 2);
        char      sampleTotalFromStr[32];
        sprintf_s(sampleTotalFromStr, "%d", sampleTotalFrom);
        char      sampleTotalToStr[32];
        sprintf_s(sampleTotalToStr, "%d", sampleTotalTo);

        char      sampleRateFromStr[32];
        sprintf_s(sampleRateFromStr, "%d", sampleRateFrom);
        char      sampleRateToStr[32];
        sprintf_s(sampleRateToStr, "%d", sampleRateTo);
        char      iterateNStr[32];
        sprintf_s(iterateNStr, "%d", convolutionN * 2 / GROUP_THREAD_COUNT);
        char      groupThreadCountStr[32];
        sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

        char      highPrecisionStr[32];
        sprintf_s(highPrecisionStr, "%d", 0 != highPrecision);

        const D3D_SHADER_MACRO defines[] = {
                "CONV_START", convStartStr,
                "CONV_END", convEndStr,
                "CONV_COUNT", convCountStr,
                "SAMPLE_TOTAL_FROM", sampleTotalFromStr,
                "SAMPLE_TOTAL_TO", sampleTotalToStr,

                "SAMPLE_RATE_FROM", sampleRateFromStr,
                "SAMPLE_RATE_TO", sampleRateToStr,
                "ITERATE_N", iterateNStr,
                "GROUP_THREAD_COUNT", groupThreadCountStr,
                "HIGH_PRECISION", highPrecisionStr,
                nullptr, nullptr
        };

        HRG(mDC.CreateShader(L"SincConvolution3.hlsl", "CSMain", "cs_5_0", defines, mCS));
    }

    HRG(mDC.CreateSrvUavHeap(GB_NUM, mSUHeap));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(float), sampleTotalFrom, sampleFrom,         mGpuBuf[GB_InputPCM],            mSrv[GB_InputPCM]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(int),   sampleTotalTo,   resamplePosArray,   mGpuBuf[GB_ResamplePosBuf],      mSrv[GB_ResamplePosBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(float), sampleTotalTo,   fractionArray,      mGpuBuf[GB_ResampleFractionBuf], mSrv[GB_ResampleFractionBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(float), sampleTotalTo,   sinPreComputeArray, mGpuBuf[GB_SinPrecomputeBuf],    mSrv[GB_SinPrecomputeBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsUAV(mSUHeap, sizeof(float), sampleTotalTo, mGpuBuf[GB_OutPCM], mUav));
    HRG(mDC.CreateComputeState(mCS, NUM_CONSTS, NUM_SRV, NUM_UAV, mCState));
    
    ZeroMemory(&shaderParams, sizeof shaderParams);
    HRG(mDC.CreateConstantBuffer(sizeof shaderParams, mCBuf));
    HRG(mDC.UpdateConstantBufferData(mCBuf, &shaderParams));

    // CPU上で準備したデータをGPUに送り出し、完了するまで待つ。
    HRG(mDC.CloseExecResetWait());

end:
    delete[] sinPreComputeArray;
    sinPreComputeArray = nullptr;

    delete[] fractionArray;
    fractionArray = nullptr;

    delete[] resamplePosArray;
    resamplePosArray = nullptr;

    return hr;
}

HRESULT
WWResampleGpu::Dispatch(
    int startPos,
    int count)
{
    HRESULT hr = S_OK;
    bool result = true;

    ConstShaderParams shaderParams;
    ZeroMemory(&shaderParams, sizeof shaderParams);
    shaderParams.c_convOffs = 0;
    shaderParams.c_dispatchCount = m_convolutionN * 2 / GROUP_THREAD_COUNT;
    shaderParams.c_sampleToStartPos = startPos;
    HRG(mDC.UpdateConstantBufferData(mCBuf, &shaderParams));

    HRGR(mDC.Run(mCState, &mCBuf, mSUHeap, 0, count, 1, 1));

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

HRESULT
WWResampleGpu::ResultGetFromGpuMemory(
    float* outputTo,
    int outputToElemNum)
{
    HRESULT hr = S_OK;

    assert(outputTo);
    assert(outputToElemNum <= m_sampleTotalTo);

    ZeroMemory(outputTo, outputToElemNum * sizeof(float));

    // 計算結果をGPUのUAVからCPUに持ってくる。
    HRG(mDC.CopyGpuBufValuesToCpuMemory(mGpuBuf[GB_OutPCM], outputTo, outputToElemNum * sizeof(float)));

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

