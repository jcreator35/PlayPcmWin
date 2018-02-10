#pragma once

#include <Windows.h>
#include <comdef.h>

void WWErrorDescription(HRESULT hr);

#ifdef _DEBUG
# define _CRTDBG_MAP_ALLOC
#endif

#ifdef _DEBUG
#  include <stdio.h>
#  define dprintf(x, ...) printf(x, __VA_ARGS__)
#else
#  define dprintf(x, ...)
#endif

#define HRG(x)                                              \
{                                                           \
    dprintf("D: invoke %s\n", #x);                          \
    hr = x;                                                 \
    if (FAILED(hr)) {                                       \
        _com_error err(hr);                                 \
        LPCWSTR errMsg = err.ErrorMessage();                \
        printf("E: %s failed (%08x) %S\n", #x, hr, errMsg); \
        WWErrorDescription(hr);                             \
        goto end;                                           \
    }                                                       \
}                                                           \

#define HRR(x)                                   \
{                                                \
      dprintf("D: invoke %s\n", #x);             \
    hr = x;                                      \
    if (FAILED(hr)) {                            \
        printf("E: %s failed (%08x)\n", #x, hr); \
        WWErrorDescription(hr);                  \
        return hr;                               \
    }                                            \
}                                                \

#define CHK(x)                     \
{   if (!x) {                      \
    printf("E: %s is nullptr\n", #x); \
        assert(0);                 \
        return E_FAIL;             \
    }                              \
}                                  \

template <class T> void SafeRelease(T **ppT)
{
    if (*ppT)
    {
        (*ppT)->Release();
        *ppT = nullptr;
    }
}

