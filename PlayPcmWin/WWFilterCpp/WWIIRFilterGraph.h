#pragma once

#include "WWIIRFilterBlock.h"
#include <assert.h>

class WWIIRFilterGraph
{
protected:
    WWIIRFilterBlock *mFilterBlockArray;
    int mCount;
    int mCapacity;
    int mOsr;

public:
    WWIIRFilterGraph(int nBlocks);

    virtual ~WWIIRFilterGraph(void);

    /// <summary>
    /// ‘½€®p‚Í’¼—ñÚ‘±‚³‚ê‚éB(p“¯m‚ğŠ|‚¯‚Ä‚¢‚­Š´‚¶‚É‚È‚é)
    /// </summary>
    virtual void Add(int aCount, const double *a, int bCount, const double *b);

    virtual void Filter(int n, const double *buffIn, double *buffOut) = 0;

    void SetOSR(int osr) { mOsr = osr; }
};
