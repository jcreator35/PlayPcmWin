// 日本語。

#include "WWDirectConvolutionGpu.h"
#include "WWDCUtil.h"

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。TGSMのサイズはハードウェアで決まっている。32KB ?
#define GROUP_THREAD_COUNT 1024

/// シェーダーに渡す定数。
struct ConstShaderParams {
    uint32_t cConvOffs;
    uint32_t cDispatchCount;
    uint32_t cSampleStartPos;

    uint32_t cReserved[61];
};

HRESULT
WWDirectConvolutionGpu::Setup(
    float* inputAry,
    int inputCount,
    double* convAry,
    int convCount)
{
    bool    result = true;
    HRESULT hr = S_OK;

    ConstShaderParams shaderParams;

    double* convAryFlip = nullptr;

    assert(convAry);
    assert(0 < convCount);
    assert(convCount & 1); //< convCountは奇数個。
    assert(inputAry);
    assert(0 < inputCount);

    mInputCount = inputCount;
    mConvCount = convCount;

    //　畳み込み係数バッファーを左右反転したものを作る。
    convAryFlip = new double[convCount];
    assert(convAryFlip);
    for (int i = 0; i < convCount; ++i) {
        convAryFlip[i] = convAry[convCount - 1 - i];
    }

    HRG(mDC.Init(0));

    {   // コンピュートシェーダーをコンパイルする。
        // HLSLの中の#defineの値を決めます。
        char      inputCountStr[32];
        sprintf_s(inputCountStr, "%d", inputCount);

        char      convStartStr[32];
        sprintf_s(convStartStr, "%d", -convCount /2);
        char      convEndStr[32];
        sprintf_s(convEndStr, "%d", convCount/2);
        char      convCountStr[32];
        sprintf_s(convCountStr, "%d", convCount);

        char      iterateNStr[32];
        sprintf_s(iterateNStr, "%d", convCount / GROUP_THREAD_COUNT);
        char      groupThreadCountStr[32];
        sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

        const D3D_SHADER_MACRO defines[] = {
                "INPUT_COUNT", inputCountStr,

                "CONV_HALF_LEN", convEndStr,
                "CONV_START", convStartStr,
                "CONV_END",   convEndStr,
                "CONV_COUNT", convCountStr,

                "ITERATE_N", iterateNStr,
                "GROUP_THREAD_COUNT", groupThreadCountStr,
                nullptr, nullptr
        };

        HRG(mDC.CreateShader(L"DirectConvolution.hlsl", "CSMain", "cs_5_0", defines, mCS));
    }

    HRG(mDC.CreateSrvUavHeap(GB_NUM, mSUHeap));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(float),  inputCount, inputAry,    mGpuBuf[GB_InputAry],      mSrv[GB_InputAry]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(double), convCount,  convAryFlip, mGpuBuf[GB_ConvCoeffsAry], mSrv[GB_ConvCoeffsAry]));
    HRG(mDC.CreateGpuBufferAndRegisterAsUAV(mSUHeap, sizeof(float),  inputCount, mGpuBuf[GB_OutAry], mUav));
    HRG(mDC.CreateComputeState(mCS, NUM_CONSTS, NUM_SRV, NUM_UAV, mCState));
    
    ZeroMemory(&shaderParams, sizeof shaderParams);
    HRG(mDC.CreateConstantBuffer(sizeof shaderParams, mCBuf));
    HRG(mDC.UpdateConstantBufferData(mCBuf, &shaderParams));

    // CPU上で準備したデータをGPUに送り出し、完了するまで待つ。
    HRG(mDC.CloseExecResetWait());

end:
    delete[] convAryFlip;
    convAryFlip = nullptr;

    return hr;
}

HRESULT
WWDirectConvolutionGpu::Dispatch(
    int startPos,
    int count)
{
    HRESULT hr = S_OK;
    bool result = true;

    ConstShaderParams shaderParams;
    ZeroMemory(&shaderParams, sizeof shaderParams);
    shaderParams.cConvOffs = 0;
    shaderParams.cDispatchCount = mConvCount / GROUP_THREAD_COUNT;
    shaderParams.cSampleStartPos = startPos;
    HRG(mDC.UpdateConstantBufferData(mCBuf, &shaderParams));

    HRGR(mDC.Run(mCState, &mCBuf, mSUHeap, 0, count, 1, 1));

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

HRESULT
WWDirectConvolutionGpu::ResultGetFromGpuMemory(
    float* outputTo,
    int outputToElemNum)
{
    HRESULT hr = S_OK;

    assert(outputTo);
    assert(outputToElemNum <= mInputCount);

    // 計算結果をGPUのUAVからCPUに持ってくる。
    HRG(mDC.CopyGpuBufValuesToCpuMemory(mGpuBuf[GB_OutAry], outputTo, outputToElemNum * sizeof(float)));

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

