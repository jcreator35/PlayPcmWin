#include "FlacDecodeDLL.h"
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <assert.h>



static void
PrintUsage(const wchar_t *argv0)
{
    printf("Usage: %S inputFlacFilePath skipSamples outputBinFilePath\n"
        " or : %S inputFlacFilePath          (display metadata)\n", argv0, argv0);
}

static bool
DisplayFlacMeta(const wchar_t *inPath)
{
    printf("D: %s:%d FlacDecodeDLL_DecodeStart from sample#%d %S\n", __FILE__, __LINE__, inPath);
    int id = FlacDecodeDLL_DecodeStart(inPath, -1);
    if (id < 0) {
        printf("E: %s:%d FlacDecodeDLL_DecodeStart %d\n", __FILE__, __LINE__, id);
        return false;
    }

    int bitsPerSample = FlacDecodeDLL_GetBitsPerSample(id);
    int channels      = FlacDecodeDLL_GetNumOfChannels(id);
    int sampleRate    = FlacDecodeDLL_GetSampleRate(id);
    int64_t numFrames = FlacDecodeDLL_GetNumFrames(id);
    int numFramesPerBlock = FlacDecodeDLL_GetNumFramesPerBlock(id);

    wchar_t titleStr[16];
    wchar_t albumStr[16];
    wchar_t artistStr[16];
    FlacDecodeDLL_GetTitleStr(id, titleStr, sizeof titleStr);
    FlacDecodeDLL_GetAlbumStr(id, albumStr, sizeof albumStr);
    FlacDecodeDLL_GetArtistStr(id, artistStr, sizeof artistStr);

    {
        int pictureBytes = FlacDecodeDLL_GetPictureBytes(id);
        if (0 < pictureBytes) {
            char *pictureData = (char*)malloc(pictureBytes);
            assert(pictureData);
            int rv = FlacDecodeDLL_GetPictureData(id, 0, pictureBytes, pictureData);
            if (0 < rv) {
                FILE *fp = NULL;
                errno_t erno = _wfopen_s(&fp, L"image.bin", L"wb");
                assert(erno == 0);
                assert(fp);

                fwrite(pictureData, 1, pictureBytes, fp);

                fclose(fp);
                fp = NULL;

            }

            free(pictureData);
            pictureData = NULL;
        }
    }

    printf("D: decodeId=%d bitsPerSample=%d sampleRate=%d numFrames=%lld channels=%d numFramesPerBlock=%d\n",
        id,
        bitsPerSample,
        sampleRate,
        numFrames,
        channels,
        numFramesPerBlock);

    printf("D: title=%S\n", titleStr);
    printf("D: album=%S\n", albumStr);
    printf("D: artist=%S\n", artistStr);

    {
        int nCuesheets = FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(id);
        printf("D: cuesheet=%d\n", nCuesheets);
        for (int i=0; i<nCuesheets; ++i) {
            int trackNr = FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(id, i);
            int64_t offs = FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(id, i);
            printf("  %d trackNr=%d offs=%lld\n", i, trackNr, offs);
        }
    }

    FlacDecodeDLL_DecodeEnd(id);
    return true;
}

static bool
DecodeFlacFile(const wchar_t *inPath, int skipSamples, const wchar_t *outPath)
{
    printf("D: %s:%d FlacDecodeDLL_DecodeStart from sample#%d %S\n", __FILE__, __LINE__, skipSamples, inPath);
    int id = FlacDecodeDLL_DecodeStart(inPath, skipSamples);
    if (id < 0) {
        printf("E: %s:%d FlacDecodeDLL_DecodeStart %d\n", __FILE__, __LINE__, id);
        return false;
    }

    int bitsPerSample = FlacDecodeDLL_GetBitsPerSample(id);
    int channels      = FlacDecodeDLL_GetNumOfChannels(id);
    int sampleRate    = FlacDecodeDLL_GetSampleRate(id);
    int64_t numFrames = FlacDecodeDLL_GetNumFrames(id);
    int numFramesPerBlock = FlacDecodeDLL_GetNumFramesPerBlock(id);

    wchar_t titleStr[16];
    wchar_t albumStr[16];
    wchar_t artistStr[16];
    FlacDecodeDLL_GetTitleStr(id, titleStr, sizeof titleStr);
    FlacDecodeDLL_GetAlbumStr(id, albumStr, sizeof albumStr);
    FlacDecodeDLL_GetArtistStr(id, artistStr, sizeof artistStr);

    {
        int pictureBytes = FlacDecodeDLL_GetPictureBytes(id);
        if (0 < pictureBytes) {
            char *pictureData = (char*)malloc(pictureBytes);
            assert(pictureData);
            int rv = FlacDecodeDLL_GetPictureData(id, 0, pictureBytes, pictureData);
            if (0 < rv) {
                FILE *fp = NULL;
                errno_t erno = _wfopen_s(&fp, L"image.bin", L"wb");
                assert(erno == 0);
                assert(fp);

                fwrite(pictureData, 1, pictureBytes, fp);

                fclose(fp);
                fp = NULL;

            }

            free(pictureData);
            pictureData = NULL;
        }
    }

    printf("D: decodeId=%d bitsPerSample=%d sampleRate=%d numFrames=%lld channels=%d numFramesPerBlock=%d\n",
        id,
        bitsPerSample,
        sampleRate,
        numFrames,
        channels,
        numFramesPerBlock);

    printf("D: title=%S\n", titleStr);
    printf("D: album=%S\n", albumStr);
    printf("D: artist=%S\n", artistStr);

    {
        int nCuesheets = FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(id);
        printf("D: cuesheet=%d\n", nCuesheets);
        for (int i=0; i<nCuesheets; ++i) {
            int trackNr = FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(id, i);
            int64_t offs = FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(id, i);
            printf("  %d trackNr=%d offs=%lld\n", i, trackNr, offs);
        }
    }

    {
        FILE *fp = NULL;
        errno_t erno = _wfopen_s(&fp, outPath, L"wb");
        assert(erno == 0);
        assert(fp);

        int     ercd    = 0;
        int     nFrames = (1048576 / numFramesPerBlock) * numFramesPerBlock;
        int     bytesPerFrame = channels * bitsPerSample / 8;
        int64_t pcmPos  = 0;
        char *data   = (char *)malloc(nFrames * bytesPerFrame);
        do {
            memset(data, 0xee, nFrames * bytesPerFrame);

            int rv = FlacDecodeDLL_GetNextPcmData(id, nFrames, data);
            ercd   = FlacDecodeDLL_GetLastResult(id);

            if (0 < rv) {
                fwrite(data, 1, rv * bytesPerFrame, fp);
                pcmPos += rv;
            }
            // printf("D: GetNextPcmData get %d samples. total %lld\n", rv, pcmPos);

            if (rv <= 0 || ercd == FDRT_Completed) {
                printf("D: GetNextPcmData rv=%d ercd=%d\n", rv, ercd);
                break;
            }
        } while (true);

        fclose(fp);
        fp = NULL;

        free(data);
        data = NULL;

        if (ercd != 1) {
            printf("D: ERROR result=%d\n", ercd);
        }
    }

    FlacDecodeDLL_DecodeEnd(id);
    return true;
}

int
wmain(int argc, wchar_t* argv[])
{
    bool result = false;

    if (argc != 4 && argc != 2) {
        PrintUsage(argv[0]);
        return 1;
    }

    if (argc == 4) {
        int skipSamples = _wtoi(argv[2]);
        if (skipSamples < 0) {
            PrintUsage(argv[0]);
            return 1;
        }
        result = DecodeFlacFile(argv[1], skipSamples, argv[3]);
    }

    if (argc == 2) {
        result = DisplayFlacMeta(argv[1]);
    }

    return result == true ? 0 : 1;
}

