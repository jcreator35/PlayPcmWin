// 日本語。

#include "TestDirectConvolutionGpu.h"
#include "WWDirectConvolutionGpu.h"
#include <stdio.h>
#include "framework.h"
#include "WWPerformanceCounter.h"

static int
Test1(void)
{
    HRESULT hr = S_OK;
    WWDirectConvolutionGpu dc;

    const int INPUT_COUNT = 10;
    const int CONV_COUNT  = 5; 

    float inputAry[INPUT_COUNT]; // {1,1,1,1,1, 1,1,1,1,1}
    for (int i = 0; i < INPUT_COUNT; ++i) {
        inputAry[i] = 1.0f;
    }

    double convCoeffsAry[CONV_COUNT]; // {1,2,3,4,5}
    for (int i = 0; i < CONV_COUNT; ++i) {
        convCoeffsAry[i] = i + 1;
    }

    float outAry[INPUT_COUNT]; // Conv result, should be {6,10,15,15,15, 15,15,15,14,12}

    HRG(dc.Setup(inputAry, INPUT_COUNT, convCoeffsAry, CONV_COUNT));

#if 1
    {   // Calc all
        ZeroMemory(outAry, sizeof outAry);

        HRG(dc.Dispatch(0, INPUT_COUNT));
        HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

        for (int i = 0; i < INPUT_COUNT; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
}
#endif

#if 0
    {   // Calc the first half
        ZeroMemory(outAry, sizeof outAry);

        HRG(dc.Dispatch(0, INPUT_COUNT / 2));
        HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

        for (int i = 0; i < INPUT_COUNT; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
    }
#endif

#if 0
    {   // Calc the latter half
        ZeroMemory(outAry, sizeof outAry);

        HRG(dc.Dispatch(INPUT_COUNT / 2, INPUT_COUNT));
        HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

        for (int i = 0; i < INPUT_COUNT; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
    }
#endif

end:
    return hr;
}

int
Benchmark(void)
{
    HRESULT hr = S_OK;
    WWDirectConvolutionGpu dc;
    WWPerformanceCounter pc;

    const int INPUT_COUNT = 16 * 65536;
    const int CONV_COUNT = 65535;

    float * inputAry = new float[INPUT_COUNT];
    for (int i = 0; i < INPUT_COUNT; ++i) {
        inputAry[i] = 1.0f;
    }

    double * convCoeffsAry = new double[CONV_COUNT];
    for (int i = 0; i < CONV_COUNT; ++i) {
        convCoeffsAry[i] = 1;
    }

    float * outAry = new float[INPUT_COUNT];

    HRG(dc.Setup(inputAry, INPUT_COUNT, convCoeffsAry, CONV_COUNT));

    {   // Calc all
        ZeroMemory(outAry, sizeof outAry);

        pc.Start();

#if 0
        // 256個ずつ計算(シェーダーが長時間かかりすぎてGPUが異常停止する場合用。)
        for (int i=0; i<INPUT_COUNT;) {
            HRG(dc.Dispatch(i, (i+256)));
            i += 256;
        }
#else
        // 1回で計算(速い場合可能。)
        HRG(dc.Dispatch(0, INPUT_COUNT));
#endif
        HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

        printf("%f seconds\n", pc.ElapsedSec());

        for (int i = 0; i < 10; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
        printf("...\n");
        for (int i = INPUT_COUNT-10; i < INPUT_COUNT; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
    }

end:
    delete[] outAry;
    outAry = nullptr;

    delete[] convCoeffsAry;
    convCoeffsAry = nullptr;

    delete[] inputAry;
    inputAry = nullptr;

    return hr;
}
int
TestDirectConvolutionGpu(void)
{
    int hr = S_OK;

    // hr = Test1();
    hr = Benchmark();

    return hr;
}
