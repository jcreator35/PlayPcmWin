// 日本語。


#include <stdio.h>
#include <string.h>
#include <crtdbg.h>
#include "WWMFReader.h"
#include "WWMFReadFragments.h"
#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "WWCommonUtil.h"
#include <mfapi.h>

static HRESULT
ReadHeader(const wchar_t *inputFile)
{
    HRESULT hr = S_OK;
    unsigned char *picture = nullptr;
    WWMFReaderMetadata meta;

    hr = WWMFReaderReadHeader(inputFile, 0, &meta);
    if (FAILED(hr)) {
        printf("Error: read failed %S\n", inputFile);
        return hr;
    }

    int durationSec = (int)(meta.numFrames/meta.sampleRate);

    printf("bitsPerSample = %d\n", meta.bitsPerSample);
    printf("sampleRate    = %d\n", meta.sampleRate);
    printf("numChannels   = %d\n", meta.numChannels);
    printf("bitrate       = %d\n", meta.bitRate);
    printf("numFrames     = %lld\n", meta.numFrames);
    printf("duration      = %d:%02d\n", durationSec/60, durationSec%60);
    printf("Title         = %S\n", meta.title);
    printf("Artist        = %S\n", meta.artist);
    printf("Album         = %S\n", meta.album);
    printf("Composer      = %S\n", meta.composer);
    printf("pictureBytes  = %lld\n", meta.pictureBytes);

    picture = new unsigned char[meta.pictureBytes];

    if (0 < meta.pictureBytes) {
        hr = WWMFReaderGetCoverart(inputFile, picture, &meta.pictureBytes);
        if (FAILED(hr)) {
            printf("Error: error read coverart\n");
            return hr;
        }

        {
            FILE *fp = nullptr;
            errno_t erno = fopen_s(&fp, "picture.jpg", "wb");
            if (erno != 0 || fp == nullptr) {
                printf("Error: write picture.jpg failed\n");
                return E_FAIL;
            }
            fwrite(picture, meta.pictureBytes, 1, fp);
            fclose(fp);
        }

        delete [] picture;
        picture = nullptr;
    }

    return hr;
}

static int
ReadHeaderAndData(
        const wchar_t *inputFile)
{
    uint8_t *data = nullptr;
    int64_t dataBytes = 0;
    int64_t readBytes = 0;
    WWMFReaderMetadata meta;
    WWMFReadFragments mReader;
    HRESULT hr = S_OK;

    hr = WWMFReaderReadHeader(inputFile, WWMFREADER_FLAG_RESOLVE_NUM_FRAMES, &meta);
    if (hr < 0) {
        printf("Error: WWMFReaderReadHeader failed %S\n", inputFile);
        return hr;
    }

    dataBytes = meta.PcmBytes();
    printf("data bytes = %lld\n", dataBytes);
    data = new uint8_t[dataBytes];
    if (nullptr == data) {
        return E_OUTOFMEMORY;
    }

    hr = mReader.Start(inputFile);
    if (hr < 0) {
        printf("Error: WWMFReaderReadData failed %S\n", inputFile);
        goto end;
    }

    while (readBytes < dataBytes) {
        int64_t wantBytes = dataBytes - readBytes;
        HRG(mReader.ReadFragment(&data[readBytes], &wantBytes));
        readBytes += wantBytes;
    }

end:

    mReader.End();

    delete [] data;
    data = nullptr;

    return 0;
}

static int
ReadSeekTest(
        const wchar_t *inputFile)
{
    uint8_t *data = nullptr;
    int64_t totalBytes = 0;
    int64_t posBytes = 0;
    WWMFReaderMetadata meta;
    WWMFReadFragments mReader;
    HRESULT hr = S_OK;

    hr = WWMFReaderReadHeader(inputFile, WWMFREADER_FLAG_RESOLVE_NUM_FRAMES, &meta);
    if (hr < 0) {
        printf("Error: WWMFReaderReadHeader failed %S\n", inputFile);
        return hr;
    }

    totalBytes = meta.PcmBytes();
    printf("data bytes = %lld\n", totalBytes);
    data = new uint8_t[totalBytes];
    if (nullptr == data) {
        return E_OUTOFMEMORY;
    }

    hr = mReader.Start(inputFile);
    if (hr < 0) {
        printf("Error: WWMFReaderReadData failed %S\n", inputFile);
        goto end;
    }

    // シークします。
    int64_t posFrames = meta.numFrames/2;
    posBytes = posFrames * meta.BytesPerFrame();
    HRG(mReader.SeekToFrame(posFrames));

    while (posBytes < totalBytes) {
        int64_t wantBytes = totalBytes - posBytes;

        int64_t readBytes = wantBytes;
        HRG(mReader.ReadFragment(&data[posBytes], &readBytes));
        if (readBytes == 0) {
            break;
        }

        posBytes += readBytes;
    }

end:

    mReader.End();

    delete [] data;
    data = nullptr;

    return 0;
}

static void
PrintUsage(const wchar_t *appName)
{
    printf(
        "Usage:\n"
        "    %S -h inputFile\n"
        "        read header and print\n"
        "    %S -a inputFile\n"
        "        whole PCM extract test\n",
        "    %S -s inputFile\n"
        "        seek test\n",
        appName, appName, appName);
}

int
wmain(int argc, wchar_t *argv[])
{
    int hr = S_OK;
    int rv = 1;

    bool printUsage = true;

    // _CrtSetBreakAlloc(35);
    // COM leak cannot be detected by debug heap manager ...
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

    (void)HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0); 

    // Initialize the COM library.
    HRG(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE));

    switch (argc) {
    case 3:
        if (0 == wcsncmp(L"-h", argv[1], 3)) {
            rv = ReadHeader(argv[2]);
            printUsage = false;
        }
        if (0 == wcsncmp(L"-a", argv[1], 3)) {
            rv = ReadHeaderAndData(argv[2]);
            printUsage = false;
        }
        if (0 == wcsncmp(L"-s", argv[1], 3)) {
            rv = ReadSeekTest(argv[2]);
            printUsage = false;
        }
        break;
    default:
        break;
    }

    if (printUsage) {
        PrintUsage(argv[0]);
    }

end:
    CoUninitialize();

    return rv;
}
