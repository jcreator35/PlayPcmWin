// 日本語。

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>
#include <stdint.h>

#include "WWMFReaderMetadata.h"

// ヘッダー部にPCMデータサイズが書かれていないとき、PCMデータをすべて読んでmeta_return->numFramesを確定する。
#define WWMFREADER_FLAG_RESOLVE_NUM_FRAMES (1)

int
WWMFReaderReadHeader(
        const wchar_t *wszSourceFile, int flags,
        WWMFReaderMetadata *meta_return);

int
WWMFReaderGetCoverart(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout);


