// 日本語。

#include "WWResampleGpu.h"
#include "WWDCUtil.h"

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。
/// この数値を書き換えたらシェーダーも書き換える必要あるかもしれない。
#define GROUP_THREAD_COUNT 1024

/// シェーダーに渡す定数。
struct ConstShaderParams {
    uint32_t c_convOffs;
    uint32_t c_dispatchCount;
    uint32_t c_sampleToStartPos;
    uint32_t c_reserved2;
};

HRESULT
WWResampleGpu::Init(void)
{
    return mDC.Init();
}

HRESULT
WWResampleGpu::ChooseAdapter(int idx)
{
    return mDC.ChooseAdapter(idx);
}

float*
WWResampleGpu::AllocSampleFromMem(int sampleTotalFrom)
{
    assert(0 < sampleTotalFrom);
    mSampleTotalFrom = sampleTotalFrom;

    assert(mSampleFrom == nullptr);
    mSampleFrom = new float[sampleTotalFrom];

    // 念のためメモリにタッチする。
    ZeroMemory(mSampleFrom, sampleTotalFrom * sizeof(float));

    return mSampleFrom;
}

HRESULT
WWResampleGpu::Setup(
        int convolutionN,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        bool highPrecision)
{
    bool    result = true;
    HRESULT hr = S_OK;
    int* resamplePosArray = nullptr;
    double* fractionArray = nullptr;
    double* sinPreComputeArray = nullptr;
    ConstShaderParams shaderParams;

    assert(0 < convolutionN);

    assert(0 < mSampleTotalFrom);
    assert(mSampleFrom);

#if 0
    printf("mSampleFrom: ");
    for (int i = 0; i < 10; ++i) {
        printf("%f ", mSampleFrom[i]);
    }
    printf("\n");
#endif

    /* sampleRateを下げるときは、あらかじめオリジナル信号にローパスフィルターを通し
     * sampleRateTo/2以上の信号が無い状態にして呼んで下さい。
     * そうしないとエイリアシング雑音で聴くに堪えない騒音になります。
     */
     //assert(sampleRateFrom <= sampleRateTo);

    assert(0 < sampleTotalTo);

    mConvolutionN = convolutionN;

    mSampleRateFrom = sampleRateFrom;
    mSampleRateTo = sampleRateTo;
    mSampleTotalTo = sampleTotalTo;

    assert(mSampleTo == nullptr);

    resamplePosArray = new int[sampleTotalTo];
    assert(resamplePosArray);

    fractionArray = new double[sampleTotalTo];
    assert(fractionArray);

    sinPreComputeArray = new double[sampleTotalTo];
    assert(sinPreComputeArray);

    for (int i = 0; i < sampleTotalTo; ++i) {
        double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
#if 1
        /* -0.5 <= fraction<+0.5 の範囲になるようにする。
         * 最後のほうで範囲外を指さないようにする。
         */
        int resamplePosI = (int)(resamplePos + 0.5);
        if (mSampleTotalFrom <= resamplePosI) {
            resamplePosI = mSampleTotalFrom - 1;
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
        fractionArray[i] = fraction;
        sinPreComputeArray[i] = sin(-PI_D * fraction);
    }

    // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    {   // コンピュートシェーダーをコンパイルする。
        // HLSLの中の#defineの値を決めます。
        char      convStartStr[32];
        sprintf_s(convStartStr, "%d", -convolutionN);
        char      convEndStr[32];
        sprintf_s(convEndStr, "%d", convolutionN);
        char      convCountStr[32];
        sprintf_s(convCountStr, "%d", convolutionN * 2);
        char      sampleTotalFromStr[32];
        sprintf_s(sampleTotalFromStr, "%d", mSampleTotalFrom);
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
                nullptr, nullptr
        };

        assert(nullptr == mCS.shader.Get());
        HRG(mDC.CreateShader(L"SincConvolution3.hlsl", "CSMain", "cs_5_0", defines, mCS));
    }

    HRG(mDC.CreateSrvUavHeap(GB_NUM, mSUHeap));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(float), mSampleTotalFrom, mSampleFrom,         mGpuBuf[GB_InputPCM],            mSrv[GB_InputPCM]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(int),    sampleTotalTo,   resamplePosArray,   mGpuBuf[GB_ResamplePosBuf],      mSrv[GB_ResamplePosBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(double), sampleTotalTo,   fractionArray,      mGpuBuf[GB_ResampleFractionBuf], mSrv[GB_ResampleFractionBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(double), sampleTotalTo,   sinPreComputeArray, mGpuBuf[GB_SinPrecomputeBuf],    mSrv[GB_SinPrecomputeBuf]));
    HRG(mDC.CreateGpuBufferAndRegisterAsUAV(mSUHeap, sizeof(float),  sampleTotalTo, mGpuBuf[GB_OutPCM], mUav));
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
    shaderParams.c_dispatchCount = mConvolutionN * 2 / GROUP_THREAD_COUNT;
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
WWResampleGpu::ResultCopyGpuMemoryToCpuMemory(void)
{
    HRESULT hr = S_OK;

    assert(mSampleTo == nullptr);
    mSampleTo = new float[mSampleTotalTo];
    assert(mSampleTo != nullptr);

    // 計算結果をGPUのUAVからCPUに持ってくる。
    HRG(mDC.CopyGpuBufValuesToCpuMemory(mGpuBuf[GB_OutPCM], mSampleTo, mSampleTotalTo * sizeof(float)));

#if 0
    printf("mSampleTo: ");
    for (int i = 0; i < 10; ++i) {
        printf("%f ", mSampleTo[i]);
    }
    printf("\n");
#endif


end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        // Dispatchでエラーが起きた時、ここでこのエラーがよく起きる。
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

void
WWResampleGpu::Unsetup(void)
{
    delete[] mSampleTo;
    mSampleTo = nullptr;

    delete[] mSampleFrom;
    mSampleFrom = nullptr;

    for (int i = 0; i < GB_NUM; ++i) {
        mGpuBuf[i].Reset();
    }
    mCBuf.Reset();
    mCState.Reset();
    mSUHeap.Reset();
    mCS.Reset();
}

void
WWResampleGpu::Term(void)
{
    delete[] mSampleFrom;
    mSampleFrom = nullptr;

    for (int i = 0; i < GB_NUM; ++i) {
        mGpuBuf[i].Reset();
    }
    mCBuf.Reset();
    mCState.Reset();
    mSUHeap.Reset();
    mCS.Reset();

    mDC.Term();
}
