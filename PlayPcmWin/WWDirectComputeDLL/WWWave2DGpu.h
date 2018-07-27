//　日本語

#pragma once

#include <Windows.h>
#include "WWDirectComputeUser.h"

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
    WWWave2DUAV_NUM,
};

enum WWWave2DCSenum {
    WWWave2DCS_UpdateStim,
    WWWave2DCS_UpdateV,
    WWWave2DCS_UpdateP,
    WWWave2DCS_NUM
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

    HRESULT Run(int cRepeat, int stimNum, WWWave2DStim stim[],
            float *v, float *p);

    // @return コピーした要素数。
    int CopyResultV(float *vTo, int count);
    // @return コピーした要素数。
    int CopyResultP(float *pTo, int count);


private:
    WWDirectComputeUser mCU;
    ID3D11ComputeShader *mpCS[WWWave2DCS_NUM];
    ID3D11ShaderResourceView  *mpSRVs[WWWave2DSRV_NUM];
    ID3D11UnorderedAccessView *mpUAVs[WWWave2DUAV_NUM];
    int    mDataCount;
    float *mV;
    float *mP;
    int64_t mTickTotal;
};
