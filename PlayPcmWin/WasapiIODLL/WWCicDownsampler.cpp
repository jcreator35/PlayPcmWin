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
    for (int bit = 0; bit < 16; ++bit) {
        v = 1 & (inSdm >> (15 - bit));

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

    // Sinc4
    v -= 32768; // -32768 <= v <= 32768 になる。
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
        // 1次のノイズシェイピング。
        v = (int32_t)((float)0x8000 * (inPcm[k] + mQerr));
        mQerr = (inPcm[k] - (float)v/0x8000);

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

#if 0
    // CICの入力データは16ビットint
    // これを、16bit intになるようシフトするには
    // sinc1 16xダウンサンプルの時16倍 (4ビット右シフトで16ビット値を得る)
    // sinc2 16xダウンサンプルの時256倍 (8ビット右シフトで16ビット値を得る)
    // sinc3 16xダウンサンプルの時4096倍 (12ビット右シフトで16ビット値を得る)
    // sinc4 16xダウンサンプルの時65536倍 (16ビット右シフトで16ビット値を得る)
    switch (Order) {
    case 1:
        v <<= 12;
        break;
    case 2:
        v <<= 8;
        break;
    case 3:
        v <<= 4;
        break;
    case 4:
        break;
    }
#else
    // Sinc4フィルター。vには32ビット値が出てくる。
    float f = ((float)v) / 2147483648.0f;
#endif
    return f;
}


