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

// ���펞��dprintf���o��o�[�W�����BHRGR���Q�ƁB
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

// �G���[����goto end;�ł͂Ȃ�return hr;����HRG�B
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

// ���펞��dprintf��}������HRG�B���s�����result=false;����B
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
