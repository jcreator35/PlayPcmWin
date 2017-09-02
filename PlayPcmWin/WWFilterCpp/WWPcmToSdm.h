// 日本語

#pragma once

#include "WWCicUpsampler.h"
#include "WWHalfbandFilterUpsampler.h"
#include <stdint.h>

/** 1チャンネルのfloat型PCMストリームを入力して64倍アップサンプルして1ビットSDM出力を得る。
 * ハーフバンドフィルターアップサンプラーで2倍
 * ハーフバンドフィルターアップサンプラーで2倍
 * CICアップサンプラーで16倍
 * 2 * 2 * 16 = 64x
 * 4次CRFBループフィルターでSDM化。
 */
class WWPcmToSdm {
public:


};
