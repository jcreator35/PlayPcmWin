// 日本語 UTF-8

#pragma once

#include "WWPcmFloat.h"
#include "WWTrackEnum.h"
#include <list>

// static channels (12) + dynamic channels (20)
#define NUM_CHANNELS (32)

// 1e-6f == -120dB
#define SILENT_NOISE_LEVEL (1e-6f)

/// WWPcmFloatの実体を保持するクラス。
/// 他のクラスはWWPcmFloatの実体をnew/deleteしないようにする。
class WWPcmStore {
public:
    WWPcmStore(void) {
    }

    // 全てのWWPcmFloatを削除する。
    void Clear(void) {
        for (int ch = 0; ch < NUM_CHANNELS; ++ch) {
            ClearPcmOfSpecifiedChannel(ch);
        }
    }

    void ClearPcmOfSpecifiedChannel(int ch) {
        assert(0 <= ch && ch < NUM_CHANNELS);
        std::list<WWPcmFloat *> &r = mPcm[ch];

        while (0 < r.size()) {
            auto ite = r.begin();
            auto *p = *ite;

            p->Clear();
            delete p;

            r.pop_front();
        }
    }

    /// 新しい無音WWPcmFloatを生成し登録。
    /// @param numSamples 無音のサンプル数。(バイト数では無い。)
    WWPcmFloat *NewSilentPcm(int ch, WWTrackEnum trackType, int64_t numSamples) {
        assert(0 <= ch && ch < NUM_CHANNELS);
        
        auto *p = new WWPcmFloat();
        p->ch = ch;
        p->trackType = trackType;
        p->pcm.resize(numSamples, 0.0f);

#if 0
        // add small noise
        for (int64_t pos = 0; pos != numSamples; ++pos) {
            float v = SimpleNoiseGenerator(SILENT_NOISE_LEVEL);
            p->pcm[pos] = v;
        }
#endif

        mPcm[ch].push_back(p);

        return p;
    }

private:
    std::list<WWPcmFloat *> mPcm[NUM_CHANNELS];

    float SimpleNoiseGenerator(float magnitude) {
        float f = (float)(rand() - RAND_MAX/2) / (RAND_MAX/2);
        f *= magnitude;
        return f;
    }
};
