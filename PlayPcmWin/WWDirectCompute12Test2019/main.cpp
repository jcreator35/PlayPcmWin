// 日本語。

#include "TestCopyShader.h"
#include "TestResampleGpu.h"
#include "TestDirectConvolutionGpu.h"
#include "TestSandboxShader.h"
#include "framework.h"

int wmain(int argc, wchar_t *argv[])
{
    HRESULT hr = S_OK;

    int gpuNr = -1;
    if (1 < argc) {
        wchar_t* endPtr = nullptr;
        gpuNr = (int)wcstol(argv[1], &endPtr, 10);
        printf("GpuNr=%d\n", gpuNr);
    }

    //hr = TestCopyShader();

    hr = TestResampleGpu(gpuNr);

    //hr = TestDirectConvolutionGpu();

    //hr = TestSandboxShader();

    // 成功: 0
    // 失敗: 1
    return FAILED(hr);
}
