// 日本語

#pragma once

#include "WWCicDownsampler.h"
#include "WWHalfbandFilterDownsampler.h"
#include <stdint.h>
#include <assert.h>

/** 1チャンネルのSDMストリームを入力して64分の1ダウンサンプルしてfloat型のPCM出力を得る。
 * SDMストリームをCICダウンサンプラーで16分の1ダウンサンプルして
 * ハーフバンドフィルターダウンサンプラーで2分の1ダウンサンプル
 * ハーフバンドフィルターダウンサンプラーで2分の1ダウンサンプル
 * 16 * 2 * 2 = 64x
 */
class WWSdmToPcm {
public:
    WWSdmToPcm(void) : mHBDS23(23), mHBDS47(47), mTmp1Count(0), mTmp2Count(0),
        mOutPcm(nullptr), mOutCount(0), mTotalOutSamples(0) { }

    ~WWSdmToPcm(void);

    void Start(int totalOutSamples);

    /// Sdmデータを16サンプル投入する。
    /// @param inSdm 1ビットのSDMデータが16個、ビッグエンディアンビットオーダーで入っている。
    void AddInputSamples(const uint16_t inSdm) {
        mTmp1Pcm[mTmp1Count++] = mCicDS.Filter(inSdm);

        if (2 == mTmp1Count) {
            mTmp1Count = 0;

            mHBDS23.Filter(mTmp1Pcm, 2, &mTmp2Pcm[mTmp2Count++]);
            if (2 == mTmp2Count) {
                mTmp2Count = 0;
                mHBDS47.Filter(mTmp2Pcm, 2, &mOutPcm[mOutCount++]);
            }
        }
    }

    // すべて足したら呼ぶ。(フラッシュしてディレイに滞留しているサンプルを出す。)
    void Drain(void);

    // 中に持っている出力バッファーの最初の出力データを指しているポインタを戻す。
    const float *GetOutputPcm(void) const;

    // 中に持っている出力バッファーを削除する。
    void End(void);

    int FilterDelay(void) const {
        return 13;
    }

private:
    WWCicDownsampler mCicDS;
    WWHalfbandFilterDownsampler mHBDS23;
    WWHalfbandFilterDownsampler mHBDS47;
    float mTmp1Pcm[2];
    int mTmp1Count;
    float mTmp2Pcm[2];
    int mTmp2Count;
    float *mOutPcm;
    int mOutCount;
    int mTotalOutSamples;
};
