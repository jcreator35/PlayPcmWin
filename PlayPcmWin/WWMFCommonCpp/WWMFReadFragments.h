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
    WWMFReadFragments(void) : mReader(nullptr), mMFStarted(false) {
        memset(&mMfext, 0, sizeof mMfext);
    }

    IMFSourceReader *mReader;
    WAVEFORMATEXTENSIBLE mMfext;
    bool mMFStarted;

    ~WWMFReadFragments(void) {
        End();
    }

    HRESULT Start(const wchar_t *wszSourceFile);

    /// ファイルの先頭からのフレーム数指定でシークする。
    /// 指定フレーム番号のところにピッタリ移動できないことがあるので注意。
    /// ピッタリ移動できなかったとき、どこに移動したかは不明！
    HRESULT SeekToFrame(int64_t &nFrame_inout);

    /// std::length_errorがthrowされることがある。
    HRESULT ReadFragment(unsigned char *data_return, int64_t *dataBytes_inout);
    
    void End(void);
};
