// 日本語。

#pragma once

#include "framework.h"
#include <map>
#include <list>
#include <stdint.h>
#include <assert.h>
#include <vector>

#define WW_DIRECTCOMPUTE12_ADAPTER_NAME_COUNT (128)

struct WWConstantBuffer {
    ComPtr<ID3D12Resource> cBufUpload;
    int bytes = 0;

    void Reset(void)
    {
        cBufUpload.Reset();
    }
};


struct WWSrvUavHeap {
    enum HeapEntryType : int {
        HET_SRV,
        HET_UAV,
    };

    ComPtr<ID3D12DescriptorHeap> heap;
    std::vector<HeapEntryType> entryTypes; ///< 例えば {srv,srv,uav,srv,srv,uav}という情報が溜まる。
    int numEntries = 0;
    UINT srvUavDescSize = 0;

    void Reset(void)
    {
        heap.Reset();
        entryTypes.clear();
    }
};

struct WWSrv {
    int suHeapIdx = -1;
};

struct WWUav {
    int suHeapIdx = -1;
};

/// @brief GPUバッファ。SRV、UAV、または両方になる。
struct WWGpuBuf {
    ComPtr<ID3D12Resource> buf;
    ComPtr<ID3D12Resource> upload;
    int elemBytes = 0;
    int elemCount = 0;

    void Reset(void)
    {
        buf.Reset();
        upload.Reset();
    }
};

struct WWShader {
    ComPtr<ID3DBlob> shader;

    void Reset(void)
    {
        shader.Reset();
    }
};

struct WWComputeState {
    ComPtr<ID3D12RootSignature> rootSignature;
    ComPtr<ID3D12PipelineState> state;
    int useConstBufCount = 0;
    int useSRVCount = 0;
    int useUAVCount = 0;

    void Reset(void)
    {
        rootSignature.Reset();
        state.Reset();
    }
};

std::wstring
WWDxgiAdapterFlagsToStr(uint32_t f);

struct WWDirectCompute12AdapterInf {
    int idx;
    int supportsFeatureLv;
    int dedicatedVideoMemoryMiB;
    int dedicatedSystemMemoryMiB;
    int sharedSystemMemoryMiB;

    // DXGI_ADAPTER_FLAG
    uint32_t dxgiAdapterFlags;
    int pad0;
    int pad1;

    wchar_t name[WW_DIRECTCOMPUTE12_ADAPTER_NAME_COUNT];
};

class WWDirectCompute12User {
public:
    WWDirectCompute12User(void);
    ~WWDirectCompute12User(void);

    /// @brief Initialization. with enum adapters available. paired with Term().
    HRESULT Init(void);

    /// @brief Re-enumerate adapters and update adapter list.
    HRESULT EnumGpuAdapters(void);

    int NumOfAdapters(void) const { return (int)mGpuAdapterDescs.size(); }

    HRESULT GetNthAdapterInf(int nth, WWDirectCompute12AdapterInf& adap_out);

    /// @brief choose Direct3d12 Device
    /// @param useGpuId GPU idx to use. -1 to chooose most powerful device available.
    HRESULT ChooseAdapter(int useGpuId);

    void    Term(void);

    // ShaderをコンパイルしてGPUに送る
    HRESULT CreateShader(
        LPCWSTR path,
        LPCSTR entryPoint,
        LPCSTR shaderVersion,
        const D3D_SHADER_MACRO* defines,
        WWShader& s_out);

    HRESULT CreateConstantBuffer(
        unsigned int bytes,
        WWConstantBuffer& cBuf_out);

    HRESULT UpdateConstantBufferData(
        WWConstantBuffer& cBuf,
        const void* data);

    /// @brief SRVとUAVの置き場作成。
    HRESULT CreateSrvUavHeap(
        int numEntries,
        WWSrvUavHeap& heap_out);

    /// @brief SRV作成。CPUは書き込み、GPUは読みだすバッファー。ヒープに登録する。
    HRESULT CreateGpuBufferAndRegisterAsSRV(
        WWSrvUavHeap& suHeap,
        unsigned int elemBytes,
        unsigned int elemCount,
        const void* data,
        WWGpuBuf& gpuBuf_out,
        WWSrv& srv_out);

    /// @brief UAV作成。GPUが書き込み、CPUは読み出すバッファー。ヒープに登録する。
    HRESULT CreateGpuBufferAndRegisterAsUAV(
        WWSrvUavHeap& suHeap,
        unsigned int elemBytes,
        unsigned int elemCount,
        WWGpuBuf& gpuBuf_out,
        WWUav& uav_out);

    HRESULT CreateComputeState(
        WWShader& csShader,
        int shaderUseConstBufCount,
        int shaderUseSRVCount,
        int shaderUseUAVCount,
        WWComputeState& cs_out);

    /// @brief コンピュートシェーダーを実行する。
    /// @param firstHeapIdx suHeapの、コンピュートシェーダーに見せたい最初のエントリー番号。
    /// @param uavIdx suHeapのUAVのエントリー番号。
    HRESULT Run(
        WWComputeState& cState,
        WWConstantBuffer* cBuf,
        WWSrvUavHeap& suHeap,
        int firstHeapIdx,
        UINT x, UINT y, UINT z);

    HRESULT CopyGpuBufValuesToCpuMemory(WWGpuBuf& gpuBuf, void* to, int toBytes);

    /// @brief コマンドリストにたまったコマンドを実行し、完了するまで待つ。
    HRESULT CloseExecResetWait(void);

private:
    ComPtr<ID3D12Device> mDevice;
    ComPtr<IDXGIFactory6> mDxgiFactory;
    ComPtr<ID3D12CommandQueue> mCQueue;
    ComPtr<ID3D12CommandAllocator> mCAllocator;
    ComPtr<ID3D12GraphicsCommandList> mCList;
    UINT64 mFenceValue = 42;
    int mBestAdapter = -1;
    int mActiveAdapter = -1;
    LUID mActiveAdapterLuid = {};
    HANDLE mAdapterChangeEvent = nullptr;
    DWORD mAdapterChangeRegistrationCookie = 0;
    std::wstring mAssetsPath;

    struct DxgiAdapterInfo {
        DXGI_ADAPTER_DESC1 desc;
        bool supportsFeatureLv;
    };
    std::vector<DxgiAdapterInfo> mGpuAdapterDescs;
    DXGI_GPU_PREFERENCE mActiveGpuPreference = DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE;
    D3D_FEATURE_LEVEL mD3dFeatureLv = D3D_FEATURE_LEVEL_11_1;

    std::wstring GetAssetFullPath(LPCWSTR assetName);
    HRESULT CreateGpuAdapter(IDXGIAdapter1** ppAdapter);
};
