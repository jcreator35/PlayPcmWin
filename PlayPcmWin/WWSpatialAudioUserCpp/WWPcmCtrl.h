// 日本語 UTF-8

#pragma once

#include "WWPcmFloat.h"
#include "WWPcmStore.h"
#include <assert.h>

class WWPcmCtrl {
private:
    int ch = -1;
    WWPcmStore *ps = nullptr;

    WWPcmFloat *sound = nullptr;

    WWPcmFloat *start = nullptr;
    WWPcmFloat *splice = nullptr;
    WWPcmFloat *end = nullptr;

    WWPcmFloat *cur = nullptr;

    // 48kHz PCMを想定。
    const int START_SILENCE_FRAMES  = 40000;
    const int END_SILENCE_FRAMES    = 24000;
    const int SPLICE_SILENCE_FRAMES = 4800;

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

        assert(!start);
        assert(!splice);
        assert(!end);
        assert(!cur);

        start  = ps->NewSilentPcm(ch, START_SILENCE_FRAMES);
        end    = ps->NewSilentPcm(ch, END_SILENCE_FRAMES);
        splice = ps->NewSilentPcm(ch, SPLICE_SILENCE_FRAMES);

        cur = start;
        start->next = sound;
        sound->next = end;
        end->next = nullptr;

        // 再生位置変更時はsplice->next = sound。
        // 再生停止ボタン押下時はフェードアウトのためsplice->next = endになる。
        splice->next = sound;
    }

    /// @return サンプル数。
    int64_t GetSoundSamples(void) {
        if (sound == nullptr) {
            return 0;
        }

        return (int64_t)sound->pcm.size();
    }

    /// @return サンプル数。
    int64_t GetPlayPosition(void) {
        if (sound == nullptr) {
            return 0;
        }

        return sound->pos;
    }

    bool IsEmpty(void) {
        return sound == nullptr;
    }

    void Rewind(void) {
        cur = start;
        start->next = sound;
        sound->next = end;
        end->next = nullptr;
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
            if (accSamples < wantSamples) {
                cur = cur->next;
            }
        }

        if (accSamples < wantSamples) {
            // 必要数が取得できない場合。
            // 不足部分に無音をセットする。
            memset(&buff[accSamples], 0, sizeof(float)*(wantSamples - accSamples));
        }

        return accSamples < wantSamples;
    }
};
