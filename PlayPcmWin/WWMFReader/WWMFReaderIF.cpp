// 日本語

#include "WWMFReaderIF.h"
#include "WWMFReader.h"
#include "WWMFReadFragments.h"
#include "WWCommonUtil.h"

#include <map>
#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <stdio.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>

extern "C" __declspec(dllexport) int __stdcall
WWMFReaderIFReadHeader(
        const wchar_t *wszSourceFile,
        WWMFReaderMetadata *meta_return)
{
    return WWMFReaderReadHeader(wszSourceFile, WWMFREADER_FLAG_RESOLVE_NUM_FRAMES, meta_return);
}

extern "C" __declspec(dllexport) int __stdcall
WWMFReaderIFGetCoverart(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout)
{
    return WWMFReaderGetCoverart(wszSourceFile, data_return, dataBytes_inout);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// 少しづつ読む版。
// 大きいファイル用。

static int gNextInstanceId = 100;
static std::map<int, WWMFReadFragments*> gInstances;

static WWMFReadFragments *
FindInstance(int idx)
{
    auto ite = gInstances.find(idx);
    if (ite == gInstances.end()) {
        return nullptr;
    }

    return ite->second;
}

#define FIND_INSTANCE                   \
    HRESULT hr = S_OK;                  \
    auto *p = FindInstance(instanceId); \
    if (nullptr == p) {                 \
        return E_INVALIDARG;            \
    }

/// 少しづつ読み出す。
/// @return instanceIdが戻る。
extern "C" __declspec(dllexport) int __stdcall
WWMFReaderIFReadDataStart(
    const wchar_t *wszSourceFile)
{
    auto *p = new WWMFReadFragments();
    HRESULT hr = p->Start(wszSourceFile, nullptr);

    if (FAILED(hr)) {
        delete p;
        return hr;
    }

    // 成功。
    int instanceId = gNextInstanceId++;

    gInstances.insert(std::pair<int, WWMFReadFragments*>(instanceId, p));
    return instanceId;
}

/// 少しづつ読み出す。
/// @return instanceIdが戻る。
extern "C" __declspec(dllexport) int __stdcall
WWMFReaderIFReadDataFragment(
    int instanceId,
    unsigned char *data_return,
    int64_t *dataBytes_inout)
{
    FIND_INSTANCE;

    assert(data_return);
    assert(dataBytes_inout);
    assert(0 < *dataBytes_inout);

    return p->ReadFragment(data_return, dataBytes_inout);
}

/// 少しづつ読み出す。
/// @return instanceIdが戻る。
extern "C" __declspec(dllexport) int __stdcall
WWMFReaderIFReadDataEnd(
    int instanceId)
{
    FIND_INSTANCE;

    p->End();
    delete p;
    gInstances.erase(instanceId);

    return 0;
}

