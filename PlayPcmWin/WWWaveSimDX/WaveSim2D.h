#pragma once
#include "WWWave2DGpu.h"

class WaveSim2D {
public:
    WaveSim2D(void);
    ~WaveSim2D(void);

    HRESULT Init(int gridW, int gridH, float c0, float deltaT, float sc);
    void Term(void);

    HRESULT Update(void);

private:
    WWWave2DGpu mWave2D;
    float      *mRoh;
    float      *mCr;
    float      *mLoss;
    float       mC0;
    float       mDeltaT;
    float       mSc;
    int         mGridW;
    int         mGridH;
    int         mGridCount;

    void SetRoh(int x, int y, float v);
    void SetLoss(int x, int y, float v);
};
