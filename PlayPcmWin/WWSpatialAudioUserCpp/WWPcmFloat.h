// 日本語 UTF-8

#pragma once

#include <vector>
#include <stdint.h>
#include "WWTrackEnum.h"

/// float型のPCMデータに特化したPCMクラス。
class WWPcmFloat {
public:
    void Clear(void) {
        ch = -1;
        trackType = WWTE_None;
        next = nullptr;
        pos = 0;
        pcm.clear();
    }

    /// PCMデータをwantCountサンプルだけbufToにコピーする。
    /// @param bufTo [out] 取得できたPCMサンプルデータを書き込む場所。
    /// @param wantSamples コピーしたいサンプル数。(バイト数では無い)
    /// @return 実際にコピーできたサンプル数。
    int GetNextPcm(float *bufTo, int wantSamples) {
        assert(0 <= wantSamples);

        int copySamples = wantSamples;
        if (pcm.size() <= (uint64_t)(pos + wantSamples)) {
            copySamples = (int)(pcm.size() - pos);
        }

        if (copySamples == 0) {
            return 0;
        }

        memcpy(bufTo, &pcm[pos], sizeof(float) * copySamples);
        pos += copySamples;
        return copySamples;
    }

    int ch;
    WWTrackEnum trackType = WWTE_None;
    WWPcmFloat *next = nullptr;
    int64_t pos = 0;
    std::vector<float> pcm;
};

