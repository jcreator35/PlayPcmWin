//　日本語

#pragma once

#include <Windows.h>
#include "WWDirectComputeUser.h"
#include "WWWave1DStim.h"

struct WWWave1DParams {
    int dataCount;
    float deltaT;
    float sc;
    float c0;
};

enum WWWave1DSRVenum {
    WWWave1DSRV_LOSS,
    WWWave1DSRV_ROH,
    WWWave1DSRV_CR,
    WWWave1DSRV_NUM
};

enum WWWave1DUAVenum {
    WWWave1DUAV_V0,
    WWWave1DUAV_P0,
    WWWave1DUAV_V1,
    WWWave1DUAV_P1,
    WWWave1DUAV_NUM,
};

enum WWWave1DCSenum {
    WWWave1DCS_UpdateStim,
    WWWave1DCS_UpdateV,
    WWWave1DCS_UpdateP,
    WWWave1DCS_NUM
};

class WWWave1DGpu {
public:
    WWWave1DGpu(void);
    ~WWWave1DGpu(void);

    /*
    WWWave1DGpu 使い方
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

    HRESULT Setup(const WWWave1DParams &p, float *loss, float *roh, float *cr);
    void Unsetup(void);

    HRESULT Run(int cRepeat, int stimNum, WWWave1DStim stim[],
            float *v, float *p);

    // @return コピーした要素数。
    int CopyResultV(float *vTo, int count);
    // @return コピーした要素数。
    int CopyResultP(float *pTo, int count);


private:
    WWDirectComputeUser mCU;
    ID3D11ComputeShader *mpCS[WWWave1DCS_NUM];
    ID3D11ShaderResourceView  *mpSRVs[WWWave1DSRV_NUM];
    ID3D11UnorderedAccessView *mpUAVs[WWWave1DUAV_NUM];
    int    mDataCount;
    float *mV;
    float *mP;
    int64_t mTickTotal;
};
