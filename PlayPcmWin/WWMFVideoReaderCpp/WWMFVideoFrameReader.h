// 日本語。

#pragma once

#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdint.h>
#include <stdio.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>
#include <string>

#include "WWMFVideoFormat.h"

class WWMFVideoFrameReader
{
public:
    ~WWMFVideoFrameReader(void) {
        ReadEnd();
    }

    // プログラム起動時に1度だけ呼ぶ。
    static HRESULT StaticInit(void);
    static void StaticTerm(void);

    HRESULT ReadStart(const wchar_t *path);

    /// @param posToSeek シークする位置。負のときシークしないで次のフレームを取得。
    /// @param ppImg_return new[]されたポインタが戻る。 delete[]で開放して下さい。
    /// @param pImgBytes_return 画像のバイト数が戻る。4 * vf.w * vf.h
    /// @param vf_return ビデオフォーマットが戻る。
    HRESULT ReadImage(int64_t posToSeek, uint8_t **ppImg_return,
        int *imgBytes_return, WWMFVideoFormat *vf_return);

    void ReadEnd(void);

private:
    static bool mStaticInit;
    IMFSourceReader *mReader = nullptr;
    WWMFVideoFormat mVideoFmt;

    HRESULT GetVideoFormat(IMFSample *pSample, WWMFVideoFormat *pFormat);
    HRESULT CanSeek(BOOL *bSeek_return, BOOL *bSlowSeek_return);
    HRESULT GetDuration(int64_t *duration_return);
    HRESULT Seek(int64_t pos);
};
