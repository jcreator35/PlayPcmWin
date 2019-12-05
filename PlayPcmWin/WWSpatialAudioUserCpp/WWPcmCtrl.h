// 日本語 UTF-8

#pragma once

#include "WWPcmFloat.h"
#include "WWPcmStore.h"
#include <assert.h>
#include <stdint.h>
#include "WWTrackEnum.h"
#include "WWChangeTrackMethod.h"

class WWPcmCtrl {
private:
    int ch = -1;
    WWPcmStore *ps = nullptr;

    WWPcmFloat *sound = nullptr;

    WWPcmFloat *prologue = nullptr;
    WWPcmFloat *epilogue = nullptr;
    WWPcmFloat *splice = nullptr;

    WWPcmFloat *cur = nullptr;

    // 48kHz PCMを想定。
    const int START_SILENCE_FRAMES  = 24000;
    const int END_SILENCE_FRAMES    = 24000;
    const int SPLICE_SILENCE_FRAMES = 480;

    WWPcmFloat * GetPcm(WWTrackEnum te) {
        switch (te) {
        case WWTE_None:
            return nullptr;
        case WWTE_Prologue:
            return prologue;
        case WWTE_Epilogue:
            return epilogue;
        case WWTE_Splice:
            return splice;
            break;
        case WWTE_Track0:
            return sound;
        default:
            assert(0);
            return nullptr;
        }
    }

    int ChangeCurrentPcmImmediately(WWTrackEnum te) {
        HRESULT hr = S_OK;

        auto *next = GetPcm(te);
        cur = next;

        return S_OK;
    }

public:
    int Channel(void) const {
        return ch;
    }

    /// @param aCh channel number, 0 <= aCh
    /// @param aSound sound data
    void Init(int aCh, WWPcmStore &aPs, WWPcmFloat *aSound) {
        ch = aCh;
        ps = &aPs;
        sound = aSound;

        assert(0 <= ch);
        assert(ps);
        assert(sound);

        assert(!prologue);
        assert(!splice);
        assert(!epilogue);
        assert(!cur);

        prologue = ps->NewSilentPcm(ch, WWTE_Prologue, START_SILENCE_FRAMES);
        epilogue = ps->NewSilentPcm(ch, WWTE_Epilogue, END_SILENCE_FRAMES);
        splice   = ps->NewSilentPcm(ch, WWTE_Splice, SPLICE_SILENCE_FRAMES);

        cur = nullptr;
        prologue->next = sound;
        sound->next = epilogue;
        epilogue->next = nullptr;

        // 再生位置変更時はsplice->next = sound。
        // 再生停止ボタン押下時はフェードアウトのためsplice->next = endになる。
        splice->next = sound;
    }

    int SetCurrentPcm(WWTrackEnum te, WWChangeTrackMethod ctm) {
        HRESULT hr = S_OK;
        if (cur == nullptr || ctm == WWCTM_Immediately) {
            return ChangeCurrentPcmImmediately(te);
        }

        if (cur->trackType == ctm) {
            // すでに再生中である。
            return S_OK;
        }

        WWPcmFloat *next = GetPcm(te);

        int advance = splice->CreateCrossfadeDataPcm(
            *cur, cur->pos,
            *next, 0);

        // splice後の再生位置をsoundのframeからadvanceサンプル後に設定。
        next->pos = advance;
        splice->next = WWPcmFloat::AdvanceFrames(next, advance);

        // spliceを再生する。spliceの次はnextをposから再生する。
        cur = splice;

        return S_OK;
    }

    /// @return サンプル数。
    int64_t GetSoundSamples(void) const {
        if (sound == nullptr) {
            return 0;
        }

        return (int64_t)sound->pcm.size();
    }

    /// @return 音声の再生位置(サンプル)。
    int64_t GetPlayPosition(void) const {
        if (sound == nullptr) {
            return 0;
        }

        return sound->pos;
    }

    /// @return WWTrackEnumが戻る。
    int GetPlayingTrackNr(void) const {
        if (cur == nullptr) {
            return WWTE_None;
        }

        return cur->trackType;

        assert(0);
        return WWTE_None;
    }

    int UpdatePlayPosition(int64_t frame) {
        if (cur != sound) {
            // nullptr : 再生していない。
            // prologue: 再生開始直後は無音を送出する必要あり。
            // splice  : すでにSplice中。あまり起こらないはず。
            // epilogue: 再生終了中のシーク要求は断る。
            return E_NOT_VALID_STATE;
        }

        int advance = splice->CreateCrossfadeDataPcm(
            *sound, sound->pos,
            *sound, frame);

        // splice後の再生位置をsoundのframeからadvanceサンプル後に設定。
        sound->pos = frame;
        splice->next = WWPcmFloat::AdvanceFrames(sound, advance);

        // spliceを再生する。
        cur = splice;

        return S_OK;
    }

    bool IsEmpty(void) const {
        return sound == nullptr;
    }

    void Rewind(void) {
        sound->pos = 0;
        prologue->pos = 0;
        epilogue->pos = 0;
        splice->pos = 0;

        cur = prologue;
        prologue->next = sound;
        sound->next = epilogue;
        epilogue->next = nullptr;
    }

    /// @param buff [out] 取得したPCMを置く場所。
    /// @param wantSamples 取得したいサンプル数。(バイト数では無い。)
    /// @retval true endに達し、必要数が取得できない。(不足部分は無音を入れてある。)
    /// @retval false PCMデータはまだある。
    bool GetNextPcm(float *buff, int wantSamples) {
        int accSamples = 0;
        while (cur != nullptr) {
            int copySamples = cur->GetNextPcm(&buff[accSamples], wantSamples - accSamples);
            accSamples += copySamples;
            if (accSamples == wantSamples) {
                break;
            }

            // 巻き戻してから次のPCMに移動。(行儀が良い。)
            cur->pos = 0;
            cur = cur->next;
        }

        if (accSamples < wantSamples) {
            // 必要数が取得できない場合。
            // 不足部分に無音をセットする。
            memset(&buff[accSamples], 0, sizeof(float)*(wantSamples - accSamples));
        }

        return accSamples < wantSamples;
    }
};
