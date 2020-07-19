// 日本語。

#include "TestCopyShader.h"
#include <stdio.h>
#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"

#define GROUP_THREAD_COUNT 1024

/// @brief シェーダー定数バッファの構造。
/// バイト数が256の倍数である必要がある？ D3D12ExecuteIndirect.h:61参照。
struct ShaderConsts {
    uint32_t c_count;

    /// pad
    uint32_t c_reserved[63];
};

int
TestCopyShader(void)
{
    enum suHeapIdx {
        SHI_SRV_IN,
        SHI_UAV_OUT,
        SHI_NUM
    };

    HRESULT hr = S_OK;
    WWDirectCompute12User dc;
    WWConstantBuffer cBuf;
    WWShader copyShader;
    WWSrvUavHeap suHeap;
    WWGpuBuf gpuBufAry[SHI_NUM];
    WWSrv srvInData;
    WWUav uavOutData;
    WWComputeState cState;

    ShaderConsts shaderConsts = {};

    const int NUM_CONSTANT = 1;
    const int NUM_SRV = 1;
    const int NUM_UAV = 1;
    const float inputData[1024] = { 1,2,3,4 };
    float outputData[1024] = {};

    // 準備作業。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    HRG(dc.Init(0));

    {   // コンピュートシェーダーをコンパイルする。
        char      groupThreadCountStr[32];
        sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

        const D3D_SHADER_MACRO defines[] = {
            "GROUP_THREAD_COUNT", groupThreadCountStr,
            nullptr, nullptr
        };

        HRG(dc.CreateShader(L"Copy.hlsl", "CSMain", "cs_5_0", defines, copyShader));
    }

    HRG(dc.CreateConstantBuffer(sizeof cBuf, cBuf));

    // 入力SRV1個、出力UAV1個。
    HRG(dc.CreateSrvUavHeap(NUM_SRV + NUM_UAV, suHeap));
    HRG(dc.CreateGpuBufferAndRegisterAsSRV(suHeap, sizeof(inputData[0]), ARRAYSIZE(inputData), inputData, gpuBufAry[SHI_SRV_IN], srvInData));
    HRG(dc.CreateGpuBufferAndRegisterAsUAV(suHeap, sizeof(outputData[0]), ARRAYSIZE(outputData), gpuBufAry[SHI_UAV_OUT], uavOutData));
    HRG(dc.CreateComputeState(copyShader, NUM_CONSTANT, NUM_SRV, NUM_UAV, cState));

    // 1回目実行。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    // 定数バッファに定数を送る。
    shaderConsts.c_count = 2;
    HRG(dc.UpdateConstantBufferData(cBuf, &shaderConsts));

    // GPU上でシェーダーを実行。
    HRG(dc.Run(cState, &cBuf, suHeap, SHI_SRV_IN, GROUP_THREAD_COUNT, 1, 1));

    // GPUメモリ上の計算結果をCPUメモリに持ってくる。
    HRG(dc.CopyGpuBufValuesToCpuMemory(gpuBufAry[SHI_UAV_OUT], outputData, sizeof outputData));

    // 計算結果を表示。
    for (int i = 0; i < 4; ++i) {
        printf("%f ", outputData[i]);
    }
    printf("\n");

    // 2回目実行。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    // 定数バッファに定数を送る。
    shaderConsts.c_count = 4;
    HRG(dc.UpdateConstantBufferData(cBuf, &shaderConsts));

    // GPU上でシェーダーを実行。
    HRG(dc.Run(cState, &cBuf, suHeap, SHI_SRV_IN, GROUP_THREAD_COUNT, 1, 1));

    // GPUメモリ上の計算結果をCPUメモリに持ってくる。
    HRG(dc.CopyGpuBufValuesToCpuMemory(gpuBufAry[SHI_UAV_OUT], outputData, sizeof outputData));

    // 計算結果を表示。
    for (int i = 0; i < 4; ++i) {
        printf("%f ", outputData[i]);
    }

end:
    dc.Term();
    return hr;
}
