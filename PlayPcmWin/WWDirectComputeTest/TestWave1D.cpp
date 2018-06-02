#include "TestWave1D.h"

#include "WWDirectComputeUser.h"
#include "WWUpsampleGpu.h"
#include "WWUpsampleCpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>

enum SRVenum {
    SRV_LOSS,
    SRV_ROH,
    SRV_CR,
    SRV_NUM
};

enum UAVenum {
    UAV_V,
    UAV_P,
    UAV_NUM
};

// �萔�B16�o�C�g�̔{���̃T�C�Y�̍\���́B
struct ShaderConstants {
    // �X�V�����̌J��Ԃ��񐔁B
    uint32_t cRepeat;

    // �p�����[�^Sc
    float cSc;
    
    // �p�����[�^C0
    float cC0;

    uint32_t dummy0;
};

static void
TestWave1D1(ShaderConstants & shaderConstants, const int dataCount, float *loss, float *roh, float *cr, float *v, float *p)
{
    HRESULT hr = S_OK;
    ID3D11ShaderResourceView *pSRVs[SRV_NUM];
    ID3D11UnorderedAccessView *pUAVs[UAV_NUM];
    ID3D11ComputeShader *pCS  = nullptr;
    WWDirectComputeUser c;

    // �ő�ő��10�i10���B
    char dataCountStr[16];
    sprintf_s(dataCountStr, "%d", dataCount);

    c.Init();

    // HLSL ComputeShader���R���p�C���B
    const D3D_SHADER_MACRO defines[] = {
        "LENGTH", dataCountStr,
        nullptr, nullptr
    };
    HRG(c.CreateComputeShader(L"Wave1DTest.hlsl", "CSMain", defines, &pCS));
    assert(pCS);

    WWTexture1DParams params[SRV_NUM + UAV_NUM] = {
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, loss, dataCount, "loss", &pSRVs[0], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, roh, dataCount, "roh", &pSRVs[1], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_IMMUTABLE, D3D11_BIND_SHADER_RESOURCE, 0, 0, cr, dataCount, "cr", &pSRVs[2], nullptr},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_UNORDERED_ACCESS, 0, 0, v, dataCount, "v", nullptr, &pUAVs[0]},
        {dataCount, 1, 1, DXGI_FORMAT_R32_FLOAT, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_UNORDERED_ACCESS, 0, 0, p, dataCount, "p", nullptr, &pUAVs[1]},
    };
    HRG(c.CreateSeveralTexture1D(SRV_NUM + UAV_NUM, params));

    HRG(c.Run(pCS, SRV_NUM, pSRVs, UAV_NUM, pUAVs, nullptr, &shaderConstants, sizeof(ShaderConstants), dataCount, 1, 1));

    // �v�Z���ʂ�CPU�������[�Ɏ����Ă���B
    HRG(c.RecvResultToCpuMemory(pUAVs[0], v, dataCount * sizeof(float)));
    HRG(c.RecvResultToCpuMemory(pUAVs[1], p, dataCount * sizeof(float)));

end:
    c.Term();
}

void
TestWave1D(void)
{
    const int dataCount = 256;
    float *loss = new float[dataCount];
    float *roh  = new float[dataCount];
    float *cr = new float [dataCount];
    float *v = new float [dataCount];
    float *p = new float[dataCount];

    for (int i=0; i<dataCount; ++i) {
        loss[i] = 0.0f;
        roh[i] = 1.0f;
        cr[i] = 1.0f;
        v[i] = 1.0f;
        p[i] = 2.0f;
    }

    ShaderConstants sc = {
        1,    // cRepeat
        1.0f, // �p�����[�^Sc
        1.0f, // �p�����[�^C0
        0,    // dummy
    };

    TestWave1D1(sc, dataCount, loss, roh, cr, v, p);

    SAFE_DELETE(p);
    SAFE_DELETE(v);
    SAFE_DELETE(cr);
    SAFE_DELETE(roh);
    SAFE_DELETE(loss);
}