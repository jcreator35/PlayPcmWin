#pragma once

#include <Windows.h>
#include "WWDirectComputeUser.h"

struct WWWave1DStim {
    int type; //< STIM_GAUSSIAN or STIM_SINE
    int counter;
    int posX;
    float magnitude;
    float halfPeriod;
    float width;
    float freq;
    float sinePeriod;
};

enum WWWave1DSRVenum {
    WWWave1DSRV_LOSS,
    WWWave1DSRV_ROH,
    WWWave1DSRV_CR,
    WWWave1DSRV_NUM
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

    HRESULT Init(const int dataCount, float deltaT, float sc, float c0, float *loss, float *roh, float *cr);

    HRESULT Run(int cRepeat, int stimNum, WWWave1DStim stim[],
            float *v, float *p);

    // @return コピーした要素数。
    int CopyResultV(float *vTo, int count);
    // @return コピーした要素数。
    int CopyResultP(float *pTo, int count);

    void Term(void);

private:
    WWDirectComputeUser mCU;
    ID3D11ComputeShader *mpCS[WWWave1DCS_NUM];
    ID3D11ShaderResourceView *mpSRVs[WWWave1DSRV_NUM];
    int    mDataCount;
    float *mV;
    float *mP;

};
