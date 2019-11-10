// 日本語。

#define WWMFRESAMPLER_EXPORTS
#include "WWMFResamplerCppIF.h"
#include "WWMFResampler.h"

#include "targetver.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <list>

static std::list<WWMFResampler*> gResamplerList;

static WWMFResampler *
FindInstance(int idx)
{
    if (idx < 0 || gResamplerList.size() <= idx) {
        return nullptr;
    }

    auto ite = gResamplerList.begin();
    for (int i = 0; i < idx; ++i) {
        ++ite;
    }

    return *ite;
}

WWMFRESAMPLER_API int __stdcall
WWMFResamplerInit(
        const WWMFPcmFormatMarshal *inPFM,
        const WWMFPcmFormatMarshal *outPFM,
        int halfFilterLength)
{
    HRESULT hr = S_OK;
    const int id = (int)gResamplerList.size();
    WWMFPcmFormat inPF((WWMFBitFormatType)inPFM->sampleFormat, inPFM->nChannels, inPFM->bits, inPFM->sampleRate, inPFM->dwChannelMask, inPFM->validBitsPerSample);
    WWMFPcmFormat outPF((WWMFBitFormatType)outPFM->sampleFormat, outPFM->nChannels, outPFM->bits, outPFM->sampleRate, outPFM->dwChannelMask, outPFM->validBitsPerSample);

    auto *p = new WWMFResampler;
    if (nullptr == p) {
        return E_OUTOFMEMORY;
    }

    hr = p->Initialize(inPF, outPF, halfFilterLength);
    if (FAILED(hr)) {
        return hr;
    }

    gResamplerList.push_back(p);
    return id;
}

WWMFRESAMPLER_API int __stdcall
WWMFResamplerTerm(int idx)
{
    if (idx < 0 || gResamplerList.size() <= idx) {
        return E_INVALIDARG;
    }

    auto ite = gResamplerList.begin();
    for (int i = 0; i < idx; ++i) {
        ++ite;
    }

    WWMFResampler *p = *ite;
    p->Finalize();

    delete p;
    p = nullptr;
    *ite = nullptr;

    gResamplerList.erase(ite);

    return S_OK;
}

WWMFRESAMPLER_API int __stdcall
WWMFResamplerResample(int instanceId, const unsigned char *buff, int bytes, unsigned char * buffResult_inout, int * resultBytes_inout)
{
    HRESULT hr = S_OK;

    auto *p = FindInstance(instanceId);
    if (nullptr == p) {
        return E_INVALIDARG;
    }

    WWMFSampleData sd;
    hr = p->Resample(buff, bytes, &sd);
    if (FAILED(hr)) {
        return hr;
    }

    if (*resultBytes_inout < (int)sd.bytes) {
        sd.Release();
        return E_INVALIDARG;
    }

    memcpy(buffResult_inout, sd.data, sd.bytes);
    *resultBytes_inout = sd.bytes;

    sd.Release();

    return hr;
}

/// 最後の入力データをResample()に送ったあとに1回呼ぶ。
/// バッファに溜まった残り滓(必要入力サンプルが足りないので計算がペンディングしていたデータ)が出てくる。
WWMFRESAMPLER_API int __stdcall
WWMFResamplerDrain(int instanceId, unsigned char * buffResult_inout, int * resultBytes_inout)
{
    HRESULT hr = S_OK;

    auto *p = FindInstance(instanceId);
    if (nullptr == p) {
        return E_INVALIDARG;
    }

    WWMFSampleData sd;
    hr = p->Drain(*resultBytes_inout, &sd);
    if (FAILED(hr)) {
        return hr;
    }

    if (sd.bytes == 0) {
        sd.Release();
        *resultBytes_inout = 0;
        return S_OK;
    }

    if (*resultBytes_inout < (int)sd.bytes) {
        sd.Release();
        return E_INVALIDARG;
    }

    memcpy(buffResult_inout, sd.data, sd.bytes);
    *resultBytes_inout = sd.bytes;

    sd.Release();

    return hr;
}
