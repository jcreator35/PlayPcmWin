// 日本語。

#pragma warning(disable:4127)  // Disable warning C4127: conditional expression is constant

#define WINVER _WIN32_WINNT_WIN7

#include "WWMFResampler.h"
#include "WWMFRSUtil.h"
#include <crtdbg.h>
#include <math.h>
#include <stdio.h>
#include <stdint.h>

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
WriteInt16(FILE *fpw, short value)
{
    DWORD writeBytes = 0;

    writeBytes = fwrite(&value, 1, 2, fpw);
    if (2U != writeBytes) {
        printf("write error\n");
        return E_FAIL;
    }

    return S_OK;
}

static HRESULT
WriteInt32(FILE *fpw, int value)
{
    DWORD writeBytes = 0;

    writeBytes = fwrite(&value, 1, 4, fpw);
    if (4U != writeBytes) {
        printf("write error\n");
        return E_FAIL;
    }

    return S_OK;
}

static HRESULT
WriteBytes(FILE *fpw, const char *s, DWORD bytes)
{
    DWORD writeBytes = 0;

    writeBytes = fwrite(s, 1, bytes, fpw);
    if (bytes != writeBytes) {
        printf("write error\n");
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

static HRESULT
WriteWavHeader(FILE *fpw, WWMFPcmFormat &format, DWORD dataBytes)
{
    HRESULT hr = E_FAIL;
    int dataChunkSize = ((dataBytes+1)&(~1)) + 4;

    HRG(WriteBytes(fpw, "RIFF", 4U));
    HRG(WriteInt32(fpw, dataChunkSize + 0x24));
    HRG(WriteBytes(fpw, "WAVE", 4U));

    HRG(WriteBytes(fpw, "fmt ", 4U));
    HRG(WriteInt32(fpw, 16));

    // fmt audioFormat size==2 1==int 3==float
    switch (format.sampleFormat) {
    case WWMFBitFormatInt:
        HRG(WriteInt16(fpw, 1));
        break;
    case WWMFBitFormatFloat:
        HRG(WriteInt16(fpw, 3));
        break;
    default:
        goto end;
    }

    // fmt numChannels size==2
    HRG(WriteInt16(fpw, format.nChannels));

    // fmt sampleRate size==4
    HRG(WriteInt32(fpw, format.sampleRate));

    // fmt byteRate size==4
    HRG(WriteInt32(fpw, format.BytesPerSec()));

    // fmt blockAlign size==2
    HRG(WriteInt16(fpw, format.FrameBytes()));

    // fmt bitspersample size==2
    HRG(WriteInt16(fpw, format.bits));

    HRG(WriteBytes(fpw, "data", 4U));
    HRG(WriteInt32(fpw, dataChunkSize));

end:
    return hr;
}

static HRESULT
FixWavHeader(FILE *fpw, DWORD writeDataTotalBytes)
{
    HRESULT hr = E_FAIL;

    fseek(fpw, 4, SEEK_SET);
    HRG(WriteInt32(fpw, writeDataTotalBytes + 0x24));

    fseek(fpw, 0x28, SEEK_SET);
    HRG(WriteInt32(fpw, writeDataTotalBytes));

end:
    return hr;
}

static void
PrintUsage(const wchar_t *name)
{
    printf(
            "Usage: %S inputWavFile outputWavFile outputSampleRate outputBitdepth conversionQuality\n"
            "outputBitDepth: 16, 24 or 32. If 32 is specified, output format becomes float, otherwise int.\n"
            "conversionQuality: 1 to 60. 1 is worst quality. 60 is best quality.", name);
}

int wmain(int argc, wchar_t *argv[])
{
    // _CrtSetBreakAlloc(35);
    // COM leak cannot be detected by debug heap manager ...
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

    HRESULT hr = S_OK;
    bool bCoInitialize = false;
    FILE *fpr = NULL;
    FILE *fpw = NULL;
    errno_t ercd;
    BYTE *buff = NULL;
    DWORD buffBytes = 0;
    DWORD readBytes = 0;
    DWORD remainBytes = 0;
    DWORD expectedOutputDataBytes = 0;
    DWORD result = 0;
    DWORD writeDataTotalBytes = 0;
    int conversionQuality = 60;
    WWMFResampler resampler;
    WWMFPcmFormat inputFormat;
    WWMFPcmFormat outputFormat;
    WWMFSampleData sampleData;

    if (argc != 6) {
        PrintUsage(argv[0]);
        return 1;
    }

    HRG(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE));
    bCoInitialize = true;

    ercd = _wfopen_s(&fpr, argv[1], L"rb");
    if (0 != ercd) {
        printf("file open error %S\n", argv[1]);
        PrintUsage(argv[0]);
        hr = E_FAIL;
        goto end;
    }

    ercd = _wfopen_s(&fpw, argv[2], L"wb");
    if (0 != ercd) {
        printf("file open error %S\n", argv[2]);
        PrintUsage(argv[0]);
        hr = E_FAIL;
        goto end;
    }

    HRG(ReadWavHeader(fpr, &inputFormat, &remainBytes));

    outputFormat = inputFormat;
    outputFormat.sampleRate = _wtoi(argv[3]);
    outputFormat.bits = (short)_wtoi(argv[4]);

    conversionQuality = _wtoi(argv[5]);

    if (0 == outputFormat.sampleRate ||
        0 == conversionQuality) {
        PrintUsage(argv[0]);
        hr = E_FAIL;
        goto end;
    }

    outputFormat.validBitsPerSample = outputFormat.bits;

    switch (outputFormat.bits) {
    case 16:
    case 24:
        outputFormat.sampleFormat = WWMFBitFormatInt;
        break;
    case 32:
        outputFormat.sampleFormat = WWMFBitFormatFloat;
        break;
    default:
        PrintUsage(argv[0]);
        hr = E_FAIL;
        goto end;
    }

    expectedOutputDataBytes = (int64_t)remainBytes
        * outputFormat.BytesPerSec()
        / inputFormat .BytesPerSec();

    HRG(WriteWavHeader(fpw, outputFormat, expectedOutputDataBytes));

    HRG(resampler.Initialize(inputFormat, outputFormat, conversionQuality));

    buffBytes = 128 * 1024 * inputFormat.FrameBytes();
    buff = new BYTE[buffBytes];

    for (;;) {
        // read PCM data from file
        readBytes = buffBytes;
        if (remainBytes < readBytes) {
            readBytes = remainBytes;
        }
        remainBytes -= readBytes;

        result = fread(buff, 1, readBytes, fpr);
        if (result != readBytes) {
            printf("file read error\n");
            hr = E_FAIL;
            goto end;
        }

        // convert
        HRG(resampler.Resample(buff, readBytes, &sampleData));

        // write to file
        result = fwrite(sampleData.data, 1, sampleData.bytes, fpw);
        if (result != sampleData.bytes) {
            printf("file write error\n");
            hr = E_FAIL;
            goto end;
        }
        writeDataTotalBytes += sampleData.bytes;
        sampleData.Release();

        if (remainBytes == 0) {
            // end
            HRG(resampler.Drain(buffBytes, &sampleData));

            // write remaining PCM data to file
            result = fwrite(sampleData.data, 1, sampleData.bytes, fpw);
            if (result != sampleData.bytes) {
                printf("file write error\n");
                hr = E_FAIL;
                goto end;
            }
            writeDataTotalBytes += sampleData.bytes;
            sampleData.Release();
            break;
        }
    }

    // data chunk align is 2 bytes
    if (writeDataTotalBytes & 1) {
        if (0 != fputc(0, fpw)) {
            printf("file write error\n");
            hr = E_FAIL;
            goto end;
        }
        ++writeDataTotalBytes;
    }
    HRG(FixWavHeader(fpw, writeDataTotalBytes));

    hr = S_OK;

end:
    resampler.Finalize();

    if (bCoInitialize) {
        CoUninitialize();
        bCoInitialize = false;
    }

    delete[] buff;
    buff = NULL;

    if (fpw != NULL) {
        fclose(fpw);
        fpw = NULL;
    }
    if (fpr != NULL) {
        fclose(fpr);
        fpr = NULL;
    }

    return SUCCEEDED(hr) ? 0 : 1;
}

