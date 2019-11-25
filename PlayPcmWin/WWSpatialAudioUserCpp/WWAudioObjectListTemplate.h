// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include "WWAudioObject.h"

template <typename T_SpatialAudioObject>
class WWAudioObjectListTemplate {
public:
    void ReleaseAll(void) {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            delete[] r.buffer;
            r.buffer = nullptr;
        }
        mAudioObjectList.clear();
    }

    void Rewind(void) {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            r.Rewind();
        }
    }

    std::list<T_SpatialAudioObject> mAudioObjectList;
};
