#pragma once

#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>

/// 1スレッドグループに所属するスレッドの数。TGSMを共有する。
/// 2の乗数。
/// この数値を書き換えたらシェーダーも書き換える必要あり。
#define GROUP_THREAD_COUNT 1024



/// シェーダーに渡す定数。16バイトの倍数でないといけないらしい。
struct ConstShaderParams {
    unsigned int c_convOffs;
    unsigned int c_dispatchCount;
    unsigned int c_sampleToStartPos;
    unsigned int c_reserved2;
};

class WWUpsampleGpu {
public:
    void Init(void);
    void Term(void);

    HRESULT Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo);

    HRESULT Dispatch(
        int startPos,
        int count);

    HRESULT ResultGetFromGpuMemory(
        float * outputTo,
        int outputToElemNum);

    void Unsetup(void);

    // limit level to fit to the audio sampledata range
    static float LimitSampleData(
        float * sampleData,
        int sampleDataCount);

private:
    int m_convolutionN;
    float * m_sampleFrom;
    int m_sampleTotalFrom;
    int m_sampleRateFrom;
    int m_sampleRateTo;
    int m_sampleTotalTo;

    WWDirectComputeUser *m_pDCU;
    ID3D11ComputeShader *m_pCS;

    ID3D11ShaderResourceView*   m_pBuf0Srv;
    ID3D11ShaderResourceView*   m_pBuf1Srv;
    ID3D11ShaderResourceView*   m_pBuf2Srv;
    ID3D11ShaderResourceView*   m_pBuf3Srv;
    ID3D11UnorderedAccessView*  m_pBufResultUav;
};
