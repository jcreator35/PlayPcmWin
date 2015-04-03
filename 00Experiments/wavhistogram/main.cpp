#include <stdio.h>

#include <windows.h>
#include <assert.h>

#include <mfapi.h>
#pragma comment(lib, "mfplat")
#pragma comment(lib, "mf")
#pragma comment(lib, "mfuuid")
#pragma comment(lib, "wmcodecdspuuid")

/// sample data type. int or float
/// it is compatible to WWBitFormatType on WasapiUser.h
enum WWMFBitFormatType {
    WWMFBitFormatUnknown = -1,
    WWMFBitFormatInt,
    WWMFBitFormatFloat,
    WWMFBitFormatNUM
};

struct WWMFPcmFormat {
    WWMFBitFormatType sampleFormat;
    WORD  nChannels;
    WORD  bits;
    DWORD sampleRate;
    DWORD dwChannelMask;
    WORD  validBitsPerSample;

    WWMFPcmFormat(void) {
        sampleFormat       = WWMFBitFormatUnknown;
        nChannels          = 0;
        bits               = 0;
        sampleRate         = 0;
        dwChannelMask      = 0;
        validBitsPerSample = 0;
    }

    WWMFPcmFormat(WWMFBitFormatType aSampleFormat, WORD aNChannels, WORD aBits,
            DWORD aSampleRate, DWORD aDwChannelMask, WORD aValidBitsPerSample) {
        sampleFormat       = aSampleFormat;
        nChannels          = aNChannels;
        bits               = aBits;
        sampleRate         = aSampleRate;
        dwChannelMask      = aDwChannelMask;
        validBitsPerSample = aValidBitsPerSample;
    }

    WORD FrameBytes(void) const {
        return (WORD)(nChannels * bits /8U);
    }

    DWORD BytesPerSec(void) const {
        return sampleRate * FrameBytes();
    }
};

/// WWMFSampleData contains new[] ed byte buffer pointer(data) and buffer size(bytes).
struct WWMFSampleData {
    DWORD  bytes;
    BYTE  *data;

    WWMFSampleData(void) : bytes(0), data(NULL) { }

    /// @param aData must point new[] ed memory address
    WWMFSampleData(BYTE *aData, int aBytes) {
        data  = aData;
        bytes = aBytes;
    }

    ~WWMFSampleData(void) {
        assert(NULL == data);
    }

    void Release(void) {
        delete[] data;
        data = NULL;
        bytes = 0;
    }

    void Forget(void) {
        data  = NULL;
        bytes = 0;
    }

    HRESULT Add(WWMFSampleData &rhs) {
        BYTE *buff = new BYTE[bytes + rhs.bytes];
        if (NULL == buff) {
            return E_FAIL;
        }

        memcpy(buff, data, bytes);
        memcpy(&buff[bytes], rhs.data, rhs.bytes);

        delete[] data;
        data = buff;
        bytes += rhs.bytes;
        return S_OK;
    }

    /**
     * If this instance is not empty, rhs content is concatenated to this instance. rhs remains untouched.
     * If this instance is empty, rhs content moves to this instance. rhs becomes empty.
     * rhs.Release() must be called to release memory either way!
     */
    HRESULT MoveAdd(WWMFSampleData &rhs) {
        if (bytes != 0) {
            return Add(rhs);
        }

        assert(NULL == data);
        *this = rhs; //< Just copy 8 bytes. It's way faster than Add()
        rhs.Forget();

        return S_OK;
    }
};


template <class T> void SafeRelease(T **ppT) {
    if (*ppT) {
        (*ppT)->Release();
        *ppT = NULL;
    }
}

#ifdef _DEBUG
#  include <stdio.h>
#  define dprintf(x, ...) printf(x, __VA_ARGS__)
#else
#  define dprintf(x, ...)
#endif

#define HRG(x)                                    \
{                                                 \
    dprintf("D: %s\n", #x);                       \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        printf("E: %s:%d %s failed (%08x)\n",     \
            __FILE__, __LINE__, #x, hr);          \
        goto end;                                 \
    }                                             \
}                                                 \

static HRESULT
ReadInt16(FILE *fpr, short *value_return)
{
    DWORD readBytes = 0;

    readBytes = fread(value_return, 1, 2, fpr);
    if (2U != readBytes) {
        printf("read error\n");
        return E_FAIL;
    }

    return S_OK;
}

static HRESULT
ReadInt32(FILE *fpr, int *value_return)
{
    DWORD readBytes = 0;

    readBytes = fread(value_return, 1, 4, fpr);
    if (4U != readBytes) {
        printf("read error\n");
        return E_FAIL;
    }

    return S_OK;
}

static HRESULT
ReadBytes(FILE *fpr, DWORD bytes, BYTE *s_return)
{
    DWORD readBytes = 0;

    readBytes = fread(s_return, 1, bytes, fpr);
    if (bytes != readBytes) {
        printf("read error\n");
        return E_FAIL;
    }

    return S_OK;
}

static HRESULT
ReadWavHeader(FILE *fpr, WWMFPcmFormat *format_return, DWORD *dataBytes_return)
{
    HRESULT hr = E_FAIL;
    BYTE buff[16];
    int chunkBytes = 0;
    int fmtChunkSize = 0;
    short shortValue;
    int intValue;

    HRG(ReadBytes(fpr, 12U, buff));

    if (0 != memcmp(buff, "RIFF", 4)) {
        printf("file is not riff wave file\n");
        goto end;
    }
    if (0 != memcmp(&buff[8], "WAVE", 4)) {
        printf("file is not riff wave file\n");
        goto end;
    }

    for (;;) {
        HRG(ReadBytes(fpr, 4U, buff));

        if (0 == memcmp(buff, "fmt ", 4)) {
            // fmt chunk

            // chunkSize size==4
            HRG(ReadInt32(fpr, &fmtChunkSize));
            if (16 != fmtChunkSize && 18 != fmtChunkSize && 40 != fmtChunkSize) {
                printf("unrecognized format");
                goto end;
            }
            // audioFormat size==2
            HRG(ReadInt16(fpr, &shortValue));
            if (1 == shortValue) {
                format_return->sampleFormat = WWMFBitFormatInt;
            } else if (3 == shortValue) {
                format_return->sampleFormat = WWMFBitFormatFloat;
            } else if (0xfffe == (unsigned short)shortValue) {
                // SampleFormat is written on WAVEFORMATEXTENSIBLE
                format_return->sampleFormat = WWMFBitFormatUnknown;
            } else {
                printf("unrecognized format");
                goto end;
            }

            // numChannels size==2
            HRG(ReadInt16(fpr, &shortValue));
            format_return->nChannels = shortValue;

            // sampleRate size==4
            HRG(ReadInt32(fpr, &intValue));
            format_return->sampleRate = intValue;

            // byteRate size==4
            HRG(ReadInt32(fpr, &intValue));

            // blockAlign size==2
            HRG(ReadInt16(fpr, &shortValue));

            // bitspersample size==2
            HRG(ReadInt16(fpr, &shortValue));
            format_return->bits = shortValue;
            format_return->validBitsPerSample = shortValue;

            if (16 < fmtChunkSize) {
                // subchunksize
                HRG(ReadInt16(fpr, &shortValue));
                if (0 == shortValue) {
                    hr = S_OK;
                    goto end;
                } else if (22 == shortValue) {
                    // validbitspersample
                    HRG(ReadInt16(fpr, &shortValue));
                    format_return->validBitsPerSample = shortValue;

                    // dwChannelMask
                    HRG(ReadInt32(fpr, (int*)&format_return->dwChannelMask));

                    // format GUID
                    HRG(ReadBytes(fpr, 16U, buff));
                    if (0 == memcmp(buff, &MFAudioFormat_Float, 16)) {
                        format_return->sampleFormat = WWMFBitFormatFloat;
                    } else if (0 == memcmp(buff, &MFAudioFormat_PCM, 16)) {
                        format_return->sampleFormat = WWMFBitFormatInt;
                    } else {
                        printf("unrecognized format guid");
                        goto end;
                    }

                    hr = S_OK;
                    goto end;
                } else {
                    printf("unrecognized format");
                    goto end;
                }
            }

        } else if (0 == memcmp(buff, "data", 4)) {
            // data chunk
            HRG(ReadInt32(fpr, (int*)dataBytes_return));
            break;
        } else {
            // skip this chunk
            HRG(ReadInt32(fpr, &chunkBytes));
            if (chunkBytes < 4) {
                printf("E: chunk bytes == %d\n", chunkBytes);
                goto end;
            }
            fseek(fpr, chunkBytes, SEEK_CUR);
        }

    }
end:
    if (S_OK == hr && format_return->sampleFormat == WWMFBitFormatUnknown) {
        printf("unrecognized format");
        hr = E_FAIL;
    }

    return hr;
}

int
main(int argc, char *argv[])
{
    HRESULT hr;

    if (argc != 2) {
        printf("Usage: %s readWavFile\n", argv[0]);
        return 1;
    }

    FILE *fpr = fopen(argv[1], "rb");
    if (NULL == fpr) {
        printf("read error %s\n", argv[1]);
        return 1;
    }

    WWMFPcmFormat fmt;
    DWORD remainBytes = 0;
    unsigned char *pUC = NULL;
    unsigned short *pUS = NULL;
    int readBytes = 0;
    int prevValue = INT_MAX;
    int prevPosition = INT_MAX;
    int *histogram16 = NULL;
    int *histogram24 = NULL;

    HRG(ReadWavHeader(fpr, &fmt, &remainBytes));
    pUC = new unsigned char[remainBytes];
    pUS = (unsigned short *)pUC;

    readBytes = fread(pUC, 1, remainBytes, fpr);
    if (readBytes != remainBytes) {
        printf("read error\n");
        hr = E_FAIL;
        goto end;
    }
    int totalSamples = readBytes/(fmt.bits/8);

    switch (fmt.bits) {
    case 16:
        histogram16 = new int[65536];
        memset(histogram16, 0, 65536*4);
        for (int i=0; i<remainBytes/2; ++i) {
            ++histogram16[pUS[i]];
        }
        prevPosition = 32767;
        for (int i=32768; i<65536; ++i) {
            if (prevValue == histogram16[i]) {
                continue;
            }
            if (prevPosition != i-1) {
                printf("%d %d %f\n", i-1-65536, prevValue, (double)prevValue / totalSamples);
            }
            prevValue = histogram16[i];
            prevPosition = i;
            printf("%d %d %f\n", i-65536, histogram16[i], (double)histogram16[i] / totalSamples);
        }
        prevPosition -= 65536;
        for (int i=0; i<32768; ++i) {
            if (prevValue == histogram16[i]) {
                continue;
            }
            if (prevPosition != i-1) {
                printf("%d %d %f\n", i-1, prevValue, (double)prevValue / totalSamples);
            }
            prevValue = histogram16[i];
            prevPosition = i;
            printf("%d %d %f\n", i, histogram16[i], (double)histogram16[i] / totalSamples);
        }
        break;
    case 24:
        histogram24 = new int[16777216];
        memset(histogram24, 0, 16777216*4);
        for (int i=0; i<remainBytes/3; ++i) {
            unsigned int v = pUC[i*3] + 256*pUC[i*3+1] + 65536*pUC[i*3+2];
            ++histogram24[v];
        }
        prevPosition = 8388607;
        for (int i=8388608; i<16777216; ++i) {
            if (abs(prevValue - histogram24[i]) < 1) {
                continue;
            }
            if (prevPosition != i-1) {
                printf("%12d %12d %f\n", i-1-16777216, prevValue, (double)prevValue / totalSamples);
            }
            prevValue = histogram24[i];
            prevPosition = i;
            printf("%12d %12d %f\n", i-16777216, histogram24[i], (double)histogram24[i] / totalSamples);
        }
        for (int i=0; i<8388608; ++i) {
            if (abs(prevValue - histogram24[i]) < 1) {
                continue;
            }
            if (prevPosition != i-1) {
                printf("%12d %12d %f\n", i-1, prevValue, (double)prevValue / totalSamples);
            }
            prevValue = histogram24[i];
            prevPosition = i;
            printf("%12d %12d %f\n", i, histogram24[i], (double)histogram24[i] / totalSamples);
        }
        break;
    default:
        printf("fmt.bits is not 16 nor 24\n");
        break;
    }

end:
    fclose(fpr);
    fpr = NULL;

    delete [] histogram16;
    histogram16 = NULL;

    delete [] histogram24;
    histogram24 = NULL;

    delete [] pUC;
    pUC = NULL;

    pUS = NULL;

    return hr == S_OK ? 0 : 1;
}
