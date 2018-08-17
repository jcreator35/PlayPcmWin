#pragma once
#include "WWWave2DGpu.h"

class WaveSim2D {
public:
    WaveSim2D(void);
    ~WaveSim2D(void);

    HRESULT Init(ID3D11DeviceContext* displayCtx, ID3D11Device *displayDevice,
            int gridW, int gridH, float c0, float deltaT, float sc);
    void Term(void);

    HRESULT Update(void);

    WWDirectComputeUser &GetCU(void);

    ID3D11ShaderResourceView *GetResultTexSRV(void) { return mResultTexSRV; }

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
    ID3D11Texture2D *mResultTex;
    ID3D11ShaderResourceView *mResultTexSRV;
    ID3D11DeviceContext *mDisplayCtx;
    ID3D11Device *mDisplayDevice;

    void SetRoh(int x, int y, float v);
    void SetLoss(int x, int y, float v);

    HRESULT CreateResultTex(void);
    HRESULT CopyMemoryToTexture2D(void);
};
