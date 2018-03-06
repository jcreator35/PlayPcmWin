// ���{�� SJIS
// �Q�l�FBasicCompute11.cpp

#include "WWDirectComputeUser.h"
#include "WWUtil.h"
#include <d3dcompiler.h>
#include <assert.h>
#include <d3dx11.h>

WWDirectComputeUser::WWDirectComputeUser(void)
{
    m_pDevice = NULL;
    m_pContext = NULL;
}

WWDirectComputeUser::~WWDirectComputeUser(void)
{
    assert(NULL == m_pDevice);
    assert(NULL == m_pContext);
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

    assert(m_rwGpuBufInfo.size() == 0);
    assert(m_readGpuBufInfo.size() == 0);
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

    *ppDevice           = NULL;
    *ppImmediateContext = NULL;

    // ������f�o�C�X���쐬�����ꍇ��D3D11������܂���Ƃ����G���[���P�񂾂��o���悤�ɂ���t���O�B
    static bool bMessageAlreadyShown = false;

    HMODULE hModD3D11 = LoadLibrary( L"d3d11.dll" );
    if ( hModD3D11 == NULL ) {
        // D3D11���Ȃ��B

        if ( !bMessageAlreadyShown ) {
            OSVERSIONINFOEX osv;
            memset( &osv, 0, sizeof(osv) );
            osv.dwOSVersionInfoSize = sizeof(osv);
            GetVersionEx( (LPOSVERSIONINFO)&osv );

            if ( ( osv.dwMajorVersion > 6 )
                || ( osv.dwMajorVersion == 6 && osv.dwMinorVersion >= 1 ) 
                || ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 && osv.dwBuildNumber > 6002 ) ) {
                MessageBox(0,
                    L"�G���[: Direct3D 11 �R���|�[�l���g��������܂���ł����B",
                    L"Error",
                    MB_ICONEXCLAMATION );
                // This should not happen, but is here for completeness as the system could be
                // corrupted or some future OS version could pull D3D11.DLL for some reason
            } else if ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 && osv.dwBuildNumber == 6002 ) {
                MessageBox(0,
                    L"�G���[: Direct3D 11 �R���|�[�l���g��������܂���ł������A"
                    L"����Windows�p��Direct3D 11 �R���|�[�l���g�͓���\�ł��B\n"
                    L"�}�C�N���\�t�gKB #971644���������������B\n"
                    L" http://support.microsoft.com/default.aspx/kb/971644/",
                    L"Error", MB_ICONEXCLAMATION );
            } else if ( osv.dwMajorVersion == 6 && osv.dwMinorVersion == 0 ) {
                MessageBox(0,
                    L"�G���[: Direct3D 11 �R���|�[�l���g��������܂���ł����B"
                    L"�ŐV�̃T�[�r�X�p�b�N��K�p���Ă��������B\n"
                    L"�ڂ����̓}�C�N���\�t�gKB #935791���������������B\n"
                    L" http://support.microsoft.com/default.aspx/kb/935791",
                    L"Error", MB_ICONEXCLAMATION );
            } else {
                MessageBox(0,
                    L"�G���[: ���̃o�[�W������Windows������Direct3D 11 �͂���܂���B",
                    L"Error", MB_ICONEXCLAMATION);
            }

            bMessageAlreadyShown = true;
        }

        hr = E_FAIL;
        goto end;
    }

    // D3D11�f�o�C�X�����݂���ꍇ�B

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
                L"�G���[: ����GPU��ComputeShader5.0�̔{���x���������_���I�v�V����"
                L"(double-precision support)���g�p�ł��܂���B",
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

    assert(NULL == m_pDevice);
    assert(NULL == m_pContext);
    
    UINT uCreationFlags = D3D11_CREATE_DEVICE_SINGLETHREADED;
#if defined(DEBUG) || defined(_DEBUG)
    uCreationFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

    D3D_FEATURE_LEVEL flOut;
    static const D3D_FEATURE_LEVEL flvl[] = { D3D_FEATURE_LEVEL_11_0 };
    
    HRG(CreateDeviceInternal(
        NULL,                        // Use default graphics card
        D3D_DRIVER_TYPE_HARDWARE,    // Try to create a hardware accelerated device
        NULL,                        // Do not use external software rasterizer module
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
    ID3DBlob * pErrorBlob = NULL;
    ID3DBlob * pBlob      = NULL;

    assert(m_pDevice);

    DWORD dwShaderFlags = D3DCOMPILE_ENABLE_STRICTNESS;
#if defined( DEBUG ) || defined( _DEBUG )
    // D3DCOMPILE_DEBUG�t���O���w�肷��ƁA�V�F�[�_�[�Ƀf�o�b�O���𖄂ߍ��ނ�
    // �œK���͂���邵�ARELEASE�Ɠ����̓�������A���\�������Ȃ��c�炵���B
    dwShaderFlags |= D3DCOMPILE_DEBUG;
#endif

    // CS�V�F�[�_�[�v���t�@�C��5.0���w��B
    LPCSTR pProfile = "cs_5_0";

    hr = D3DX11CompileFromFile(path, defines, NULL, entryPoint, pProfile,
        dwShaderFlags, NULL, NULL, &pBlob, &pErrorBlob, NULL );
    if (FAILED(hr)) {
        WCHAR erStr[256];
        ZeroMemory(erStr, sizeof erStr);

        if (pErrorBlob) {
            const char *s = (const char *)pErrorBlob->GetBufferPointer();
            MultiByteToWideChar(CP_ACP, 0, s, -1,
                erStr, sizeof erStr/sizeof erStr[0]-1);
        }
        MessageBox(0, erStr, L"D3DX11CompileFromFile���s", MB_ICONEXCLAMATION);
        goto end;
    }

    assert(pBlob);

    hr = m_pDevice->CreateComputeShader(
        pBlob->GetBufferPointer(), pBlob->GetBufferSize(), NULL, ppCS);

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
    // name==NULL�ł��B
    // pInitData==NULL�ł��B

    *ppBufOut = NULL;

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
        hr = m_pDevice->CreateBuffer(&desc, NULL, ppBufOut);
    }

    if (FAILED(hr)) {
        goto end;
    }

#   if defined(DEBUG) || defined(PROFILE)
    if (NULL != name) {
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
    *ppUavOut = NULL;

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
    ID3D11Buffer *pBuf = NULL;

    assert(ppSrv);
    *ppSrv = NULL;

    HRG(CreateStructuredBuffer(uElementSize, uCount, pSendData, NULL, &pBuf));
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
    ID3D11Buffer *pBuf = NULL;

    assert(ppUav);
    *ppUav = NULL;

    HRG(CreateStructuredBuffer(uElementSize, uCount, pSendData, NULL, &pBuf));
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
    ID3D11Buffer * pBuffer = NULL;
    ID3D11Buffer * pReturn = NULL;
    D3D11_BUFFER_DESC desc;
    D3D11_MAPPED_SUBRESOURCE mr;

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

    pBuffer = ite->second.pBuf;

    assert(pBuffer);

    ZeroMemory(&desc, sizeof desc);

    pBuffer->GetDesc(&desc);
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    desc.Usage = D3D11_USAGE_STAGING;
    desc.BindFlags = 0;
    desc.MiscFlags = 0;

    HRG(m_pDevice->CreateBuffer(&desc, NULL, &pReturn));

#if defined(DEBUG) || defined(PROFILE)
    if (pReturn) {
        const char *name = "ResultRecv";
        pReturn->SetPrivateData(WKPDID_D3DDebugObjectName, lstrlenA(name), name);
    }
#endif

    m_pContext->CopyResource(pReturn, pBuffer);
    assert(pReturn);

    ZeroMemory(&mr, sizeof mr);
    HRG(m_pContext->Map(pReturn, 0, D3D11_MAP_READ, 0, &mr));
    assert(mr.pData);
    // Unmap���Ȃ���goto end���Ă͂����Ȃ�

    memcpy(dest, mr.pData, bytes);

    m_pContext->Unmap(pReturn, 0);

end:
    SafeRelease(&pReturn);

    return hr;
}

HRESULT
WWDirectComputeUser::SetupDispatch(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumViews,
        ID3D11ShaderResourceView ** pShaderResourceViews,
        ID3D11UnorderedAccessView * pUnorderedAccessView)
{
    HRESULT hr = S_OK;

    assert(m_pDevice);
    assert(m_pContext);
    assert(pComputeShader);
    assert(pShaderResourceViews);
    assert(pUnorderedAccessView);

    // �V�F�[�_�[�ƃp�����[�^���Z�b�g����B

    m_pContext->CSSetShader(pComputeShader, NULL, 0);
    m_pContext->CSSetShaderResources(0, nNumViews, pShaderResourceViews);
    m_pContext->CSSetUnorderedAccessViews(0, 1, &pUnorderedAccessView, NULL);

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
    bool result = true;
    HRESULT hr = S_OK;
    D3D11_MAPPED_SUBRESOURCE mr;

    assert(m_pContext);
    // pCBCS==NULL�ł��B
    // pCSData==NULL�ł��B

    if (pCBCS) {
        ZeroMemory(&mr, sizeof mr);

        HRGR(m_pContext->Map(pCBCS, 0, D3D11_MAP_WRITE_DISCARD, 0, &mr));
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

    m_pContext->CSSetShader(NULL, NULL, 0);

    ID3D11UnorderedAccessView * ppUAViewNULL[1] = { NULL };
    m_pContext->CSSetUnorderedAccessViews(0, 1, ppUAViewNULL, NULL);

    ID3D11ShaderResourceView * ppSRVNULL[2] = { NULL, NULL };
    m_pContext->CSSetShaderResources(0, 2, ppSRVNULL);

    ID3D11Buffer * ppCBNULL[1] = { NULL };
    m_pContext->CSSetConstantBuffers(0, 1, ppCBNULL);
}

HRESULT
WWDirectComputeUser::Run(
        ID3D11ComputeShader * pComputeShader,
        UINT nNumViews,
        ID3D11ShaderResourceView ** pShaderResourceViews,
        ID3D11UnorderedAccessView * pUnorderedAccessView,
        ID3D11Buffer * pCBCS,
        void * pCSData,
        DWORD dwNumDataBytes,
        UINT X,
        UINT Y,
        UINT Z)
{
    HRESULT hr = S_OK;
    bool result = true;

    HRGR(SetupDispatch(
        pComputeShader, nNumViews, pShaderResourceViews,
        pUnorderedAccessView));

    // ���s����B
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

    // uElementSize��16�̔{���łȂ��Ƃ����Ȃ��炵���B
    assert((uElementSize%16) ==0);
    assert(0<uCount);
    assert(ppBufOut);
    *ppBufOut = NULL;
    assert(m_pDevice);

    D3D11_BUFFER_DESC desc;
    ZeroMemory(&desc, sizeof desc);

    desc.BindFlags      = D3D11_BIND_CONSTANT_BUFFER;
    desc.Usage          = D3D11_USAGE_DYNAMIC;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
    desc.MiscFlags      = 0;
    desc.ByteWidth      = uElementSize * uCount;
    desc.StructureByteStride = 0;

    HRG(m_pDevice->CreateBuffer(&desc, NULL, ppBufOut));

end:
    return hr;
}
