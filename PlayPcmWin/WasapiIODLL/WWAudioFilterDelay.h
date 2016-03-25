#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
#include "WWPcmDelay.h"

class WWAudioFilterDelay : public WWAudioFilter {
public:
    WWAudioFilterDelay(PCWSTR args);

    virtual ~WWAudioFilterDelay(void);

    virtual void UpdateSampleFormat(int sampleRate, WWPcmDataSampleFormatType format,
            WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    static const int CHANNEL_NUM = 32;
    WWPcmSampleManipulator mManip;
    WWPcmDelay mDelay[CHANNEL_NUM];
    float mDelaySeconds[CHANNEL_NUM];

    void FilterPcm(unsigned char *buff, int bytes);
};

