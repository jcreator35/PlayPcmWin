#include "TestResampleGpu.h"
#include <stdio.h>
#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include "WWResampleGpu.h"


int
TestResampleGpu(int gpuNr)
{
    bool result = true;
    HRESULT hr = S_OK;
    WWResampleGpu rg;
    DWORD t0, t1, t2;

    const bool highPrecision = true;

    // データ準備

    int sampleRateFrom = 44100;
    int sampleRateTo = 2 * 44100;

    /// 入力データ。
    int sampleTotalFrom = 16 * 65536;
    float* sampleData = new float[sampleTotalFrom];
    assert(sampleData);
    // 真ん中あたりのサンプルだけ1で、残りは0
    for (int i = 0; i < sampleTotalFrom; ++i) {
        sampleData[i] = 0;
    }
    sampleData[127] = 1.0f;

    // 出力データサンプル数はサンプルレートの比から計算。
    int sampleTotalTo = (int64_t)sampleTotalFrom * sampleRateTo / sampleRateFrom;
    float* outputSamples = new float[sampleTotalTo];
    assert(outputSamples);

    //< 32767: 14bitの変換精度。
    //< 65535: 15bitの変換精度。
    int convolutionN = 65535;

    /// @brief 何サンプルずつ出力するか。(convolutionNとは関係ない)
    int GPU_WORK_COUNT = 4096;

    HRG(rg.Setup(convolutionN, sampleData, sampleTotalFrom, sampleRateFrom,
        sampleRateTo, sampleTotalTo, highPrecision, gpuNr));
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
    HRG(rg.ResultGetFromGpuMemory(outputSamples, sampleTotalTo));

    t2 = GetTickCount();

    if (sampleTotalFrom == sampleTotalTo) {
        // 一致しているか。検証。
        for (int i = 0; i < sampleTotalFrom; ++i) {
            float diff = abs(sampleData[i] - outputSamples[i]);
            if (0.000001f < diff) {
                printf("Err sample #%d from=%f to=%f\n", i, sampleData[i], outputSamples[i]);
            }
        }
    }

    /* 
    // Excelプロット用　値出力。
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("%f\n", outputGpu[i]);
    }
    */

    {
        float scaleG = WWDCUtilLimitSampleData(outputSamples, sampleTotalTo);

        printf("GPU=%dms(%fsamples/s), sample scaling=%f x\n",
            (t1 - t0), sampleTotalTo / ((t1 - t0) / 1000.0), scaleG);
    }

end:

    delete[] outputSamples;
    outputSamples = nullptr;

    delete[] sampleData;
    sampleData = nullptr;

    rg.Term();

    return hr;
}