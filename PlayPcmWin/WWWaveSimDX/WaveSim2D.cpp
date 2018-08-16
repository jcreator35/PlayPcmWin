#include "WaveSim2D.h"



WaveSim2D::WaveSim2D(void) :
        mRoh(nullptr), mCr(nullptr), mLoss(nullptr), mGridCount(0)
{
}

WaveSim2D::~WaveSim2D(void)
{
}

HRESULT
WaveSim2D::Init(int gridW, int gridH, float c0, float deltaT, float sc)
{
    HRESULT hr = S_OK;

    mGridW  = gridW;
    mGridH  = gridH;
    mC0     = c0;
    mDeltaT = deltaT;
    mSc     = sc;
    mGridCount = gridW * gridH;

    mRoh  = new float[mGridCount];
    mCr   = new float[mGridCount];
    mLoss = new float[mGridCount];

    /*
        * 音響インピーダンスη=ρ*Ca (Schneider17, pp.63, pp.325)
        * η1から前進しη2の界面に達した波が界面で反射するとき
        *
        *             η2-η1
        * 反射率 r = ────────
        *             η2+η1
        * 
        * 媒質1のインピーダンスη1と反射率→媒質2のインピーダンスη2を得る式:
        *
        *       -(r+1)η1
        * η2 = ─────────
        *         r-1
        *         
        * Courant number Sc = c0 Δt / Δx
        */

    for (int i = 0; i < mGridCount; ++i) {
        // 相対密度。
        mRoh[i] = 1.0f;

        // 相対音速。0 < Cr < 1
        mCr[i] = 1.0f;

        mLoss[i] = 0.0f;
    }

#if 1
    // 上下左右端領域は反射率80％の壁になっている。
    float r = 0.8f; // 0.8 == 80%
    float roh2 = -(r + 1) * 1.0f / (r - 1);
    float loss2 = 0.1f;
    int edge = 3;
    for (int y = edge; y < mGridH-edge; ++y) {
        for (int x = edge; x < mGridW * 1 / 20; ++x) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
        for (int x = mGridW * 19 / 20; x < mGridW-edge; ++x) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
    }
    for (int x = edge; x < mGridW-edge; ++x) {
        for (int y = edge; y < mGridH * 1 / 20; ++y) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
        for (int y = mGridH * 19 / 20; y < mGridH-edge; ++y) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
    }
#endif

    // 2次のABC用の過去データ置き場。
    //for (int i = 0; i < mDelayArray.Length; ++i) {
    //    mDelayArray[i].FillZeroes();
    //}

    //{
    //    // ABCの係数。

    //    float Cp = mRoh[0] * mCr[0] * mCr[0] * mC0 * mSc;
    //    float Cv = 2.0f * mSc / ((mRoh[0] + mRoh[0 + 1]) * mC0);

    //    mAbcCoef = new float[3];
    //    float ScPrime = 1.0f; // (float)Math.Sqrt(Cp * Cv);
    //    float denom = 1.0f / ScPrime + 2.0f + ScPrime;
    //    mAbcCoef[0] = -(1.0f / ScPrime - 2.0f + ScPrime) / denom;
    //    mAbcCoef[1] = +(2.0f * ScPrime - 2.0f / ScPrime) / denom;
    //    mAbcCoef[2] = -(4.0f * ScPrime + 4.0f / ScPrime) / denom;
    //}

    {
        auto &cu = mWave2D.GetCU();
        cu.Init();
        cu.EnumAdapters();
        int nAdapters = cu.GetNumOfAdapters();
        if (nAdapters <= 0) {
            printf("Error: No Graphics adapter available!\n");
            return E_FAIL;
        }
        hr = cu.ChooseAdapter(0);
        if (FAILED(hr)) {
            printf("Error: ChooseAdapter failed %08x\n", hr);
            return hr;
        }

        mWave2D.Init();
    }

    {
        WWWave2DParams p;
        p.fieldW = mGridW;
        p.fieldH = mGridH;
        p.deltaT = mDeltaT;
        p.sc     = mSc;
        p.c0     = mC0;
        hr = mWave2D.Setup(p, mLoss, mRoh, mCr);
        if (FAILED(hr)) {
            printf("Error: Setup failed %08x\n", hr);
            return hr;
        }
    }

    return hr;
}

void
WaveSim2D::SetRoh(int x, int y, float v)
{
    int pos = x + y * mGridW;
    mRoh[pos] = v;
}

void
WaveSim2D::SetLoss(int x, int y, float v)
{
    int pos = x + y * mGridW;
    mLoss[pos] = v;
}


void
WaveSim2D::Term(void)
{
    mWave2D.Unsetup();
    mWave2D.Term();
}

HRESULT
WaveSim2D::Update(void)
{
    return mWave2D.Run2(2, 0, nullptr);
}

