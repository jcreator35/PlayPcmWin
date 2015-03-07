#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"
#include <functional>

class WWAudioFilter;

class WWAudioFilterSequencer {
public:
    WWAudioFilterSequencer(void);
    ~WWAudioFilterSequencer(void);

    void Init(void);
    void Term(void);

    void Append(WWAudioFilter *af);

    /// 登録されているフィルターを全て登録解除する
    void UnregisterAll(void);

    bool IsAvailable(void) const { return m_audioFilter != nullptr; }

    void UpdateSampleFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    void ProcessSamples(unsigned char *buff, int bytes);

private:
    WWPcmDataSampleFormatType m_format;
    WWStreamType m_streamType;
    int m_numChannels;
    WWAudioFilter *m_audioFilter;

    WWAudioFilter *Last(void);

    void Loop(std::function<void(WWAudioFilter*)> f);
};
