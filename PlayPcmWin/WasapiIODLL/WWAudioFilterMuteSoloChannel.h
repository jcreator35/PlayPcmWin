#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
#include "WWTypes.h"

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
    bool mEnableFlags[WW_CHANNEL_NUM]; ///< 要素番号が有効・無効チャンネル番号。

    bool IsMuteChannel(int ch) const;
    void FilterDoP(unsigned char *buff, int bytes);
    void FilterPcm(unsigned char *buff, int bytes);
};

