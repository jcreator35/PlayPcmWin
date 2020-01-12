// 日本語。

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>
#include <stdint.h>

#include "WWMFReaderMetadata.h"

int
WWMFReaderReadHeader(
        const wchar_t *wszSourceFile,
        WWMFReaderMetadata *meta_return);

int
WWMFReaderGetCoverart(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout);


