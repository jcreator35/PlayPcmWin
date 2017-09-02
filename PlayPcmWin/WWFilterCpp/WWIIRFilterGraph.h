// 日本語

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
    int mDecimation;

public:
    WWIIRFilterGraph(int nBlocks);

    virtual ~WWIIRFilterGraph(void);

    /// <summary>
    /// 多項式pは直列接続される。(p同士を掛けていく感じになる)
    /// </summary>
    virtual void Add(int aCount, const double *a, int bCount, const double *b);

    virtual void Filter(int nIn, const double *buffIn, int nOut, double *buffOut) = 0;

    void SetOSR(int osr) { mOsr = osr; }
    void SetDecimation(int decimation) { mDecimation = decimation; }
};
