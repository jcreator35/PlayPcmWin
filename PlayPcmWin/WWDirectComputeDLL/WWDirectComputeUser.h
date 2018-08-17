//　日本語

#pragma once

#include <d3d11.h>
#include <map>
#include <list>
#include <stdint.h>
#include <vector>

/// 読み出し専用GPUメモリ管理情報
struct WWReadOnlyGpuBufferInfo {
    ID3D11Resource           *pBuf;
    ID3D11ShaderResourceView *pSrv;
};

/// 読み書き可能GPUメモリ管理情報
struct WWReadWriteGpuBufferInfo {
    ID3D11Resource            *pBuf;
    ID3D11UnorderedAccessView *pUav;
};

/// Either pSrv or pUav is needed. nullptr == unneeded
struct WWTexture1DParams {
    int Width;
    int MipLevels;
    int ArraySize;
    DXGI_FORMAT Format;
    D3D11_USAGE Usage;
    int BindFlags;
    int CPUAccessFlags;
    int MiscFlags;
    const float * data;
    int dataCount;
    const char *name;
    ID3D11ShaderResourceView **pSrv;
    ID3D11UnorderedAccessView **pUav;
};

/// Either pSrv or pUav is needed. nullptr == unneeded
struct WWTexture2DParams {
    int Width;
    int Height;
    int MipLevels;
    int ArraySize;
    DXGI_FORMAT Format;

    /// for no multisample, set count == 1 and quality == 0
    DXGI_SAMPLE_DESC SampleDesc;
    D3D11_USAGE Usage;
    int BindFlags;
    int CPUAccessFlags;
    int MiscFlags;
    const float * data;
    int dataCount;
    const char *name;
    ID3D11ShaderResourceView **pSrv;
    ID3D11UnorderedAccessView **pUav;
};

/// Either pSrv or pUav is needed. nullptr == unneeded
struct WWStructuredBufferParams {
    uint32_t uElementSize;
    uint32_t uCount;
    void * pInitData;
    const char *name;
    ID3D11ShaderResourceView **pSrv;  //< inout
    ID3D11UnorderedAccessView **pUav; //< inout
};

struct WWDirectComputeAdapter {
    IDXGIAdapter* adapter;
    DXGI_ADAPTER_DESC desc;
};

class WWDirectComputeUser {
public:
    WWDirectComputeUser(void);
    ~WWDirectComputeUser(void);

    void Init(void);
    void Term(void);

    HRESULT EnumAdapters(void);
    int GetNumOfAdapters(void);
    HRESULT GetAdapterDesc(int idx, wchar_t *desc, int descBytes);
    HRESULT GetAdapterVideoMemoryBytes(int idx, int64_t *videoMemoryBytes);
    HRESULT ChooseAdapter(int idx);

    // ComputeShaderをコンパイルしてGPUに送る。
    HRESULT CreateComputeShader(
            LPCWSTR path,
            LPCSTR entryPoint,
            const D3D_SHADER_MACRO *defines,
            ID3D11ComputeShader **ppCS);

    void DestroyComputeShader(ID3D11ComputeShader *pCS);

    // 実行。中でブロックする。
    HRESULT Run(
            ID3D11ComputeShader * pComputeShader,
            UINT nNumSRV,
            ID3D11ShaderResourceView * ppSRV[],
            UINT nNumUAV,
            ID3D11UnorderedAccessView * ppUAV[],
            void * pCSData,
            DWORD dwNumDataBytes,
            UINT X,
            UINT Y,
            UINT Z);

    // 計算結果をGPUから取り出す。
    HRESULT RecvResultToCpuMemory(
            ID3D11UnorderedAccessView * pUav,
            void *dest,
            int bytes);

    ID3D11Device *GetDevice(void) { return mDevice; }

    HRESULT CreateSeveralStructuredBuffer(int n, WWStructuredBufferParams *params);
    HRESULT CreateSeveralTexture1D(int n, WWTexture1DParams *params);
    HRESULT CreateSeveralTexture2D(int n, WWTexture2DParams *params);
    HRESULT CreateConstantBuffer(unsigned int uElementSize, unsigned int uCount,
            const char *name, ID3D11Buffer **ppBufOut);

    void DestroyConstantBuffer(ID3D11Buffer * pBuf);

    void DestroyResourceAndSRV(ID3D11ShaderResourceView * pSrv);
    void DestroyResourceAndUAV(ID3D11UnorderedAccessView * pUav);

    ID3D11Resource *FindResourceOfUAV(ID3D11UnorderedAccessView * pUav);
    ID3D11Resource *FindResourceOfSRV(ID3D11ShaderResourceView * pSrv);

    /// 単にReleaseするだけ。
    void DestroyTexture1D(ID3D11Texture1D *pTex) const;
    /// 単にReleaseするだけ。
    void DestroyTexture2D(ID3D11Texture2D *pTex) const;

    ID3D11Device *Device(void) { return mDevice; }
    ID3D11DeviceContext *DeviceCtx(void) { return mCtx; }

private:
    ID3D11Device*                         mDevice;
    ID3D11DeviceContext*                  mCtx;
    ID3D11Buffer                         *mConstBuffer;
    std::vector <WWDirectComputeAdapter>  mAdapters;
    std::list<ID3D11ComputeShader*>       mComputeShaderList;
    std::map<ID3D11ShaderResourceView *, WWReadOnlyGpuBufferInfo>   mReadGpuBufMap;
    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo> m_rwGpuBufMap;
    
    HRESULT CreateComputeDevice(void);

    // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
    // Structured Buffer

    HRESULT CreateStructuredBuffer(
            unsigned int uElementSize,
            unsigned int uCount,
            void * pInitData,
            const char *name,
            ID3D11Buffer ** ppBufOut);

    HRESULT CreateBufferSRV(
            ID3D11Buffer * pBuffer,
            const char *name,
            ID3D11ShaderResourceView ** ppSrvOut);

    HRESULT CreateBufferUAV(
            ID3D11Buffer * pBuffer,
            const char *name,
            ID3D11UnorderedAccessView ** ppUavOut);

    // 入力データ(読み出し専用)をGPUメモリに送る。
    HRESULT CreateBufferAndSRV(
            unsigned int uElementSize,
            unsigned int uCount,
            void * pSendData,
            const char *name,
            ID3D11ShaderResourceView **ppSrv);

    /// 入出力可能データをGPUメモリに作成。
    /// @param pSendData nullptrでも可。
    HRESULT CreateBufferAndUAV(
            unsigned int uElementSize,
            unsigned int uCount,
            void *pSendData,
            const char *name,
            ID3D11UnorderedAccessView **ppUav);

    // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
    // Texture1D

    /// @param pInitialData can be nullptr but the value becomes undefined!
    HRESULT CreateTexture1D(
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
            ID3D11Texture1D **ppTexOut
            );

    HRESULT CreateTexture1DAndSRV(
            int Width,
            int MipLevels,
            int ArraySize,
            DXGI_FORMAT Format,
            D3D11_USAGE Usage,
            int BindFlags,
            int CPUAccessFlags,
            int MiscFlags,
            const float * data,
            int dataCount,
            const char *name,
            ID3D11ShaderResourceView **ppSrv);

    HRESULT CreateTexture1DAndUAV(
            int Width,
            int MipLevels,
            int ArraySize,
            DXGI_FORMAT Format,
            D3D11_USAGE Usage,
            int BindFlags,
            int CPUAccessFlags,
            int MiscFlags,
            const float * data,
            int dataCount,
            const char *name,
            ID3D11UnorderedAccessView **ppUav);

    HRESULT CreateTexture1DSRV(
            ID3D11Texture1D * pTex,
            DXGI_FORMAT format,
            const char *name,
            ID3D11ShaderResourceView ** ppSrvOut);

    HRESULT CreateTexture1DUAV(
            ID3D11Texture1D * pTex,
            DXGI_FORMAT format,
            const char *name,
            ID3D11UnorderedAccessView ** ppUavOut);

    // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
    // Texture2D

    /// @param pInitialData can be nullptr but the value becomes undefined!
    HRESULT CreateTexture2D(
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
            ID3D11Texture2D **ppTexOut
            );

    HRESULT CreateTexture2DAndSRV(
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
            const float * data,
            int dataCount,
            const char *name,
            ID3D11ShaderResourceView **ppSrv);

    HRESULT CreateTexture2DAndUAV(
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
            const float * data,
            int dataCount,
            const char *name,
            ID3D11UnorderedAccessView **ppUav);

    HRESULT CreateTexture2DSRV(
            ID3D11Texture2D * pTex,
            DXGI_FORMAT format,
            const char *name,
            ID3D11ShaderResourceView ** ppSrvOut);

    HRESULT CreateTexture2DUAV(
            ID3D11Texture2D * pTex,
            DXGI_FORMAT format,
            const char *name,
            ID3D11UnorderedAccessView ** ppUavOut);

    // Runの代わりに、SetupDispatch() Dispatch() UnsetupDispatch()しても良い。
    HRESULT SetupDispatch(
            ID3D11ComputeShader * pComputeShader,
            UINT nNumSRV,
            ID3D11ShaderResourceView * ppSRV[],
            UINT nNumUAV,
            ID3D11UnorderedAccessView * ppUAV[],
            void *pCSData, //< constant buffer data on CPU memory
            DWORD dwNumDataBytes);

    /// Constant buffer無しバージョン。
    void Dispatch(
            UINT X,
            UINT Y,
            UINT Z);

    void UnsetupDispatch(void);
};
