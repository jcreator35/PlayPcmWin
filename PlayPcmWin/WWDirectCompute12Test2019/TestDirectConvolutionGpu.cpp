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
    const int CONV_COUNT = 10;

    float inputAry[INPUT_COUNT];
    double convCoeffsAry[CONV_COUNT];

    float outAry[INPUT_COUNT];

    HRG(dc.Setup(inputAry, INPUT_COUNT, convCoeffsAry, CONV_COUNT));

    HRG(dc.Dispatch(0, INPUT_COUNT));

    HRG(dc.ResultGetFromGpuMemory(outAry, INPUT_COUNT));

    for (int i = 0; i < INPUT_COUNT; ++i) {
        printf("%f\n", outAry[i]);
    }

end:
    return hr;
}
