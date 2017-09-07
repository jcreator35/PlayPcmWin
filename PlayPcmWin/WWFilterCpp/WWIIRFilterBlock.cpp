// 日本語

#include "WWIIRFilterBlock.h"

WWIIRFilterBlock::WWIIRFilterBlock(void)
{
}

WWIIRFilterBlock::~WWIIRFilterBlock(void)
{
}

void
WWIIRFilterBlock::Initialize(int aCount, const double *a, int bCount, const double *b)
{
    if (aCount < 1 || 3 < aCount) {
        throw new std::invalid_argument("aCount");
    }
    if (bCount < 1 || 3 < bCount) {
        throw new std::invalid_argument("bCount");
    }

    memset(mA, 0, sizeof mA);
    memset(mB, 0, sizeof mB);
    memset(mV, 0, sizeof mV);

    for (int i = 0; i <= bCount; ++i) {
        mB[i] = b[i];
    }

    for (int i = 0; i < aCount; ++i) {
        mA[i] = a[i];
    }
}

void
WWIIRFilterBlock::Finalize(void)
{
}

