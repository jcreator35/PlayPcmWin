#include "TestWWUpsample.h"
#include "WWUpsampleGpu.h"
#include "WWUpsampleCpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

static void
Test2(void)
{
    bool result = true;
    HRESULT hr = S_OK;
    WWUpsampleGpu us;

    us.Init();

    // データ準備
    int convolutionN    = 256*256;
    int sampleTotalFrom = 256*256;
    int sampleRateFrom  = 44100;
    int sampleRateTo    = 441000;

    int sampleTotalTo   = (int64_t)sampleTotalFrom * sampleRateTo / sampleRateFrom;

    float *sampleData = new float[sampleTotalFrom];
    assert(sampleData);

    float *outputCpu = new float[sampleTotalTo];
    assert(outputCpu);

    float *outputGpu = new float[sampleTotalTo];
    assert(outputGpu);

    /*
    // 全部1
    for (int i=0; i<sampleTotalFrom; ++i) {
        sampleData[i] = 1.0f;
    }
    */

    /*
    // 44100Hzサンプリングで1000Hzのsin
    for (int i=0; i<sampleTotalFrom; ++i) {
        float xS = PI_F * i * 1000 / 44100;
        sampleData[i] = sinf(xS);
    }
    */

    /*
    // 最初のサンプルだけ1で、残りは0
    for (int i=0; i<sampleTotalFrom; ++i) {
        sampleData[i] = 0;
    }
    sampleData[0] = 1.0f;
    */

    // 真ん中あたりのサンプルだけ1で、残りは0
    for (int i=0; i<sampleTotalFrom; ++i) {
        sampleData[i] = 0;
    }
    sampleData[127] = 1.0f;

    HRG(us.Setup(convolutionN, sampleData, sampleTotalFrom, sampleRateFrom, sampleRateTo, sampleTotalTo));
    DWORD t0 = GetTickCount();
    for (int i=0; i<1; ++i ) { // sampleTotalTo; ++i) {
        HRGR(us.Dispatch(0, sampleTotalTo));
    }
    DWORD t1 = GetTickCount()+1;
    HRG(us.ResultGetFromGpuMemory(outputGpu, sampleTotalTo));

    DWORD t2 = GetTickCount();

    HRG(WWUpsampleCpu(convolutionN, sampleData, sampleTotalFrom, sampleRateFrom, sampleRateTo, outputCpu, sampleTotalTo));

    DWORD t3 = GetTickCount()+1;

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("%7d outGpu=%f outCpu=%f diff=%12.8f\n",
            i, outputGpu[i], outputCpu[i],
            fabsf(outputGpu[i]-outputCpu[i]));
    }
    */

    /*
        1 (秒)       x(サンプル/秒)
        ───── ＝ ────────
        14 (秒)       256(サンプル)

            x = 256 ÷ 14
        */
    float scaleG = WWUpsampleGpu::LimitSampleData(outputGpu, sampleTotalTo);
    float scaleC = WWUpsampleGpu::LimitSampleData(outputCpu, sampleTotalTo);

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("%d, %12.8f\n", i, outputGpu[i]);
    }
    */

    printf("GPU=%dms(%fsamples/s)s=%f CPU=%dms(%fsamples/s)s=%f\n",
        (t1-t0),  sampleTotalTo / ((t1-t0)/1000.0), scaleG,
        (t3-t2),  sampleTotalTo / ((t3-t2)/1000.0), scaleC);

end:
    us.Unsetup();
    us.Term();

    delete[] outputGpu;
    outputGpu = nullptr;

    delete[] outputCpu;
    outputCpu = nullptr;

    delete[] sampleData;
    sampleData = nullptr;
}