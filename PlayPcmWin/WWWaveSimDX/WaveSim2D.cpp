#include "WaveSim2D.h"
#include "WWWinUtil.h"
#include <assert.h>

static const int STIM_NUM = 4;

WaveSim2D::WaveSim2D(void) :
        mRoh(nullptr), mCr(nullptr), mLoss(nullptr), mGridCount(0),
        mResultTex(nullptr), mResultTexSRV(nullptr), mDisplayCtx(nullptr),
        mDisplayDevice(nullptr)
{
}

WaveSim2D::~WaveSim2D(void)
{
}

HRESULT
WaveSim2D::Init(ID3D11DeviceContext *displayCtx,
        ID3D11Device *displayDevice, int gridW, int gridH,
        float c0, float deltaT, float sc)
{
    HRESULT hr = S_OK;

    mDisplayCtx    = displayCtx;
    mDisplayDevice = displayDevice;
    mGridW  = gridW;
    mGridH  = gridH;
    mC0     = c0;
    mDeltaT = deltaT;
    mSc     = sc;
    mGridCount = gridW * gridH;

    mRoh  = new float[mGridCount];
    mCr   = new float[mGridCount];
    mLoss = new float[mGridCount];

    /*
        * �����C���s�[�_���X��=��*Ca (Schneider17, pp.63, pp.325)
        * ��1����O�i����2�̊E�ʂɒB�����g���E�ʂŔ��˂���Ƃ�
        *
        *             ��2-��1
        * ���˗� r = ����������������
        *             ��2+��1
        * 
        * �}��1�̃C���s�[�_���X��1�Ɣ��˗����}��2�̃C���s�[�_���X��2�𓾂鎮:
        *
        *       -(r+1)��1
        * ��2 = ������������������
        *         r-1
        *         
        * Courant number Sc = c0 ��t / ��x
        */

    for (int i = 0; i < mGridCount; ++i) {
        // ���Ζ��x�B
        mRoh[i] = 1.0f;

        // ���Ή����B0 < Cr < 1
        mCr[i] = 1.0f;

        mLoss[i] = 0.0f;
    }

#if 1
    // �㉺���E�[�̈�͔��˗�80���̕ǂɂȂ��Ă���B
    float r = 0.8f; // 0.8 == 80%
    float roh2 = -(r + 1) * 1.0f / (r - 1);
    float loss2 = 0.1f;
    int edge = 3;
    for (int y = edge; y < mGridH-edge; ++y) {
        for (int x = edge; x < mGridW * 1 / 20; ++x) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
        for (int x = mGridW * 19 / 20; x < mGridW-edge; ++x) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
    }
    for (int x = edge; x < mGridW-edge; ++x) {
        for (int y = edge; y < mGridH * 1 / 20; ++y) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
        for (int y = mGridH * 19 / 20; y < mGridH-edge; ++y) {
            SetRoh(x, y, roh2);
            SetLoss(x, y, loss2);
        }
    }
#endif

    // 2����ABC�p�̉ߋ��f�[�^�u����B
    //for (int i = 0; i < mDelayArray.Length; ++i) {
    //    mDelayArray[i].FillZeroes();
    //}

    //{
    //    // ABC�̌W���B

    //    float Cp = mRoh[0] * mCr[0] * mCr[0] * mC0 * mSc;
    //    float Cv = 2.0f * mSc / ((mRoh[0] + mRoh[0 + 1]) * mC0);

    //    mAbcCoef = new float[3];
    //    float ScPrime = 1.0f; // (float)Math.Sqrt(Cp * Cv);
    //    float denom = 1.0f / ScPrime + 2.0f + ScPrime;
    //    mAbcCoef[0] = -(1.0f / ScPrime - 2.0f + ScPrime) / denom;
    //    mAbcCoef[1] = +(2.0f * ScPrime - 2.0f / ScPrime) / denom;
    //    mAbcCoef[2] = -(4.0f * ScPrime + 4.0f / ScPrime) / denom;
    //}

    {
        auto &cu = mWave2D.GetCU();
        cu.Init();
        cu.EnumAdapters();
        int nAdapters = cu.GetNumOfAdapters();
        if (nAdapters <= 0) {
            printf("Error: No Graphics adapter available!\n");
            return E_FAIL;
        }
        hr = cu.ChooseAdapter(0);
        if (FAILED(hr)) {
            printf("Error: ChooseAdapter failed %08x\n", hr);
            return hr;
        }

        mWave2D.Init();
    }

    {
        WWWave2DParams p;
        p.fieldW = mGridW;
        p.fieldH = mGridH;
        p.deltaT = mDeltaT;
        p.sc     = mSc;
        p.c0     = mC0;
        hr = mWave2D.Setup(p, mLoss, mRoh, mCr);
        if (FAILED(hr)) {
            printf("Error: Setup failed %08x\n", hr);
            return hr;
        }
    }

    hr = CreateResultTex();
    if (FAILED(hr)) {
        printf("Error: CreateResultTex failed %08x\n", hr);
        return hr;
    }

    return hr;
}

void
WaveSim2D::SetRoh(int x, int y, float v)
{
    int pos = x + y * mGridW;
    mRoh[pos] = v;
}

void
WaveSim2D::SetLoss(int x, int y, float v)
{
    int pos = x + y * mGridW;
    mLoss[pos] = v;
}


void
WaveSim2D::Term(void)
{
    // �����́A�Q�Ƃ������Ă��邾���B
    mDisplayCtx    = nullptr;
    mDisplayDevice = nullptr;

    SAFE_RELEASE(mResultTexSRV);
    SAFE_RELEASE(mResultTex);

    mWave2D.Unsetup();
    mWave2D.Term();
}

HRESULT
WaveSim2D::Update(int repeatCount)
{
    assert(0 < repeatCount);

    HRESULT hr = S_OK;
    int nStim = 0;
    WWWave1DStim stims[STIM_NUM];

    for (auto ite=mStimList.begin(); ite!=mStimList.end(); ++ite) {
        stims[nStim] = *ite;
        ++nStim;
        if (STIM_NUM <= nStim) {
            break;
        }
    }

    HRG(mWave2D.Run(repeatCount, nStim, stims));
    HRG(CopyMemoryToTexture2D());

    // �h���̍X�V�B
    for (auto ite=mStimList.begin(); ite!=mStimList.end(); ++ite) {
        ite->counter -= repeatCount;
        if (ite->counter <= 0) {
            ite = mStimList.erase(ite);
        }
    }

end:

    return hr;
}

WWDirectComputeUser &
WaveSim2D::GetCU(void)
{
    return mWave2D.GetCU();
}

HRESULT
WaveSim2D::CreateResultTex(void)
{
    assert(nullptr == mResultTex);
    assert(nullptr == mResultTexSRV);

    HRESULT hr = S_OK;
    D3D11_TEXTURE2D_DESC desc;
    D3D11_SHADER_RESOURCE_VIEW_DESC sdesc;
    
    const char *name = "ResultTex";

    memset(&desc, 0, sizeof desc);
    desc.Width     = mGridW;
    desc.Height    = mGridH;
    desc.MipLevels = desc.ArraySize = 1;
    desc.Format    = DXGI_FORMAT_R32_FLOAT;

    // �}���`�T���v�������B
    desc.SampleDesc.Count   = 1;
    desc.SampleDesc.Quality = 0;

    // Dynamic: GPU: Read only, CPU: Write only
    desc.Usage     = D3D11_USAGE_DYNAMIC;
    desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;

    // �e�N�X�`���[����������CPU������e���X�V����̂ŁB
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
    desc.MiscFlags      = 0;

    HRG(mDisplayDevice->CreateTexture2D(&desc, nullptr, &mResultTex));

    ZeroMemory( &sdesc, sizeof sdesc);
    sdesc.Format = DXGI_FORMAT_R32_FLOAT;
    sdesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
    sdesc.Texture2D.MostDetailedMip = 0;
    sdesc.Texture2D.MipLevels = -1; //< �S�Ďg�p����B

    HRG(mDisplayDevice->CreateShaderResourceView(mResultTex, &sdesc, &mResultTexSRV));
#if !defined(NDEBUG)
    if (mResultTexSRV) {
        mResultTexSRV->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

HRESULT
WaveSim2D::CopyMemoryToTexture2D(void)
{
    HRESULT hr = S_OK;
    const float *from = mWave2D.GetPptr();
    D3D11_MAPPED_SUBRESOURCE res;
    memset(&res, 0, sizeof res);

    const int copyBytes = sizeof(float) * mGridW * mGridH;

    HRG(mDisplayCtx->Map(mResultTex, 0, D3D11_MAP_WRITE_DISCARD, 0, &res));

    memcpy(res.pData, from, copyBytes);

    mDisplayCtx->Unmap(mResultTex, 0);

end:
    return hr;
}

HRESULT
WaveSim2D::AddStimulus(const WWWave1DStim &a)
{
    mStimList.push_back(a);
    return S_OK;
}
