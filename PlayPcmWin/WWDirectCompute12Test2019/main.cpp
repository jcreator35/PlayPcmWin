// 日本語。

#include "TestCopyShader.h"
#include "TestResampleGpu.h"
#include "TestDirectConvolutionGpu.h"
#include "TestSandboxShader.h"
#include "framework.h"

int wmain(int argc, wchar_t *argv[])
{
    HRESULT hr = S_OK;

    int useGpuIdx = -1;
    if (1 < argc) {
        wchar_t* endPtr = nullptr;
        useGpuIdx = (int)wcstol(argv[1], &endPtr, 10);
        printf("GpuNr=%d\n", useGpuIdx);
    }

    //hr = TestCopyShader();

    //hr = TestResampleGpu(useGpuIdx);

    //hr = TestDirectConvolutionGpu();

    hr = TestSandboxShader(useGpuIdx);

    // 成功: 0
    // 失敗: 1
    return FAILED(hr);
}
