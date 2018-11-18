// 日本語。


#pragma once
#include <Windows.h>

HRESULT
WWUpsampleCpu(
        int convolutionN,
        float * sampleData,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        float * outputTo,
        int sampleTotalTo);

