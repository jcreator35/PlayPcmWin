// 日本語 UTF-8

#include "WWPcmDelay.h"

void
WWPcmDelay::Init(int n)
{
    assert(!mDelay);

    mDelayLength = n;
    mPos = 0;
    mDelay = new float[n];
    FillZeroes();
}

void
WWPcmDelay::Term(void)
{
    delete[] mDelay;
    mDelay = nullptr;
}

void
WWPcmDelay::FillZeroes(void)
{
    for (int i = 0; i < mDelayLength; ++i) {
        mDelay[i] = 0.0;
    }
    mPos = 0;
}
