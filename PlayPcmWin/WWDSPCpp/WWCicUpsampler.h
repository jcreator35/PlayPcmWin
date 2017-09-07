// 日本語

#pragma once

#include <stdint.h>

/// 入力のfloat型 PCM信号をCIC Sincフィルターで16倍アップサンプルする。
class WWCicUpsampler {
public:
    WWCicUpsampler(void) {
        Clear();
    }

    /// ディレイと積分器の蓄積値をクリアする。
    void Clear(void);

    /// 1サンプルのPCM値を入力し、16サンプルのPCM値を戻す。
    /// Richard Lyons, Understanding Digital Signal Processing 3rd ed., pp.562 fig.10-41
    void Filter(const float inPcm, float outPcm_r[16]);

    int FilterDelay(void) const { return 0; }

private:
    /// Order==3 : Sinc3 CIC filter
    /// Order==4 : Sinc4 CIC filter
    static const int Order=4;

    int32_t mDelay[Order];
    int32_t mInteg[Order];

    float mQerr;
};
