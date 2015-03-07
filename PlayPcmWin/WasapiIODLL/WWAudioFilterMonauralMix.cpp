// 日本語 UTF-8

#include "WWAudioFilterMonauralMix.h"
#include <assert.h>

void
WWAudioFilterMonauralMix::UpdateSampleFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels)
{
    mManip.UpdateFormat(format, streamType, numChannels);
}

void
WWAudioFilterMonauralMix::Filter(unsigned char *buff, int bytes)
{
    if (mManip.StreamType() == WWStreamDop) {
        // 対応していない
    } else {
        FilterPcm(buff, bytes);
    }
}

void
WWAudioFilterMonauralMix::FilterPcm(unsigned char *buff, int bytes)
{
    bool rv;

    int nFrames = bytes / mManip.NumChannels() / (mManip.BitsPerSample() / 8);

    for (int i=0; i<nFrames; ++i) {
        float vAcc = 0.0f;
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            float v = 0.0f;
            rv = mManip.GetFloatSample(buff, bytes, i, ch, v);
            assert(rv);
            vAcc += v;
        }

        vAcc /= mManip.NumChannels();
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            rv = mManip.SetFloatSample(buff, bytes, i, ch, vAcc);
            assert(rv);
        }
    }
}

