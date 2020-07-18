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
    const int CONV_COUNT = 5; // {1, 2, 3, 4, 5}

    float inputAry[INPUT_COUNT];
    double convCoeffsAry[CONV_COUNT];
    float outAry[INPUT_COUNT];

    for (int i = 0; i < INPUT_COUNT; ++i) {
        inputAry[i] = 1.0f;
    }
    for (int i = 0; i < CONV_COUNT; ++i) {
        convCoeffsAry[i] = i + 1;
    }

    HRG(dc.Setup(inputAry, INPUT_COUNT, convCoeffsAry, CONV_COUNT));

#if 0
    {
        ZeroMemory(outAry, sizeof outAry);

        HRG(dc.Dispatch(0, INPUT_COUNT / 2));
        HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

        for (int i = 0; i < INPUT_COUNT; ++i) {
            printf("%d : %f\n", i, outAry[i]);
        }
    }
#endif

#if 1
    {
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
