//　日本語
#include "WWWave2DGpu.h"
#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>

// 2次元。
#define DIMENSION (2)

static const int THREAD_W = 16;
static const int THREAD_H = 16;

WWWave2DGpu::WWWave2DGpu(void)
    : mV(nullptr), mP(nullptr), mTickTotal(-1)
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

HRESULT
WWWave2DGpu::Init(void)
{
    return S_OK;
}

void
WWWave2DGpu::Term(void)
{
    mCU.Term();
}

#define STIM_COUNT (4)

// 定数。16バイトの倍数のサイズの構造体。
struct ShaderConstants {
    int nStim;    ///< stimの有効要素数。
    int dummy1;
    int dummy2;
    int dummy3;

    WWWave1DStim stim[STIM_COUNT];
};

HRESULT
WWWave2DGpu::Setup(const WWWave2DParams &p, float *loss, float *roh, float *cr)
{
    HRESULT hr = S_OK;

    mParams = p;

    assert(nullptr == mP);
    assert(nullptr == mV);

    mNumOfPoints = p.fieldW * p.fieldH;

    //各点あたり2個、上端分と下端分で計4倍。
    mEdgeABCPoints = (p.fieldW + p.fieldH)*4;

    /* 圧力pはスカラー場。
     * 速度vはベクトル場。
     */
    const int pBytes = sizeof(float) * mNumOfPoints;
    const int vBytes = sizeof(float) * mNumOfPoints * DIMENSION;
    const int edgeABCBytes = sizeof(float)*mEdgeABCPoints;

    mP = new float[mNumOfPoints];
    mV = new float[mNumOfPoints * DIMENSION];
    mEdgeABC = new float[mEdgeABCPoints];

    // This is surely necessary!
    memset(mP, 0, pBytes);
    memset(mV, 0, vBytes);
    memset(mEdgeABC, 0, edgeABCBytes);

    // 最大で大体10進10桁なので16bytes有れば良いでしょう。
    char fieldWStr[16];
    sprintf_s(fieldWStr, "%d", p.fieldW);

    char fieldHStr[16];
    sprintf_s(fieldHStr, "%d", p.fieldH);

    char threadWStr[16];
    sprintf_s(threadWStr, "%d", THREAD_W);

    char threadHStr[16];
    sprintf_s(threadHStr, "%d", THREAD_H);

    char stimCountStr[16];
    sprintf_s(stimCountStr, "%d", STIM_COUNT);

    char scStr[16];
    sprintf_s(scStr, "%f", p.sc);

    char c0Str[16];
    sprintf_s(c0Str, "%f", p.c0);

    char deltaTStr[16];
    sprintf_s(deltaTStr, "%f", p.deltaT);

    // HLSL ComputeShaderをコンパイル。
    const D3D_SHADER_MACRO defines[] = {
        "FIELD_W", fieldWStr,
        "FIELD_H", fieldHStr,
        "THREAD_W", threadWStr,
        "THREAD_H", threadHStr,
        "STIM_COUNT", stimCountStr,
        "SC", scStr,
        "C0", c0Str,
        "DELTA_T", deltaTStr,
        nullptr, nullptr
    };

    // 1DのStimシェーダーをそのまま使用。
    HRG(mCU.CreateComputeShader(L"Wave1DShaderUpdateStim.hlsl", "CSUpdateStim", defines, &mpCS[WWWave2DCS_UpdateStim]));
    assert(mpCS[WWWave2DCS_UpdateStim]);

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdateV.hlsl", "CSUpdateV", defines, &mpCS[WWWave2DCS_UpdateV]));
    assert(mpCS[WWWave2DCS_UpdateV]);

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdateP.hlsl", "CSUpdateP", defines, &mpCS[WWWave2DCS_UpdateP]));
    assert(mpCS[WWWave2DCS_UpdateP]);

    HRG(mCU.CreateComputeShader(L"Wave2DShaderUpdatePEdgeABC.hlsl", "CSUpdate", defines, &mpCS[WWWave2DCS_UpdatePEdgeABC]));
    assert(mpCS[WWWave2DCS_UpdatePEdgeABC]);

    {
        WWStructuredBufferParams params[WWWave2DSRV_NUM] = {
            {sizeof(float), mNumOfPoints, loss, "loss", &mpSRVs[WWWave2DSRV_LOSS], nullptr},
            {sizeof(float), mNumOfPoints, roh,  "roh",  &mpSRVs[WWWave2DSRV_ROH], nullptr},
            {sizeof(float), mNumOfPoints, cr,   "cr",   &mpSRVs[WWWave2DSRV_CR], nullptr},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave2DSRV_NUM, params));
        assert(mpSRVs[0]);
        assert(mpSRVs[1]);
        assert(mpSRVs[2]);
    }

    {
        // 読み込み用VPと書き込み用V,Pがあるので倍の数がある。
        WWStructuredBufferParams params[WWWave2DUAV_NUM] = {
            {sizeof(float)*DIMENSION, mNumOfPoints, mV, "v0", nullptr, &mpUAVs[WWWave2DUAV_V0]},
            {sizeof(float),           mNumOfPoints, mP, "p0", nullptr, &mpUAVs[WWWave2DUAV_P0]},
            {sizeof(float)*DIMENSION, mNumOfPoints, mV, "v1", nullptr, &mpUAVs[WWWave2DUAV_V1]},
            {sizeof(float),           mNumOfPoints, mP, "p1", nullptr, &mpUAVs[WWWave2DUAV_P1]},
            {sizeof(float),           mEdgeABCPoints, mEdgeABC, "edgeABC0", nullptr, &mpUAVs[WWWave2DUAV_Edge0]},
            {sizeof(float),           mEdgeABCPoints, mEdgeABC, "edgeABC1", nullptr, &mpUAVs[WWWave2DUAV_Edge1]},
        };
        HRG(mCU.CreateSeveralStructuredBuffer(WWWave2DUAV_NUM, params));
        assert(mpUAVs[0]);
        assert(mpUAVs[1]);
        assert(mpUAVs[2]);
        assert(mpUAVs[3]);
        assert(mpUAVs[4]);
        assert(mpUAVs[5]);
    }

    /*
    {
        // 結果書き出し用のテクスチャー。
        WWTexture2DParams params = {
                p.fieldW, p.fieldH, 0, 1, DXGI_FORMAT_R32_FLOAT, {1, 0}, D3D11_USAGE_DEFAULT,
                D3D11_BIND_UNORDERED_ACCESS, 0, 0, nullptr, 0, "ResultP", nullptr, &mResultPTex2DUAV};
        HRG(mCU.CreateSeveralTexture2D(1, &params));
        assert(mResultPTex2DUAV);
    }
    */

    mTickTotal = 0;

end:
    return hr;
}

void
WWWave2DGpu::Unsetup(void)
{
    for (int i=WWWave2DUAV_NUM-1; 0<=i; --i) {
        mCU.DestroyResourceAndUAV(mpUAVs[i]);
        mpUAVs[i] = nullptr;
    }
    for (int i=WWWave2DSRV_NUM-1; 0<=i; --i) {
        mCU.DestroyResourceAndSRV(mpSRVs[i]);
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
WWWave2DGpu::Run_(int cRepeat, int stimNum, WWWave1DStim stim[])
{
    HRESULT hr = S_OK;

    if (STIM_COUNT < stimNum) {
        printf("Error: stimNum is too large!\n");
        return E_FAIL;
    }

    const int pBytes = sizeof(float) * mNumOfPoints;
    const int vBytes = sizeof(float) * mNumOfPoints * DIMENSION;
    ID3D11UnorderedAccessView *pUAVs_V[3];
    ID3D11UnorderedAccessView *pUAVs_P[3];
    ID3D11UnorderedAccessView *pUAVs_Edge[4];

    for (int i=0; i<cRepeat; ++i) {
        ++mTickTotal;
        // 最初の1回目はmTickTotal == 0になる。

        ShaderConstants shaderConstants = {
            stimNum, //< stim配列の有効要素数。
            0,
            0,
            0,
        };
        for (int j=0; j<stimNum; ++j) {
            if (0 < stim[j].counter) {
                --stim[j].counter;
            }

            shaderConstants.stim[j] = stim[j];
        }

        if ((mTickTotal&1)==0) {
            pUAVs_V[0] = mpUAVs[WWWave2DUAV_V0]; //< vIn
            pUAVs_V[1] = mpUAVs[WWWave2DUAV_P0]; //< pIn
            pUAVs_V[2] = mpUAVs[WWWave2DUAV_V1]; //< vOut

            pUAVs_P[0] = mpUAVs[WWWave2DUAV_V1]; //< vIn
            pUAVs_P[1] = mpUAVs[WWWave2DUAV_P0]; //< pIn
            pUAVs_P[2] = mpUAVs[WWWave2DUAV_P1]; //< pOut

            pUAVs_Edge[0] = mpUAVs[WWWave2DUAV_P1]; //< pIn
            pUAVs_Edge[1] = mpUAVs[WWWave2DUAV_P0]; //< pOut
            pUAVs_Edge[2] = mpUAVs[WWWave2DUAV_Edge0]; //< delayIn
            pUAVs_Edge[3] = mpUAVs[WWWave2DUAV_Edge1]; //< delayOut
        } else {
            pUAVs_V[0] = mpUAVs[WWWave2DUAV_V1]; //< vIn
            pUAVs_V[1] = mpUAVs[WWWave2DUAV_P0]; //< pIn
            pUAVs_V[2] = mpUAVs[WWWave2DUAV_V0]; //< vOut

            pUAVs_P[0] = mpUAVs[WWWave2DUAV_V0]; //< vIn
            pUAVs_P[1] = mpUAVs[WWWave2DUAV_P0]; //< pIn
            pUAVs_P[2] = mpUAVs[WWWave2DUAV_P1]; //< pOut

            pUAVs_Edge[0] = mpUAVs[WWWave2DUAV_P1]; //< pIn
            pUAVs_Edge[1] = mpUAVs[WWWave2DUAV_P0]; //< pOut
            pUAVs_Edge[2] = mpUAVs[WWWave2DUAV_Edge1]; //< delayIn
            pUAVs_Edge[3] = mpUAVs[WWWave2DUAV_Edge0]; //< delayOut
        }

        const int dispatchX = mParams.fieldW / THREAD_W;
        const int dispatchY = mParams.fieldH / THREAD_H;

        //                                                                    pIn
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateStim], 0,               nullptr, 1, &pUAVs_V[1],      &shaderConstants, sizeof(ShaderConstants), 1,         1,         1));
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateV],    WWWave2DSRV_NUM, mpSRVs,  3, pUAVs_V,          nullptr,          0,                       dispatchX, dispatchY, 1));
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdateP],    WWWave2DSRV_NUM, mpSRVs,  3, pUAVs_P,          nullptr,          0,                       dispatchX, dispatchY, 1));
        HRG(mCU.Run(mpCS[WWWave2DCS_UpdatePEdgeABC], 1, &mpSRVs[WWWave2DSRV_CR], 4, pUAVs_Edge, nullptr,          0,                       dispatchX, dispatchY, 1));
    }

end:
    return hr;
}

HRESULT
WWWave2DGpu::Run(int cRepeat, int stimNum, WWWave1DStim stim[])
{
    HRESULT hr = S_OK;
    ID3D11UnorderedAccessView *pP = nullptr;
    ID3D11UnorderedAccessView *pV = nullptr;
    const int pBytes = sizeof(float) * mNumOfPoints;
    const int vBytes = sizeof(float) * mNumOfPoints * DIMENSION;

    HRG(Run_(cRepeat, stimNum, stim));

    // 計算結果をCPUメモリーに持ってくる。
    // mTickTotalはRun1()で更新されるので順番に注意。
    if ((mTickTotal&1)==0) {
        pV = mpUAVs[WWWave2DUAV_V1]; //< vOut
        pP = mpUAVs[WWWave2DUAV_P0]; //< pOut
    } else {
        pV = mpUAVs[WWWave2DUAV_V0]; //< vOut
        pP = mpUAVs[WWWave2DUAV_P0]; //< pOut
    }
    HRG(mCU.RecvResultToCpuMemory(pV, mV, vBytes));
    HRG(mCU.RecvResultToCpuMemory(pP, mP, pBytes));

end:
    return hr;
}

int
WWWave2DGpu::CopyResultV(float *vTo, int count)
{
    if (mNumOfPoints < count) {
        count = mNumOfPoints;
    }

    memcpy(vTo, mV, sizeof(float) * count * DIMENSION);
    return count;
}

int
WWWave2DGpu::CopyResultP(float *pTo, int count)
{
    if (mNumOfPoints < count) {
        count = mNumOfPoints;
    }

    memcpy(pTo, mP, sizeof(float) * count);
    return count;
}

