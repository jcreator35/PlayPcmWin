// 日本語。

#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

class WWPerformanceCounter {
public:
    void Start(void);
    void Stop(void);

    void Restart(void);

    float ElapsedSec(void);
private:
    LARGE_INTEGER mFreq = {};
    LARGE_INTEGER mStart = {};
};

