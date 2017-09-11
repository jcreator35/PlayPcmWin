// 日本語

#include "WWPcmToSdm.h"

#if 0
// 3次。
const float WWPcmToSdm::mA[] = { 0.044083602820343515f, 0.24307907841927734f, 0.55590710005219635f };
const float WWPcmToSdm::mB[] = { 0.044083602820343515f, 0.24307907841927734f, 0.55590710005219635f, 1.0f };
const float WWPcmToSdm::mG[] = { 0.0014455686595564732f };
#endif

#if 1
// 4次。
const float WWPcmToSdm::mA[] = { 0.0057254522986342048f, 0.051607454170822936f, 0.24845882976302058f, 0.556014463585628f };
const float WWPcmToSdm::mB[] = { 0.0057254522986342048f, 0.051607454170822936f, 0.24845882976302058f, 0.556014463585628f, 1.0f };
const float WWPcmToSdm::mG[] = { 0.001786565462119416f, 0.00027850892877778755f };
#endif

#if 0
// 5次。
const float WWPcmToSdm::mA[] = { 0.0006604571797801384f, 0.0084244803698276111f, 0.054686031805914838f, 0.25018126352089132f, 0.55615015456296357f };
const float WWPcmToSdm::mB[] = { 0.0006604571797801384f, 0.0084244803698276111f, 0.054686031805914838f, 0.25018126352089132f, 0.55615015456296357f, 1.0f };
const float WWPcmToSdm::mG[] = { 0.001978322017535783f, 0.00069861261557968568f };
#endif

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
    mOutSdm = new uint16_t[mTotalOutSamples16 + FilterDelay16()];
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

#if 0
        double tmp4Pcm[16];
        for (int i=0; i<16; ++i) {
            tmp4Pcm[i] = tmp3Pcm[i];
        }
#else
        const float *tmp4Pcm = tmp3Pcm;
#endif

        uint8_t outSdm[2];
        mLoopFilter.Filter(16, tmp4Pcm, outSdm);
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



void
WWPcmToSdm::End(void)
{
    delete [] mOutSdm;
    mOutSdm = nullptr;

    mHB47.End();
    mHB23.End();
}

