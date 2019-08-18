// 日本語

#include "WWIIRFilterGraph.h"

WWIIRFilterGraph::WWIIRFilterGraph(int nBlocks)
{
    assert(nBlocks <= WW_IIR_FILTER_BLOCK_NUM);

    mCapacity = nBlocks;
    mCount = 0;
    mOsr = 1;
    mDecimation = 1;
}

WWIIRFilterGraph::~WWIIRFilterGraph(void)
{
    for (int i=mCount-1; 0<=i; --i) {
        mFilterBlockArray[i].Finalize();
    }
}

/// <summary>
/// 多項式pは直列接続される。(p同士を掛けていく感じになる)
/// </summary>
void
WWIIRFilterGraph::Add(int aCount, const double *a, int bCount, const double *b)
{
    assert(mCount < mCapacity);
    mFilterBlockArray[mCount++].Initialize(aCount, a, bCount, b);
}
