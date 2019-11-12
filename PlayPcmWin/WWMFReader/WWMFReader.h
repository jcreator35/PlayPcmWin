// 日本語。

#pragma once

#ifdef WWMFREADER_EXPORTS
#define WWMFREADER_API __declspec(dllexport)
#else
#define WWMFREADER_API __declspec(dllimport)
#endif

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>
#include <stdint.h>

#define WWMFReaderStrCount 256

extern "C" {

struct WWMFReaderMetadata {
    int sampleRate;
    int numChannels;
    int bitsPerSample;
    int bitRate;

    uint32_t dwChannelMask;
    uint32_t dummy0;

    /// おおよその値が戻る。
    int64_t numApproxFrames;
    int64_t numExactFrames;

    int64_t pictureBytes;

    wchar_t title[WWMFReaderStrCount];
    wchar_t artist[WWMFReaderStrCount];
    wchar_t album[WWMFReaderStrCount];
    wchar_t composer[WWMFReaderStrCount];

    int64_t CalcApproxDataBytes(void) const {
        return numApproxFrames * numChannels * bitsPerSample / 8;
    }
};

WWMFREADER_API int __stdcall
WWMFReaderReadHeader(
        const wchar_t *wszSourceFile,
        WWMFReaderMetadata *meta_return);

WWMFREADER_API int __stdcall
WWMFReaderGetCoverart(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout);

WWMFREADER_API int __stdcall
WWMFReaderReadData(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout);


}; // extern "C"

