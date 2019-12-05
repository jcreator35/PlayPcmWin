// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include "WWAudioObject.h"
#include "WWTrackEnum.h"

/// @param T_WWAudioObject WWAudioObject または WWAudioHrtfObject
template <typename T_WWAudioObject>
class WWAudioObjectListTemplate {
public:
    void ReleaseAll(void) {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            r.ReleaseAll();
        }
        mAudioObjectList.clear();
    }

    void Rewind(void) {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            r.Rewind();
        }
    }

    /// @return 再生する音声の長さ(サンプル)
    int64_t GetSoundDuration(int ch) const {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            if (r.Channel() != ch) {
                continue;
            }

            return r.pcmCtrl.GetSoundSamples();
        }
        return 0;
    }

    /// @return 音声再生位置(サンプル)
    int64_t GetPlayPosition(int ch) const {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            if (r.Channel() != ch) {
                continue;
            }

            return r.pcmCtrl.GetPlayPosition();
        }
        return 0;
    }

    /// @return WWTrackEnum
    int GetPlayingTrackNr(int ch) const {
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            if (r.Channel() != ch) {
                continue;
            }

            return r.pcmCtrl.GetPlayingTrackNr();
        }
        return WWTE_None;
    }

    HRESULT SetCurrentPcm(WWTrackEnum te, WWChangeTrackMethod ctm) {
        HRESULT hr = S_OK;
        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;
            hr = r.pcmCtrl.SetCurrentPcm(te, ctm);
            if (FAILED(hr)) {
                return hr;
            }
        }

        return S_OK;
    }

    HRESULT UpdatePlayPosition(int64_t frame) {
        HRESULT hr = S_OK;

        for (auto ite = mAudioObjectList.begin(); ite != mAudioObjectList.end(); ++ite) {
            auto &r = *ite;

            r.pcmCtrl.UpdatePlayPosition(frame);
        }

        return hr;
    }

    std::list<T_WWAudioObject> mAudioObjectList;
};
