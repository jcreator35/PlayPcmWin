// 日本語。

#include "WWUpsampleGpu.h"
#include "WWUtil.h"



float
WWUpsampleGpu::LimitSampleData(
        float * sampleData,
        int sampleDataCount)
{
    float minV = 0.0f;
    float maxV = 0.0f;

    for (int i=0; i<sampleDataCount; ++i) {
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
        for (int i=0; i<sampleDataCount; ++i) {
            sampleData[i] *= scale;
        }
    }

    return scale;
}

void
WWUpsampleGpu::Init(void)
{
    int m_convolutionN = 0;
    float * m_sampleFrom = nullptr;
    int m_sampleTotalFrom = 0;
    int m_sampleRateFrom = 0;
    int m_sampleRateTo = 0;
    int m_sampleTotalTo = 0;

    m_pDCU = nullptr;
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
    assert(m_pDCU == nullptr);
    assert(m_pCS  == nullptr);

    assert(m_pBuf0Srv == nullptr);
    assert(m_pBuf1Srv == nullptr);
    assert(m_pBuf2Srv == nullptr);
    assert(m_pBuf3Srv == nullptr);
    assert(m_pBufResultUav == nullptr);
}

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
    int * resamplePosArray = nullptr;
    float * fractionArray = nullptr;
    float * sinPreComputeArray = nullptr;


    assert(0 < convolutionN);
    assert(sampleFrom);
    assert(0 < sampleTotalFrom);
    assert(sampleRateFrom <= sampleRateTo);
    assert(0 < sampleTotalTo);

    m_convolutionN    = convolutionN;
    m_sampleFrom      = sampleFrom;
    m_sampleTotalFrom = sampleTotalFrom;
    m_sampleRateFrom  = sampleRateFrom;
    m_sampleRateTo    = sampleRateTo;
    m_sampleTotalTo   = sampleTotalTo;

    resamplePosArray = new int[sampleTotalTo];
    assert(resamplePosArray);

    fractionArray = new float[sampleTotalTo];
    assert(fractionArray);

    sinPreComputeArray = new float[sampleTotalTo];
    assert(sinPreComputeArray);

    for (int i=0; i<sampleTotalTo; ++i) {
        double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
#if 1
        /* -0.5 <= fraction<+0.5‚É‚È‚é‚æ‚¤‚ÉresamplePos‚ð‘I‚ÔB
         * ÅŒã‚Ì‚Ù‚¤‚Å”ÍˆÍŠO‚ðŽw‚‚È‚¢‚æ‚¤‚É‚‚éB
         */
        int resamplePosI = (int)(resamplePos+0.5);
        if (sampleTotalFrom <= resamplePosI) {
            resamplePosI = sampleTotalFrom -1;
        }
#else
        /* 0<=fraction<1‚É‚È‚é‚ÉresamplePosI‚ð‘I‚ÔB
         * ‚±‚ê‚Í1‚É‹ß‚¢’l‚ª•po‚‚é‚Ì‚Å‚æ‚‚È‚¢B
         */
        int resamplePosI = (int)(resamplePos+0.5);
        assert(resamplePosI < sampleTotalFrom);
#endif
        double fraction = resamplePos - resamplePosI;

        resamplePosArray[i]   = resamplePosI;
        fractionArray[i]      = (float)fraction;
        sinPreComputeArray[i] = (float)sin(-PI_D * fraction);
    }

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("i=%6d rPos=%6d fraction=%+f\n",
            i, resamplePosArray[i], fractionArray[i]);
    }
    printf("resamplePos created\n");
    */

    // HLSL‚Ì#define‚ðì‚éB
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

    m_pDCU = new WWDirectComputeUser();
    assert(m_pDCU);

    HRG(m_pDCU->Init());

    // HLSL ComputeShader‚ðƒRƒ“ƒpƒCƒ‹‚‚ÄGPU‚É‘—‚éB
    HRG(m_pDCU->CreateComputeShader(L"SincConvolution2.hlsl", "CSMain", defines, &m_pCS));
    assert(m_pCS);

    // “ü—Íƒf[ƒ^‚ðGPUƒƒ‚ƒŠ[‚É‘—‚é
    HRG(m_pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(float), sampleTotalFrom, sampleFrom, "SampleFromBuffer", &m_pBuf0Srv));
    assert(m_pBuf0Srv);

    HRG(m_pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(int), sampleTotalTo, resamplePosArray, "ResamplePosBuffer", &m_pBuf1Srv));
    assert(m_pBuf1Srv);

    HRG(m_pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(float), sampleTotalTo, fractionArray, "FractionBuffer", &m_pBuf2Srv));
    assert(m_pBuf2Srv);

    HRG(m_pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(float), sampleTotalTo, sinPreComputeArray, "SinPreComputeBuffer", &m_pBuf3Srv));
    assert(m_pBuf3Srv);
    
    // Œ‹‰Êo—Í—Ìˆæ‚ðGPU‚Éì¬B
    HRG(m_pDCU->CreateBufferAndUnorderedAccessView(
        sizeof(float), sampleTotalTo, nullptr, "OutputBuffer", &m_pBufResultUav));
    assert(m_pBufResultUav);

end:
    delete [] sinPreComputeArray;
    sinPreComputeArray = nullptr;

    delete [] fractionArray;
    fractionArray = nullptr;

    delete [] resamplePosArray;
    resamplePosArray = nullptr;

    return hr;
}

HRESULT
WWUpsampleGpu::Dispatch(
        int startPos,
        int count)
{
    HRESULT hr = S_OK;
    bool result = true;

    // GPUã‚ÅComputeShaderŽÀsB
    ID3D11ShaderResourceView* aRViews[] = { m_pBuf0Srv, m_pBuf1Srv, m_pBuf2Srv, m_pBuf3Srv };
    ConstShaderParams shaderParams;
    ZeroMemory(&shaderParams, sizeof shaderParams);
#if 1
    // ‚‚±‚‚‚¯‘¬‚¢B’†‚Åƒ‹[ƒv‚‚é‚æ‚¤‚É‚‚B
    shaderParams.c_convOffs = 0;
    shaderParams.c_dispatchCount = m_convolutionN*2/GROUP_THREAD_COUNT;
    shaderParams.c_sampleToStartPos = startPos;
    HRGR(m_pDCU->Run(m_pCS, sizeof aRViews/sizeof aRViews[0], aRViews, 1, &m_pBufResultUav,
        &shaderParams, sizeof shaderParams, count, 1, 1));
#else
    // ’x‚¢
    for (int i=0; i<convolutionN*2/GROUP_THREAD_COUNT; ++i) {
        shaderParams.c_convOffs = i * GROUP_THREAD_COUNT;
        shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
        shaderParams.c_sampleToStartPos = startPos;
        HRGR(m_pDCU->Run(m_pCS, sizeof aRViews/sizeof aRViews[0], aRViews, m_pBufResultUav,
            &shaderParams, sizeof shaderParams, count, 1, 1));
    }
#endif

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED reason=%08x\n",
            m_pDCU->GetDevice()->GetDeviceRemovedReason());
    }

    return hr;
}

HRESULT
WWUpsampleGpu::ResultGetFromGpuMemory(
        float *outputTo,
        int outputToElemNum)
{
    HRESULT hr = S_OK;

    assert(m_pDCU);
    assert(m_pBufResultUav);

    assert(outputTo);
    assert(outputToElemNum <= m_sampleTotalTo);

    // ŒvŽZŒ‹‰Ê‚ðCPUƒƒ‚ƒŠ[‚ÉŽ‚Á‚Ä‚‚éB
    HRG(m_pDCU->RecvResultToCpuMemory(m_pBufResultUav, outputTo, outputToElemNum * sizeof(float)));
end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED reason=%08x\n",
            m_pDCU->GetDevice()->GetDeviceRemovedReason());
    }

    return hr;
}

void
WWUpsampleGpu::Unsetup(void)
{
    if (m_pDCU) {
        m_pDCU->DestroyDataAndUnorderedAccessView(m_pBufResultUav);
        m_pBufResultUav = nullptr;

        m_pDCU->DestroyDataAndShaderResourceView(m_pBuf3Srv);
        m_pBuf3Srv = nullptr;

        m_pDCU->DestroyDataAndShaderResourceView(m_pBuf2Srv);
        m_pBuf2Srv = nullptr;

        m_pDCU->DestroyDataAndShaderResourceView(m_pBuf1Srv);
        m_pBuf1Srv = nullptr;

        m_pDCU->DestroyDataAndShaderResourceView(m_pBuf0Srv);
        m_pBuf0Srv = nullptr;

        if (m_pCS) {
            m_pDCU->DestroyComputeShader(m_pCS);
            m_pCS = nullptr;
        }

        m_pDCU->Term();
    }

    SAFE_DELETE(m_pDCU);
}
