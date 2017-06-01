#include "stdafx.h"
#include "WWIIRFilterGraph.h"

WWIIRFilterGraph::WWIIRFilterGraph(int nBlocks)
{
    mFilterBlockArray = new WWIIRFilterBlock[nBlocks];
    mCount = 0;
    mCapacity = nBlocks;
    mOsr = 1;
}

WWIIRFilterGraph::~WWIIRFilterGraph(void)
{
    for (int i=mCount-1; 0<=i; --i) {
        mFilterBlockArray[i].Finalize();
    }

    delete [] mFilterBlockArray;
    mFilterBlockArray = nullptr;
}

/// <summary>
/// ‘½€®p‚Í’¼—ñÚ‘±‚³‚ê‚éB(p“¯m‚ğŠ|‚¯‚Ä‚¢‚­Š´‚¶‚É‚È‚é)
/// </summary>
void
WWIIRFilterGraph::Add(int aCount, const double *a, int bCount, const double *b)
{
    assert(mCount < mCapacity);
    mFilterBlockArray[mCount++].Initialize(aCount, a, bCount, b);
}
