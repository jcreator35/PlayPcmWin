// 日本語。

#pragma once

#include <stdint.h>
#include "WWRational.h"
#include "WWImageWH.h"

#define WW_MF_VIDEO_IMAGE_FMT_TopDown 1
#define WW_MF_VIDEO_IMAGE_FMT_CAN_SEEK 2
#define WW_MF_VIDEO_IMAGE_FMT_SLOW_SEEK 4
#define WW_MF_VIDEO_IMAGE_FMT_LIMITED_RANGE_16_to_235 8

struct WWMFVideoFormat {
    WWImageWH pixelWH;
    WWImageWH aspectStretchedWH;
    WWImageWH apertureWH; //< Geometric Aperture
    WWRational32 aspectRatio;
    WWRational32 frameRate;
    int64_t duration;
    int64_t timeStamp;
    uint32_t flags; //< WW_MF_VIDEO_IMAGE_FMT_???
};

void WWMFVideoFormatPrint(WWMFVideoFormat &vf);
