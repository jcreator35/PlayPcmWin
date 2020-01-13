// 日本語

#pragma once

#define WWMFReaderStrCount 256
#include <stdint.h>

extern "C" {

    struct WWMFReaderMetadata {
        int sampleRate;
        int numChannels;
        int bitsPerSample; // 8, 16, 18, 20, 24, 32
        int bitRate;       // 不明のとき0になる。original file bitrate something like 128kbps

        uint32_t dwChannelMask;
        uint32_t dummy0;

        int64_t numFrames;

        int64_t pictureBytes;

        wchar_t title[WWMFReaderStrCount];
        wchar_t artist[WWMFReaderStrCount];
        wchar_t album[WWMFReaderStrCount];
        wchar_t composer[WWMFReaderStrCount];

        int BytesPerSample(void) const {
            // 8の倍数に繰り上げる。
            int containerBits = (bitsPerSample + 7) & ~7;
            return containerBits / 8;
        }

        int BytesPerFrame(void) const {
            return BytesPerSample() * numChannels;
        }

        int64_t PcmBytes(void) const {
            return numFrames * BytesPerFrame();
        }

    };
}; // extern "C"
