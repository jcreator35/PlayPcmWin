// 日本語。

#pragma once

#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

class WWDirectConvolutionGpu {
public:
    /// @brief 入力データ列と畳み込み係数列をGPUに送り、コンピュートシェーダーをコンパイルする。
    /// @param convAry 畳み込み係数列。convCountサンプル。
    /// @param convCount 畳み込み係数列の要素数。奇数。
    HRESULT Setup(
        float* inputAry,
        int inputCount,
        double* convAry,
        int convCount);

    /// @brief 少しずつConvolutionする。
    /// @param startPos inputDataのstartオフセット。
    /// @param count inputData処理数量。
    HRESULT Dispatch(
        int startPos,
        int count);

    /// @brief 計算結果をCPUメモリに持ってくる。最初から最後までDispatchしてから呼ぶ。
    HRESULT ResultGetFromGpuMemory(
        float* outputTo,
        int outputToElemNum);

private:
    int mInputCount = 0;
    int mConvCount = 0;

    WWDirectCompute12User mDC;
    WWShader mCS;
    WWSrvUavHeap mSUHeap;
    WWComputeState mCState;
    WWConstantBuffer mCBuf;

    enum GpuBufType {
        GB_InputAry,
        GB_ConvCoeffsAry,
        GB_OutAry,
        GB_NUM
    };

    static const int NUM_CONSTS = 1;
    static const int NUM_SRV = 2;
    static const int NUM_UAV = 1;

    WWGpuBuf mGpuBuf[GB_NUM];

    WWSrv mSrv[NUM_SRV];
    WWUav mUav;
};
