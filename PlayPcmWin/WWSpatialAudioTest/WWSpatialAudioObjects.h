// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include "WWDynamicAudioStreamChannel.h"

class WWSpatialAudioObjects {
public:
    void ReleaseAll(void) {
        for (auto ite = mDynAudioStreamsList.begin(); ite != mDynAudioStreamsList.end(); ++ite) {
            auto &r = *ite;
            delete[] r.buffer;
            r.buffer = nullptr;
        }
        mDynAudioStreamsList.clear();
    }

    WWDynamicAudioStreamChannel *Find(int idx) {
        for (auto ite = mDynAudioStreamsList.begin(); ite != mDynAudioStreamsList.end(); ++ite) {
            auto &r = *ite;
            if (r.idx == idx) {
                return &r;
            }
        }
        return nullptr;
    }

    std::list<WWDynamicAudioStreamChannel> mDynAudioStreamsList;
};
