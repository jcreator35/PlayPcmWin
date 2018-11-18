// 日本語。


#include <stdio.h>
#include <string.h>
#include <crtdbg.h>
#include "WWMFReader.h"
#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "../WasapiIODLL/WWUtil.h"
#include <mfapi.h>

static int
ReadHeader(const wchar_t *inputFile)
{
    unsigned char *picture = nullptr;
    WWMFReaderMetadata meta;
    int rv = WWMFReaderReadHeader(inputFile, &meta);
    if (rv < 0) {
        printf("Error: read failed %S\n", inputFile);
        return rv;
    }

    int durationSec = (int)(meta.numApproxFrames/meta.sampleRate);

    printf("bitsPerSample = %d\n", meta.bitsPerSample);
    printf("sampleRate    = %d\n", meta.sampleRate);
    printf("numChannels   = %d\n", meta.numChannels);
    printf("bitrate       = %d\n", meta.bitRate);
    printf("nApproxFrames = %lld\n", meta.numApproxFrames);
    printf("duration      = %d:%02d\n", durationSec/60, durationSec%60);
    printf("Title         = %S\n", meta.title);
    printf("Artist        = %S\n", meta.artist);
    printf("Album         = %S\n", meta.album);
    printf("Composer      = %S\n", meta.composer);
    printf("pictureBytes  = %lld\n", meta.pictureBytes);

    picture = new unsigned char[meta.pictureBytes];

    if (0 < meta.pictureBytes) {
        rv = WWMFReaderGetCoverart(inputFile, picture, &meta.pictureBytes);
        if (rv < 0) {
            printf("Error: error read coverart\n");
        }

        {
            FILE *fp = fopen("picture.jpg", "wb");
            fwrite(picture, meta.pictureBytes, 1, fp);
            fclose(fp);
        }

        delete [] picture;
        picture = nullptr;
    }

    return rv;
}

static int
ReadHeaderAndData(
        const wchar_t *inputFile)
{
    unsigned char *data = nullptr;
    int64_t dataBytes = 0;
    WWMFReaderMetadata meta;
    HRESULT hr = S_OK;

    hr = WWMFReaderReadHeader(inputFile, &meta);
    if (hr < 0) {
        printf("Error: WWMFReaderReadHeader failed %S\n", inputFile);
        return hr;
    }

    data = new unsigned char[meta.CalcApproxDataBytes()];
    if (nullptr == data) {
        return E_OUTOFMEMORY;
    }

    dataBytes = meta.CalcApproxDataBytes();
    hr = WWMFReaderReadData(inputFile, data, &dataBytes);
    if (hr < 0) {
        printf("Error: WWMFReaderReadData failed %S\n", inputFile);
    } else {
        printf("data bytes = %lld\n", dataBytes);

        int bytesPerFrame = meta.numChannels * meta.bitsPerSample / 8;
        int64_t numFrames = dataBytes / bytesPerFrame;
        meta.numExactFrames = numFrames;

        int sec = (int)(dataBytes / (meta.sampleRate * bytesPerFrame));

        printf("duration   = %d:%02d\n", sec / 60, sec % 60);
    }

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
        "        convert to wav\n",
        appName, appName);
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
