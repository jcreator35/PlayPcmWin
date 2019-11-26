// 日本語 UTF-8

#pragma once

#include <vector>
#include <stdint.h>

/// float型のPCMデータに特化したPCMクラス。
class WWPcmFloat {
public:
    void Clear(void) {
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

        memcpy(bufTo, &pcm[pos], sizeof(float) * copySamples);
        pos += copySamples;
        return copySamples;
    }

    WWPcmFloat *next = nullptr;
    int64_t pos = 0;
    std::vector<float> pcm;
};

