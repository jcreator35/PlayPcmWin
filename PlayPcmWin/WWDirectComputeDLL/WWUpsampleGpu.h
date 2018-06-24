// 日本語

#pragma once

#include "WWDirectComputeUser.h"

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

    // With resamplePosArray
    HRESULT Setup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int * resamplePosArray,
        double *fractionArrayD);

    HRESULT Dispatch(
        int startPos,
        int count);

    HRESULT GetResultFromGpuMemory(
        float * outputTo,
        int outputToElemNum);

    void Unsetup(void);

    HRESULT UpsampleCpuSetup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo);

    /// With resamplePosArray
    HRESULT UpsampleCpuSetup(
        int convolutionN,
        float * sampleFrom,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        int sampleTotalTo,
        int * resamplePosArray,
        double *fractionArrayD);

    // output[0]～output[count-1]に書き込む
    HRESULT UpsampleCpuDo(
        int startPos,
        int count,
        float *output);

    void UpsampleCpuUnsetup(void);

private:
    int m_convolutionN;
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

    //CPU処理用
    float        * m_sampleFrom;
    int          * m_resamplePosArray;
    double       * m_fractionArray;
    float        * m_sinPreComputeArray;
};

