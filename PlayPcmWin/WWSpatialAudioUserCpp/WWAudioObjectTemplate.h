// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include "WWPcmCtrl.h"
#include <assert.h>

/// @param T_SpatialAudioObject ISpatialAudioObject　または ISpatialAudioObjectForHrtf
template <typename T_SpatialAudioObject>
class WWAudioObjectTemplate {
public:
    void Init(int ch, WWPcmStore &ps, WWPcmFloat *sound, AudioObjectType aAot, float aVolume) {
        assert(nullptr == sao);
        pcmCtrl.Init(ch, ps, sound);
        aot = aAot;
        volume = aVolume;
    }

    void
    ReleaseAll(void) {
        SafeRelease(&sao);
    }

    /// @retval true 最後のPCMデータを送出した。
    /// @retval false まだ再生できるPCMデータが残っている。
    bool
    CopyNextPcmTo(BYTE *buffTo, int buffToBytes) {
        assert(buffTo);
        assert(0 <= buffToBytes);
        return pcmCtrl.GetNextPcm((float*)buffTo, buffToBytes / sizeof(float));
    }

    void
    Rewind(void) {
        SafeRelease(&sao);
        pcmCtrl.Rewind();
    }

    void
    SetPos3D(float x, float y, float z) {
        posX = x;
        posY = y;
        posZ = z;
    }

    int Channel(void) const {
        return pcmCtrl.Channel();
    }

    // saoの実体はこのクラスが管理する。
    T_SpatialAudioObject *sao = nullptr;

    WWPcmCtrl pcmCtrl;

    // このオーディオオブジェクトの番号。
    int idx = -1;

    // AudioObjectType_Dynamic      for any dynamic positioned speaker
    // AudioObjectType_FrontLeft... for static front left speaker
    AudioObjectType aot = AudioObjectType_Dynamic;

    // SetPosition(posX, posY, posZ)
    float posX   = +0.0f;   ///< in meters, positive value : right
    float posY   = +0.0f;     ///< in meters, positive value : above
    float posZ   = -1.0f;   ///< in meters, negative value : in front

    // SetVolume(volume)
    // or SetGain(volume)
    float volume = +1.0f; ///< 0 to 1
};
