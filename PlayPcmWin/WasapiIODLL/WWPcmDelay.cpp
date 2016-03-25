// 日本語 UTF-8

#include "WWPcmDelay.h"

void
WWPcmDelay::Init(int n)
{
    assert(!mDelay);

    mStoreSamples = n + 1;
    mPos = 0;

    mDelay = new float[mStoreSamples];
    FillZeroes();
}

void
WWPcmDelay::Term(void)
{
    delete[] mDelay;
    mDelay = nullptr;

    mStoreSamples = 0;
    mPos = 0;
}

void
WWPcmDelay::FillZeroes(void)
{
    for (int i = 0; i < mStoreSamples; ++i) {
        mDelay[i] = 0.0;
    }
    mPos = 0;
}
