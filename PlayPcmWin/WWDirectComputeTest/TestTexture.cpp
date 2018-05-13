#include "TestWWUpsample.h"

#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWUpsampleCpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>


void
TestTexture(void)
{
    HRESULT hr = S_OK;
    float *textureIn = nullptr;
    float *output = nullptr;
    const int dataCount = 256;
    ID3D11ShaderResourceView *pSRVTex1D = nullptr;
    ID3D11UnorderedAccessView *pUAVOutput = nullptr;
    ID3D11ComputeShader *pCS  = nullptr;
    WWDirectComputeUser c;

    char dataCountStr[256];
    sprintf_s(dataCountStr, "%d", dataCount);

    c.Init();

    textureIn = new float[dataCount];
    assert(textureIn);
    for (int i=0; i<dataCount; ++i) {
        textureIn[i] = (float)i;
    }

    output = new float[dataCount];
    assert(output);

    // HLSL ComputeShaderをコンパイル。
    const D3D_SHADER_MACRO defines[] = {
        "GROUP_THREAD_COUNT", dataCountStr,
        nullptr, nullptr
    };
    HRG(c.CreateComputeShader(L"TextureFetchTest.hlsl", "CSMain", defines, &pCS));
    assert(pCS);
    
    HRG(c.CreateTexture1DAndShaderResourceView(dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE,
        D3D11_BIND_SHADER_RESOURCE, 0, 0, textureIn, dataCount, "Tex1D_in", &pSRVTex1D));
    assert(pSRVTex1D);

    HRG(c.CreateBufferAndUnorderedAccessView(sizeof(float), dataCount, nullptr, "OutputBuffer", &pUAVOutput));
    assert(pUAVOutput);

    ID3D11ShaderResourceView* aRViews[] = { pSRVTex1D };
    HRG(c.Run(pCS, sizeof aRViews/sizeof aRViews[0], aRViews, 1, &pUAVOutput, nullptr, nullptr, 0, dataCount, 1, 1));

    // 計算結果をCPUメモリーに持ってくる。
    HRG(c.RecvResultToCpuMemory(pUAVOutput, output, dataCount * sizeof(float)));

end:
    delete [] output;
    output = nullptr;

    delete [] textureIn;
    textureIn = nullptr;

    c.Term();
}