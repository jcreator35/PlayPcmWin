#include "TestSincConvolution.h"
#include <stdio.h>
#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"

#define GROUP_THREAD_COUNT 1024

int
TestSincConvolution(void)
{
    HRESULT hr = S_OK;
    WWDirectCompute12User dc;
    WWConstantBuffer cBuf;
    WWShader cShader;
    WWSrvUavHeap suHeap;
    WWSrv srvInputData;

    const bool highPrecision = true;
    const int convolutionN = 1024;
    const int sampleTotalFrom = 65536;
    const int sampleRateFrom = 44100;
    const int sampleRateTo = 88200;

    const int sampleTotalTo = (int)((int64_t)sampleTotalFrom * sampleRateTo / sampleRateFrom);

    const float inputData[4] = { 1,2,3,4 };



    HRG(dc.Init(false));

    {
        // HLSLの中の#defineの値を決めます。
        char      convStartStr[32];
        sprintf_s(convStartStr, "%d", -convolutionN);
        char      convEndStr[32];
        sprintf_s(convEndStr, "%d", convolutionN);
        char      convCountStr[32];
        sprintf_s(convCountStr, "%d", convolutionN * 2);
        char      sampleTotalFromStr[32];
        sprintf_s(sampleTotalFromStr, "%d", sampleTotalFrom);
        char      sampleTotalToStr[32];
        sprintf_s(sampleTotalToStr, "%d", sampleTotalTo);

        char      sampleRateFromStr[32];
        sprintf_s(sampleRateFromStr, "%d", sampleRateFrom);
        char      sampleRateToStr[32];
        sprintf_s(sampleRateToStr, "%d", sampleRateTo);
        char      iterateNStr[32];
        sprintf_s(iterateNStr, "%d", convolutionN * 2 / GROUP_THREAD_COUNT);
        char      groupThreadCountStr[32];
        sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

        char      highPrecisionStr[32];
        sprintf_s(highPrecisionStr, "%d", 0 != highPrecision);

        const D3D_SHADER_MACRO defines[] = {
                "CONV_START", convStartStr,
                "CONV_END", convEndStr,
                "CONV_COUNT", convCountStr,
                "SAMPLE_TOTAL_FROM", sampleTotalFromStr,
                "SAMPLE_TOTAL_TO", sampleTotalToStr,

                "SAMPLE_RATE_FROM", sampleRateFromStr,
                "SAMPLE_RATE_TO", sampleRateToStr,
                "ITERATE_N", iterateNStr,
                "GROUP_THREAD_COUNT", groupThreadCountStr,
                "HIGH_PRECISION", highPrecisionStr,

                nullptr, nullptr
        };
        HRG(dc.CreateShader(L"SincConvolution3.hlsl", "CSMain", "cs_5_0", defines, cShader));
    }

    HRG(dc.CreateConstantBuffer(16, cBuf));

    //HRG(dc.CreateSrvUavHeap(1, suHeap));

    //HRG(dc.CreateShaderResourceView(suHeap, 0, sizeof(inputData[0]), ARRAYSIZE(inputData), inputData, srvInputData));

end:
    dc.Term();
    return hr;
}