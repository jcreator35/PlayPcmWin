//　日本語

#pragma once

#include <Windows.h>
#include "WWDirectComputeUser.h"
#include "WWWave1DStim.h"

enum WWWave2DSRVenum {
    WWWave2DSRV_LOSS,
    WWWave2DSRV_ROH,
    WWWave2DSRV_CR,
    WWWave2DSRV_NUM
};

enum WWWave2DUAVenum {
    WWWave2DUAV_V0,
    WWWave2DUAV_P0,
    WWWave2DUAV_V1,
    WWWave2DUAV_P1,
    WWWave2DUAV_EdgeABC0,
    WWWave2DUAV_EdgeABC1,
    WWWave2DUAV_NUM,
};

enum WWWave2DCSenum {
    WWWave2DCS_UpdateStim,
    WWWave2DCS_UpdateV,
    WWWave2DCS_UpdateP,
    WWWave2DCS_NUM
};

struct WWWave2DStim {
    int type; //< STIM_GAUSSIAN or STIM_SINE
    int counter;
    int posX;
    float magnitude;
    float halfPeriod;
    float width;
    float freq;
    float sinePeriod;
};

struct WWWave2DParams {
    int fieldW;
    int fieldH;
    float deltaT;
    float sc;
    float c0;
};

class WWWave2DGpu {
public:
    WWWave2DGpu(void);
    ~WWWave2DGpu(void);

    /*
    WWWave2DGpu 使い方
    Init()
    GetCU().EnumAdapter()
    GetCU().GetAdapterDesc()
    GetCU().GetAdapterVideoMemoryBytes()
    GetCU().ChooseAdaper()
    Setup()
    Run(), CopyResultV(), CopyResultP()
    Run(), CopyResultV(), CopyResultP()
    ...
    Term()
     */

    void Init(void);
    void Term(void);

    WWDirectComputeUser &GetCU(void) { return mCU; }

    HRESULT Setup(const WWWave2DParams &p, float *loss, float *roh, float *cr);
    void Unsetup(void);

    /// CPUメモリに結果mVとmPを書き込む。
    HRESULT Run(int cRepeat, int stimNum, WWWave1DStim stim[]);

    /// Run()の結果のVを取得。
    /// @return コピーした要素数。
    int CopyResultV(float *vTo, int count);

    /// Run()の結果のPを取得。
    /// @return コピーした要素数。
    int CopyResultP(float *pTo, int count);

    /// GPUメモリ上のUAVのvとpを更新。結果のpを2DテクスチャーmResultPTex2Dにする。
    HRESULT RunGPU(int cRepeat, int stimNum, WWWave1DStim stim[]);

    /// RunGPU()の結果のPテクスチャーを取得。
    ID3D11UnorderedAccessView *GetPTexture(void) const { return mResultPTex2D; }

private:
    WWDirectComputeUser mCU;
    ID3D11ComputeShader *mpCS[WWWave2DCS_NUM];
    ID3D11ShaderResourceView  *mpSRVs[WWWave2DSRV_NUM];
    ID3D11UnorderedAccessView *mpUAVs[WWWave2DUAV_NUM];
    ID3D11UnorderedAccessView *mResultPTex2D;
    WWWave2DParams mParams;
    int mNumOfPoints;
    int mEdgeABCPoints;
    float *mV;
    float *mP;
    float *mEdgeABC;
    int64_t mTickTotal;
};
