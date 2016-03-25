#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
class WWPcmDelay;

class WWAudioFilterZohNosdacCompensation : public WWAudioFilter {
public:
    WWAudioFilterZohNosdacCompensation(void) : mDelay(nullptr) {}

    virtual ~WWAudioFilterZohNosdacCompensation(void);

    virtual void UpdateSampleFormat(int sampleRate,
            WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWPcmSampleManipulator mManip;
    WWPcmDelay *mDelay;

    void FilterPcm(unsigned char *buff, int bytes);
    float Convolution(int ch);
};

