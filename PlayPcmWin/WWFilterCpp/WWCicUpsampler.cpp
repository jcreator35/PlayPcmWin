// 日本語

#include "stdafx.h"
#include "WWCicUpsampler.h"
#include <stdint.h>
#include <assert.h>

void
WWCicUpsampler::Clear(void)
{
    for (int i=0; i<Order;++i) {
        mInteg[i] = 0;
        mDelay[i] = 0;
    }
    mQerr = 0;
}

/// このinPcmの宣言、16と書いても文法上は意味ない (ただのポインタ型になる)
void
WWCicUpsampler::Filter(const float inPcm, float outPcm_r[16])
{
    // 1次のノイズシェイピング。
    int32_t v = (int32_t)((float)0x8000 * (inPcm + mQerr));
    mQerr = (inPcm - (float)v/0x8000);

    // delay
    for (int i = 0; i < Order; ++i) {
        int32_t tmp = mDelay[i];
        mDelay[i] = v;
        v -= tmp;
    }

    for (int offs=0; offs<16; ++offs) {
        // 16x ZOH upsample
        // ゲインは16分の1

        // integrator
        for (int i = 0; i < Order; ++i) {
            v += mInteg[i];
            mInteg[i] = v;
        }

        // CICのゲインは
        // sinc1 16xダウンサンプルの時16倍 (4ビット右シフトで16ビット値を得る)
        // sinc2 16xダウンサンプルの時256倍 (8ビット右シフトで16ビット値を得る)
        // sinc3 16xダウンサンプルの時4096倍 (12ビット右シフトで16ビット値を得る)
        // sinc4 16xダウンサンプルの時65536倍 (16ビット右シフトで16ビット値を得る)
        // これにZOHのゲインをかける。

        switch (Order) {
        case 1:
            v <<= 16;
            break;
        case 2:
            v <<= 12;
            break;
        case 3:
            v <<= 8;
            break;
        case 4:
            v <<= 4;
            break;
        }

        outPcm_r[offs] = ((float)v) / 2147483648.0f;

        v = 0;
    }
}


