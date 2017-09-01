#include "stdafx.h"
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
    mOutPcm = new float[totalOutSamples + FilterDelay()];
}

void
WWSdmToPcm::Drain(void)
{
    // 出力データをさらに遅延サンプル分出す。
    const int flushSampleIn = FilterDelay() * 64 / 16;
    for (int i=0; i<flushSampleIn; ++i) {
        AddInputSamples(0x6969);
    }
}

const float *
WWSdmToPcm::GetOutputPcm(void) const
{
    assert(mOutPcm);
    return & mOutPcm[FilterDelay()];
}

void
WWSdmToPcm::End(void)
{
    delete [] mOutPcm;
    mOutPcm = nullptr;

    mHBDS47.End();
    mHBDS23.End();
}

