// 日本語

#pragma once

#include "WWCicUpsampler.h"
#include "WWHalfbandFilterUpsampler.h"
#include "WWLoopFilterCRFB.h"
#include <stdint.h>
#include <assert.h>

/** 1チャンネルのfloat型PCMストリームを入力して64倍アップサンプルして1ビットSDM出力を得る。
 * ハーフバンドフィルターアップサンプラーで2倍
 * ハーフバンドフィルターアップサンプラーで2倍
 * CICアップサンプラーで16倍
 * 2 * 2 * 16 = 64x
 * 4次CRFBループフィルターでSDM化。
 */
class WWPcmToSdm {
public:
    WWPcmToSdm(void) : mHB47(47), mHB23(23), mLoopFilter(4,mA,mB,mG,1.0f),
        mOutSdm(nullptr), mOutCount(0), mTotalOutSamples16(0) { }

    ~WWPcmToSdm(void);

    void Start(int totalOutSamples);

    /// Pcmデータを最大16サンプル投入する。
    void AddInputSamples(const float *inPcm, int inPcmCount);

    // すべて足したら呼ぶ。(フラッシュしてディレイに滞留しているサンプルを出す。)
    void Drain(void);

    // 中に持っている出力バッファーの最初の出力データを指しているポインタを戻す。
    const uint16_t *GetOutputSdm(void) const {
        assert(mOutSdm);
        return & mOutSdm[FilterDelay16()];
    }

    // 中に持っている出力バッファーを削除する。
    void End(void);

    // 出力から見たディレイサンプル数/16
    int FilterDelay16(void) const {
        return 55;
    }

private:
    WWHalfbandFilterUpsampler mHB47;
    WWHalfbandFilterUpsampler mHB23;
    WWCicUpsampler mCic;
    WWLoopFilterCRFB<float> mLoopFilter;
    enum { IN_PCM_CAPACITY = 16 };
    uint16_t *mOutSdm;
    int mOutCount;
    int mTotalOutSamples16;

    static const float mA[4];
    static const float mB[5];
    static const float mG[2];
};
