#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"

enum WWAFMSModeType {
    WWAFMSMode_Mute,
    WWAFMSMode_Solo,
};

class WWAudioFilterMuteSoloChannel : public WWAudioFilter {
public:
    WWAudioFilterMuteSoloChannel(WWAFMSModeType mode, PCWSTR args);
    virtual ~WWAudioFilterMuteSoloChannel(void) {}
    virtual void UpdateSampleFormat(int sampleRate,
            WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWAFMSModeType mMode;
    WWPcmSampleManipulator mManip;
    int mMuteChannel;

    bool IsMuteChannel(int ch) const;
    void FilterDoP(unsigned char *buff, int bytes);
    void FilterPcm(unsigned char *buff, int bytes);
};

