// 日本語 UTF-8

#include "WWAudioFilterSequencer.h"
#include "WWAudioFilter.h"
#include <assert.h>

WWAudioFilterSequencer::WWAudioFilterSequencer(void)
      : m_sampleRate(-1),
        m_format(WWPcmDataSampleFormatSint16),
        m_streamType(WWStreamPcm),
        m_numChannels(2),
        m_audioFilter(nullptr)
{
}

WWAudioFilterSequencer::~WWAudioFilterSequencer(void)
{
    assert(m_audioFilter == nullptr);
}

void
WWAudioFilterSequencer::Init(void)
{
    assert(m_audioFilter == nullptr);
}

void
WWAudioFilterSequencer::Term(void)
{
    UnregisterAll();
}

WWAudioFilter *
WWAudioFilterSequencer::Last(void)
{
    if (m_audioFilter == nullptr) {
        return nullptr;
    }

    WWAudioFilter *p = m_audioFilter;

    while (p->Next() != nullptr) {
        p = p->Next();
    }
    return p;
};

void
WWAudioFilterSequencer::Append(WWAudioFilter *af)
{
    af->UpdateSampleFormat(m_sampleRate, m_format, m_streamType, m_numChannels);
    af->SetNext(nullptr);

    if (m_audioFilter == nullptr) {
        m_audioFilter = af;
    } else {
        WWAudioFilter *p = Last();
        p->SetNext(af);
    }
}

void
WWAudioFilterSequencer::Loop(std::function<void(WWAudioFilter*)> f)
{
    WWAudioFilter *p = m_audioFilter;
    while (p != nullptr) {
        WWAudioFilter *next = p->Next();

        f(p);

        p = next;
    }
}

void
WWAudioFilterSequencer::UnregisterAll(void)
{
    Loop([](WWAudioFilter *p) {
        delete p;
    });

    m_audioFilter = nullptr;
}

void
WWAudioFilterSequencer::UpdateSampleFormat(
        int sampleRate,
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    m_sampleRate = sampleRate;
    m_format = format;
    m_streamType = streamType;
    m_numChannels = numChannels;

    Loop([sampleRate, format, streamType, numChannels](WWAudioFilter*p) {
        p->UpdateSampleFormat(sampleRate, format, streamType, numChannels);
    });
}

void
WWAudioFilterSequencer::SaturateSamples(unsigned char *buff, int bytes)
{
    switch (m_format) {
    case WWPcmDataSampleFormatSfloat:
        {
            // [-1.0, 1.0)の範囲でSaturateする。

            float *p = (float *)buff;
            for (int idx=0; idx<bytes/4; ++idx) {
                float v = p[idx];

                if (v < -1.0f) {
                    v = -1.0f;
                }
                if (((float)0x7fffff / 0x800000) < v) {
                    v = (float)0x7fffff / 0x800000;
                }

                p[idx] = v;
            }
        }
        break;
    default:
        // 整数値のときは処理の必要なし。
        break;
    }
}

void
WWAudioFilterSequencer::ProcessSamples(unsigned char *buff, int bytes)
{
    Loop([buff,bytes](WWAudioFilter*p) {
        p->Filter(buff, bytes);
    });

    // 最後にSaturate処理する。
    SaturateSamples(buff, bytes);
}
