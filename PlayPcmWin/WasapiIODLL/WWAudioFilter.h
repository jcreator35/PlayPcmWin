#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"

class WWAudioFilter {
public:
    virtual ~WWAudioFilter(void) {}
    virtual void UpdateSampleFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels) = 0;
    virtual void Filter(unsigned char *buff, int bytes) = 0;

    WWAudioFilter *Next(void) { return m_next; }

    void SetNext(WWAudioFilter *af) { m_next = af; }

private:
    WWAudioFilter *m_next;
};

