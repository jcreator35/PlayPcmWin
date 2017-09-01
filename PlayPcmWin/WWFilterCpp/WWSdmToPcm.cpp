﻿#include "stdafx.h"
#include "WWSdmToPcm.h"
#include "WWDelay.h"
#include <stdint.h>
#include <assert.h>

WWSdmToPcm::~WWSdmToPcm(void)
{
    assert(mOutPcm == nullptr);
}

void
WWSdmToPcm::Start(int totalOutSamples)
{
    mTmp1Count = 0;
    mTmp2Count = 0;
    mOutCount = 0;

    mCicDS.Clear();
    mHBDS23.Start();
    mHBDS47.Start();
    mTotalOutSamples = totalOutSamples;
    assert(mOutPcm == nullptr);
    mOutPcm = new float[totalOutSamples + mDelaySamples];
}

void
WWSdmToPcm::Drain(void)
{
    // 出力データをさらに25サンプル出す: 25サンプル遅延して出てくるので。
    const int flushSampleIn = mDelaySamples * 64 / 16;
    for (int i=0; i<flushSampleIn; ++i) {
        AddInputSamples(0x6969);
    }
}

const float *
WWSdmToPcm::GetOutputPcm(void) const
{
    assert(mOutPcm);
    return & mOutPcm[mDelaySamples];
}

void
WWSdmToPcm::End(void)
{
    delete [] mOutPcm;
    mOutPcm = nullptr;
}

