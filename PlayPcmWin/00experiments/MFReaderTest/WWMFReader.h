#pragma once

#define WINVER _WIN32_WINNT_WIN7

#include <windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <stdio.h>
#include <mferror.h>

struct WWMFMetadata {
    int numChannels;
    int samplesPerSec;
    int bitsPerSample;
    int validBitsPerSample;

    static const int META_STR_COUNT = 256;

    WCHAR title[META_STR_COUNT];
    WCHAR albumName[META_STR_COUNT];
    WCHAR artistName[META_STR_COUNT];
    int albumCoverArtBytes;
    char *albumCoverArt;

    void Delete(void) {
        delete[] albumCoverArt;
        albumCoverArt = nullptr;
    }
};

struct WWMFPcmData {
    int bytes;
    void *data;

    void Delete(void) {
        delete[] data;
        data = nullptr;
    }
};

class WWMFReader {
public:
    WWMFReader(void);
    ~WWMFReader(void);

    HRESULT Init(LPCWSTR path);
    void Term(void);

    // title, albumname, artist and album cover art image
    HRESULT RetrieveMFMetadata(WWMFMetadata &metadata_return);

    // @param streamFlags_return MF_SOURCE_READERF_ENDOFSTREAM or MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED if stream ends
    HRESULT ReadNextSamples(WWMFPcmData &pcmData, DWORD &streamFlags_return);

private:
    IMFMediaSource *mSource;
    IMFSourceReader *mReader;
    IMFMediaType *mPcmAudio;
};
