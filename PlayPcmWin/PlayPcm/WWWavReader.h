#pragma once

#include "WWPcmData.h"

WWPcmData * WWReadWavFile(const char *path, WWPcmDataStreamAllocType t = WWPDSA_Normal);
