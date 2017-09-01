#pragma once

#include <stdint.h>

/// 入力の1ビットSDM信号をCIC Sincフィルターで16分の1ダウンサンプルする。
class WWCicDownsampler {
public:
    WWCicDownsampler(void) {
        Clear();
    }

    /// ディレイと積分器の蓄積値をクリアする。
    void Clear(void);

    /// ビッグエンディアンビットオーダーの1bit SDMデータを16ビット受け取り、
    /// 1サンプルのPCM値を戻す。
    /// Richard Lyons, Understanding Digital Signal Processing 3rd ed., pp.562 fig.10-41
    float Filter(const uint16_t inSdm);

    /// float値を16サンプル受け取り1サンプルのPCM値を戻す。
    /// このinPcmの宣言、16と書いても文法上は意味ない (ただのポインタ型になる)
    float Filter(const float v[16]);

private:
    /// Order==3 : Sinc3 CIC filter
    static const int Order=4;

    int32_t mDelay[Order];
    int32_t mInteg[Order];

    float mQerr;
};


