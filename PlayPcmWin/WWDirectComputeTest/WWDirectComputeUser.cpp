// 日本語 SJIS
// 参考：BasicCompute11.cpp

#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <d3dcompiler.h>
#include <assert.h>
#include <d3dx11.h>
#include <algorithm>

WWDirectComputeUser::WWDirectComputeUser(void)
{
    m_pDevice = nullptr;
    m_pContext = nullptr;
}

WWDirectComputeUser::~WWDirectComputeUser(void)
{
    assert(nullptr == m_pDevice);
    assert(nullptr == m_pContext);
}

HRESULT
WWDirectComputeUser::Init(void)
{
    HRESULT hr = S_OK;

    HRG(CreateComputeDevice());

end:
    return hr;
}

void
WWDirectComputeUser::Term(void)
{
    SafeRelease( &m_pContext );
    SafeRelease( &m_pDevice );

    for (auto ite=m_rwGpuBufInfo.begin(); ite != m_rwGpuBufInfo.end(); ++ite) {
        SAFE_RELEASE(ite->second.pUav);
        SAFE_RELEASE(ite->second.pBuf);
    }
    m_rwGpuBufInfo.clear();

    for (auto ite=m_readGpuBufInfo.begin(); ite != m_readGpuBufInfo.end(); ++ite) {
        SAFE_RELEASE(ite->second.pSrv);
        SAFE_RELEASE(ite->second.pBuf);
    }
    m_readGpuBufInfo.clear();

    for (auto ite=m_computeShaderList.begin(); ite != m_computeShaderList.end(); ++ite) {
        auto * p = *ite;
        SAFE_RELEASE(p);
    }
    m_computeShaderList.clear();
}

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

    // 複数回デバイスを作成した場合にD3D11がありませんというエラーを１回だけ出すようにするフラグ。
    static bool bMessageAlreadyShown = false;

    HMODULE hModD3D11 = LoadLibrary( L"d3d11.dll" );
    if ( hModD3D11 == nullptr ) {
        // D3D11がない。

        if ( !bMessageAlreadyShown ) {
            OSVERSIONINFOEX osv;
            memset( &osv, 0, sizeof(osv) );
            osv.dwOSVersionInfoSize = sizeof(osv);
            GetVersionEx( (LPOSVERSIONINFO)&osv );

            if ( ( osv.dwMajorVersion > 6 )
                || ( osv.dwMajorVersion == 6 && osv.dwMinorVersion >= 1 ) 
                || ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 && osv.dwBuildNumber > 6002 ) ) {
                MessageBox(0,
                    L"エラー: Direct3D 11 コンポーネントが見つかりませんでした。",
                    L"Error",
                    MB_ICONEXCLAMATION );
                // This should not happen, but is here for completeness as the system could be
                // corrupted or some future OS version could pull D3D11.DLL for some reason
            } else if ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 && osv.dwBuildNumber == 6002 ) {
                MessageBox(0,
                    L"エラー: Direct3D 11 コンポーネントが見つかりませんでしたが、"
                    L"このWindows用のDirect3D 11 コンポーネントは入手可能です。\n"
                    L"マイクロソフトKB #971644をご覧ください。\n"
                    L" http://support.microsoft.com/default.aspx/kb/971644/",
                    L"Error", MB_ICONEXCLAMATION );
            } else if ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 ) {
                MessageBox(0,
                    L"エラー: Direct3D 11 コンポーネントが見つかりませんでした。"
                    L"最新のサービスパックを適用してください。\n"
                    L"詳しくはマイクロソフトKB #935791をご覧ください。\n"
                    L" http://support.microsoft.com/default.aspx/kb/935791",
                    L"Error", MB_ICONEXCLAMATION );
            } else {
                MessageBox(0,
                    L"エラー: このバージョンのWindows向けのDirect3D 11 はありません。",
                    L"Error", MB_ICONEXCLAMATION);
            }

            bMessageAlreadyShown = true;
        }

        hr = E_FAIL;
        goto end;
    }

    // D3D11デバイスが存在する場合。

    typedef HRESULT (WINAPI * LPD3D11CREATEDEVICE)(
        IDXGIAdapter*, D3D_DRIVER_TYPE, HMODULE, UINT32,
        CONST D3D_FEATURE_LEVEL*, UINT, UINT32, ID3D11Device**,
        D3D_FEATURE_LEVEL*, ID3D11DeviceContext** );

    LPD3D11CREATEDEVICE pDynamicD3D11CreateDevice =
        (LPD3D11CREATEDEVICE)GetProcAddress( hModD3D11, "D3D11CreateDevice" );

    HRG(pDynamicD3D11CreateDevice(
        pAdapter, DriverType, Software, Flags, pFeatureLevels, FeatureLevels,
        SDKVersion, ppDevice, pFeatureLevel, ppImmediateContext));

    assert(*ppDevice);
    assert(*ppImmediateContext);

    // A hardware accelerated device has been created, so check for Compute Shader support.
    // If we have a device >= D3D_FEATURE_LEVEL_11_0 created,
    // full CS5.0 support is guaranteed, no need for further checks.

    // Double-precision support is an optional feature of CS 5.0.
    D3D11_FEATURE_DATA_DOUBLES hwopts;
    (*ppDevice)->CheckFeatureSupport( D3D11_FEATURE_DOUBLES, &hwopts, sizeof(hwopts) );
    if ( !hwopts.DoublePrecisionFloatShaderOps ) {
        if ( !bMessageAlreadyShown ) {
            MessageBox(0,
                L"エラー: このGPUはComputeShader5.0の倍精度浮動小数点数オプション"
                L"(double-precision support)が使用できません。",
                L"Error", MB_ICONEXCLAMATION);
            bMessageAlreadyShown = true;
        }
        hr = E_FAIL;
        goto end;
    }

end:
    return hr;
}

HRESULT
WWDirectComputeUser::CreateComputeDevice(void)
{
    HRESULT hr = S_OK;

    assert(nullptr == m_pDevice);
    assert(nullptr == m_pContext);
    
    UINT uCreationFlags = D3D11_CREATE_DEVICE_SINGLETHREADED;
//#if defined(DEBUG) || defined(_DEBUG)
//    uCreationFlags |= D3D11_CREATE_DEVICE_DEBUG;
//#endif

    D3D_FEATURE_LEVEL flOut;
    static const D3D_FEATURE_LEVEL flvl[] = { D3D_FEATURE_LEVEL_11_0 };
    
    HRG(CreateDeviceInternal(
        nullptr,                        // Use default graphics card
        D3D_DRIVER_TYPE_HARDWARE,    // Try to create a hardware accelerated device
        nullptr,                        // Do not use external software rasterizer module
        uCreationFlags,              // Device creation flags
        flvl,
        sizeof(flvl) / sizeof(D3D_FEATURE_LEVEL),
        D3D11_SDK_VERSION,           // SDK version
        &m_pDevice,                  // Device out
        &flOut,                      // Actual feature level created
        &m_pContext));

    assert(flOut == D3D_FEATURE_LEVEL_11_0);
end:
    return hr;
}

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

    assert(m_pDevice);

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
        WCHAR erStr[65536];
        ZeroMemory(erStr, sizeof erStr);

        if (pErrorBlob) {
            const char *s = (const char *)pErrorBlob->GetBufferPointer();
            MultiByteToWideChar(CP_ACP, 0, s, -1,
                erStr, sizeof erStr/sizeof erStr[0]-1);
        }
        MessageBox(0, erStr, L"D3DX11CompileFromFile失敗", MB_ICONEXCLAMATION);
        goto end;
    }

    assert(pBlob);

    hr = m_pDevice->CreateComputeShader(
        pBlob->GetBufferPointer(), pBlob->GetBufferSize(), nullptr, ppCS);
    if (SUCCEEDED(hr) && *ppCS) {
        m_computeShaderList.push_back(*ppCS);
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
    auto ite = std::find(m_computeShaderList.begin(), m_computeShaderList.end(), pCS);
    if (ite != m_computeShaderList.end()) {
        m_computeShaderList.erase(ite);
    }

    SAFE_RELEASE(pCS);
}

HRESULT
WWDirectComputeUser::CreateStructuredBuffer(
        unsigned int uElementSize,
        unsigned int uCount,
        void * pInitData,
        const char *name,
        ID3D11Buffer ** ppBufOut)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
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

        hr = m_pDevice->CreateBuffer(&desc, &initData, ppBufOut);
    } else {
        hr = m_pDevice->CreateBuffer(&desc, nullptr, ppBufOut);
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

void
WWDirectComputeUser::DestroyConstantBuffer(ID3D11Buffer * pBuf)
{
    SAFE_RELEASE(pBuf);
}

HRESULT
WWDirectComputeUser::CreateBufferShaderResourceView(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
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

    HRG(m_pDevice->CreateShaderResourceView(pBuffer, &desc, ppSrvOut));

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
WWDirectComputeUser::CreateBufferUnorderedAccessView(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
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
    
    HRG(m_pDevice->CreateUnorderedAccessView(pBuffer, &desc, ppUavOut));

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
WWDirectComputeUser::SendReadOnlyDataAndCreateShaderResourceView(
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

    HRG(CreateBufferShaderResourceView(pBuf, name, ppSrv));
    assert(*ppSrv);

    WWReadOnlyGpuBufferInfo info;
    info.pBuf = pBuf;
    info.pSrv = *ppSrv;

    m_readGpuBufInfo[info.pSrv] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pBuf);
    }

    return hr;
}

void
WWDirectComputeUser::DestroyDataAndShaderResourceView(
        ID3D11ShaderResourceView *pSrv)
{
    std::map<ID3D11ShaderResourceView *, WWReadOnlyGpuBufferInfo>::iterator
        ite = m_readGpuBufInfo.find(pSrv);
    if (ite == m_readGpuBufInfo.end()) {
        return;
    }

    SAFE_RELEASE(ite->second.pSrv);
    SAFE_RELEASE(ite->second.pBuf);

    m_readGpuBufInfo.erase(pSrv);
}

HRESULT
WWDirectComputeUser::CreateBufferAndUnorderedAccessView(
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

    HRG(CreateBufferUnorderedAccessView(pBuf, name, ppUav));
    assert(*ppUav);

    WWReadWriteGpuBufferInfo info;
    info.pBuf = pBuf;
    info.pUav = *ppUav;

    m_rwGpuBufInfo[info.pUav] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pBuf);
    }

    return hr;
}

void
WWDirectComputeUser::DestroyDataAndUnorderedAccessView(
        ID3D11UnorderedAccessView * pUav)
{
    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo>::iterator
        ite = m_rwGpuBufInfo.find(pUav);
    if (ite == m_rwGpuBufInfo.end()) {
        return;
    }

    SAFE_RELEASE(ite->second.pUav);
    SAFE_RELEASE(ite->second.pBuf);

    m_rwGpuBufInfo.erase(pUav);
}

HRESULT
WWDirectComputeUser::RecvResultToCpuMemory(
        ID3D11UnorderedAccessView * pUav,
        void *dest,
        int bytes)
{
    HRESULT hr = S_OK;
    D3D11_MAPPED_SUBRESOURCE mr;
    D3D11_RESOURCE_DIMENSION dimen;

    assert(m_pDevice);
    assert(m_pContext);
    assert(pUav);
    assert(dest);

    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo>::iterator
        ite = m_rwGpuBufInfo.find(pUav);
    if (ite == m_rwGpuBufInfo.end()) {
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
            HRG(m_pDevice->CreateBuffer(&desc, nullptr, &pReturn));

            m_pContext->CopyResource(pReturn, pBuffer);
            assert(pReturn);

            ZeroMemory(&mr, sizeof mr);
            HRG(m_pContext->Map(pReturn, 0, D3D11_MAP_READ, 0, &mr));
            assert(mr.pData);
            // Unmapしないでgoto endしてはいけない

            memcpy(dest, mr.pData, bytes);

            m_pContext->Unmap(pReturn, 0);
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

            HRG(m_pDevice->CreateTexture1D(&desc, nullptr, &pReturn));
            assert(pReturn);
            
            m_pContext->CopyResource(pReturn, pTex);
            assert(pReturn);

            ZeroMemory(&mr, sizeof mr);
            HRG(m_pContext->Map(pReturn, 0, D3D11_MAP_READ, 0, &mr));
            assert(mr.pData);
            // Unmapしないでgoto endしてはいけない

            memcpy(dest, mr.pData, bytes);

            m_pContext->Unmap(pReturn, 0);
            SafeRelease(&pReturn);
        }
    }

end:
    return hr;
}

HRESULT
WWDirectComputeUser::SetupDispatch(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumSRV,
        ID3D11ShaderResourceView * ppSRV[],
        UINT nNumUAV,
        ID3D11UnorderedAccessView * ppUAV[])
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
    assert(m_pContext);
    assert(pComputeShader);

    // シェーダーとパラメータをセットする。

    m_pContext->CSSetShader(pComputeShader, nullptr, 0);
    if (0 < nNumSRV) {
        m_pContext->CSSetShaderResources(0, nNumSRV, ppSRV);
    }
    if (0 < nNumUAV) {
        m_pContext->CSSetUnorderedAccessViews(0, nNumUAV, ppUAV, nullptr);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::Dispatch(
        ID3D11Buffer * pCBCS,
        void * pCSData,
        DWORD dwNumDataBytes,
        UINT X,
        UINT Y,
        UINT Z)
{
    HRESULT hr = S_OK;
    D3D11_MAPPED_SUBRESOURCE mr;

    assert(0 < X);
    assert(0 < Y);
    assert(0 < Z);

    assert(m_pContext);

    if (pCBCS) {
        assert(pCSData);
        ZeroMemory(&mr, sizeof mr);

        HRG(m_pContext->Map(pCBCS, 0, D3D11_MAP_WRITE_DISCARD, 0, &mr));
        assert(mr.pData);

        memcpy(mr.pData, pCSData, dwNumDataBytes);
        m_pContext->Unmap(pCBCS, 0);

        ID3D11Buffer* ppCB[1] = { pCBCS };
        m_pContext->CSSetConstantBuffers(0, 1, ppCB);
    }

    m_pContext->Dispatch(X, Y, Z);

end:
    return hr;
}

void
WWDirectComputeUser::UnsetupDispatch(void)
{
    assert(m_pContext);

    m_pContext->CSSetShader(nullptr, nullptr, 0);

    ID3D11UnorderedAccessView * ppUAViewNULL[1] = { nullptr };
    m_pContext->CSSetUnorderedAccessViews(0, 1, ppUAViewNULL, nullptr);

    ID3D11ShaderResourceView * ppSRVNULL[2] = { nullptr, nullptr };
    m_pContext->CSSetShaderResources(0, 2, ppSRVNULL);

    ID3D11Buffer * ppCBNULL[1] = { nullptr };
    m_pContext->CSSetConstantBuffers(0, 1, ppCBNULL);
}

HRESULT
WWDirectComputeUser::Run(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumSRV,
        ID3D11ShaderResourceView * ppSRV[],
        UINT nNumUAV,
        ID3D11UnorderedAccessView * ppUAV[],
        ID3D11Buffer * pCBCS,
        void * pCSData,
        DWORD dwNumDataBytes,
        UINT X,
        UINT Y,
        UINT Z)
{
    HRESULT hr = S_OK;
    bool result = true;

    HRG(SetupDispatch(
        pComputeShader, nNumSRV, ppSRV, nNumUAV, ppUAV));

    // 実行する。
    HRGR(Dispatch(
        pCBCS, pCSData, dwNumDataBytes,
        X, Y, Z));

end:
    UnsetupDispatch();

    return hr;
}

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
    assert(m_pDevice);

    D3D11_BUFFER_DESC desc;
    ZeroMemory(&desc, sizeof desc);

    desc.BindFlags      = D3D11_BIND_CONSTANT_BUFFER;
    desc.Usage          = D3D11_USAGE_DYNAMIC;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
    desc.MiscFlags      = 0;
    desc.ByteWidth      = uElementSize * uCount;
    desc.StructureByteStride = 0;

    HRG(m_pDevice->CreateBuffer(&desc, nullptr, ppBufOut));

end:
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// Texture1D

HRESULT
WWDirectComputeUser::CreateTexture1DShaderResourceView(
        ID3D11Texture1D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
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

    HRG(m_pDevice->CreateShaderResourceView(pTex, &desc, ppSrvOut));

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
WWDirectComputeUser::CreateTexture1DUnorderedAccessView(
        ID3D11Texture1D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
    assert(pTex);
    assert(name);
    assert(ppUavOut);
    *ppUavOut = nullptr;

    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    ZeroMemory( &desc, sizeof desc);
    desc.Format = format;
    desc.ViewDimension = D3D11_UAV_DIMENSION_TEXTURE1D;
    desc.Texture1D.MipSlice = 0;

    HRG(m_pDevice->CreateUnorderedAccessView(pTex, &desc, ppUavOut));

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

    D3D11_SUBRESOURCE_DATA srd;
    memset(&srd, 0, sizeof srd);
    srd.pSysMem = pInitialData;
    srd.SysMemPitch = 0;

    HRG(m_pDevice->CreateTexture1D(&desc, &srd, ppTexOut));

end:
    return hr;
}

void
WWDirectComputeUser::DestroyTexture1D(
        ID3D11Texture1D *pTex)
{
    if (pTex == nullptr) {
        return;
    }

    pTex->Release();
    pTex = nullptr; //< あまり意味ない。
    return;
}

HRESULT
WWDirectComputeUser::CreateTexture1DAndShaderResourceView(
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

    HRG(CreateTexture1DShaderResourceView(pTex, format, name, ppSrv));
    assert(*ppSrv);

    WWReadOnlyGpuBufferInfo info;
    info.pBuf = pTex;
    info.pSrv = *ppSrv;

    m_readGpuBufInfo[info.pSrv] = info;

end:
    if (FAILED(hr)) {
        SafeRelease(&pTex);
    }

    return hr;
}

HRESULT
WWDirectComputeUser::CreateTexture1DAndUnorderedAccessView(
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

    HRG(CreateTexture1DUnorderedAccessView(pTex, format, name, ppUav));
    assert(*ppUav);

    WWReadWriteGpuBufferInfo info;
    info.pBuf = pTex;
    info.pUav = *ppUav;

    m_rwGpuBufInfo[info.pUav] = info;

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
            hr = CreateTexture1DAndShaderResourceView(p.Width, p.MipLevels, p.ArraySize, p.Format, p.Usage, p.BindFlags, p.CPUAccessFlags,
                p.MiscFlags, p.data, p.dataCount, p.name, p.pSrv);
        } else if (p.pUav != nullptr) {
            hr = CreateTexture1DAndUnorderedAccessView(p.Width, p.MipLevels, p.ArraySize, p.Format, p.Usage, p.BindFlags, p.CPUAccessFlags,
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

