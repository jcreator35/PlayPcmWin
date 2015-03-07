#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"

class WWAudioFilterPolarityInvert : public WWAudioFilter {
public:
    virtual ~WWAudioFilterPolarityInvert(void) {}
    virtual void UpdateSampleFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWPcmSampleManipulator mManip;

    void FilterDoP(unsigned char *buff, int bytes);
    void FilterPcm(unsigned char *buff, int bytes);
};

