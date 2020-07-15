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

enum WWHeapEntryType : int {
    WWHET_SRV,
    WWHET_UAV,
};

struct WWSrvUavHeap {
    ComPtr<ID3D12DescriptorHeap> heap;
    std::vector<WWHeapEntryType> entryTypes; ///< 例えば {srv,srv,uav,srv,srv,uav}
    int numEntries;
    UINT srvUavDescSize;
};

struct WWSrv {
    ComPtr<ID3D12Resource> srv;
    ComPtr<ID3D12Resource> upload;
};

struct WWUav {
    ComPtr<ID3D12Resource> uav;
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
        WWShader & s_return);

    HRESULT CreateConstantBuffer(
        unsigned int bytes,
        WWConstantBuffer& cBuf_return);

    HRESULT UpdateConstantBufferData(
        WWConstantBuffer& cBuf,
        const void* data);

    /// @brief SRVとUAVの置き場作成。
    // ヒープには、SRV,SRV,UAV, SRV,SRV,UAVのように、SRV→UAVの順に並べる事。
    HRESULT CreateSrvUavHeap(
        int numEntries,
        WWSrvUavHeap& heap_return);

    /// @brief SRV作成。CPUは書き込み、GPUは読みだすバッファー。ヒープに登録する。
    HRESULT CreateRegisterShaderResourceView(
        WWSrvUavHeap& suHeap,
        unsigned int uElementSize,
        unsigned int uCount,
        const void* data,
        WWSrv& srv_return);
    
    /// @brief UAV作成。GPUが書き込み、CPUは読み出すバッファー。ヒープに登録する。
    HRESULT CreateRegisterUnorderedAccessView(
        WWSrvUavHeap& suHeap,
        unsigned int uElementSize,
        unsigned int uCount,
        WWUav& uav_return);

    HRESULT CreateComputeState(
        WWShader& csShader,
        int shaderUseConstBufCount,
        int shaderUseSRVCount,
        int shaderUseUAVCount,
        WWComputeState& cs_return);

    /// @brief コンピュートシェーダーを実行する。
    /// @param firstHeapIdx suHeapの、コンピュートシェーダーに見せたい最初のエントリー番号。
    /// @param uavIdx suHeapのUAVのエントリー番号。
    HRESULT Run(WWComputeState &cState, WWConstantBuffer *cBuf, WWSrvUavHeap &suHeap,
        int firstHeapIdx,
        UINT x, UINT y, UINT z);

    HRESULT CopyUavValuesToCpuMemory(WWUav& uav, void* to, int toBytes);

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
