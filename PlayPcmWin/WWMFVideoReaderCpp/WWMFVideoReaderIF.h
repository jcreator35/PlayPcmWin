﻿// 日本語。

#pragma once

#ifdef WWMFVIDEOREADER_EXPORTS
#define WWMFVIDEOREADER_API __declspec(dllexport)
#else
#define WWMFVIDEOREADER_API __declspec(dllimport)
#endif

#include "WWMFVideoFormat.h"

extern "C" {

    WWMFVIDEOREADER_API int __stdcall
        WWMFVReaderIFStaticInit(void);

    WWMFVIDEOREADER_API void __stdcall
        WWMFVReaderIFStaticTerm(void);

    /// インスタンスを作成し、1つのファイルを読む。
    /// @return instanceIdが戻る。
    /// @retval 負の値 読み出し時の失敗のHRESULT
    WWMFVIDEOREADER_API int __stdcall
        WWMFVReaderIFReadStart(
            const wchar_t *wszSourceFile);

    /// 作ったインスタンスを消す。
    /// @retval S_OK インスタンスが見つかって、削除成功。
    /// @retval E_INVALIDARG インスタンスがない。
    WWMFVIDEOREADER_API int __stdcall
        WWMFVReaderIFReadEnd(
            int instanceId);

    /// @param posToSeek シークする位置。負のときシークしないで次のフレームを取得。
    /// @param ppImg_return new[]されたポインタが戻る。 delete[]で開放して下さい。
    /// @param pImgBytes_return 画像のバイト数が戻る。4 * vf.w * vf.h
    /// @param vf_return ビデオフォーマットが戻る。
    WWMFVIDEOREADER_API int __stdcall
        WWMFVReaderIFReadImage(
            int instanceId, int64_t posToSeek, uint8_t **ppImg_return,
            int *imgBytes_return, WWMFVideoFormat *vf_return);

}; // extern "C"

