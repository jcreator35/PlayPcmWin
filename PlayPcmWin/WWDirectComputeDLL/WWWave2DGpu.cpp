//　日本語
#include "WWWave2DGpu.h"
#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>

// 2次元。
#define DIMENSION (2)

WWWave2DGpu::WWWave2DGpu(void)
    : mDataCount(0), mV(nullptr), mP(nullptr), mTickTotal(0)
{
    memset(mpCS, 0, sizeof mpCS);
    memset(mpSRVs, 0, sizeof mpSRVs);
    memset(mpUAVs, 0, sizeof mpUAVs);
}

WWWave2DGpu::~WWWave2DGpu(void)
{
    assert(nullptr == mV);
    assert(nullptr == mP);
}

void
WWWave2DGpu::Init(void)
{
}

void
WWWave2DGpu::Term(void)
{
    mCU.Term();
}

#define STIM_COUNT (4)



// 定数。16バイトの倍数のサイズの構造体。
struct ShaderConstants {
    /// 更新処理の繰り返し回数。
    int cRepeat;

    /// stimの有効要素数。
    int nStim;

    int cSinePeriod;
    int dummy1;

    WWWave2DStim stim[STIM_COUNT];
};

HRESULT
WWWave2DGpu::Setup(const WWWave2DParams &p, float *loss, float *roh, float *cr)
{
    HRESULT hr = S_OK;

    mDataCount = p.dataCount;
    const int dataBytes = sizeof(float)*mDataCount;

    // 最大で大体10進10桁。
    char dataCountStr[16];
    sprintf_s(dataCountStr, "%d", mDataCount);

    char stimCountStr[16];
    sprintf_s(stimCountStr, "%d", STIM_COUNT);

    char scStr[16];
    sprintf_s(scStr, "%f", p.sc);

    char c0Str[16];
    sprintf_s(c0Str, "%f", p.c0);

    char deltaTStr[16];
    sprintf_s(deltaTStr, "%f", p.deltaT);

    assert(nullptr == mV);
    assert(nullptr == mP);

    mV = new float[mDataCount*DIMENSION];
    mP = new float[mDataCount];

    // This is surely necessary!
    memset(mV,0,dataBytes*2);
    memset(mP,0,dataBytes);

    // HLSL ComputeShaderをコンパイル。
    const D3D_SHADER_MACRO defines[] = {
        "LENGTH", dataCountStr,
        "STIM_COUNT", stimCountStr,
        "SC", scStr,
        "C0", c0Str,
        "DELTA_T", deltaTStr,
        nullptr, nullptr
    };

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdateStim.hlsl", "CSUpdateStim", defines, &mpCS[WWWave2DCS_UpdateStim]));
    assert(mpCS[WWWave2DCS_UpdateStim]);

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdateV.hlsl", "CSUpdateV", defines, &mpCS[WWWave2DCS_UpdateV]));
    assert(mpCS[WWWave2DCS_UpdateV]);

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdateP.hlsl", "CSUpdateP", defines, &mpCS[WWWave2DCS_UpdateP]));
    assert(mpCS[WWWave2DCS_UpdateP]);

    {
#if 1
        WWStructuredBufferParams params[WWWave2DSRV_NUM] = {
            {sizeof(float), mDataCount, loss, "loss", &mpSRVs[0], nullptr},
            {sizeof(float), mDataCount, roh,  "roh",  &mpSRVs[1], nullptr},
            {sizeof(float), mDataCount, cr,   "cr",   &mpSRVs[2], nullptr},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave2DSRV_NUM, params));
#else
        WWTexture1DParams params[WWWave2DSRV_NUM] = {
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, loss, mDataCount, "loss", &mpSRVs[0], nullptr},
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, roh, mDataCount, "roh", &mpSRVs[1], nullptr},
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, cr, mDataCount, "cr", &mpSRVs[2], nullptr},
        };
        HRG(mCU.CreateSeveralTexture1D(WWWave2DSRV_NUM, params));
#endif
        assert(mpSRVs[0]);
        assert(mpSRVs[1]);
        assert(mpSRVs[2]);
    }

    {
        // 読み込み用VPと書き込み用V,Pがあるので倍の数がある。
        WWStructuredBufferParams params[WWWave2DUAV_NUM] = {
            {sizeof(float)*DIMENSION, mDataCount, mV, "v0", nullptr, &mpUAVs[0]},
            {sizeof(float), mDataCount, mP, "p0", nullptr, &mpUAVs[1]},
            {sizeof(float)*DIMENSION, mDataCount, mV, "v1", nullptr, &mpUAVs[2]},
            {sizeof(float), mDataCount, mP, "p1", nullptr, &mpUAVs[3]},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave2DUAV_NUM, params));
        assert(mpUAVs[0]);
        assert(mpUAVs[1]);
        assert(mpUAVs[2]);
        assert(mpUAVs[3]);
    }

    mTickTotal = 0;

end:
    return hr;
}

void
WWWave2DGpu::Unsetup(void)
{
    for (int i=WWWave2DUAV_NUM-1; 0<=i; --i) {
        mCU.DestroyDataAndUnorderedAccessView(mpUAVs[i]);
        mpUAVs[i] = nullptr;
    }
    for (int i=WWWave2DSRV_NUM-1; 0<=i; --i) {
        mCU.DestroyDataAndShaderResourceView(mpSRVs[i]);
        mpSRVs[i] = nullptr;
    }
    for (int i=WWWave2DCS_NUM-1; 0<=i; --i) {
        mCU.DestroyComputeShader(mpCS[i]);
        mpCS[i] = nullptr;
    }
    SAFE_DELETE(mP);
    SAFE_DELETE(mV);
}

HRESULT
WWWave2DGpu::Run(int cRepeat, int stimNum, WWWave2DStim stim[],
        float *v, float *p)
{
    HRESULT hr = S_OK;

    if (STIM_COUNT < stimNum) {
        printf("Error: stimNum is too large!\n");
        return E_FAIL;
    }

    const int dataBytes = sizeof(float)*mDataCount;
    ID3D11UnorderedAccessView *pUAVs_V[3];
    ID3D11UnorderedAccessView *pUAVs_P[3];

    for (int i=0; i<cRepeat; ++i) {
        ShaderConstants shaderConstants = {
            stimNum,
            0,
            0,
            0,
        };
        for (int j=0; j<STIM_COUNT; ++j) {
            if (0 < stim[j].counter) {
                --stim[j].counter;
            }

            shaderConstants.stim[j] = stim[j];
        }

        if ((mTickTotal&1)==0) {
            pUAVs_V[0] = mpUAVs[0]; //< vIn
            pUAVs_V[1] = mpUAVs[1]; //< pIn
            pUAVs_V[2] = mpUAVs[2]; //< vOut
            pUAVs_P[0] = mpUAVs[2]; //< vIn
            pUAVs_P[1] = mpUAVs[1]; //< pIn
            pUAVs_P[2] = mpUAVs[3]; //< pOut
        } else {
            pUAVs_V[0] = mpUAVs[2]; //< vIn
            pUAVs_V[1] = mpUAVs[3]; //< pIn
            pUAVs_V[2] = mpUAVs[0]; //< vOut
            pUAVs_P[0] = mpUAVs[0]; //< vIn
            pUAVs_P[1] = mpUAVs[3]; //< pIn
            pUAVs_P[2] = mpUAVs[1]; //< pOut
        }

        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateStim], 0,               nullptr, 2, pUAVs_V, &shaderConstants, sizeof(ShaderConstants), 1,          1, 1));
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateV],    WWWave2DSRV_NUM, mpSRVs,  3, pUAVs_V, nullptr,          0,                       mDataCount, 1, 1));
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateP],    WWWave2DSRV_NUM, mpSRVs,  3, pUAVs_P, nullptr,          0,                       mDataCount, 1, 1));
        ++mTickTotal;
    }

    // 計算結果をCPUメモリーに持ってくる。
    HRG(mCU.RecvResultToCpuMemory(pUAVs_V[2], mV, dataBytes*DIMENSION));
    HRG(mCU.RecvResultToCpuMemory(pUAVs_P[2], mP, dataBytes));

end:
    return hr;
}

int
WWWave2DGpu::CopyResultV(float *vTo, int count)
{
    if (mDataCount < count) {
        count = mDataCount;
    }

    memcpy(vTo, mV, count * sizeof(float) * DIMENSION);
    return count;
}

int
WWWave2DGpu::CopyResultP(float *pTo, int count)
{
    if (mDataCount < count) {
        count = mDataCount;
    }

    memcpy(pTo, mP, count * sizeof(float));
    return count;
}

