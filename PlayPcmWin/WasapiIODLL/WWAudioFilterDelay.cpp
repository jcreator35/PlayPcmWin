// 日本語 UTF-8

#include "WWAudioFilterDelay.h"
#include "WWPcmDelay.h"
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>

WWAudioFilterDelay::WWAudioFilterDelay(PCWSTR args)
{
    wchar_t s[512];
    wcsncpy_s(s, args, 511);
    s[511] = 0;

    wchar_t *tokenCtx = nullptr;
    wchar_t *p = wcstok_s(s, L",", &tokenCtx);
    if (p == nullptr) {
        printf("Error: args must contain 32 delay numbers\n");
        assert(p);
        return;
    }
    for (int i=0; i<CHANNEL_NUM; ++i) {
        if (p == nullptr) {
            printf("Error: args must contain 32 delay numbers\n");
            assert(p);
            return;
        }
        swscanf_s(p, L"%f", &mDelaySeconds[i]);
        p = wcstok_s(nullptr, L",", &tokenCtx);
    }
}

WWAudioFilterDelay::~WWAudioFilterDelay(void)
{
    for (int i=0; i<CHANNEL_NUM; ++i) {
        mDelay[i].Term();
    }
}

void
WWAudioFilterDelay::UpdateSampleFormat(
        int sampleRate,
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    if (sampleRate < 0) {
        // サンプルレートが不明な状態で呼び出された。
        return;
    }

    mManip.UpdateFormat(format, streamType, numChannels);

    // mDelayを作り直す。
    for (int i=0; i<CHANNEL_NUM; ++i) {
        mDelay[i].Term();
    }

    for (int ch=0; ch<numChannels; ++ch) {
        int nSamples = (int)(mDelaySeconds[ch] * sampleRate);
        // 偶数のサンプル数にする。(DoPのマーカーが2サンプルで1周するため)
        nSamples &= ~1;

        mDelay[ch].Init(nSamples);
        if (mManip.StreamType() == WWStreamDop) {
            // DoP無音で埋める。
            // DoPマーカー＝最上位バイトに0x05と0xfaが交互に現れる。
            // 無音＝0x69
            float f0 = (float)((int)0x05696900) / (float)(0x80000000LL);
            float f1 = (float)((int)0xfa696900) / (float)(0x80000000LL);
            for (int n=0; n<nSamples/2; ++n) {
                mDelay[ch].Filter(f0);
                mDelay[ch].Filter(f1);
            }
        }
    }
}

void
WWAudioFilterDelay::Filter(unsigned char *buff, int bytes)
{
    int nFrames = bytes / (mManip.NumChannels() * mManip.BitsPerSample() / 8);

    for (int i=0; i<nFrames; ++i) {
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            float v = 0.0f;
            mManip.GetFloatSample(buff, bytes, i, ch, v);
            float result = mDelay[ch].Filter(v);
            mManip.SetFloatSample(buff, bytes, i, ch, result);
        }
    }
}
