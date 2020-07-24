// 日本語。

// 参考: D3D12nBodyGravity.cpp
// 参考: DirectML-master\Samples\HelloDirectML
// 参考: Samples/Desktop/D3D12ExecuteIndirect

#include "WWDirectCompute12User.h"
#include "WWDCUtil.h"
#include <d3dcompiler.h>
#include <assert.h>
#include <d3d12.h>
#include <algorithm>
#include <d3d12.h>
#include <dxgi1_6.h>
#include <dxgidebug.h>
#include <DirectXMath.h>

// D3D12オブジェクトにデバッグ用の名前を付ける。
#define NAME_D3D12_OBJECT(x) SetName((x).Get(), L#x)

#if defined(_DEBUG) || defined(DBG)
static void SetName(ID3D12Object* pObject, LPCWSTR name)
{
    // エラーが出ても続行。
    pObject->SetName(name);
}

static void SetNameIndexed(ID3D12Object* pObject, LPCWSTR name, UINT index)
{
    WCHAR fullName[50];
    if (swprintf_s(fullName, L"%s[%u]", name, index) > 0) {
        // エラーが出ても続行。
        pObject->SetName(fullName);
    }
}
#else
static void SetName(ID3D12Object*, LPCWSTR)
{
}

static void SetNameIndexed(ID3D12Object*, LPCWSTR, UINT)
{
}
#endif

static void
GetAssetsPath(_Out_writes_(pathSize) WCHAR* path, UINT pathSize)
{
    if (path == nullptr) {
        throw std::exception();
    }

    DWORD size = GetModuleFileName(nullptr, path, pathSize);
    if (size == 0 || size == pathSize) {
        throw std::exception();
    }

    WCHAR* lastSlash = wcsrchr(path, L'\\');
    if (lastSlash) {
        *(lastSlash + 1) = L'\0';
    }
}

std::wstring
WWDxgiAdapterFlagsToStr(uint32_t f)
{
    std::wstring s;

    if (f & DXGI_ADAPTER_FLAG_REMOTE) {
        s += L"FLAG_REMOTE";
    }
    if (f & DXGI_ADAPTER_FLAG_SOFTWARE) {
        if (0 < s.size()) {
            s += L"|";
        }
        s += L"FLAG_SOFTWARE";
    }

    if (s.size() == 0) {
        s = L"FLAG_NONE";
    }

    return s;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

WWDirectCompute12User::WWDirectCompute12User(void)
{
    WCHAR assetsPath[512];
    GetAssetsPath(assetsPath, _countof(assetsPath));
    mAssetsPath = assetsPath;
}

WWDirectCompute12User::~WWDirectCompute12User(void)
{
}

std::wstring
WWDirectCompute12User::GetAssetFullPath(LPCWSTR assetName)
{
    return mAssetsPath + assetName;
}

static const char *
D3DFeatureLevelToStr(D3D_FEATURE_LEVEL v)
{
    switch (v) {
    case D3D_FEATURE_LEVEL_1_0_CORE: return "1.0";
    case D3D_FEATURE_LEVEL_9_1: return "9.1";
    case D3D_FEATURE_LEVEL_9_2: return "9.2";
    case D3D_FEATURE_LEVEL_9_3: return "9.3";
    case D3D_FEATURE_LEVEL_10_0: return "10.0";
    case D3D_FEATURE_LEVEL_10_1: return "10.1";
    case D3D_FEATURE_LEVEL_11_0: return "11.0";
    case D3D_FEATURE_LEVEL_11_1: return "11.1";
    case D3D_FEATURE_LEVEL_12_0: return "12.0";
    case D3D_FEATURE_LEVEL_12_1: return "12.1";
    default:
        assert(0);
        return "";
    }
}

static void
GetHardwareAdapter(int gpuNr, D3D_FEATURE_LEVEL d3dFeatureLv, IDXGIFactory2* pFactory, IDXGIAdapter1** ppAdapter)
{
    ComPtr<IDXGIAdapter1> adapter;
    *ppAdapter = nullptr;
    UINT i = 0;

    //printf("Finding Direct3D Feature Level %s hardware adapter...\n", D3DFeatureLevelToStr(d3dFeatureLv));

    for (i=0; DXGI_ERROR_NOT_FOUND != pFactory->EnumAdapters1(i, &adapter); ++i) {
        DXGI_ADAPTER_DESC1 desc;
        adapter->GetDesc1(&desc);

        //printf("Adapter#%u: Video memory=%lldMB, %S : ", i, desc.DedicatedVideoMemory / 1024 / 1024, desc.Description);

        if (SUCCEEDED(D3D12CreateDevice(adapter.Get(), d3dFeatureLv, _uuidof(ID3D12Device), nullptr))) {
            // printf("OK\n");
            if (0 <= gpuNr) {
                if (gpuNr == i) {
                    break;
                }
            } else {
                break;
            }
        } else {
            //printf("NA\n");
        }
    }

    //printf("Use Adapter#%u.\n", i);
    *ppAdapter = adapter.Detach();
}

HRESULT
WWDirectCompute12User::Init(void)
{
    HRESULT hr = S_OK;
    UINT dxgiFactoryFlags = 0;

    mActiveAdapter = -1;

#ifdef _DEBUG
    {
        ComPtr<ID3D12Debug> debugController;
        if (SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debugController)))) {
            debugController->EnableDebugLayer();

            // Enable additional debug layers.
            dxgiFactoryFlags |= DXGI_CREATE_FACTORY_DEBUG;
        }
    }
#endif

    HRG(CreateDXGIFactory2(dxgiFactoryFlags, IID_PPV_ARGS(&mDxgiFactory)));

    {
        ComPtr<IDXGIFactory7> spDxgiFactory7;
        if (SUCCEEDED(mDxgiFactory->QueryInterface(IID_PPV_ARGS(&spDxgiFactory7)))) {
            mAdapterChangeEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
            if (mAdapterChangeEvent == nullptr) {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto end;
            }
            HRG(spDxgiFactory7->RegisterAdaptersChangedEvent(mAdapterChangeEvent, &mAdapterChangeRegistrationCookie));
        }
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::EnumGpuAdapters(void)
{
    HRESULT hr = S_OK;
    ComPtr<IDXGIAdapter1> adapter;
    mGpuAdapterDescs.clear();

    mBestAdapter = -1;

    //printf("Finding Direct3D Feature Level %s hardware adapter...\n", D3DFeatureLevelToStr(mD3dFeatureLv));

    for (UINT i = 0; DXGI_ERROR_NOT_FOUND != mDxgiFactory->EnumAdapterByGpuPreference(
            i, mActiveGpuPreference, IID_PPV_ARGS(&adapter)); ++i) {
        DxgiAdapterInfo ai;
        HRG(adapter->GetDesc1(&ai.desc));
        ai.supportsFeatureLv = SUCCEEDED(D3D12CreateDevice(adapter.Get(), mD3dFeatureLv, _uuidof(ID3D12Device), nullptr));

        if (mBestAdapter < 0 && ai.supportsFeatureLv) {
            mBestAdapter = i;
        }

        mGpuAdapterDescs.push_back(std::move(ai));
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::GetNthAdapterInf(int nth, WWDirectCompute12AdapterInf& adap_out)
{
    if (nth < 0 || mGpuAdapterDescs.size() <= nth) {
        return E_INVALIDARG;
    }

    ZeroMemory(&adap_out, sizeof adap_out);

    auto& a = mGpuAdapterDescs[nth];

    adap_out.idx = nth;
    adap_out.supportsFeatureLv = a.supportsFeatureLv;
    adap_out.dedicatedSystemMemoryMiB = (int)(a.desc.DedicatedSystemMemory / 1024 / 1024);
    adap_out.dedicatedVideoMemoryMiB = (int)(a.desc.DedicatedVideoMemory / 1024 / 1024);
    adap_out.sharedSystemMemoryMiB = (int)(a.desc.SharedSystemMemory / 1024 / 1024);
    adap_out.dxgiAdapterFlags = a.desc.Flags;
    wcsncpy_s(adap_out.name, a.desc.Description, ARRAYSIZE(adap_out.name) - 1);
    return S_OK;
}

HRESULT
WWDirectCompute12User::CreateGpuAdapter(IDXGIAdapter1** ppAdapter)
{
    HRESULT hr = S_OK;
    ComPtr<IDXGIAdapter1> adapter;
    *ppAdapter = nullptr;

    if (mDxgiFactory->EnumAdapterByGpuPreference(
            mActiveAdapter, mActiveGpuPreference,
            IID_PPV_ARGS(&adapter)) != DXGI_ERROR_NOT_FOUND) {
        HRG(D3D12CreateDevice(adapter.Get(), mD3dFeatureLv, _uuidof(ID3D12Device), nullptr));
        *ppAdapter = adapter.Detach();
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::ChooseAdapter(int useGpuId)
{
    HRESULT hr = S_OK;
    auto clType = D3D12_COMMAND_LIST_TYPE_COMPUTE;

    mActiveAdapter = useGpuId;

    assert(mDxgiFactory.Get());

    if (mBestAdapter < 0) {
        printf("Error: No %s device is found.\n", D3DFeatureLevelToStr(mD3dFeatureLv));
        hr = E_FAIL;
        goto end;
    }

    if (mActiveAdapter < 0) {
        mActiveAdapter = mBestAdapter;
    }

    dprintf("D: WWDirectCompute12User::ChooseAdapter(%d) Use Adapter#%u.\n", useGpuId, mActiveAdapter);

    if (!mGpuAdapterDescs[mActiveAdapter].supportsFeatureLv) {
        printf("Error: Adapter %d does not support %s.\n", mActiveAdapter, D3DFeatureLevelToStr(mD3dFeatureLv));
        hr = E_FAIL;
        goto end;
    }

    {
        ComPtr<IDXGIAdapter1> hardwareAdapter;

        HRG(CreateGpuAdapter(&hardwareAdapter));
        HRG(D3D12CreateDevice(hardwareAdapter.Get(), mD3dFeatureLv, IID_PPV_ARGS(&mDevice)));
        mActiveAdapterLuid = mGpuAdapterDescs[mActiveAdapter].desc.AdapterLuid;
    }

    {   // Double型がシェーダーで使えることを確認する必要がある。
        // https://docs.microsoft.com/en-us/windows/win32/api/d3d12/ns-d3d12-d3d12_feature_data_d3d12_options

        D3D12_FEATURE_DATA_D3D12_OPTIONS opt = {};
        HRG(mDevice->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &opt, sizeof opt));
        if (!opt.DoublePrecisionFloatShaderOps) {
            printf("Error: This GPU does not have double-precision support.");
            hr = E_FAIL;
            goto end;
        }
    }

    {
        D3D12_COMMAND_QUEUE_DESC queueDesc = { clType, 0, D3D12_COMMAND_QUEUE_FLAG_NONE };
        HRG(mDevice->CreateCommandQueue(&queueDesc, IID_PPV_ARGS(&mCQueue)));
        NAME_D3D12_OBJECT(mCQueue);
    }

    HRG(mDevice->CreateCommandAllocator(clType, IID_PPV_ARGS(&mCAllocator)));
    NAME_D3D12_OBJECT(mCAllocator);

    HRG(mDevice->CreateCommandList(0, clType, mCAllocator.Get(), nullptr, IID_PPV_ARGS(&mCList)));
    NAME_D3D12_OBJECT(mCList);

end:
    return hr;
}

void
WWDirectCompute12User::Term(void)
{
    HRESULT hr = S_OK;

    mCList.Reset();
    mCAllocator.Reset();
    mCQueue.Reset();
    mDevice.Reset();
    mGpuAdapterDescs.clear();

    {
        ComPtr<IDXGIFactory7> spDxgiFactory7;
        if (mAdapterChangeRegistrationCookie != 0 && SUCCEEDED(mDxgiFactory->QueryInterface(IID_PPV_ARGS(&spDxgiFactory7)))) {
            HRG(spDxgiFactory7->UnregisterAdaptersChangedEvent(mAdapterChangeRegistrationCookie));
            mAdapterChangeRegistrationCookie = 0;
            CloseHandle(mAdapterChangeEvent);
            mAdapterChangeEvent = nullptr;
        }
    }

    mDxgiFactory.Reset();

end:
    
#if defined(_DEBUG)
    {
        ComPtr<IDXGIDebug1> dxgiDebug;
        if (SUCCEEDED(DXGIGetDebugInterface1(0, IID_PPV_ARGS(&dxgiDebug)))) {
            dxgiDebug->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_FLAGS(DXGI_DEBUG_RLO_SUMMARY | DXGI_DEBUG_RLO_IGNORE_INTERNAL));
        }
    }
#endif
    return;
}

HRESULT
WWDirectCompute12User::CreateShader(
        LPCWSTR path,
        LPCSTR entryPoint,
        LPCSTR csVersion,
        const D3D_SHADER_MACRO *defines,
        WWShader &cs_out)
{
    HRESULT hr = S_OK;
    assert(mDevice.Get());
    ComPtr<ID3DBlob> errors;

#if defined(_DEBUG)
    UINT compileFlags = D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION;
#else
    UINT compileFlags = 0;
#endif

    hr = D3DCompileFromFile(GetAssetFullPath(path).c_str(), defines, nullptr,
            entryPoint, csVersion, compileFlags, 0, &cs_out.shader, &errors);
    if (FAILED(hr)) {
        if (errors != nullptr) {
            const char* s = (const char*)errors->GetBufferPointer();
            printf("Error: D3DCompileFromFile failed. %s\n", s);
        }
        goto end;
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::CreateConstantBuffer(
        unsigned int bytes,
        WWConstantBuffer& cBuf_out)
{
    HRESULT hr = S_OK;

    cBuf_out.bytes = 0;

    assert(mDevice.Get());

    assert(nullptr == cBuf_out.cBufUpload.Get());

    // DX11では、bytesは16の倍数である必要があった。
    // DX12ではどうなのだろうか。
    assert((bytes % 16) == 0);

    HRG(mDevice->CreateCommittedResource(
        &CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
        D3D12_HEAP_FLAG_NONE,
        &CD3DX12_RESOURCE_DESC::Buffer(bytes),
        D3D12_RESOURCE_STATE_GENERIC_READ,
        nullptr,
        IID_PPV_ARGS(&cBuf_out.cBufUpload)));
    NAME_D3D12_OBJECT(cBuf_out.cBufUpload);

    cBuf_out.bytes = bytes;

end:
    return hr;
}

HRESULT
WWDirectCompute12User::UpdateConstantBufferData(
    WWConstantBuffer& cBuf,
    const void * data)
{
    HRESULT hr = S_OK;
    void* p = nullptr;

    assert(mDevice.Get());
    assert(nullptr != cBuf.cBufUpload.Get());
    assert(data);
    assert(0 < cBuf.bytes);

    // 書き込むバッファなので読み出し範囲無し。
    CD3DX12_RANGE readRange(0, 0);
    HRG(cBuf.cBufUpload->Map(0, &readRange, &p));
    memcpy(p, data, cBuf.bytes);

end:
    if (p) {
        cBuf.cBufUpload->Unmap(0, nullptr);
        p = nullptr;
    }

    return hr;
}

HRESULT
WWDirectCompute12User::CloseExecResetWait(void)
{
    HRESULT hr = S_OK;
    ComPtr<ID3D12Fence> fence;

    ID3D12CommandList* ppCommandLists[] = { mCList.Get() };

    HANDLE fenceEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (fenceEvent == nullptr) {
        hr = E_FAIL;
        goto end;
    }

    HRG(mCList->Close());

    mCQueue->ExecuteCommandLists(_countof(ppCommandLists), ppCommandLists);

    HRG(mDevice->CreateFence(mFenceValue, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&fence)));
    ++mFenceValue;

    HRG(mCQueue->Signal(fence.Get(), mFenceValue));

    HRG(fence->SetEventOnCompletion(mFenceValue, fenceEvent));
    ++mFenceValue; // 念のため。

    WaitForSingleObject(fenceEvent, INFINITE);

    HRG(mCAllocator->Reset());
    HRG(mCList->Reset(mCAllocator.Get(), nullptr));

end:
    if (fenceEvent) {
        CloseHandle(fenceEvent);
        fenceEvent = nullptr;
    }

    return hr;
}

HRESULT
WWDirectCompute12User::CreateSrvUavHeap(
    int numEntries,
    WWSrvUavHeap& heap_out)
{
    HRESULT hr = S_OK;

    assert(mDevice.Get());
    assert(nullptr == heap_out.heap.Get());
    assert(0 == heap_out.entryTypes.size());
    assert(0 < numEntries);

    heap_out.numEntries = numEntries;

    D3D12_DESCRIPTOR_HEAP_DESC srvUavHeapDesc = {};
    srvUavHeapDesc.NumDescriptors = numEntries;
    srvUavHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
    srvUavHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
    HRG(mDevice->CreateDescriptorHeap(&srvUavHeapDesc, IID_PPV_ARGS(&heap_out.heap)));
    NAME_D3D12_OBJECT(heap_out.heap);

    heap_out.srvUavDescSize = mDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

end:
    return hr;
}

HRESULT
WWDirectCompute12User::CreateGpuBufferAndRegisterAsSRV(
    WWSrvUavHeap &suHeap,
    unsigned int elemBytes,
    unsigned int elemCount,
    const void* data,
    WWGpuBuf& gpuBuf_out,
    WWSrv& srv_out)
{
    HRESULT hr = S_OK;

    assert(mDevice.Get());
    assert(nullptr == gpuBuf_out.buf.Get());
    assert(nullptr == gpuBuf_out.upload.Get());

    gpuBuf_out.elemBytes = elemBytes;
    gpuBuf_out.elemCount = elemCount;

    const UINT dataSize = elemBytes * elemCount;

    D3D12_HEAP_PROPERTIES defaultHeapProperties = CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT);
    D3D12_HEAP_PROPERTIES uploadHeapProperties = CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD);
    D3D12_RESOURCE_DESC bufferDesc = CD3DX12_RESOURCE_DESC::Buffer(dataSize, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);
    D3D12_RESOURCE_DESC uploadBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(dataSize);

    D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
    srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
    srvDesc.Format = DXGI_FORMAT_UNKNOWN;
    srvDesc.ViewDimension = D3D12_SRV_DIMENSION_BUFFER;
    srvDesc.Buffer.FirstElement = 0;
    srvDesc.Buffer.NumElements = elemCount;
    srvDesc.Buffer.StructureByteStride = elemBytes;
    srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_NONE;

    D3D12_SUBRESOURCE_DATA srData = {};
    srData.pData = reinterpret_cast<const void*>(data);
    srData.RowPitch = dataSize;
    srData.SlicePitch = srData.RowPitch;

    HRG(mDevice->CreateCommittedResource(
        &defaultHeapProperties,
        D3D12_HEAP_FLAG_NONE,
        &bufferDesc,
        D3D12_RESOURCE_STATE_COPY_DEST, //< 初期データをCPUからコピーするためのステート。
        nullptr,
        IID_PPV_ARGS(&gpuBuf_out.buf)));
    NAME_D3D12_OBJECT(gpuBuf_out.buf);

    HRG(mDevice->CreateCommittedResource(
        &uploadHeapProperties,
        D3D12_HEAP_FLAG_NONE,
        &uploadBufferDesc,
        D3D12_RESOURCE_STATE_GENERIC_READ,
        nullptr,
        IID_PPV_ARGS(&gpuBuf_out.upload)));
    NAME_D3D12_OBJECT(gpuBuf_out.upload);

    // uploadを使用してdataをGPUにコピーするコマンドを追加。
    UpdateSubresources<1>(mCList.Get(), gpuBuf_out.buf.Get(), gpuBuf_out.upload.Get(), 0, 0, 1, &srData);

    // srvの状態をCOPY_DEST状態からシェーダーリソース状態にするコマンドを追加。
    mCList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(gpuBuf_out.buf.Get(),
        D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE));

    {
        srv_out.suHeapIdx = (int)suHeap.entryTypes.size();

        CD3DX12_CPU_DESCRIPTOR_HANDLE srvHandle(suHeap.heap->GetCPUDescriptorHandleForHeapStart(), srv_out.suHeapIdx, suHeap.srvUavDescSize);
        mDevice->CreateShaderResourceView(gpuBuf_out.buf.Get(), &srvDesc, srvHandle);

        suHeap.entryTypes.push_back(WWSrvUavHeap::HET_SRV);
        assert(suHeap.entryTypes.size() <= suHeap.numEntries);
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::CreateGpuBufferAndRegisterAsUAV(
    WWSrvUavHeap& suHeap,
    unsigned int uElementSize,
    unsigned int uCount,
    WWGpuBuf& gpuBuf_out,
    WWUav& uav_out)
{
    HRESULT hr = S_OK;

    assert(mDevice.Get());
    assert(nullptr == gpuBuf_out.buf.Get());

    gpuBuf_out.elemBytes = uElementSize;
    gpuBuf_out.elemCount = uCount;

    const UINT dataSize = uElementSize * uCount;

    D3D12_HEAP_PROPERTIES defaultHeapProperties = CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT);
    D3D12_RESOURCE_DESC bufferDesc = CD3DX12_RESOURCE_DESC::Buffer(dataSize, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);

    D3D12_UNORDERED_ACCESS_VIEW_DESC uavDesc = {};
    uavDesc.Format = DXGI_FORMAT_UNKNOWN;
    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_BUFFER;
    uavDesc.Buffer.FirstElement = 0;
    uavDesc.Buffer.NumElements = uCount;
    uavDesc.Buffer.StructureByteStride = uElementSize;
    uavDesc.Buffer.CounterOffsetInBytes = 0;
    uavDesc.Buffer.Flags = D3D12_BUFFER_UAV_FLAG_NONE;

    HRG(mDevice->CreateCommittedResource(
        &defaultHeapProperties,
        D3D12_HEAP_FLAG_NONE,
        &bufferDesc,
        D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
        nullptr,
        IID_PPV_ARGS(&gpuBuf_out.buf)));
    assert(gpuBuf_out.buf.Get());

    {
        uav_out.suHeapIdx = (int)suHeap.entryTypes.size(); //< increments from zero.

        CD3DX12_CPU_DESCRIPTOR_HANDLE uavHandle(suHeap.heap->GetCPUDescriptorHandleForHeapStart(), uav_out.suHeapIdx, suHeap.srvUavDescSize);
        mDevice->CreateUnorderedAccessView(gpuBuf_out.buf.Get(), nullptr, &uavDesc, uavHandle);

        suHeap.entryTypes.push_back(WWSrvUavHeap::HET_UAV);
        assert(suHeap.entryTypes.size() <= suHeap.numEntries);
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::CreateComputeState(
    WWShader & csShader,
    int useConstBufCount,
    int useSRVCount,
    int useUAVCount,
    WWComputeState& cState_out)
{
    HRESULT hr = S_OK;

    ComPtr<ID3DBlob> signature;
    ComPtr<ID3DBlob> errors;
    D3D12_FEATURE_DATA_ROOT_SIGNATURE featureData = {};
    CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC sDesc;
    CD3DX12_DESCRIPTOR_RANGE1 ranges[2];
    CD3DX12_ROOT_PARAMETER1 rootParams[2];
    D3D12_COMPUTE_PIPELINE_STATE_DESC pDesc = {};

    cState_out.useConstBufCount = useConstBufCount;
    cState_out.useSRVCount = useSRVCount;
    cState_out.useUAVCount = useUAVCount;

    assert(0 <= useConstBufCount && useConstBufCount <= 1);
    assert(0 <= useSRVCount);
    assert(0 <= useUAVCount);

    {
        // create root signature
        int nRange = 0;
        int nParams = 0;

        if (0 < useSRVCount) {
            ranges[nRange++].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, useSRVCount, 0, 0, D3D12_DESCRIPTOR_RANGE_FLAG_DESCRIPTORS_VOLATILE); // シェーダでt0, t1, t2, ...
        }
        if (0 < useUAVCount) {
            ranges[nRange++].Init(D3D12_DESCRIPTOR_RANGE_TYPE_UAV, useUAVCount, 0, 0, D3D12_DESCRIPTOR_RANGE_FLAG_DATA_VOLATILE);        // シェーダでu0, u1, u2, ...
        }

        if (0 < useConstBufCount) {
            rootParams[nParams++].InitAsConstantBufferView(0, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC, D3D12_SHADER_VISIBILITY_ALL);
        }
        if (0 < nRange) {
            // SRVとUAVをまとめて1個のrootParamsにする。
            rootParams[nParams++].InitAsDescriptorTable(nRange, &ranges[0], D3D12_SHADER_VISIBILITY_ALL);
        }

        if (0 == nParams) {
            sDesc.Init_1_1(0, nullptr, 0, nullptr);
        } else {
            sDesc.Init_1_1(nParams, rootParams, 0, nullptr);
            // Init_1_1()はrootParamsのポインタを保持する：この時点でrootParamsのメモリが解放されると問題が起きる。
            // rootParamsのスコープはD3DX12SerializeVersionedRootSignature()まで維持する必要あり。
        }
    }

    {
        featureData.HighestVersion = D3D_ROOT_SIGNATURE_VERSION_1_1;
        if (FAILED(mDevice->CheckFeatureSupport(D3D12_FEATURE_ROOT_SIGNATURE, &featureData, sizeof(featureData)))) {
            featureData.HighestVersion = D3D_ROOT_SIGNATURE_VERSION_1_0;
        }

        if (FAILED(D3DX12SerializeVersionedRootSignature(&sDesc, featureData.HighestVersion, &signature, &errors))) {
            if (errors != nullptr) {
                const char* s = (const char*)errors->GetBufferPointer();
                printf("Error: D3DX12SerializeVersionedRootSignature failed. %s\n", s);
            }
            goto end;
        }

        HRG(mDevice->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(),
                IID_PPV_ARGS(&cState_out.rootSignature)));
        assert(cState_out.rootSignature.Get());
        NAME_D3D12_OBJECT(cState_out.rootSignature);
    }

    {   // create state
        pDesc.pRootSignature = cState_out.rootSignature.Get();
        pDesc.CS = CD3DX12_SHADER_BYTECODE(csShader.shader.Get());

        HRG(mDevice->CreateComputePipelineState(&pDesc, IID_PPV_ARGS(&cState_out.state)));
        NAME_D3D12_OBJECT(cState_out.state);
    }

end:
    return hr;
}

HRESULT
WWDirectCompute12User::Run(WWComputeState& cState, WWConstantBuffer *cBuf, WWSrvUavHeap& suHeap,
    int firstHeapIdx,
    UINT x, UINT y, UINT z)
{
    HRESULT hr = S_OK;
    ID3D12DescriptorHeap* ppHeaps[] = { suHeap.heap.Get() };

    assert(0 <= firstHeapIdx);
    assert(firstHeapIdx < suHeap.entryTypes.size());

    CD3DX12_GPU_DESCRIPTOR_HANDLE firstSrvHandle(suHeap.heap->GetGPUDescriptorHandleForHeapStart(), firstHeapIdx, suHeap.srvUavDescSize);

    /// idx: computeStateの作成の時のrootParamsのidx。
    int idx = 0;

    mCList->SetPipelineState(cState.state.Get());
    mCList->SetComputeRootSignature(cState.rootSignature.Get());

    mCList->SetDescriptorHeaps(_countof(ppHeaps), ppHeaps);

    if (0 < cState.useConstBufCount) {
        assert(cBuf);
        mCList->SetComputeRootConstantBufferView(idx++, cBuf->cBufUpload->GetGPUVirtualAddress());
    }
    mCList->SetComputeRootDescriptorTable(idx++, firstSrvHandle);

    mCList->Dispatch(x,y,z);

    HRG(CloseExecResetWait());

end:
    return hr;
}

HRESULT
WWDirectCompute12User::CopyGpuBufValuesToCpuMemory(WWGpuBuf& gpuBuf, void* to, int toBytes)
{
    HRESULT hr = S_OK;
    CD3DX12_RANGE range(0, toBytes);
    void* p = nullptr;
    ComPtr<ID3D12Resource> readbackBuf;
    const UINT dataBytes = gpuBuf.elemBytes * gpuBuf.elemCount;

    // uavのバッファーはsuHeapに関連付けられているためMapができない。
    // readbackBufに内容をコピーし、readbackBufをMapすることでGPUの計算結果をCPUに持ってくる。

    // GPUの計算中に異常が発生すると、以下の関数呼び出しで失敗が戻ることがある。
    HRG(mDevice->CreateCommittedResource(
        &CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_READBACK),
        D3D12_HEAP_FLAG_NONE,
        &CD3DX12_RESOURCE_DESC::Buffer(dataBytes),
        D3D12_RESOURCE_STATE_COPY_DEST,
        nullptr, IID_PPV_ARGS(&readbackBuf)));

    // uavバッファーの状態をCOPY_SOURCE状態に変更するコマンドを追加。
    // uavバッファーの内容をreadbackBufにコピーするコマンドを追加。
    // uavバッファーの状態をUAV状態に戻すコマンドを追加。
    // 実行。

    mCList->ResourceBarrier(1,&CD3DX12_RESOURCE_BARRIER::Transition(gpuBuf.buf.Get(),
        D3D12_RESOURCE_STATE_UNORDERED_ACCESS, D3D12_RESOURCE_STATE_COPY_SOURCE));

    mCList->CopyResource(readbackBuf.Get(), gpuBuf.buf.Get());

    mCList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(gpuBuf.buf.Get(),
        D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_UNORDERED_ACCESS));

    HRG(CloseExecResetWait());

    // 念のためメモリ領域にタッチする。
    ZeroMemory(to, toBytes);

    HRG(readbackBuf->Map(0, &range, &p));

    memcpy(to, p, toBytes);


end:
    if (p) {
        readbackBuf->Unmap(0, nullptr);
        p = nullptr;
    }

    return hr;
}
