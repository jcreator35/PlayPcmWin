// 日本語。

#pragma once

#include "framework.h"
#include <map>
#include <list>
#include <stdint.h>
#include <assert.h>
#include <vector>

struct WWConstantBuffer {
    ComPtr<ID3D12Resource> cBufUpload;
    int bytes = 0;
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
};

struct WWShader {
    ComPtr<ID3DBlob> shader;
};

struct WWComputeState {
    ComPtr<ID3D12RootSignature> rootSignature;
    ComPtr<ID3D12PipelineState> state;
    int useConstBufCount = 0;
    int useSRVCount = 0;
    int useUAVCount = 0;
};

class WWDirectCompute12User {
public:
    WWDirectCompute12User(void);
    ~WWDirectCompute12User(void);

    enum InitFlags {
        DCU2IF_USE_WARP = 1,
    };

    /// @param initFlags DCU2IF_USE_WARP WARP software rasterizer
    HRESULT Init(int initFlags);
    void    Term(void);

    // ShaderをコンパイルしてGPUに送る
    HRESULT CreateShader(
        LPCWSTR path,
        LPCSTR entryPoint,
        LPCSTR shaderVersion,
        const D3D_SHADER_MACRO* defines,
        WWShader & s_out);

    HRESULT CreateConstantBuffer(
        unsigned int bytes,
        WWConstantBuffer& cBuf_out);

    HRESULT UpdateConstantBufferData(
        WWConstantBuffer& cBuf,
        const void* data);

    /// @brief SRVとUAVの置き場作成。
    // ヒープには、SRV,SRV,UAV, SRV,SRV,UAVのように、SRV→UAVの順に並べる事。
    HRESULT CreateSrvUavHeap(
        int numEntries,
        WWSrvUavHeap& heap_out);

    /// @brief SRV作成。CPUは書き込み、GPUは読みだすバッファー。ヒープに登録する。
    HRESULT CreateGpuBufferAndRegisterAsSRV(
        WWSrvUavHeap& suHeap,
        unsigned int elemBytes,
        unsigned int elemCount,
        const void* data,
        WWGpuBuf & gpuBuf_out,
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
    HRESULT Run(WWComputeState &cState, WWConstantBuffer *cBuf, WWSrvUavHeap &suHeap,
        int firstHeapIdx,
        UINT x, UINT y, UINT z);

    HRESULT CopyGpuBufValuesToCpuMemory(WWGpuBuf& gpuBuf, void* to, int toBytes);

private:
    ComPtr<ID3D12Device> mDevice;
    ComPtr<ID3D12CommandQueue> mCQueue;
    ComPtr<ID3D12CommandAllocator> mCAllocator;
    ComPtr<ID3D12GraphicsCommandList> mCList;
    UINT64 mFenceValue = 1;

    std::wstring mAssetsPath;
    std::wstring GetAssetFullPath(LPCWSTR assetName);

    HRESULT CloseExecResetWait(void);
};
