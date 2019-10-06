#pragma once
// 日本語 UTF-8

#include <Windows.h>
#include <mmsystem.h>
#include <MMReg.h>
#include <string>
#include <vector>

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
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        goto end;                                 \
    }                                             \
}                                                 \

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

#define HRB(x)                                    \
{                                                 \
    dprintf("D: %s\n", #x);                       \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        break;                                    \
    }                                             \
}                                                 \

#define HRB_Quiet(x)                              \
{                                                 \
    hr = x;                                       \
    if (FAILED(hr)) {                             \
        dprintf("E: %s:%d %s failed (%08x)\n",    \
            __FILE__, __LINE__, #x, hr);          \
        break;                                    \
    }                                             \
}                                                 \

#define CHK(x)                           \
{   if (!x) {                            \
        dprintf("E: %s:%d %s is nullptr\n", \
            __FILE__, __LINE__, #x);     \
        return E_FAIL;                   \
    }                                    \
}                                        \

template <class T> void SafeRelease(T **ppT)
{
    if (*ppT)
    {
        (*ppT)->Release();
        *ppT = nullptr;
    }
}

#define SAFE_RELEASE(x) { if (x) { x->Release(); x = nullptr; } }

#define SAFE_DELETE(x) { delete x; x=nullptr; }

void
WWWaveFormatDebug(WAVEFORMATEX *v);

void
WWWFEXDebug(WAVEFORMATEXTENSIBLE *v);

/// white spaceで区切られたトークン列から、トークンの配列を取り出す。
void
WWSplit(std::wstring s, std::vector<std::wstring> & result);

/// comma separated numberから、フラグ配列をセット。
/// flagCount==8のとき
/// 例: "1"     → 0,1,0,0,0,0,0,0
/// 例: "1,3,4" → 0,1,0,1,1,0,0,0
/// 例: "-1"    → 1,1,1,1,1,1,1,1
void
WWCommaSeparatedIdxToFlagArray(const std::wstring sIn, bool *flagAry_out, const int flagCount);
