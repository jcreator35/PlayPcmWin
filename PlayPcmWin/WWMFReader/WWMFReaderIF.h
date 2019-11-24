// 日本語。

#pragma once

#ifdef WWMFREADER_EXPORTS
#define WWMFREADER_API __declspec(dllexport)
#else
#define WWMFREADER_API __declspec(dllimport)
#endif

#include "WWMFReader.h"

extern "C" {

    WWMFREADER_API int __stdcall
    WWMFReaderIFReadHeader(
            const wchar_t *wszSourceFile,
            WWMFReaderMetadata *meta_return);

    WWMFREADER_API int __stdcall
    WWMFReaderIFGetCoverart(
            const wchar_t *wszSourceFile,
            unsigned char *data_return,
            int64_t *dataBytes_inout);

    /// 少しづつ読み出す。
    /// @return instanceIdが戻る。
    WWMFREADER_API int __stdcall
    WWMFReaderIFReadDataStart(
        const wchar_t *wszSourceFile);

    /// 少しづつ読み出す。
    /// @return instanceIdが戻る。
    WWMFREADER_API int __stdcall
    WWMFReaderIFReadDataFragment(
        int instanceId,
        unsigned char *data_return,
        int64_t *dataBytes_inout);

    /// 少しづつ読み出す。
    /// @return instanceIdが戻る。
    WWMFREADER_API int __stdcall
    WWMFReaderIFReadDataEnd(
        int instanceId);

}; // extern "C"

