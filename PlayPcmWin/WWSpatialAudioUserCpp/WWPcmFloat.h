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

    /// @return cross fade PCM sampel count
    int CreateCrossfadeDataPcm(
            const WWPcmFloat &fromPcm, int64_t fromPosFrame,
            const WWPcmFloat &toPcm, int64_t toPosFrame) {
        // クロスフェードのPCMデータは2Gもない(assertでチェック)。
        // このためクロスフェードデータについてはpcm.size()はintにキャストすることができる。
        assert(0 < pcm.size() && pcm.size() <= 0x7fffffff);

        const WWPcmFloat *pcm0 = &fromPcm;
        int64_t pcm0Pos = fromPosFrame;

        const WWPcmFloat *pcm1 = &toPcm;
        int64_t pcm1Pos = toPosFrame;

        for (uint32_t x = 0; x < pcm.size(); ++x) {
            float ratio = (float)x / pcm.size();

            float y0 = pcm0->pcm[pcm0Pos];
            float y1 = pcm1->pcm[pcm1Pos];

            pcm[x] = y0 * (1.0f - ratio) + y1 * ratio;

            ++pcm0Pos;
            if ((int)pcm0->pcm.size() <= pcm0Pos && nullptr != pcm0->next) {
                pcm0 = pcm0->next;
                pcm0Pos = 0;
            }

            ++pcm1Pos;
            if ((int)pcm1->pcm.size() <= pcm1Pos && nullptr != pcm1->next) {
                pcm1 = pcm1->next;
                pcm1Pos = 0;
            }
        }

        pos = 0;

        return (int)pcm.size();
    }

    int64_t AvailableFrames(void) const {
        return (int64_t)(pcm.size() - pos);
    }

    /// 現在のposからframesだけ進める。
    /// @return 再生位置をframesだけ進めた結果、到達したWWPcmFloatのポインターが戻る。
    static WWPcmFloat *AdvanceFrames(WWPcmFloat *p, int64_t skipFrames) {
        while (0 < skipFrames) {
            int64_t advance = skipFrames;
            if (p->AvailableFrames() <= advance) {
                advance = p->AvailableFrames();

                // 頭出ししておく。
                p->pos = 0;

                p = p->next;

                p->pos = 0;
            } else {
                p->pos += advance;
            }

            skipFrames -= advance;
        }
        return p;
    }

    int ch;
    WWTrackEnum trackType = WWTE_None;
    WWPcmFloat *next = nullptr;
    int64_t pos = 0;
    std::vector<float> pcm;
};

