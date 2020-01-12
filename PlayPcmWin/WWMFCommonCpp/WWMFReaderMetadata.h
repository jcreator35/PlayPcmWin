// 日本語

#pragma once

#define WWMFReaderStrCount 256
#include <stdint.h>

extern "C" {

    struct WWMFReaderMetadata {
        int sampleRate;
        int numChannels;
        int bitsPerSample;
        int bitRate;

        uint32_t dwChannelMask;
        uint32_t dummy0;

        /// おおよその値が戻る。(0のとき不明)
        int64_t numApproxFrames;

        int64_t pictureBytes;

        wchar_t title[WWMFReaderStrCount];
        wchar_t artist[WWMFReaderStrCount];
        wchar_t album[WWMFReaderStrCount];
        wchar_t composer[WWMFReaderStrCount];

        /*
        int64_t CalcApproxDataBytes(void) const {
            return numApproxFrames * numChannels * bitsPerSample / 8;
        }
        */
    };
}; // extern "C"
