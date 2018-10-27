#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <SDKDDKVer.h>

#include <stdio.h>
#include <stdlib.h>

#define BHRG(F)                                       \
    if (!F) {                                         \
        hr = E_FAIL;                                  \
        printf("Error: %s:%d\n", __FILE__, __LINE__); \
        goto end;                                     \
    }

#define HRG(x)                                                 \
    hr = x;                                                    \
    if (FAILED(hr)) {                                          \
        printf("Error %08x: %s:%d\n", hr, __FILE__, __LINE__); \
        goto end;                                              \
    }

#define ALLOC_MEM(x, t, s)                                     \
    x = (t)malloc(s);                                          \
    if (nullptr == x) {                                        \
        hr = E_OUTOFMEMORY;                                    \
        printf("Error %08x: %s:%d\n", hr, __FILE__, __LINE__); \
        goto end;                                              \
    }                                                          \
    memset(x, 0, s);

