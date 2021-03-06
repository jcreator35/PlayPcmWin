﻿// 日本語

#include "WWMFVideoReaderIF.h"
#include "WWMFVideoFrameReader.h"
#include "WWCommonUtil.h"
#include <map>

extern "C" __declspec(dllexport) int __stdcall
WWMFVReaderIFStaticInit(void)
{
    return WWMFVideoFrameReader::StaticInit();
}

extern "C" __declspec(dllexport) void __stdcall
WWMFVReaderIFStaticTerm(void)
{
    WWMFVideoFrameReader::StaticTerm();
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
// インスタンスを生成して行う処理。

static int gNextInstanceId = 100;
static std::map<int, WWMFVideoFrameReader*> gInstances;

static WWMFVideoFrameReader *
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



/// インスタンスを作成し、1つのファイルを読む。
/// @return instanceIdが戻る。
/// @retval 負の値 読み出し時の失敗のHRESULT
WWMFVIDEOREADER_API int __stdcall
WWMFVReaderIFReadStart(
        const wchar_t *wszSourceFile)
{
    int instanceId = gNextInstanceId++;

    //printf("WWMFVReaderIFReadStart %S\n", wszSourceFile);

    auto *p = new WWMFVideoFrameReader(instanceId);
    HRESULT hr = p->ReadStart(wszSourceFile);

    if (FAILED(hr)) {
        p->ReadEnd();
        delete p;
        return hr;
    }

    // 成功。
    gInstances.insert(std::pair<int, WWMFVideoFrameReader*>(instanceId, p));
    return instanceId;
}

/// 作ったインスタンスを消す。
/// @retval S_OK インスタンスが見つかって、削除成功。
/// @retval E_INVALIDARG インスタンスがない。
WWMFVIDEOREADER_API int __stdcall
WWMFVReaderIFReadEnd(
    int instanceId)
{
    FIND_INSTANCE;

    //printf("WWMFVReaderIFReadEnd\n");

    p->ReadEnd();
    delete p;
    gInstances.erase(instanceId);

    return S_OK;
}

WWMFVIDEOREADER_API int __stdcall
WWMFVReaderIFReadImage(
        int instanceId, int64_t posToSeek, uint8_t *pImg_io,
        int *imgBytes_io, WWMFVideoFormat *vf_return)
{
    FIND_INSTANCE;

    assert(pImg_io);

    //printf("WWMFVReaderIFReadImage posToSeek=%lld\n", posToSeek);

    HRG(p->ReadImage(posToSeek, pImg_io, imgBytes_io, vf_return));

end:

    return hr;
}
