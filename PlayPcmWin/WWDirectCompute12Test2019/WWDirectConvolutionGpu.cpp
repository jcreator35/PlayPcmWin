// 日本語。

#include "WWDirectConvolutionGpu.h"
#include "WWDCUtil.h"

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。TGSMのサイズはハードウェアで決まっている。32KB ?
#define GROUP_THREAD_COUNT 1024

/// 最適化版シェーダー使用。
#define USE_OPTIMIZED_SHADER 1

#define TOSTRING_(x) #x
#define TOSTRING(x) TOSTRING_(x)

/// シェーダーに渡す定数。
struct ConstShaderParams {
    uint32_t cSampleStartPos;

    uint32_t cReserved[63];
};

HRESULT
WWDirectConvolutionGpu::Setup(
    WW_DIRECT_CONV_GPU_INOUT_TYPE* inputAry,
    int inputCount,
    WW_DIRECT_CONV_GPU_CONV_TYPE* convAry,
    int convCount)
{
    bool    result = true;
    HRESULT hr = S_OK;
    int useGpuId = -1;
    ConstShaderParams shaderParams;

    WW_DIRECT_CONV_GPU_CONV_TYPE* convAryFlip = nullptr;

    assert(convAry);
    assert(0 < convCount);
    assert(convCount & 1); //< convCountは奇数個。
    assert(inputAry);
    assert(0 < inputCount);

    mInputCount = inputCount;
    mConvCount = convCount;

    //　畳み込み係数バッファーを左右反転したものを作る。
    convAryFlip = new WW_DIRECT_CONV_GPU_CONV_TYPE[convCount];
    assert(convAryFlip);
    for (int i = 0; i < convCount; ++i) {
        convAryFlip[i] = convAry[convCount - 1 - i];
    }

    HRG(mDC.Init());
    HRG(mDC.ChooseAdapter(useGpuId));

    {   // コンピュートシェーダーをコンパイルする。
        // HLSLの中の#defineの値を決めます。
        char elemTypeStr[32];
        sprintf_s(elemTypeStr, TOSTRING(WW_DIRECT_CONV_GPU_INOUT_TYPE));

        char convTypeStr[32];
        sprintf_s(convTypeStr, TOSTRING(WW_DIRECT_CONV_GPU_CONV_TYPE));

        char      inputCountStr[32];
        sprintf_s(inputCountStr, "%d", inputCount);

        char      convStartStr[32];
        sprintf_s(convStartStr, "%d", -convCount /2);
        char      convEndStr[32];
        sprintf_s(convEndStr, "%d", convCount/2);
        char      convCountStr[32];
        sprintf_s(convCountStr, "%d", convCount);

        char      groupThreadCountStr[32];
        sprintf_s(groupThreadCountStr, "%d", GROUP_THREAD_COUNT);

        const D3D_SHADER_MACRO defines[] = {
                "ELEM_TYPE", elemTypeStr,
                "CONV_TYPE", convTypeStr,
                "INPUT_COUNT", inputCountStr,

                "CONV_HALF_LEN", convEndStr,
                "CONV_START", convStartStr,
                "CONV_END",   convEndStr,
                "CONV_COUNT", convCountStr,

                "GROUP_THREAD_COUNT", groupThreadCountStr,
                nullptr, nullptr
        };

#if USE_OPTIMIZED_SHADER
        HRG(mDC.CreateShader(L"DirectConvolution.hlsl", "CSMain", "cs_5_0", defines, mCS));
#else
        HRG(mDC.CreateShader(L"DirectConvolutionUnoptimized.hlsl", "CSMain", "cs_5_0", defines, mCS));
#endif
    }

    HRG(mDC.CreateSrvUavHeap(GB_NUM, mSUHeap));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(WW_DIRECT_CONV_GPU_INOUT_TYPE), inputCount, inputAry,    mGpuBuf[GB_InputAry],      mSrv[GB_InputAry]));
    HRG(mDC.CreateGpuBufferAndRegisterAsSRV(mSUHeap, sizeof(WW_DIRECT_CONV_GPU_CONV_TYPE),  convCount,  convAryFlip, mGpuBuf[GB_ConvCoeffsAry], mSrv[GB_ConvCoeffsAry]));
    HRG(mDC.CreateGpuBufferAndRegisterAsUAV(mSUHeap, sizeof(WW_DIRECT_CONV_GPU_INOUT_TYPE), inputCount, mGpuBuf[GB_OutAry], mUav));
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
    WW_DIRECT_CONV_GPU_INOUT_TYPE* outputTo,
    int outputToElemNum)
{
    HRESULT hr = S_OK;

    assert(outputTo);
    assert(outputToElemNum <= mInputCount);

    // 計算結果をGPUのUAVからCPUに持ってくる。
    HRG(mDC.CopyGpuBufValuesToCpuMemory(mGpuBuf[GB_OutAry], outputTo, outputToElemNum * sizeof(WW_DIRECT_CONV_GPU_INOUT_TYPE)));

end:
    if (hr == DXGI_ERROR_DEVICE_REMOVED) {
        dprintf("DXGI_ERROR_DEVICE_REMOVED.\n");
    }

    return hr;
}

