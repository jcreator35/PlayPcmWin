#include "TestResampleGpu.h"
#include <stdio.h>
#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include "WWResampleGpu.h"


int
TestResampleGpu(void)
{
    bool result = true;
    HRESULT hr = S_OK;
    WWResampleGpu rg;
    DWORD t0, t1, t2;

    rg.Init();

    const bool highPrecision = true;

    // データ準備
    int convolutionN = 4096;
    int sampleTotalFrom = 16 * 256 * 256;
    int sampleRateFrom = 44100;
    int sampleRateTo = 441000;

    int GPU_WORK_COUNT = 4096;

    int sampleTotalTo = (int64_t)sampleTotalFrom * sampleRateTo / sampleRateFrom;

    float* sampleData = new float[sampleTotalFrom];
    assert(sampleData);

    float* outputCpu = new float[sampleTotalTo];
    assert(outputCpu);

    float* outputGpu = new float[sampleTotalTo];
    assert(outputGpu);

    // 真ん中あたりのサンプルだけ1で、残りは0
    for (int i = 0; i < sampleTotalFrom; ++i) {
        sampleData[i] = 0;
    }
    sampleData[127] = 1.0f;

    HRG(rg.Setup(convolutionN, sampleData, sampleTotalFrom, sampleRateFrom,
        sampleRateTo, sampleTotalTo, highPrecision));
    t0 = GetTickCount();
    for (int i = 0; i<sampleTotalTo; i+= GPU_WORK_COUNT) {
        // 出力サンプル数countの調整。
        int count = GPU_WORK_COUNT;
        if (sampleTotalTo < i + count) {
            count = sampleTotalTo - i;
        }

        HRGR(rg.Dispatch(i, count));
    }
    t1 = GetTickCount() + 1;
    HRG(rg.ResultGetFromGpuMemory(outputGpu, sampleTotalTo));

    t2 = GetTickCount();

    if (sampleTotalFrom == sampleTotalTo) {
        // 一致しているか。検証。
        for (int i = 0; i < sampleTotalFrom; ++i) {
            float diff = abs(sampleData[i] - outputGpu[i]);
            if (0.000001f < diff) {
                printf("Err sample #%d from=%f to=%f\n", i, sampleData[i], outputGpu[i]);
            }
        }
    }

    {
        float scaleG = WWResampleGpu::LimitSampleData(outputGpu, sampleTotalTo);

        printf("GPU=%dms(%fsamples/s) sample scaling=%f x\n",
            (t1 - t0), sampleTotalTo / ((t1 - t0) / 1000.0), scaleG);
    }

end:

    return hr;
}