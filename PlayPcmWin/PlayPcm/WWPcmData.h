#pragma once

#include <stdint.h>

enum WWBitsPerSampleType {
    WWBpsNone,
    WWBps24,
    WWBps32v24,
};

enum WWPcmDataStreamAllocType {
    WWPDSA_Normal,
    WWPDSA_LargeMemory,
};

struct WWPcmData {
    int bitsPerSample;
    int validBitsPerSample;
    int nSamplesPerSec;
    int nChannels;
    int64_t nFrames;
    int posFrame;
    WWPcmDataStreamAllocType allocType;

    unsigned char *stream;

    void Init(WWPcmDataStreamAllocType t = WWPDSA_Normal);
    void Term(void);

    bool StoreStream(const unsigned char *aStream, int64_t bytes);

    WWPcmData(void) {
        stream = nullptr;
    }
    ~WWPcmData(void);
};

