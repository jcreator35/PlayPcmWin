//　日本語
#include "WWUpsampleGpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <float.h>

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。
/// この数値を書き換えたらシェーダーも書き換える必要あり。
#define GROUP_THREAD_COUNT 1024

#define PI_D 3.141592653589793238462643
#define PI_F 3.141592653589793238462643f

/// シェーダーに渡す定数。16バイトの倍数でないといけないらしい。
struct ConstShaderParams {
    unsigned int c_convOffs;
    unsigned int c_dispatchCount;
    unsigned int c_sampleToStartPos;
    unsigned int c_reserved2;
};

void
WWUpsampleGpu::Init(void)
{
    int m_convolutionN = 0;
    float * m_sampleFrom = nullptr;
    int m_sampleTotalFrom = 0;
    int m_sampleRateFrom = 0;
    int m_sampleRateTo = 0;
    int m_sampleTotalTo = 0;

    m_pCS  = nullptr;

    m_pBuf0Srv = nullptr;
    m_pBuf1Srv = nullptr;
    m_pBuf2Srv = nullptr;
    m_pBuf3Srv = nullptr;
    m_pBufResultUav = nullptr;
}

void
WWUpsampleGpu::Term(void)
{
    assert(m_pCS  == nullptr);

    assert(m_pBuf0Srv == nullptr);
    assert(m_pBuf1Srv == nullptr);
    assert(m_pBuf2Srv == nullptr);
    assert(m_pBuf3Srv == nullptr);
    assert(m_pBufResultUav == nullptr);
}

static void
PrepareResamplePosArray(
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int * resamplePosArray,
        double *fractionArrayD)
{
    for (int i=0; i<sampleTotalTo; ++i) {
        double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
        /* -0.5 <= fraction<+0.5になるようにresamplePosを選ぶ。
         * 最後のほうで範囲外を指さないようにする。
         */
        int resamplePosI = (int)(resamplePos+0.5);
        if (resamplePosI < 0) {
            resamplePosI = 0;
        }
        if (sampleTotalFrom <= resamplePosI) {
            resamplePosI = sampleTotalFrom -1;
        }
        double fraction = resamplePos - resamplePosI;

        resamplePosArray[i] = resamplePosI;
        fractionArrayD[i]   = fraction;
    }
}

static void
PrepareSinPreComputeArray(
        const double *fractionArray, int sampleTotalTo, float *sinPreComputeArray)
{
    for (int i=0; i<sampleTotalTo; ++i) {
        sinPreComputeArray[i] = (float)sin(-PI_D * fractionArray[i]);
    }
}

HRESULT
WWUpsampleGpu::Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int * resamplePosArray,
        double *fractionArrayD)
{
    HRESULT hr = S_OK;
    float * sinPreComputeArray = nullptr;

    assert(0 < convolutionN);
    assert(sampleFrom);
    assert(0 < sampleTotalFrom);
    assert(sampleRateFrom <= sampleRateTo);
    assert(0 < sampleTotalTo);

    m_convolutionN    = convolutionN;
    m_sampleTotalFrom = sampleTotalFrom;
    m_sampleRateFrom  = sampleRateFrom;
    m_sampleRateTo    = sampleRateTo;
    m_sampleTotalTo   = sampleTotalTo;

    // sinPreComputeArrayの精度を高めるためdoubleprecのfractionArrayDから計算する。
    // こうすることで歪が減ると良いなぁ。
    sinPreComputeArray = new float[sampleTotalTo];
    assert(sinPreComputeArray);
    PrepareSinPreComputeArray(fractionArrayD, sampleTotalTo, sinPreComputeArray);

    // ここでsingleprecのfractionArrayF作成。
    float *fractionArrayF = new float[sampleTotalTo];
    assert(fractionArrayF);
    for (int i=0; i<sampleTotalTo; ++i) {
        fractionArrayF[i] = (float)fractionArrayD[i];
    }

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("i=%6d rPos=%6d fraction=%+f\n",
            i, resamplePosArray[i], fractionArray[i]);
    }
    printf("sampleTotal=%d\n", i);
    */

    // HLSLの#defineを作る。
    char      convStartStr[32];
    sprintf_s(convStartStr, "%d", -convolutionN);
    char      convEndStr[32];
    sprintf_s(convEndStr,   "%d", convolutionN);
    char      convCountStr[32];
    sprintf_s(convCountStr, "%d", convolutionN*2);
    char      sampleTotalFromStr[32];
    sprintf_s(sampleTotalFromStr,   "%d", sampleTotalFrom);
    char      sampleTotalToStr[32];
    sprintf_s(sampleTotalToStr,   "%d", sampleTotalTo);

    char      sampleRateFromStr[32];
    sprintf_s(sampleRateFromStr,   "%d", sampleRateFrom);
    char      sampleRateToStr[32];
    sprintf_s(sampleRateToStr,   "%d", sampleRateTo);
    char      iterateNStr[32];
    sprintf_s(iterateNStr,  "%d", convolutionN*2/GROUP_THREAD_COUNT);
    char      groupThreadCountStr[32];
    sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

    // doubleprec
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

    // HLSL ComputeShaderをコンパイルしてGPUに送る。
    HRG(mCU.CreateComputeShader(L"SincConvolution2.hlsl", "CSMain", defines, &m_pCS));
    assert(m_pCS);

    // 入出力バッファの準備。
    WWStructuredBufferParams sb[] = {
        {sizeof sampleFrom[0], sampleTotalFrom, sampleFrom, "SampleFromBuffer", &m_pBuf0Srv, nullptr},
        {sizeof resamplePosArray[0], sampleTotalTo, resamplePosArray, "ResamplePosBuffer", &m_pBuf1Srv, nullptr},
        {sizeof fractionArrayF[0], sampleTotalTo, fractionArrayF, "FractionBuffer", &m_pBuf2Srv, nullptr},
        {sizeof sinPreComputeArray[0], sampleTotalTo, sinPreComputeArray, "SinPreComputeBuffer", &m_pBuf3Srv, nullptr},
        {sizeof(float), sampleTotalTo, nullptr, "OutputBuffer", nullptr, &m_pBufResultUav},
    };
    HRG(mCU.CreateSeveralStructuredBuffer(sizeof sb/sizeof sb[0], sb));

    assert(m_pBuf0Srv);
    assert(m_pBuf1Srv);
    assert(m_pBuf2Srv);
    assert(m_pBuf3Srv);
    assert(m_pBufResultUav);

end:
    SAFE_DELETE(fractionArrayF);
    SAFE_DELETE(sinPreComputeArray);

    return hr;
}

// without resamplePosArray
HRESULT
WWUpsampleGpu::Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo)
{
    bool    result = true;
    HRESULT hr     = S_OK;

    assert(0 < convolutionN);
    assert(sampleFrom);
    assert(0 < sampleTotalFrom);
    assert(sampleRateFrom <= sampleRateTo);
    assert(0 < sampleTotalTo);

    int * resamplePosArray = new int[sampleTotalTo];
    assert(resamplePosArray);

    double * fractionArrayD = new double[sampleTotalTo];
    assert(fractionArrayD);

    PrepareResamplePosArray(
          sampleTotalFrom,
          sampleRateFrom,
          sampleRateTo,
          sampleTotalTo,
          resamplePosArray,
          fractionArrayD);

    HRG(Setup(
        convolutionN,
        sampleFrom,
        sampleTotalFrom,
        sampleRateFrom,
        sampleRateTo,
        sampleTotalTo,
        resamplePosArray,
        fractionArrayD));

end:
    SAFE_DELETE(fractionArrayD);
    SAFE_DELETE(resamplePosArray);

    return hr;
}

HRESULT
WWUpsampleGpu::Dispatch(
        int startPos,
        int count)
{
    HRESULT hr = S_OK;
    bool result = true;

    // GPU上でComputeShader実行。
    ID3D11ShaderResourceView* aRViews[]
        = { m_pBuf0Srv, m_pBuf1Srv, m_pBuf2Srv, m_pBuf3Srv };
    ConstShaderParams shaderParams;
    ZeroMemory(&shaderParams, sizeof shaderParams);
#if 1
    // すこしだけ速い。中でループするようにした。
    shaderParams.c_convOffs = 0;
    shaderParams.c_dispatchCount = m_convolutionN*2/GROUP_THREAD_COUNT;
    shaderParams.c_sampleToStartPos = startPos;
    HRGR(mCU.Run(m_pCS, sizeof aRViews/sizeof aRViews[0], aRViews, 1,
        &m_pBufResultUav, &shaderParams, sizeof shaderParams, count, 1, 1));
#else
    // 遅い
    for (int i=0; i<convolutionN*2/GROUP_THREAD_COUNT; ++i) {
        shaderParams.c_convOffs = i * GROUP_THREAD_COUNT;
        shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
        shaderParams.c_sampleToStartPos = startPos;
        HRGR(mCU.Run(m_pCS, sizeof aRViews/sizeof aRViews[0], aRViews,
            1, &m_pBufResultUav,
            &shaderParams, sizeof shaderParams, count, 1, 1));
    }
#endif

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED reason=%08x\n",
            mCU.GetDevice()->GetDeviceRemovedReason());
    }

    return hr;
}

HRESULT
WWUpsampleGpu::GetResultFromGpuMemory(
        float *outputTo,
        int outputToElemNum)
{
    HRESULT hr = S_OK;

    assert(m_pBufResultUav);

    assert(outputTo);
    assert(outputToElemNum <= m_sampleTotalTo);

    // 計算結果をCPUメモリーに持ってくる。
    HRG(mCU.RecvResultToCpuMemory(m_pBufResultUav, outputTo,
        outputToElemNum * sizeof(float)));
end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED reason=%08x\n",
            mCU.GetDevice()->GetDeviceRemovedReason());
    }

    return hr;
}

void
WWUpsampleGpu::Unsetup(void)
{
    mCU.DestroyResourceAndUAV(m_pBufResultUav);
    m_pBufResultUav = nullptr;

    mCU.DestroyResourceAndSRV(m_pBuf3Srv);
    m_pBuf3Srv = nullptr;

    mCU.DestroyResourceAndSRV(m_pBuf2Srv);
    m_pBuf2Srv = nullptr;

    mCU.DestroyResourceAndSRV(m_pBuf1Srv);
    m_pBuf1Srv = nullptr;

    mCU.DestroyResourceAndSRV(m_pBuf0Srv);
    m_pBuf0Srv = nullptr;

    if (m_pCS) {
        mCU.DestroyComputeShader(m_pCS);
        m_pCS = nullptr;
    }

    mCU.Term();
}
