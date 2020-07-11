// 日本語。

#include "Test1.h"
#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWUpsampleCpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

enum WWGpuPrecisionType {
    WWGpuPrecision_Float,
    WWGpuPrecision_Double,
    WWGpuPrecision_NUM
};


static double
ModuloD(double left, double right)
{
    if (right < 0) {
        right = -right;
    }

    if (0 < left) {
        while (0 <= left - right) {
            left -= right;
        }
    } else if (left < 0) {
        do{
            left += right;
        } while (left < 0);
    }

    return left;
}


static HRESULT
JitterAddGpu(
        WWGpuPrecisionType precision,
        int sampleN,
        int convolutionN,
        float *sampleData,
        float *jitterX,
        float *outF)
{
    bool result = true;
    HRESULT             hr    = S_OK;
    WWDirectComputeUser *pDCU = nullptr;
    ID3D11ComputeShader *pCS  = nullptr;

    ID3D11ShaderResourceView*   pBuf0Srv = nullptr;
    ID3D11ShaderResourceView*   pBuf1Srv = nullptr;
    ID3D11ShaderResourceView*   pBuf2Srv = nullptr;
    ID3D11UnorderedAccessView*  pBufResultUav = nullptr;

    assert(0 < sampleN);
    assert(0 < convolutionN);
    assert(sampleData);
    assert(jitterX);
    assert(outF);

    const int fromCount = convolutionN + sampleN + convolutionN;
    float *from = new float[fromCount];
    assert(from);
    ZeroMemory(from, sizeof(float)* fromCount);
    for (int i=0; i<sampleN; ++i) {
        from[i+convolutionN] = sampleData[i];
    }

    char convStartStr[32];
    char convEndStr[32];
    char convCountStr[32];
    char sampleNStr[32];
    char iterateNStr[32];
    char groupThreadCountStr[32];
    sprintf_s(convStartStr, "%d", -convolutionN);
    sprintf_s(convEndStr,   "%d", convolutionN);
    sprintf_s(convCountStr, "%d", convolutionN*2);
    sprintf_s(sampleNStr,   "%d", sampleN);
    sprintf_s(iterateNStr,  "%d", convolutionN*2/GROUP_THREAD_COUNT);
    sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

    void *sinx = nullptr;
    const D3D_SHADER_MACRO *defines = nullptr;
    int sinxBufferElemBytes = 0;
    if (precision == WWGpuPrecision_Double) {
        // doubleprec

        const D3D_SHADER_MACRO definesD[] = {
            "CONV_START", convStartStr,
            "CONV_END", convEndStr,
            "CONV_COUNT", convCountStr,
            "SAMPLE_N", sampleNStr,
            "ITERATE_N", iterateNStr,
            "GROUP_THREAD_COUNT", groupThreadCountStr,
            "HIGH_PRECISION", "1",
            nullptr, nullptr
        };
        defines = definesD;

        double *sinxD = new double[sampleN];
        assert(sinxD);
        for (int i=0; i<sampleN; ++i) {
            sinxD[i] = sin(ModuloD(jitterX[i], 2.0 * PI_D));
        }
        sinx = sinxD;

        sinxBufferElemBytes = 8;
    } else {
        // singleprec

        const D3D_SHADER_MACRO definesF[] = {
            "CONV_START", convStartStr,
            "CONV_END", convEndStr,
            "CONV_COUNT", convCountStr,
            "SAMPLE_N", sampleNStr,
            "ITERATE_N", iterateNStr,
            "GROUP_THREAD_COUNT", groupThreadCountStr,
            // "HIGH_PRECISION", "1",
            nullptr, nullptr
        };
        defines = definesF;

        float *sinxF = new float[sampleN];
        assert(sinxF);
        for (int i=0; i<sampleN; ++i) {
            sinxF[i] = (float)sin(ModuloD(jitterX[i], 2.0 * PI_D));
        }
        sinx = sinxF;

        sinxBufferElemBytes = 4;
    }

    pDCU = new WWDirectComputeUser();
    assert(pDCU);

    HRG(pDCU->Init());

    HRG(pDCU->CreateComputeShader(L"SincConvolution.hlsl", "CSMain", defines, &pCS));
    assert(pCS);

    HRG(pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(float), fromCount, from, "SampleDataBuffer", &pBuf0Srv));
    assert(pBuf0Srv);

    HRG(pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sinxBufferElemBytes, sampleN, sinx, "SinxBuffer", &pBuf1Srv));
    assert(pBuf1Srv);

    HRG(pDCU->SendReadOnlyDataAndCreateShaderResourceView(
        sizeof(float), sampleN, jitterX, "XBuffer", &pBuf2Srv));
    assert(pBuf1Srv);

    HRG(pDCU->CreateBufferAndUnorderedAccessView(
        sizeof(float), sampleN, nullptr, "OutputBuffer", &pBufResultUav));
    assert(pBufResultUav);

    ConstShaderParams shaderParams;
    ZeroMemory(&shaderParams, sizeof shaderParams);

    ID3D11ShaderResourceView* aRViews[] = { pBuf0Srv, pBuf1Srv, pBuf2Srv };
    DWORD t0 = GetTickCount();
#if 1
    shaderParams.c_convOffs = 0;
    shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
    HRGR(pDCU->Run(pCS, sizeof aRViews/sizeof aRViews[0], aRViews, 1, &pBufResultUav,
        &shaderParams, sizeof shaderParams, sampleN, 1, 1));
#else
    for (int i=0; i<convolutionN*2/GROUP_THREAD_COUNT; ++i) {
        shaderParams.c_convOffs = i * GROUP_THREAD_COUNT;
        shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
        HRGR(pDCU->Run(pCS, sizeof aRViews/sizeof aRViews[0], aRViews, pBufResultUav,
            pBufConst, &shaderParams, sizeof shaderParams, sampleN, 1, 1));
    }
#endif

    HRG(pDCU->RecvResultToCpuMemory(pBufResultUav, outF, sampleN * sizeof(float)));
end:

    DWORD t1 = GetTickCount();
    printf("RunGpu=%dms ###################################\n", t1-t0);

    if (pDCU) {
        if (hr == DXGI_ERROR_DEVICE_REMOVED) {
            dprintf("DXGI_ERROR_DEVICE_REMOVED reason=%08x\n",
                pDCU->GetDevice()->GetDeviceRemovedReason());
        }

        pDCU->DestroyDataAndUnorderedAccessView(pBufResultUav);
        pBufResultUav = nullptr;

        pDCU->DestroyDataAndShaderResourceView(pBuf2Srv);
        pBuf2Srv = nullptr;

        pDCU->DestroyDataAndShaderResourceView(pBuf1Srv);
        pBuf1Srv = nullptr;

        pDCU->DestroyDataAndShaderResourceView(pBuf0Srv);
        pBuf0Srv = nullptr;

        if (pCS) {
            pDCU->DestroyComputeShader(pCS);
            pCS = nullptr;
        }

        pDCU->Term();
    }

    SAFE_DELETE(pDCU);

    delete[] sinx;
    sinx = nullptr;

    delete[] from;
    from = nullptr;

    return hr;
}


static void
JitterAddCpuD(int sampleN, int convolutionN, float *sampleData, float *jitterX, float *outF)
{
    const int fromCount = convolutionN + sampleN + convolutionN;
    float *from = new float[fromCount];
    assert(from);

    ZeroMemory(from, sizeof(float) * fromCount);
    for (int i=0; i<sampleN; ++i) {
        from[i+convolutionN] = sampleData[i];
    }

    for (int pos=0; pos<sampleN; ++pos) {
        float xOffs = jitterX[pos];
        double r = 0.0f;

        for (int i=-convolutionN; i<convolutionN; ++i) {
            double x = PI_D * (i + xOffs);
            double sinx = sin(ModuloD(xOffs, 2.0 * PI_D));
            int    posS = pos + i + convolutionN;
            double sinc =  SincD(sinx, x);

            r += from[posS] * sinc;
        }

        outF[pos] = (float)r;
    }

    delete[] from;
    from = nullptr;
}

static void
Test1(void)
{
    HRESULT hr = S_OK;

    int convolutionN = 65536 * 256;
    int sampleN      = 16384;

    float *sampleData = new float[sampleN];
    assert(sampleData);

    float *jitterX = new float[sampleN];
    assert(jitterX);

    float *outputGpu = new float[sampleN];
    assert(outputGpu);

    float *outputCpu = new float[sampleN];
    assert(outputCpu);

#if 1
    for (int i=0; i<sampleN; ++i) {
        sampleData[i] = 1.0f;
        jitterX[i]    = 0.5f;
    }
#else
    for (int i=0; i<sampleN; ++i) {
        float xS = PI_F * i * 1000 / 44100;
        float xJ = PI_F * i * 4000 / 44100;
        sampleData[i] = sinf(xS);
        jitterX[i]    = sinf(xJ)*0.5f;
    }
#endif

    DWORD t0 = GetTickCount();

    HRG(JitterAddGpu(WWGpuPrecision_Double, sampleN, convolutionN, sampleData, jitterX, outputGpu));

    DWORD t1 = GetTickCount()+1;

    JitterAddCpuD(sampleN, convolutionN, sampleData, jitterX, outputCpu);

    DWORD t2 = GetTickCount()+2;

    for (int i=0; i<sampleN; ++i) {
        printf("%7d sampleData=%f jitterX=%f outGpu=%f outCpu=%f diff=%12.8f\n",
            i, sampleData[i], jitterX[i], outputGpu[i], outputCpu[i], fabsf(outputGpu[i]- outputCpu[i]));
    }

    if (0 < (t1-t0)) {
        printf("GPU=%dms(%fsamples/s) CPU=%dms(%fsamples/s)\n",
            (t1-t0),  sampleN / ((t1-t0)/1000.0),
            (t2-t1),  sampleN / ((t2-t1)/1000.0));
    }

end:
    delete[] outputCpu;
    outputGpu = nullptr;

    delete[] outputGpu;
    outputGpu = nullptr;

    delete[] jitterX;
    jitterX = nullptr;

    delete[] sampleData;
    sampleData = nullptr;
}
