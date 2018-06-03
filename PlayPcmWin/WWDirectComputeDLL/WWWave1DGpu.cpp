#include "WWWave1DGpu.h"

#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>

WWWave1DGpu::WWWave1DGpu(void)
    : mCount(0), mV(nullptr), mP(nullptr)
{
}

WWWave1DGpu::~WWWave1DGpu(void)
{
    assert(nullptr == mV);
    assert(nullptr == mP);
}

void
WWWave1DGpu::Init(void)
{
    assert(nullptr == mV);
    assert(nullptr == mP);
}

void
WWWave1DGpu::Term(void)
{
    SAFE_DELETE(mP);
    SAFE_DELETE(mV);
}

enum SRVenum {
    SRV_LOSS,
    SRV_ROH,
    SRV_CR,
    SRV_NUM
};

enum UAVenum {
    UAV_V,
    UAV_P,
    UAV_NUM
};

// 定数。16バイトの倍数のサイズの構造体。
struct ShaderConstants {
    // 更新処理の繰り返し回数。
    int cRepeat;

    // パラメータSc
    float cSc;
    
    // パラメータC0
    float cC0;

    int   cStimCounter;
    int   cStimPosX;
    float cStimMagnitude;
    float cStimHalfPeriod;
    float cStimWidth;
};

static HRESULT
Wave1D1(ShaderConstants & shaderConstants, const int dataCount,
        float *loss, float *roh, float *cr,
        float *v, float *p)
{
    HRESULT hr = S_OK;
    ID3D11ShaderResourceView *pSRVs[SRV_NUM];
    ID3D11UnorderedAccessView *pUAVs[UAV_NUM];
    ID3D11ComputeShader *pCS  = nullptr;
    WWDirectComputeUser c;

    // 最大で大体10進10桁。
    char dataCountStr[16];
    sprintf_s(dataCountStr, "%d", dataCount);

    c.Init();

    // HLSL ComputeShaderをコンパイル。
    const D3D_SHADER_MACRO defines[] = {
        "LENGTH", dataCountStr,
        nullptr, nullptr
    };
    HRG(c.CreateComputeShader(L"Wave1DShader.hlsl", "CSMain", defines, &pCS));
    assert(pCS);

    WWTexture1DParams params[SRV_NUM + UAV_NUM] = {
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, loss, dataCount, "loss", &pSRVs[0], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, roh, dataCount, "roh", &pSRVs[1], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, cr, dataCount, "cr", &pSRVs[2], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_UNORDERED_ACCESS, 0, 0, v, dataCount, "v", nullptr, &pUAVs[0]},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_UNORDERED_ACCESS, 0, 0, p, dataCount, "p", nullptr, &pUAVs[1]},
    };
    HRG(c.CreateSeveralTexture1D(SRV_NUM + UAV_NUM, params));

    HRG(c.Run(pCS, SRV_NUM, pSRVs, UAV_NUM, pUAVs, &shaderConstants, sizeof(ShaderConstants), dataCount, 1, 1));

    // 計算結果をCPUメモリーに持ってくる。
    HRG(c.RecvResultToCpuMemory(pUAVs[0], v, dataCount * sizeof(float)));
    HRG(c.RecvResultToCpuMemory(pUAVs[1], p, dataCount * sizeof(float)));

end:
    c.Term();
    return hr;
}

HRESULT
WWWave1DGpu::Run(int cRepeat, float sc, float c0, int stimCounter,
        int stimPosX, float stimMagnitude, float stimHalfPeriod,
        float stimWidth, int dataCount, float *loss,
        float *roh, float *cr, float *v, float *p)
{
    ShaderConstants c = {
        cRepeat,
        sc,
        c0,
        stimCounter,
        stimPosX,
        stimMagnitude,
        stimHalfPeriod,
        stimWidth,
    };

    assert(nullptr == mV);
    assert(nullptr == mP);

    mCount = dataCount;

    mV = new float[dataCount];
    memcpy(mV, v, sizeof(float)*dataCount);
    mP = new float[dataCount];
    memcpy(mP, p, sizeof(float)*dataCount);

    return Wave1D1(c, dataCount, loss, roh, cr, mV, mP);
}

int
WWWave1DGpu::CopyResultV(float *vTo, int count)
{
    if (mCount < count) {
        count = mCount;
    }

    memcpy(vTo, mV, count * sizeof(float));
    return count;
}

int
WWWave1DGpu::CopyResultP(float *pTo, int count)
{
    if (mCount < count) {
        count = mCount;
    }

    memcpy(pTo, mP, count * sizeof(float));
    return count;
}

