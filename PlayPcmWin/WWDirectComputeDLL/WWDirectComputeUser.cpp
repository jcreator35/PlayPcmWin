//　日本語
// 参考：BasicCompute11.cpp

#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <d3dcompiler.h>
#include <assert.h>
#include <d3dx11.h>
#include <algorithm>
#include <DXGI.h>

#if 0
static HRESULT
CreateDeviceInternal(
        IDXGIAdapter* pAdapter,
        D3D_DRIVER_TYPE DriverType,
        HMODULE Software,
        UINT32 Flags,
        CONST D3D_FEATURE_LEVEL* pFeatureLevels,
        UINT FeatureLevels,
        UINT32 SDKVersion,
        ID3D11Device** ppDevice,
        D3D_FEATURE_LEVEL* pFeatureLevel,
        ID3D11DeviceContext** ppImmediateContext )
{
    HRESULT hr;

    *ppDevice           = nullptr;
    *ppImmediateContext = nullptr;

    HRG(D3D11CreateDevice(
        pAdapter, DriverType, Software, Flags, pFeatureLevels,
        FeatureLevels,
        SDKVersion, ppDevice, pFeatureLevel, ppImmediateContext));

    assert(*ppDevice);
    assert(*ppImmediateContext);

    // A hardware accelerated device has been created, so check for Compute Shader support.
    // If we have a device >= D3D_FEATURE_LEVEL_11_0 created,
    // full CS5.0 support is guaranteed, no need for further checks.

#if 0
    // Double-precision support is an optional feature of CS 5.0.
    D3D11_FEATURE_DATA_DOUBLES hwopts;
    (*ppDevice)->CheckFeatureSupport( D3D11_FEATURE_DOUBLES, &hwopts, sizeof(hwopts) );
    if ( !hwopts.DoublePrecisionFloatShaderOps ) {
        static bool bMessageAlreadyShown = false;
        if ( !bMessageAlreadyShown ) {
            MessageBox(0,
                L"Error: This GPU does not have ComputeShader5.0 double-precision capability.",
                L"Error", MB_ICONEXCLAMATION);
            bMessageAlreadyShown = true;
        }
        hr = E_FAIL;
        goto end;
    }
#endif

end:
    return hr;
}
#endif

WWDirectComputeUser::WWDirectComputeUser(void)
{
    mDevice = nullptr;
    mCtx = nullptr;
    mConstBuffer = nullptr;
}

WWDirectComputeUser::~WWDirectComputeUser(void)
{
    assert(nullptr == mDevice);
    assert(nullptr == mCtx);
    assert(nullptr == mConstBuffer);
}

void
WWDirectComputeUser::Init(void)
{
    mAdapters.clear();
}

HRESULT
WWDirectComputeUser::EnumAdapters(void)
{
    HRESULT hr = S_OK;
    UINT i = 0;
    IDXGIAdapter * pAdapter = nullptr;
    IDXGIFactory1* pFactory = nullptr;

    mAdapters.clear();

    HRG(CreateDXGIFactory1(__uuidof(IDXGIFactory1) ,(void**)&pFactory));

    while (pFactory->EnumAdapters(i, &pAdapter) != DXGI_ERROR_NOT_FOUND) {
        assert(pAdapter);

        WWDirectComputeAdapter a;
        a.adapter = pAdapter;
        pAdapter->GetDesc(&a.desc);
        mAdapters.push_back(a);

        printf("    Adapter %d, %S, video memory = %d MB\n", i, a.desc.Description,
                a.desc.DedicatedVideoMemory/1024/1024);

        pAdapter = nullptr;
        ++i;
    }

end:
    SAFE_RELEASE(pFactory);
    SAFE_RELEASE(mCtx);
    SAFE_RELEASE(mDevice);
    return hr;
}

int
WWDirectComputeUser::GetNumOfAdapters(void)
{
    return (int)mAdapters.size();
}

HRESULT
WWDirectComputeUser::GetAdapterDesc(int idx, wchar_t *desc, int descBytes)
{
    memset(desc, 0, descBytes);
    if (idx < 0 || (int)mAdapters.size() <= idx) {
        return E_FAIL;
    }

    wcsncpy_s(desc, descBytes/2-1, mAdapters[idx].desc.Description, descBytes/2-1);
    return S_OK;
}

HRESULT
WWDirectComputeUser::GetAdapterVideoMemoryBytes(int idx, int64_t *videoMemoryBytes)
{
    if (idx < 0 || (int)mAdapters.size() <= idx) {
        return E_FAIL;
    }
    *videoMemoryBytes = mAdapters[idx].desc.DedicatedVideoMemory;
    return S_OK;
}


HRESULT
WWDirectComputeUser::ChooseAdapter(int idx)
{
    if (idx < 0 || (int)mAdapters.size() <= idx) {
        printf("%s:%d E: idx is out of range\n", __FILE__, __LINE__);
        return E_FAIL;
    }

    IDXGIAdapter* pAdapter = mAdapters[idx].adapter;

    HRESULT hr = S_OK;

    assert(nullptr == mDevice);
    assert(nullptr == mCtx);
    
    UINT uCreationFlags = D3D11_CREATE_DEVICE_SINGLETHREADED;
//#if defined(DEBUG) || defined(_DEBUG)
//    uCreationFlags |= D3D11_CREATE_DEVICE_DEBUG;
//#endif

    D3D_FEATURE_LEVEL flOut;
    static const D3D_FEATURE_LEVEL flvl[] = { D3D_FEATURE_LEVEL_11_0 };

    HRG(D3D11CreateDevice(
        pAdapter,
        D3D_DRIVER_TYPE_UNKNOWN, // Should be UNKNOWN when pAdapter != nullptr !!!
        nullptr,                     // Do not use external software rasterizer module
        uCreationFlags,              // Device creation flags
        flvl,
        sizeof flvl / sizeof flvl[0],
        D3D11_SDK_VERSION,           // SDK version
        &mDevice,                  // Device out
        &flOut,                      // Actual feature level created
        &mCtx));

    assert(flOut == D3D_FEATURE_LEVEL_11_0);

    printf("ChooseAdapter(%d)\n", idx);
end:
    return hr;
}

void
WWDirectComputeUser::Term(void)
{
    SafeRelease( &mConstBuffer );
    SafeRelease( &mCtx );
    SafeRelease( &mDevice );

    for (auto ite=mAdapters.begin(); ite != mAdapters.end(); ++ite) {
        SAFE_RELEASE(ite->adapter);
    }
    mAdapters.clear();

    for (auto ite=m_rwGpuBufMap.begin(); ite != m_rwGpuBufMap.end(); ++ite) {
        SAFE_RELEASE(ite->second.pUav);
        SAFE_RELEASE(ite->second.pBuf);
    }
    m_rwGpuBufMap.clear();

    for (auto ite=mReadGpuBufMap.begin(); ite != mReadGpuBufMap.end(); ++ite) {
        SAFE_RELEASE(ite->second.pSrv);
        SAFE_RELEASE(ite->second.pBuf);
    }
    mReadGpuBufMap.clear();

    for (auto ite=mComputeShaderList.begin(); ite != mComputeShaderList.end(); ++ite) {
        auto * p = *ite;
        SAFE_RELEASE(p);
    }
    mComputeShaderList.clear();
}


// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Compute Shader Compile

HRESULT
WWDirectComputeUser::CreateComputeShader(
        LPCWSTR path,
        LPCSTR entryPoint,
        const D3D_SHADER_MACRO *defines,
        ID3D11ComputeShader **ppCS)
{
    HRESULT hr;
    ID3DBlob * pErrorBlob = nullptr;
    ID3DBlob * pBlob      = nullptr;

    assert(mDevice);

    DWORD dwShaderFlags = D3DCOMPILE_ENABLE_STRICTNESS;
//#if defined( DEBUG ) || defined( _DEBUG )
    // D3DCOMPILE_DEBUGフラグを指定すると、シェーダーにデバッグ情報を埋め込むが
    // 最適化はされるし、RELEASEと同等の動作をし、性能が落ちない…らしい。
    //dwShaderFlags |= D3DCOMPILE_DEBUG;
//#endif

    // CSシェーダープロファイル5.0を指定。
    LPCSTR pProfile = "cs_5_0";

    hr = D3DX11CompileFromFile(path, defines, nullptr, entryPoint, pProfile,
        dwShaderFlags, 0, nullptr, &pBlob, &pErrorBlob, nullptr );
    if (FAILED(hr)) {
        WCHAR erTitle[256];
        swprintf_s(erTitle, L"D3DX11CompileFromFile failed with %x", hr);

        WCHAR erStr[65536];
        ZeroMemory(erStr, sizeof erStr);
        if (pErrorBlob) {
            const char *s = (const char *)pErrorBlob->GetBufferPointer();
            MultiByteToWideChar(CP_ACP, 0, s, -1,
                erStr, sizeof erStr/sizeof erStr[0]-1);
        } else {
            wcsncpy_s(erStr, erTitle, 256);
        }

        MessageBox(0, erStr, erTitle, MB_ICONEXCLAMATION);
        goto end;
    }

    assert(pBlob);

    hr = mDevice->CreateComputeShader(
        pBlob->GetBufferPointer(), pBlob->GetBufferSize(), nullptr, ppCS);
    if (SUCCEEDED(hr) && *ppCS) {
        mComputeShaderList.push_back(*ppCS);
    }

#if defined(DEBUG) || defined(PROFILE)
    if (*ppCS) {
        (*ppCS)->SetPrivateData( WKPDID_D3DDebugObjectName, lstrlenA(pFunctionName), pFunctionName );
    }
#endif

end:
    SafeRelease(&pErrorBlob);
    SafeRelease(&pBlob);
    return hr;
}

void
WWDirectComputeUser::DestroyComputeShader(
        ID3D11ComputeShader *pCS)
{
    auto ite = std::find(mComputeShaderList.begin(), mComputeShaderList.end(), pCS);
    if (ite != mComputeShaderList.end()) {
        mComputeShaderList.erase(ite);
    }

    SAFE_RELEASE(pCS);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// GPU → CPU

HRESULT
WWDirectComputeUser::RecvResultToCpuMemory(
        ID3D11UnorderedAccessView * pUav,
        void *dest,
        int bytes)
{
    HRESULT hr = S_OK;
    D3D11_MAPPED_SUBRESOURCE mr;
    D3D11_RESOURCE_DIMENSION dimen;

    assert(mDevice);
    assert(mCtx);
    assert(pUav);
    assert(dest);

    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo>::iterator
        ite = m_rwGpuBufMap.find(pUav);
    if (ite == m_rwGpuBufMap.end()) {
        hr = E_FAIL;
        goto end;
    }

    ite->second.pBuf->GetType(&dimen);
    switch (dimen) {
    case D3D11_RESOURCE_DIMENSION_BUFFER:
        {
            ID3D11Buffer * pBuffer = nullptr;
            ID3D11Buffer * pReturn = nullptr;
            D3D11_BUFFER_DESC desc;

            pBuffer = (ID3D11Buffer*)ite->second.pBuf;

            ZeroMemory(&desc, sizeof desc);
            pBuffer->GetDesc(&desc);
            desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
            desc.Usage = D3D11_USAGE_STAGING;
            desc.BindFlags = 0;
            desc.MiscFlags = 0;
            HRG(mDevice->CreateBuffer(&desc, nullptr, &pReturn));

            mCtx->CopyResource(pReturn, pBuffer);
            assert(pReturn);

            ZeroMemory(&mr, sizeof mr);
            HRG(mCtx->Map(pReturn, 0, D3D11_MAP_READ, 0, &mr));
            assert(mr.pData);
            // Unmapしないでgoto endしてはいけない

            memcpy(dest, mr.pData, bytes);

            mCtx->Unmap(pReturn, 0);
            SafeRelease(&pReturn);
        }
        break;
    case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
        {
            ID3D11Texture1D *pTex = nullptr;
            ID3D11Texture1D *pReturn = nullptr;
            D3D11_TEXTURE1D_DESC desc;

            pTex = (ID3D11Texture1D*)ite->second.pBuf;

            ZeroMemory(&desc, sizeof desc);
            pTex->GetDesc(&desc);
            desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
            desc.Usage = D3D11_USAGE_STAGING;
            desc.BindFlags = 0;
            desc.MiscFlags = 0;

            HRG(mDevice->CreateTexture1D(&desc, nullptr, &pReturn));
            assert(pReturn);
            
            mCtx->CopyResource(pReturn, pTex);
            assert(pReturn);

            ZeroMemory(&mr, sizeof mr);
            HRG(mCtx->Map(pReturn, 0, D3D11_MAP_READ, 0, &mr));
            assert(mr.pData);
            // Unmapしないでgoto endしてはいけない

            memcpy(dest, mr.pData, bytes);

            mCtx->Unmap(pReturn, 0);
            SafeRelease(&pReturn);
        }
    }

end:
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Run

HRESULT
WWDirectComputeUser::SetupDispatch(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumSRV,
        ID3D11ShaderResourceView * ppSRV[],
        UINT nNumUAV,
        ID3D11UnorderedAccessView * ppUAV[],
        void *pCSData,
        DWORD dwNumDataBytes)
{
    HRESULT hr = S_OK;
    D3D11_MAPPED_SUBRESOURCE mr;

    assert(mDevice);
    assert(mCtx);
    assert(pComputeShader);

    if (pCSData) {
        assert(!mConstBuffer);
        HRG(CreateConstantBuffer(dwNumDataBytes, 1, "ShaderConstants", &mConstBuffer));

        ZeroMemory(&mr, sizeof mr);

        HRG(mCtx->Map(mConstBuffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &mr));
        assert(mr.pData);

        memcpy(mr.pData, pCSData, dwNumDataBytes);
        mCtx->Unmap(mConstBuffer, 0);

        ID3D11Buffer* ppCB[1] = { mConstBuffer };
        mCtx->CSSetConstantBuffers(0, 1, ppCB);
    } else {
        //ID3D11Buffer * ppCBNULL[1] = { nullptr };
        //mCtx->CSSetConstantBuffers(0, 1, ppCBNULL);
    }

    // シェーダーとパラメータをセットする。

    mCtx->CSSetShader(pComputeShader, nullptr, 0);
    if (0 < nNumSRV) {
        mCtx->CSSetShaderResources(0, nNumSRV, ppSRV);
    }
    if (0 < nNumUAV) {
        mCtx->CSSetUnorderedAccessViews(0, nNumUAV, ppUAV, nullptr);
    }

end:

    return hr;
}

void
WWDirectComputeUser::Dispatch(
        UINT X,
        UINT Y,
        UINT Z)
{
    assert(0 < X);
    assert(0 < Y);
    assert(0 < Z);

    assert(mCtx);

    mCtx->Dispatch(X, Y, Z);
}

void
WWDirectComputeUser::UnsetupDispatch(void)
{
    assert(mCtx);

    mCtx->CSSetShader(nullptr, nullptr, 0);

    ID3D11UnorderedAccessView * ppUAViewNULL[1] = { nullptr };
    mCtx->CSSetUnorderedAccessViews(0, 1, ppUAViewNULL, nullptr);

    ID3D11ShaderResourceView * ppSRVNULL[2] = { nullptr, nullptr };
    mCtx->CSSetShaderResources(0, 2, ppSRVNULL);

    ID3D11Buffer * ppCBNULL[1] = { nullptr };
    mCtx->CSSetConstantBuffers(0, 1, ppCBNULL);

    SAFE_RELEASE(mConstBuffer);
}

HRESULT
WWDirectComputeUser::Run(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumSRV,
        ID3D11ShaderResourceView * ppSRV[],
        UINT nNumUAV,
        ID3D11UnorderedAccessView * ppUAV[],
        void * pCSData,
        DWORD dwNumDataBytes,
        UINT X,
        UINT Y,
        UINT Z)
{
    HRESULT hr = S_OK;
    bool result = true;

    HRG(SetupDispatch(pComputeShader, nNumSRV, ppSRV, nNumUAV, ppUAV, pCSData, dwNumDataBytes));
    Dispatch(X, Y, Z);

end:
    UnsetupDispatch();

    return hr;
}

ID3D11Resource *
WWDirectComputeUser::FindResourceOfSRV(ID3D11ShaderResourceView * pSrv)
{
    std::map<ID3D11ShaderResourceView *, WWReadOnlyGpuBufferInfo>::iterator
        ite = mReadGpuBufMap.find(pSrv);
    if (ite == mReadGpuBufMap.end()) {
        return nullptr;
    }

    return ite->second.pBuf;
}

ID3D11Resource *
WWDirectComputeUser::FindResourceOfUAV(ID3D11UnorderedAccessView * pUav)
{
    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo>::iterator
        ite = m_rwGpuBufMap.find(pUav);
    if (ite == m_rwGpuBufMap.end()) {
        return nullptr;
    }

    return ite->second.pBuf;
}

void
WWDirectComputeUser::DestroyResourceAndSRV(
        ID3D11ShaderResourceView *pSrv)
{
    std::map<ID3D11ShaderResourceView *, WWReadOnlyGpuBufferInfo>::iterator
        ite = mReadGpuBufMap.find(pSrv);
    if (ite == mReadGpuBufMap.end()) {
        return;
    }

    SAFE_RELEASE(ite->second.pSrv);
    SAFE_RELEASE(ite->second.pBuf);

    mReadGpuBufMap.erase(pSrv);
}

void
WWDirectComputeUser::DestroyResourceAndUAV(
        ID3D11UnorderedAccessView * pUav)
{
    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo>::iterator
        ite = m_rwGpuBufMap.find(pUav);
    if (ite == m_rwGpuBufMap.end()) {
        return;
    }

    SAFE_RELEASE(ite->second.pUav);
    SAFE_RELEASE(ite->second.pBuf);

    m_rwGpuBufMap.erase(pUav);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Constant Buffer

HRESULT
WWDirectComputeUser::CreateConstantBuffer(
        unsigned int uElementSize,
        unsigned int uCount,
        const char *name,
        ID3D11Buffer **ppBufOut)
{
    HRESULT hr = S_OK;

    // uElementSizeは16の倍数でないといけないらしい。
    assert((uElementSize%16) ==0);
    assert(0<uCount);
    assert(ppBufOut);
    *ppBufOut = nullptr;
    assert(mDevice);

    D3D11_BUFFER_DESC desc;
    ZeroMemory(&desc, sizeof desc);

    desc.BindFlags      = D3D11_BIND_CONSTANT_BUFFER;
    desc.Usage          = D3D11_USAGE_DYNAMIC;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
    desc.MiscFlags      = 0;
    desc.ByteWidth      = uElementSize * uCount;
    desc.StructureByteStride = 0;

    HRG(mDevice->CreateBuffer(&desc, nullptr, ppBufOut));

end:
    return hr;
}

void
WWDirectComputeUser::DestroyConstantBuffer(ID3D11Buffer * pBuf)
{
    SAFE_RELEASE(pBuf);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Structured Buffer

HRESULT
WWDirectComputeUser::CreateStructuredBuffer(
        unsigned int uElementSize,
        unsigned int uCount,
        void * pInitData,
        const char *name,
        ID3D11Buffer ** ppBufOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(uElementSize);
    assert(0 < uCount);
    // name==nullptrでも可。
    // pInitData==nullptrでも可。

    *ppBufOut = nullptr;

    D3D11_BUFFER_DESC desc;
    ZeroMemory(&desc, sizeof desc);
    desc.BindFlags = D3D11_BIND_UNORDERED_ACCESS | D3D11_BIND_SHADER_RESOURCE;
    desc.ByteWidth = uElementSize * uCount;
    desc.MiscFlags = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;
    desc.StructureByteStride = uElementSize;

    if (pInitData) {
        D3D11_SUBRESOURCE_DATA initData;
        ZeroMemory(&initData, sizeof initData);
        initData.pSysMem = pInitData;

        hr = mDevice->CreateBuffer(&desc, &initData, ppBufOut);
    } else {
        hr = mDevice->CreateBuffer(&desc, nullptr, ppBufOut);
    }

    if (FAILED(hr)) {
        goto end;
    }

#   if defined(DEBUG) || defined(PROFILE)
    if (nullptr != name) {
            assert(*ppBufOut);
            (*ppBufOut)->SetPrivateData( WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#   endif

end:

    return hr;
}

HRESULT
WWDirectComputeUser::CreateBufferSRV(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pBuffer);
    assert(name);
    assert(ppSrvOut);
    *ppSrvOut = NULL;

    D3D11_BUFFER_DESC descBuf;
    ZeroMemory(&descBuf, sizeof descBuf);
    pBuffer->GetDesc(&descBuf);

    assert(descBuf.MiscFlags & D3D11_RESOURCE_MISC_BUFFER_STRUCTURED);

    D3D11_SHADER_RESOURCE_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.ViewDimension = D3D11_SRV_DIMENSION_BUFFEREX;
    desc.BufferEx.FirstElement = 0;
    desc.Format = DXGI_FORMAT_UNKNOWN;
    desc.BufferEx.NumElements = descBuf.ByteWidth / descBuf.StructureByteStride;

    HRG(mDevice->CreateShaderResourceView(pBuffer, &desc, ppSrvOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppSrvOut) {
        (*ppSrvOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}


HRESULT
WWDirectComputeUser::CreateBufferUAV(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pBuffer);
    assert(name);
    assert(ppUavOut);
    *ppUavOut = nullptr;

    D3D11_BUFFER_DESC descBuf;
    ZeroMemory(&descBuf, sizeof descBuf);
    pBuffer->GetDesc(&descBuf);

    assert(descBuf.MiscFlags & D3D11_RESOURCE_MISC_BUFFER_STRUCTURED);

    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    ZeroMemory(&desc, sizeof desc);
    desc.ViewDimension = D3D11_UAV_DIMENSION_BUFFER;
    desc.Buffer.FirstElement = 0;
    // Format must be DXGI_FORMAT_UNKNOWN, when creating a View of a Structured Buffer
    desc.Format = DXGI_FORMAT_UNKNOWN;
    desc.Buffer.NumElements = descBuf.ByteWidth / descBuf.StructureByteStride;
    
    HRG(mDevice->CreateUnorderedAccessView(pBuffer, &desc, ppUavOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppUavOut) {
        (*ppUavOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateBufferAndSRV(
        unsigned int uElementSize,
        unsigned int uCount,
        void * pSendData,
        const char *name,
        ID3D11ShaderResourceView **ppSrv)
{
    HRESULT hr = S_OK;
    ID3D11Buffer *pBuf = nullptr;

    assert(ppSrv);
    *ppSrv = nullptr;

    HRG(CreateStructuredBuffer(uElementSize, uCount, pSendData, nullptr, &pBuf));
    assert(pBuf);

    HRG(CreateBufferSRV(pBuf, name, ppSrv));
    assert(*ppSrv);

    WWReadOnlyGpuBufferInfo info;
    info.pBuf = pBuf;
    info.pSrv = *ppSrv;

    mReadGpuBufMap[info.pSrv] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pBuf);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateBufferAndUAV(
        unsigned int uElementSize,
        unsigned int uCount,
        void *pSendData,
        const char *name,
        ID3D11UnorderedAccessView **ppUav)
{
    HRESULT hr = S_OK;
    ID3D11Buffer *pBuf = nullptr;

    assert(ppUav);
    *ppUav = nullptr;

    HRG(CreateStructuredBuffer(uElementSize, uCount, pSendData, nullptr, &pBuf));
    assert(pBuf);

    HRG(CreateBufferUAV(pBuf, name, ppUav));
    assert(*ppUav);

    WWReadWriteGpuBufferInfo info;
    info.pBuf = pBuf;
    info.pUav = *ppUav;

    m_rwGpuBufMap[info.pUav] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pBuf);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateSeveralStructuredBuffer(int n, WWStructuredBufferParams *params)
{
    HRESULT hr = E_FAIL;

    for (int i=0; i<n; ++i) {
        hr = E_FAIL;

        auto &p = params[i];
        if (p.pSrv != nullptr) {
            hr = CreateBufferAndSRV(p.uElementSize, p.uCount, p.pInitData, p.name, p.pSrv);
        } else if (p.pUav != nullptr) {
            hr = CreateBufferAndUAV(p.uElementSize, p.uCount, p.pInitData, p.name, p.pUav);
        } else {
            assert(false);
        }

        if (FAILED(hr)) {
            return hr;
        }
    }

    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Texture1D

HRESULT
WWDirectComputeUser::CreateTexture1DSRV(
        ID3D11Texture1D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pTex);
    assert(name);
    assert(ppSrvOut);
    *ppSrvOut = nullptr;

/*
    これはabortする。
    D3D11_TEXTURE1D_DESC descBuf;
    ZeroMemory(&descBuf, sizeof descBuf);
    pTex->GetDesc(&descBuf);
*/

    D3D11_SHADER_RESOURCE_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.Format = format;
    desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE1D;
    desc.Texture1D.MostDetailedMip = 0;
    desc.Texture1D.MipLevels = -1; //< 全て使用する。

    HRG(mDevice->CreateShaderResourceView(pTex, &desc, ppSrvOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppSrvOut) {
        (*ppSrvOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture1DUAV(
        ID3D11Texture1D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pTex);
    assert(name);
    assert(ppUavOut);
    *ppUavOut = nullptr;

    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.Format = format;
    desc.ViewDimension = D3D11_UAV_DIMENSION_TEXTURE1D;
    desc.Texture1D.MipSlice = 0;

    HRG(mDevice->CreateUnorderedAccessView(pTex, &desc, ppUavOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppUavOut) {
        (*ppUavOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture1D(
        int Width,
        int MipLevels,
        int ArraySize,
        DXGI_FORMAT Format,
        D3D11_USAGE Usage,
        int BindFlags,
        int CPUAccessFlags,
        int MiscFlags,
        const void *pInitialData,
        uint32_t initialDataBytes,
        ID3D11Texture1D **ppTexOut)
{
    HRESULT hr = S_OK;
    assert(ppTexOut);
    *ppTexOut = nullptr;

    D3D11_TEXTURE1D_DESC desc;
    memset(&desc,0,sizeof desc);
    desc.Width = Width;
    desc.MipLevels=MipLevels;
    desc.ArraySize = ArraySize;
    desc.Format = Format;
    desc.Usage = Usage;
    desc.BindFlags = BindFlags;
    desc.CPUAccessFlags = CPUAccessFlags;
    desc.MiscFlags = MiscFlags;

    assert(Usage != D3D11_USAGE_IMMUTABLE || pInitialData != nullptr);
    D3D11_SUBRESOURCE_DATA *pSrd = nullptr;
    D3D11_SUBRESOURCE_DATA srd;
    if (pInitialData) {
        memset(&srd, 0, sizeof srd);
        srd.pSysMem          = pInitialData;
        srd.SysMemPitch      = 0;
        srd.SysMemSlicePitch = 0;
        pSrd = &srd;
    }

    HRG(mDevice->CreateTexture1D(&desc, pSrd, ppTexOut));

end:
    return hr;
}

void
WWDirectComputeUser::DestroyTexture1D(
        ID3D11Texture1D *pTex) const
{
    if (pTex == nullptr) {
        return;
    }

    pTex->Release();
    pTex = nullptr; //< あまり意味ない。
    return;
}

HRESULT
WWDirectComputeUser::CreateTexture1DAndSRV(
        int width,
        int mipLevels,
        int arraySize,
        DXGI_FORMAT format,
        D3D11_USAGE usage,
        int bindFlags,
        int cpuAccessFlags,
        int miscFlags,
        const float *data,
        int dataCount,
        const char *name,
        ID3D11ShaderResourceView **ppSrv)
{
    HRESULT hr = S_OK;
    ID3D11Texture1D *pTex = nullptr;

    assert(ppSrv);
    *ppSrv = nullptr;

    HRG(CreateTexture1D(width, mipLevels, arraySize, format, usage, bindFlags,
        cpuAccessFlags, miscFlags, data, dataCount * sizeof(float), &pTex));

    assert(pTex);

    HRG(CreateTexture1DSRV(pTex, format, name, ppSrv));
    assert(*ppSrv);

    WWReadOnlyGpuBufferInfo info;
    info.pBuf = pTex;
    info.pSrv = *ppSrv;

    mReadGpuBufMap[info.pSrv] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pTex);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture1DAndUAV(
        int width,
        int mipLevels,
        int arraySize,
        DXGI_FORMAT format,
        D3D11_USAGE usage,
        int bindFlags,
        int cpuAccessFlags,
        int miscFlags,
        const float *data,
        int dataCount,
        const char *name,
        ID3D11UnorderedAccessView **ppUav)
{
    HRESULT hr = S_OK;
    ID3D11Texture1D *pTex = nullptr;

    assert(ppUav);
    *ppUav = nullptr;

    HRG(CreateTexture1D(width, mipLevels, arraySize, format, usage, bindFlags,
            cpuAccessFlags, miscFlags, data, dataCount * sizeof(float), &pTex));

    assert(pTex);

    HRG(CreateTexture1DUAV(pTex, format, name, ppUav));
    assert(*ppUav);

    WWReadWriteGpuBufferInfo info;
    info.pBuf = pTex;
    info.pUav = *ppUav;

    m_rwGpuBufMap[info.pUav] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pTex);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateSeveralTexture1D(int n, WWTexture1DParams *params)
{
    HRESULT hr = E_FAIL;

    for (int i=0; i<n; ++i) {
        hr = E_FAIL;

        auto &p = params[i];
        if (p.pSrv != nullptr) {
            hr = CreateTexture1DAndSRV(p.Width, p.MipLevels, p.ArraySize, p.Format, p.Usage, p.BindFlags, p.CPUAccessFlags,
                p.MiscFlags, p.data, p.dataCount, p.name, p.pSrv);
        } else if (p.pUav != nullptr) {
            hr = CreateTexture1DAndUAV(p.Width, p.MipLevels, p.ArraySize, p.Format, p.Usage, p.BindFlags, p.CPUAccessFlags,
                p.MiscFlags, p.data, p.dataCount, p.name, p.pUav);
        } else {
            assert(false);
        }

        if (FAILED(hr)) {
            return hr;
        }
    }

    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Texture2D

HRESULT
WWDirectComputeUser::CreateTexture2D(
        int Width,
        int Height,
        int MipLevels,
        int ArraySize,
        DXGI_FORMAT Format,
        DXGI_SAMPLE_DESC SampleDesc,
        D3D11_USAGE Usage,
        int BindFlags,
        int CPUAccessFlags,
        int MiscFlags,
        const void *pInitialData,
        uint32_t initialDataBytes,
        ID3D11Texture2D **ppTexOut)
{
    HRESULT hr = S_OK;
    assert(ppTexOut);
    *ppTexOut = nullptr;

    D3D11_TEXTURE2D_DESC desc;
    memset(&desc,0,sizeof desc);
    desc.Width = Width;
    desc.Height = Height;
    desc.MipLevels = MipLevels;
    desc.ArraySize = ArraySize;
    desc.Format = Format;

    desc.SampleDesc = SampleDesc;
    desc.Usage = Usage;
    desc.BindFlags = BindFlags;
    desc.CPUAccessFlags = CPUAccessFlags;
    desc.MiscFlags = MiscFlags;

    assert(Usage != D3D11_USAGE_IMMUTABLE || pInitialData != nullptr);
    D3D11_SUBRESOURCE_DATA *pSrd = nullptr;
    D3D11_SUBRESOURCE_DATA srd;
    if (pInitialData) {
        memset(&srd, 0, sizeof srd);
        srd.pSysMem          = pInitialData;
        srd.SysMemPitch      = 0;
        srd.SysMemSlicePitch = 0;
        pSrd = &srd;
    }

    HRG(mDevice->CreateTexture2D(&desc, pSrd, ppTexOut));

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture2DSRV(
        ID3D11Texture2D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pTex);
    assert(name);
    assert(ppSrvOut);
    *ppSrvOut = nullptr;

/*
    これはabortする。
    D3D11_TEXTURE1D_DESC descBuf;
    ZeroMemory(&descBuf, sizeof descBuf);
    pTex->GetDesc(&descBuf);
*/

    D3D11_SHADER_RESOURCE_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.Format = format;
    desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
    desc.Texture2D.MostDetailedMip = 0;
    desc.Texture2D.MipLevels = -1; //< 全て使用する。

    HRG(mDevice->CreateShaderResourceView(pTex, &desc, ppSrvOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppSrvOut) {
        (*ppSrvOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture2DUAV(
        ID3D11Texture2D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut)
{
    HRESULT hr = S_OK;

    assert(mDevice);
    assert(pTex);
    assert(name);
    assert(ppUavOut);
    *ppUavOut = nullptr;

    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.Format = format;
    desc.ViewDimension = D3D11_UAV_DIMENSION_TEXTURE2D;
    desc.Texture1D.MipSlice = 0;

    HRG(mDevice->CreateUnorderedAccessView(pTex, &desc, ppUavOut));

#if defined(DEBUG) || defined(PROFILE)
    if (*ppUavOut) {
        (*ppUavOut)->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#else
    (void)name;
#endif

end:
    return hr;
}

void
WWDirectComputeUser::DestroyTexture2D(
        ID3D11Texture2D *pTex) const
{
    if (pTex == nullptr) {
        return;
    }

    pTex->Release();
    pTex = nullptr; //< あまり意味ない。
    return;
}

HRESULT
WWDirectComputeUser::CreateTexture2DAndSRV(
        int width,
        int height,
        int mipLevels,
        int arraySize,
        DXGI_FORMAT format,
        DXGI_SAMPLE_DESC sampleDesc,
        D3D11_USAGE usage,
        int bindFlags,
        int cpuAccessFlags,
        int miscFlags,
        const float *data,
        int dataCount,
        const char *name,
        ID3D11ShaderResourceView **ppSrv)
{
    HRESULT hr = S_OK;
    ID3D11Texture2D *pTex = nullptr;

    assert(ppSrv);
    *ppSrv = nullptr;

    HRG(CreateTexture2D(width, height, mipLevels, arraySize, format, sampleDesc,
            usage, bindFlags,
            cpuAccessFlags, miscFlags, data, dataCount * sizeof(float), &pTex));

    assert(pTex);

    HRG(CreateTexture2DSRV(pTex, format, name, ppSrv));
    assert(*ppSrv);

    WWReadOnlyGpuBufferInfo info;
    info.pBuf = pTex;
    info.pSrv = *ppSrv;

    mReadGpuBufMap[info.pSrv] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pTex);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture2DAndUAV(
        int width,
        int height,
        int mipLevels,
        int arraySize,
        DXGI_FORMAT format,
        DXGI_SAMPLE_DESC sampleDesc,
        D3D11_USAGE usage,
        int bindFlags,
        int cpuAccessFlags,
        int miscFlags,
        const float *data,
        int dataCount,
        const char *name,
        ID3D11UnorderedAccessView **ppUav)
{
    HRESULT hr = S_OK;
    ID3D11Texture2D *pTex = nullptr;

    assert(ppUav);
    *ppUav = nullptr;

    HRG(CreateTexture2D(width, height, mipLevels, arraySize, format, sampleDesc,
            usage, bindFlags,
            cpuAccessFlags, miscFlags, data, dataCount * sizeof(float), &pTex));

    assert(pTex);

    HRG(CreateTexture2DUAV(pTex, format, name, ppUav));
    assert(*ppUav);

    WWReadWriteGpuBufferInfo info;
    info.pBuf = pTex;
    info.pUav = *ppUav;

    m_rwGpuBufMap[info.pUav] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pTex);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateSeveralTexture2D(int n, WWTexture2DParams *params)
{
    HRESULT hr = E_FAIL;

    for (int i=0; i<n; ++i) {
        hr = E_FAIL;

        auto &p = params[i];
        if (p.pSrv != nullptr) {
            hr = CreateTexture2DAndSRV(p.Width, p.Height, p.MipLevels, p.ArraySize,
                p.Format, p.SampleDesc, p.Usage, p.BindFlags, p.CPUAccessFlags,
                p.MiscFlags, p.data, p.dataCount, p.name, p.pSrv);
        } else if (p.pUav != nullptr) {
            hr = CreateTexture2DAndUAV(p.Width, p.Height, p.MipLevels, p.ArraySize,
                p.Format, p.SampleDesc, p.Usage, p.BindFlags, p.CPUAccessFlags,
                p.MiscFlags, p.data, p.dataCount, p.name, p.pUav);
        } else {
            assert(false);
        }

        if (FAILED(hr)) {
            return hr;
        }
    }

    return hr;
}

