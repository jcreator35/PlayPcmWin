// 日本語
#include "WWWave1DGpu.h"
#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>

WWWave1DGpu::WWWave1DGpu(void)
    : mDataCount(0), mV(nullptr), mP(nullptr), mTickTotal(0)
{
    memset(mpCS, 0, sizeof mpCS);
    memset(mpSRVs, 0, sizeof mpSRVs);
    memset(mpUAVs, 0, sizeof mpUAVs);
}

WWWave1DGpu::~WWWave1DGpu(void)
{
    assert(nullptr == mV);
    assert(nullptr == mP);
}

void
WWWave1DGpu::Term(void)
{
    mCU.Term();

    // mCU.TermがReleaseするので、ここでReleaseする必要は無い。
    for (int i=0; i<WWWave1DCS_NUM;++i) {
        mpCS[i] = nullptr;
    }
    for (int i=0; i<WWWave1DSRV_NUM; ++i) {
        mpSRVs[i] = nullptr;
    }
    for (int i=0; i<WWWave1DUAV_NUM; ++i) {
        mpUAVs[i] = nullptr;
    }
    SAFE_DELETE(mP);
    SAFE_DELETE(mV);
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

    WWWave1DStim stim[STIM_COUNT];
};

int
WWWave1DGpu::EnumAdapter(void)
{
    int hr = S_OK;
    
    HRG(mCU.Init());

    hr = mCU.GetNumOfAdapters();
end:
    return hr;
}

HRESULT
WWWave1DGpu::GetAdapterDesc(int idx, wchar_t *desc, int descBytes)
{
    return mCU.GetAdapterDesc(idx, desc, descBytes);
}
HRESULT
WWWave1DGpu::GetAdapterVideoMemoryBytes(int idx, int64_t *videoMemoryBytes)
{
    return mCU.GetAdapterVideoMemoryBytes(idx, videoMemoryBytes);
}

HRESULT
WWWave1DGpu::ChooseAdapter(int idx)
{
    return mCU.ChooseAdapter(idx);
}


HRESULT
WWWave1DGpu::Setup(const int dataCount, float deltaT, float sc, float c0, float *loss, float *roh, float *cr)
{
    HRESULT hr = S_OK;

    mDataCount = dataCount;
    const int dataBytes = sizeof(float)*mDataCount;

    // 最大で大体10進10桁。
    char dataCountStr[16];
    sprintf_s(dataCountStr, "%d", mDataCount);

    char stimCountStr[16];
    sprintf_s(stimCountStr, "%d", STIM_COUNT);

    char scStr[16];
    sprintf_s(scStr, "%f", sc);

    char c0Str[16];
    sprintf_s(c0Str, "%f", c0);

    char deltaTStr[16];
    sprintf_s(deltaTStr, "%f", deltaT);

    mV = new float[mDataCount];
    mP = new float[mDataCount];

    // HLSL ComputeShaderをコンパイル。
    const D3D_SHADER_MACRO defines[] = {
        "LENGTH", dataCountStr,
        "STIM_COUNT", stimCountStr,
        "SC", scStr,
        "C0", c0Str,
        "DELTA_T", deltaTStr,
        nullptr, nullptr
    };

    HRG(mCU.CreateComputeShader(L"Wave1DShaderUpdateStim.hlsl", "CSUpdateStim", defines, &mpCS[WWWave1DCS_UpdateStim]));
    assert(mpCS[WWWave1DCS_UpdateStim]);

    HRG(mCU.CreateComputeShader(L"Wave1DShaderUpdateV.hlsl", "CSUpdateV", defines, &mpCS[WWWave1DCS_UpdateV]));
    assert(mpCS[WWWave1DCS_UpdateV]);

    HRG(mCU.CreateComputeShader(L"Wave1DShaderUpdateP.hlsl", "CSUpdateP", defines, &mpCS[WWWave1DCS_UpdateP]));
    assert(mpCS[WWWave1DCS_UpdateP]);

    {
#if 1
        WWStructuredBufferParams params[WWWave1DSRV_NUM] = {
            {sizeof(float), mDataCount, loss, "loss", &mpSRVs[0], nullptr},
            {sizeof(float), mDataCount, roh,  "roh",  &mpSRVs[1], nullptr},
            {sizeof(float), mDataCount, cr,   "cr",   &mpSRVs[2], nullptr},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave1DSRV_NUM, params));
#else
        WWTexture1DParams params[WWWave1DSRV_NUM] = {
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, loss, mDataCount, "loss", &mpSRVs[0], nullptr},
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, roh, mDataCount, "roh", &mpSRVs[1], nullptr},
            {mDataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, cr, mDataCount, "cr", &mpSRVs[2], nullptr},
        };
        HRG(mCU.CreateSeveralTexture1D(WWWave1DSRV_NUM, params));
#endif
        assert(mpSRVs[0]);
        assert(mpSRVs[1]);
        assert(mpSRVs[2]);
    }

    {
        // 読み込み用VPと書き込み用V,Pがあるので倍の数がある。
        WWStructuredBufferParams params[WWWave1DUAV_NUM] = {
            {sizeof(float), mDataCount, mV, "v0", nullptr, &mpUAVs[0]},
            {sizeof(float), mDataCount, mP, "p0", nullptr, &mpUAVs[1]},
            {sizeof(float), mDataCount, mV, "v1", nullptr, &mpUAVs[2]},
            {sizeof(float), mDataCount, mP, "p1", nullptr, &mpUAVs[3]},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave1DUAV_NUM, params));
        assert(mpUAVs[0]);
        assert(mpUAVs[1]);
        assert(mpUAVs[2]);
        assert(mpUAVs[3]);
    }

    mTickTotal = 0;

end:
    return hr;
}

HRESULT
WWWave1DGpu::Run(int cRepeat, int stimNum, WWWave1DStim stim[],
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

        HRG(mCU.Run(mpCS[WWWave1DCS_UpdateStim], 0,               nullptr, 2, pUAVs_V, &shaderConstants, sizeof(ShaderConstants), 1,          1, 1));
        HRG(mCU.Run(mpCS[WWWave1DCS_UpdateV],    WWWave1DSRV_NUM, mpSRVs,  3, pUAVs_V, nullptr,          0,                       mDataCount, 1, 1));
        HRG(mCU.Run(mpCS[WWWave1DCS_UpdateP],    WWWave1DSRV_NUM, mpSRVs,  3, pUAVs_P, nullptr,          0,                       mDataCount, 1, 1));
        ++mTickTotal;
    }

    // 計算結果をCPUメモリーに持ってくる。
    HRG(mCU.RecvResultToCpuMemory(pUAVs_V[2], mV, dataBytes));
    HRG(mCU.RecvResultToCpuMemory(pUAVs_P[2], mP, dataBytes));

end:
    return hr;
}

int
WWWave1DGpu::CopyResultV(float *vTo, int count)
{
    if (mDataCount < count) {
        count = mDataCount;
    }

    memcpy(vTo, mV, count * sizeof(float));
    return count;
}

int
WWWave1DGpu::CopyResultP(float *pTo, int count)
{
    if (mDataCount < count) {
        count = mDataCount;
    }

    memcpy(pTo, mP, count * sizeof(float));
    return count;
}

