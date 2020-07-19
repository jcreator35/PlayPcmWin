// 日本語。

#include "TestSandboxShader.h"
#include <stdio.h>
#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"

int
TestSandboxShader(void)
{
    enum suHeapIdx {
        SHI_UAV_OUT,
        SHI_NUM
    };

    HRESULT hr = S_OK;
    WWDirectCompute12User dc;
    WWShader shader;
    WWSrvUavHeap suHeap;
    WWGpuBuf gpuBufAry[SHI_NUM];
    WWUav uavOutData;
    WWComputeState cState;

    const int NUM_CONSTANT = 0;
    const int NUM_SRV = 0;
    const int NUM_UAV = 1;
    float outputData[1024] = {}; //< 零初期化。

    // 準備作業。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    HRG(dc.Init(0));

    // コンピュートシェーダーをコンパイルする。
    HRG(dc.CreateShader(L"Sandbox.hlsl", "CSMain", "cs_5_0", nullptr, shader));

    // 入力SRV0個、出力UAV1個。
    HRG(dc.CreateSrvUavHeap(NUM_SRV + NUM_UAV, suHeap));
    HRG(dc.CreateGpuBufferAndRegisterAsUAV(suHeap, sizeof(outputData[0]), ARRAYSIZE(outputData), gpuBufAry[SHI_UAV_OUT], uavOutData));
    HRG(dc.CreateComputeState(shader, NUM_CONSTANT, NUM_SRV, NUM_UAV, cState));

    // GPU上でシェーダーを実行。
    HRG(dc.Run(cState, nullptr, suHeap, 0, 4, 1, 1));

    // GPUメモリ上の計算結果をCPUメモリに持ってくる。
    HRG(dc.CopyGpuBufValuesToCpuMemory(gpuBufAry[SHI_UAV_OUT], outputData, sizeof outputData));

    // 計算結果を表示。
    for (int i = 0; i < 25; ++i) {
        printf("%f\n", outputData[i]);
    }
    printf("\n");

end:
    dc.Term();
    return hr;
}
