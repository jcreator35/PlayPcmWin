// 日本語。

#pragma once

#include "WWUsbCommon.h"

void
WWPrintConfDesc(int level,
    PUSB_CONFIGURATION_DESCRIPTOR cd, std::vector<WWStringDesc> &sds);

void
WWPrintBosDesc(int level, PUSB_BOS_DESCRIPTOR pbd, std::vector<WWStringDesc> &sds);

