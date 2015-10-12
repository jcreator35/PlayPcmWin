#pragma once

#include "WWPcmData.h"

WWPcmData * WWReadDsdiffFile(const char *path, WWBitsPerSampleType bitsPerSampleType, WWPcmDataStreamAllocType t = WWPDSA_Normal);
