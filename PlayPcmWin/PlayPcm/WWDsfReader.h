#pragma once

#include "WWPcmData.h"

WWPcmData * WWReadDsfFile(const char *path, WWBitsPerSampleType bitsPerSampleType, WWPcmDataStreamAllocType t = WWPDSA_Normal);
