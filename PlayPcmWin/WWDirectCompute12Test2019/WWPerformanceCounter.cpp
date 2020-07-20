// 日本語。

#include "WWPerformanceCounter.h"

void
WWPerformanceCounter::Start(void)
{
    QueryPerformanceFrequency(&mFreq);
    QueryPerformanceCounter(&mStart);
}

void
WWPerformanceCounter::Restart(void)
{
    Start();
}

void
WWPerformanceCounter::Stop(void)
{
    // 特にすることはない。
}

float
WWPerformanceCounter::ElapsedSec(void)
{
    LARGE_INTEGER now, elapsed;
    QueryPerformanceCounter(&now);

    elapsed.QuadPart = now.QuadPart - mStart.QuadPart;
    return (float)elapsed.QuadPart / mFreq.QuadPart;
}

