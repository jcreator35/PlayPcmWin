#include "stdafx.h"
#include "WWHalfbandFilterDownsampler.h"
#include "WWWindowFunc.h"
#include <stdint.h>
#define _USE_MATH_DEFINES
#include <math.h>

void
WWHalfbandFilterDownsampler::Start(void)
{
    mDelayU.FillZeroes();
    mDelayL.FillZeroes();
}

void
WWHalfbandFilterDownsampler::End(void)
{
}

static const double gSine90Table[] = { 0.0, 1.0, 0.0, -1.0 };

// Understanding Digital Signal Processing 3rd ed., pp.546
void
WWHalfbandFilterDownsampler::DesignFilter(void) {
    assert(mCoeffsU == nullptr);

    auto *coeffs = new double[mFilterLength];
    memset(coeffs, 0, sizeof(double)*mFilterLength);

    const int filterDelay = FilterDelay();
    for (int i = 0; i < filterDelay; ++i) {
        if (i != 0 && 0 == (i & 1)) {
            // coefficient is 0
            continue;
        }
        double theta = M_PI * (i * 90.0) / 180.0f;
        double v = 1.0;
        if (DBL_EPSILON < abs(theta)) {
            v = gSine90Table[i & 3] / theta;
        }
        coeffs[filterDelay - 1 - i] = v;
        coeffs[filterDelay - 1 + i] = v;
    }

    // Kaiser窓(α==9)をかける
    auto * w = new double[mFilterLength];
    WWKaiserWindow(9.0, mFilterLength, w);
    for (int i = 0; i < mFilterLength; ++i) {
        coeffs[i] *= w[i];
    }
    delete [] w;
    w = nullptr;

    // 0.5倍する
    for (int i = 0; i < mFilterLength; ++i) {
        coeffs[i] *= 0.5;
    }

    mCoeffsU = new float[(mFilterLength+1)/2];
    mCoeffL = (float)coeffs[(mFilterLength-1)/2];
    for (int i=0;i<mFilterLength; ++i) {
        if (0 == (i&1)) {
            mCoeffsU[i/2] = (float)coeffs[i];
        }
    }

    delete [] coeffs;
    coeffs = nullptr;
}

// Understanding Digital Signal Processing 3rd ed., pp.546
void
WWHalfbandFilterDownsampler::Filter(
        const float *inPcm, int numIn, float *outPcm_r)
{
    assert(mCoeffsU);
    assert(inPcm);
    assert(outPcm_r);

    int outPos = 0;
    float r = 0;
    for (int inPos=0; inPos<numIn; ++inPos) {
        float v = inPcm[inPos];

        // 偶数サンプルの始めで集計を開始。
        // 奇数サンプルの終わりに集計結果を書き込む。

        if ((inPos & 1) == 0) {

            // 偶数サンプル入力の場合。上側のディレイに投入。
            // Folded FIRの高速化手法は用いていない。
            // 上側ディレイから出力値を計算する。
            mDelayU.Filter(v);

            r += mCoeffsU[0] * v;
            for (int i=0; i<mDelayU.DelaySamples(); ++i) {
                r += mCoeffsU[i+1] * mDelayU.GetNth(i);
            }
        } else {
            // 奇数サンプル入力の場合。下側のディレイに投入。
            // 下側ディレイから出力値を計算する。
            float d = mDelayL.Filter(v);

            r += mCoeffL * d;

            // 集計結果を書き込む。
            outPcm_r[outPos++] = r;

            r = 0;
        }
    }
}
