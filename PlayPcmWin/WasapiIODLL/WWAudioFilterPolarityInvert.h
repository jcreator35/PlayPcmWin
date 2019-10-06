#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
#include "WWTypes.h"
#include <stdint.h>

class WWAudioFilterPolarityInvert : public WWAudioFilter {
public:
    WWAudioFilterPolarityInvert(PCWSTR args);
    virtual ~WWAudioFilterPolarityInvert(void) {}
    virtual void UpdateSampleFormat(int sampleRate,
            WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWPcmSampleManipulator mManip;
    bool mEnableInvert[WW_CHANNEL_NUM]; ///< 要素番号が極性反転するチャンネル番号。

    void FilterDoP(unsigned char *buff, int bytes);
    void FilterPcm(unsigned char *buff, int bytes);

    void FilterPcm1(unsigned char *buff, const int bytes, const int bytesPerSample);
};

