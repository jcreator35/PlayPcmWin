// 日本語。

#pragma once

#include <stdexcept>
#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>

#include "WWCommonUtil.h"
#include <stdint.h>

class WWMFReadFragments {
public:
    WWMFReadFragments(void) : mReader(nullptr) {
        memset(&mMfext, 0, sizeof mMfext);
    }

    IMFSourceReader *mReader;
    WAVEFORMATEXTENSIBLE mMfext;

    ~WWMFReadFragments(void) {
        SafeRelease(&mReader);
    }

    HRESULT Start(const wchar_t *wszSourceFile);

    /// std::length_errorがthrowされることがある。
    HRESULT ReadFragment(unsigned char *data_return, int64_t *dataBytes_inout);
    void End(void);
};
