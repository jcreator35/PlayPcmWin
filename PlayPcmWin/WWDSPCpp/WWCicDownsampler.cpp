// 日本語

#include "WWCicDownsampler.h"
#include <stdint.h>
#include <assert.h>


void
WWCicDownsampler::Clear(void)
{
    for (int i=0; i<Order;++i) {
        mInteg[i] = 0;
        mDelay[i] = 0;
    }
    mQerr = 0;
}

float
WWCicDownsampler::Filter(const uint16_t inSdm)
{
    int32_t v = 0;

    // integrator
    // 16x downsample
    for (int bit = 15; 0 <= bit; --bit) {
        v = 1 & (inSdm >> bit);

        for (int i = 0; i < Order; ++i) {
            v += mInteg[i];
            mInteg[i] = v;
        }
    }

    // delay
    for (int i = 0; i < Order; ++i) {
        int32_t tmp = mDelay[i];
        mDelay[i] = v;
        v -= tmp;
    }

    //  Sinc4 16xダウンサンプルの時 0 <= v <= 65536
    assert(Order==4);
    v -= 32768; // -32768 <= v <= 32768 になる。

    // コンパイラの最適化が、この除算を定数1/32768との乗算に置き換えるだろう。
    return ((float)v) / 32768.0f;
}

/// このinPcmの宣言、16と書いても文法上は意味ない (ただのポインタ型になる)
float
WWCicDownsampler::Filter(const float inPcm[16])
{
    int32_t v = 0;

    // integrator
    // 16x downsample
    for (int k = 0; k < 16; ++k) {
        v = (int32_t)((float)0x8000 * inPcm[k]);
        for (int i = 0; i < Order; ++i) {
            v += mInteg[i];
            mInteg[i] = v;
        }
    }

    // delay
    for (int i = 0; i < Order; ++i) {
        int32_t tmp = mDelay[i];
        mDelay[i] = v;
        v -= tmp;
    }

    // Sinc4フィルター。vには32ビット値が出てくる。
    assert(Order==4);
    float f = ((float)v) / 2147483648.0f;

    return f;
}


