// 日本語

#include "stdafx.h"
#include "WWPcmToSdm.h"

const float WWPcmToSdm::mA[4] = { 0.0057254522986342048f, 0.051607454170822936f,
                                 0.24845882976302058f, 0.556014463585628f };
const float WWPcmToSdm::mB[5] = { 0.0057254522986342048f, 0.051607454170822936f,
                                 0.24845882976302058f, 0.556014463585628f, 1 };
const float WWPcmToSdm::mG[2] = { 0.001786565462119416f, 0.00027850892877778755f };


WWPcmToSdm::~WWPcmToSdm(void)
{
    assert(mOutSdm == nullptr);
}

void
WWPcmToSdm::Start(int totalOutSamples)
{
    mOutCount = 0;

    mCic.Clear();
    mHB23.Start();
    mHB47.Start();
    mLoopFilter.Reset();
    mTotalOutSamples16 = totalOutSamples/16;
    assert(mOutSdm == nullptr);
    mOutSdm = new uint16_t[totalOutSamples + FilterDelay16()];
}

#if 0
static float gOutPcmDebug[2000*64];
static int gOutPcmDebugCount = 0;
#endif

void
WWPcmToSdm::AddInputSamples(const float *inPcm, int inPcmCount)
{
    assert(inPcmCount <= IN_PCM_CAPACITY);
    float tmp1Pcm[IN_PCM_CAPACITY*2];
    float tmp2Pcm[IN_PCM_CAPACITY*4];
    mHB47.Filter(inPcm, inPcmCount, tmp1Pcm);
    mHB23.Filter(tmp1Pcm, inPcmCount*2, tmp2Pcm);
    for (int i=0; i<inPcmCount*4; ++i) {
        float tmp3Pcm[16];
        mCic.Filter(tmp2Pcm[i], tmp3Pcm);

#if 0
        for (int i=0; i<16; ++i) {
            gOutPcmDebug[gOutPcmDebugCount++] = tmp3Pcm[i];
        }
#endif

        uint8_t outSdm[2];
        mLoopFilter.Filter(16, tmp3Pcm, outSdm);
        mOutSdm[mOutCount++] = (outSdm[0]<<8) + outSdm[1];
        assert(mOutCount <= mTotalOutSamples16 + FilterDelay16());
    }
}

void
WWPcmToSdm::Drain(void)
{
    // 出力データをさらに遅延サンプル分出す。
    const int flush = FilterDelay16() * 16/64;
    for (int i=0; i<flush; ++i) {
        float v = 0;
        AddInputSamples(&v, 1);
    }
}

const uint16_t *
WWPcmToSdm::GetOutputSdm(void) const
{
    assert(mOutSdm);
    return & mOutSdm[FilterDelay16()];
}

void
WWPcmToSdm::End(void)
{
    delete [] mOutSdm;
    mOutSdm = nullptr;

    mHB47.End();
    mHB23.End();
}

