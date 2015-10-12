#include "WWPcmData.h"
#include "WWUtil.h"
#include <Windows.h>
#include <stdio.h>
#include <stdint.h>

struct WaveFormatInfo {
    int bitsPerSample;
    int nChannels;
    int nSamplesPerSec;
    int nFrames;
    unsigned char *data;

    WaveFormatInfo(void) {
        data = nullptr;
    }

    ~WaveFormatInfo(void) {
        delete [] data;
        data = nullptr;
    }
};

static bool
ReadWaveChunk(FILE *fp, WaveFormatInfo &wfi)
{
    bool result = false;
    unsigned char header[8];
    size_t bytes;
    
    bytes = fread(header, 1, 8, fp);
    if (bytes != 8) {
        // end of file
        return false;
    }

    UINT32 chunkSize = *((UINT32*)(&header[4]));
    chunkSize = 0xfffffffe & (chunkSize+1);
    if (chunkSize == 0) {
        printf("E: wave chunkSize error\n");
        return false;
    }

    unsigned char *buff = new unsigned char[chunkSize];
    if (nullptr == buff) {
        printf("E: memory allocation failed\n");
        return false;
    }
    bytes = fread(buff, 1, chunkSize, fp);
    if (bytes != chunkSize) {
        printf("E: wave read error 2\n");
        goto end;
    }

    if (0 == strncmp("fmt ", (const char *)header, 4)) {
        if (chunkSize < 16) {
            printf("E: fmt chunk exists while chunk size is too small\n");
            goto end;
        }

        int wFormatTag = *((short*)(&buff[0]));
        if (wFormatTag != 1) { /* PCM */
            printf("E: wave fmt %d is not supported\n", wFormatTag);
            goto end;
        }
        wfi.nChannels = *((short*)(&buff[2]));
        wfi.nSamplesPerSec = *((int*)(&buff[4]));
        wfi.bitsPerSample = *((short*)(&buff[14]));
        if (wfi.nChannels != 2) {
            printf("E: nChannels=%d is not supported\n", wfi.nChannels);
            goto end;
        }
        result = true;
    } else if (0 == strncmp("data", (const char*)header, 4)) {
        int bytesPerFrame = wfi.nChannels * (wfi.bitsPerSample/8);
        if (bytesPerFrame == 0) {
            printf("E: nChannels=%d is not supported\n", wfi.nChannels);
            goto end;
        }

        wfi.data = buff;
        wfi.nFrames = chunkSize / bytesPerFrame;
        buff = nullptr;
        // data chunk found . no need to continue reading
        result = false;
    } else {
        // skip unknown chunk
        fseek(fp, chunkSize, SEEK_CUR);
        result = true;
    }

end:
    delete [] buff;
    buff = nullptr;

    return result;
}

WWPcmData *
WWReadWavFile(const char *path, WWPcmDataStreamAllocType t)
{
    unsigned char buff[12];
    WWPcmData *result = nullptr;
    WaveFormatInfo wfi;

    memset(&wfi, 0, sizeof wfi);

    FILE *fp = nullptr;
    fopen_s(&fp, path, "rb");
    if (nullptr == fp) {
        return nullptr;
    }

    size_t rv = fread(buff, 1, 12, fp);
    if (rv != 12) {
        printf("E: flie size is too small\n");
        goto end;
    }
    if (0 != (strncmp("RIFF", (const char*)buff, 4))) {
        goto end;
    }
    if (0 != (strncmp("WAVE", (const char*)&buff[8], 4))) {
        printf("E: WAVE not found\n");
        goto end;
    }

    for (;;) {
        if (!ReadWaveChunk(fp, wfi)) {
            break;
        }
    }

    if (wfi.data) {
        result = new WWPcmData();
        if (nullptr == result) {
            goto end;
        }

        result->Init(t);

        result->bitsPerSample  = wfi.bitsPerSample;
        result->validBitsPerSample = wfi.bitsPerSample;
        result->nChannels      = wfi.nChannels;
        result->nSamplesPerSec = wfi.nSamplesPerSec;
        result->nFrames        = wfi.nFrames;
        result->posFrame = 0;

        int64_t bytes = (int64_t)result->nFrames * result->nChannels * result->bitsPerSample / 8;
        if (!result->StoreStream(wfi.data, bytes)) {
            printf("memory allocation failed\n");
            goto end;
        }
    }
    
end:
    fclose(fp);
    return result;
}


