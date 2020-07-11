// 日本語。

#pragma once

#include <d3d11.h>
#include <map>
#include <list>
#include <stdint.h>

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

class WWDirectComputeUser {
public:
    WWDirectComputeUser(void);
    ~WWDirectComputeUser(void);

    HRESULT Init(void);
    void    Term(void);

    // ComputeShaderをコンパイルしてGPUに送る
    HRESULT CreateComputeShader(
        LPCWSTR path,
        LPCSTR entryPoint,
        const D3D_SHADER_MACRO *defines,
        ID3D11ComputeShader **ppCS);

    void DestroyComputeShader(ID3D11ComputeShader *pCS);

    // 入力データ(読み出し専用)をGPUメモリに送る
    HRESULT SendReadOnlyDataAndCreateShaderResourceView(
        unsigned int uElementSize,
        unsigned int uCount,
        void * pSendData,
        const char *name,
        ID3D11ShaderResourceView **ppSrv);

    void DestroyDataAndShaderResourceView(
        ID3D11ShaderResourceView * pSrv);

    /// 入出力可能データをGPUメモリに作成。
    /// @param pSendData nullptrでも可。
    HRESULT CreateBufferAndUnorderedAccessView(
        unsigned int uElementSize,
        unsigned int uCount,
        void *pSendData,
        const char *name,
        ID3D11UnorderedAccessView **ppUav);

    void DestroyDataAndUnorderedAccessView(
        ID3D11UnorderedAccessView * pUav);

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

    ID3D11Device *GetDevice(void) { return m_pDevice; }

    HRESULT CreateConstantBuffer(
        unsigned int uElementSize,
        unsigned int uCount,
        const char *name,
        ID3D11Buffer **ppBufOut);

    void DestroyConstantBuffer(ID3D11Buffer * pBuf);

    void DestroyTexture1D(ID3D11Texture1D *pTex);

    HRESULT CreateSeveralTexture1D(int n, WWTexture1DParams *params);

private:
    ID3D11Device*               m_pDevice;
    ID3D11DeviceContext*        m_pContext;
    ID3D11Buffer *m_pConstBuffer;

    HRESULT CreateComputeDevice(void);

    HRESULT CreateBufferShaderResourceView(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut);

    HRESULT CreateBufferUnorderedAccessView(
        ID3D11Buffer * pBuffer,
        const char *name,
        ID3D11UnorderedAccessView ** ppUavOut);

    std::map<ID3D11ShaderResourceView *, WWReadOnlyGpuBufferInfo> m_readGpuBufInfo;
    std::map<ID3D11UnorderedAccessView *, WWReadWriteGpuBufferInfo> m_rwGpuBufInfo;
    std::list<ID3D11ComputeShader*> m_computeShaderList;
    
    HRESULT CreateStructuredBuffer(
        unsigned int uElementSize,
        unsigned int uCount,
        void * pInitData,
        const char *name,
        ID3D11Buffer ** ppBufOut);

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

    HRESULT CreateTexture1DAndShaderResourceView(
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

    HRESULT CreateTexture1DAndUnorderedAccessView(
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

    HRESULT CreateTexture1DShaderResourceView(
        ID3D11Texture1D * pTex,
        DXGI_FORMAT format,
        const char *name,
        ID3D11ShaderResourceView ** ppSrvOut);

    HRESULT CreateTexture1DUnorderedAccessView(
        ID3D11Texture1D * pTex,
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
