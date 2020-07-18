// 日本語。

#include "TestDirectConvolutionGpu.h"
#include "WWDirectConvolutionGpu.h"
#include <stdio.h>
#include "framework.h"

int
TestDirectConvolutionGpu(void)
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
