#pragma once

#include <Windows.h>
#include <mmsystem.h>
#include <MMReg.h>

#ifdef _DEBUG
#  include <stdio.h>
#  define dprintf(x, ...) printf(x, __VA_ARGS__)
#else
#  define dprintf(x, ...)
#endif

// 正常時もdprintfが出るバージョン。HRGRも参照。
#define HRG(x)                                    \
{                                                 \
    dprintf("D: %s\n", #x);                       \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        goto end;                                 \
    }                                             \
}                                                 \

// エラー時にgoto end;ではなくreturn hr;するHRG。
#define HRR(x)                                    \
{                                                 \
    dprintf("D: %s\n", #x);                       \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        return hr;                                \
    }                                             \
}                                                 \

// 正常時のdprintfを抑制したHRG。失敗するとresult=false;する。
#define HRGR(x)                                   \
{                                                 \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        result = false;                           \
        goto end;                                 \
    }                                             \
}                                                 \

#define CHK(x)                           \
{   if (!x) {                            \
        dprintf("E: %s:%d %s is NULL\n", \
            __FILE__, __LINE__, #x);     \
        return E_FAIL;                   \
    }                                    \
}                                        \

template <class T> void SafeRelease(T **ppT)
{
    if (*ppT)
    {
        (*ppT)->Release();
        *ppT = NULL;
    }
}
#define SAFE_RELEASE(x) { if (x) { x->Release(); x = NULL; } }

#define SAFE_DELETE(x) { delete x; x=NULL; }

// malloc memory and copy-create 32bitWAV from 24bitWAV data
BYTE*
WWStereo24ToStereo32(BYTE *data, int bytes);

BYTE*
WWStereo24ToStereoFloat32(BYTE *data, int bytes);

BYTE*
WWStereo16ToStereoFloat32(BYTE *data, int bytes);

void
WWWaveFormatDebug(WAVEFORMATEX *v);

void
WWWFEXDebug(WAVEFORMATEXTENSIBLE *v);
