// 日本語 UTF-8

#pragma once

#include <stdint.h>

#pragma pack(push, 8)
struct WWPlayStatus {
    int64_t posFrame;
    int64_t totalFrameNum;
};
#pragma pack(pop)
