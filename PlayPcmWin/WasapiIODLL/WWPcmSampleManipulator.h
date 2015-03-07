#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"

class WWPcmSampleManipulator {
public:
    void UpdateFormat(WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    bool GetFloatSample(const unsigned char *buff, int64_t buffBytes, int64_t frameIdx, int ch, float &value_return);
    bool SetFloatSample(unsigned char *buff,       int64_t buffBytes, int64_t frameIdx, int ch, float value);

    WWPcmDataSampleFormatType SampleFormat(void) const { return mFormat; }
    WWStreamType StreamType(void) const { return mStreamType; }
    int NumChannels(void) const { return mNumChannels; }
    int BitsPerSample(void) const { return mBitsPerSample; }

private:
    WWPcmDataSampleFormatType mFormat;
    WWStreamType mStreamType;
    int mNumChannels;
    int mBitsPerSample;
};
