#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
#include "WWTypes.h"
#include <stdint.h>

class WWAudioFilterChannelRouting : public WWAudioFilter {
public:
    WWAudioFilterChannelRouting(PCWSTR args);
    virtual ~WWAudioFilterChannelRouting() {}
    virtual void UpdateSampleFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWPcmSampleManipulator mManip;
    uint32_t mRoutingTable[WW_CHANNEL_NUM];
    int mNumOfChannels;
};

