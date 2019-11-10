// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include "WWDynAudioObject.h"

template <typename T_SpatialAudioObject>
class WWDynAudioObjectListTemplate {
public:
    void ReleaseAll(void) {
        for (auto ite = mDynAudioObjectList.begin(); ite != mDynAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            delete[] r.buffer;
            r.buffer = nullptr;
        }
        mDynAudioObjectList.clear();
    }

    T_SpatialAudioObject *Find(int idx) {
        for (auto ite = mDynAudioObjectList.begin(); ite != mDynAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            if (r.idx == idx) {
                return &r;
            }
        }
        return nullptr;
    }

    std::list<T_SpatialAudioObject> mDynAudioObjectList;
};
