// 日本語。

#include "WWUtil.h"

double
SincD(double sinx, double x)
{
    if (-0.000000001 < x && x < 0.000000001) {
        return 1.0;
    } else {
        return sinx / x;
    }
}

BYTE*
WWStereo24ToStereo32(BYTE *data, int bytes)
{
    int nData = bytes / 3; // 3==24bit

    BYTE *p = (BYTE *)malloc(nData * 4);
    if (nullptr == p) {
        return nullptr;
    }

    int fromPos = 0;
    int toPos = 0;
    for (int i=0; i<nData; ++i) {
        p[toPos++] = 0;
        p[toPos++] = data[fromPos++];
        p[toPos++] = data[fromPos++];
        p[toPos++] = data[fromPos++];
    }

    return p;
}

BYTE*
WWStereo24ToStereoFloat32(BYTE *data, int bytes)
{
    int nData = bytes / 3; // 3==24bit

    float *p = (float *)malloc(nData * 4);
    if (nullptr == p) {
        return nullptr;
    }
    int fromPos = 0;
    int toPos = 0;
    for (int i=0; i<nData; ++i) {
        int v = (data[fromPos]<<8)
            + (data[fromPos+1]<<16)
            + (data[fromPos+2]<<24);

        float r = (float)(v * (1.0 / 2147483648.0));
        p[toPos++] = r;

        fromPos += 3;
    }

    return (BYTE*)p;
}

BYTE*
WWStereo16ToStereoFloat32(BYTE *data, int bytes)
{
    int nData = bytes / 2; // 2==16bit

    float *p = (float *)malloc(nData * 4);
    if (nullptr == p) {
        return nullptr;
    }
    int fromPos = 0;
    int toPos = 0;
    for (int i=0; i<nData; ++i) {
        int v = (data[fromPos]<<16)
            + (data[fromPos+1]<<24);

        float r = (float)(v * (1.0 / 2147483648.0));
        p[toPos++] = r;

        fromPos += 2;
    }

    return (BYTE*)p;
}

void
WWWaveFormatDebug(WAVEFORMATEX *v)
{
    (void)v;

    dprintf(
        "  cbSize=%d\n"
        "  nAvgBytesPerSec=%d\n"
        "  nBlockAlign=%d\n"
        "  nChannels=%d\n"
        "  nSamplesPerSec=%d\n"
        "  wBitsPerSample=%d\n"
        "  wFormatTag=0x%x\n",
        v->cbSize,
        v->nAvgBytesPerSec,
        v->nBlockAlign,
        v->nChannels,
        v->nSamplesPerSec,
        v->wBitsPerSample,
        v->wFormatTag);
}

void
WWWFEXDebug(WAVEFORMATEXTENSIBLE *v)
{
    (void)v;

    dprintf(
        "  dwChannelMask=0x%x\n"
        "  Samples.wValidBitsPerSample=%d\n"
        "  SubFormat=0x%x\n",
        v->dwChannelMask,
        v->Samples.wValidBitsPerSample,
        v->SubFormat);
}

