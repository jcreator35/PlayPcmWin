// 日本語
#include "WWAudioReadThread.h"
#include <SpatialAudioClient.h>
#include <exception>
#include <mmdeviceapi.h>
#include "WWSAUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>
#include "WWGuidToStr.h"
#include "WWPrintDeviceProp.h"
#include <assert.h>
#include "WWMFReaderMetadata.h"
#include "WWMFReaderFunctions.h"
#include "WWCommonUtil.h"

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define RESAMPLE_QUALITY (60)
#define FRAGMENT_BYTES (1024 * 1024)

DWORD
WWAudioReadThread::ReadThreadEntry(LPVOID lpThreadParameter)
{
    WWAudioReadThread* self = (WWAudioReadThread*)lpThreadParameter;
    return self->ReadThreadMain();
}

HRESULT
WWAudioReadThread::Read1(void)
{
    HRESULT hr = S_OK;
    mPlayStreamCount = 0;
    UINT32 availableDyn = 0;
    UINT32 frameCountPerBuffer = 0;
    int64_t readBytes = FRAGMENT_BYTES;
    int64_t queuedFrames = 0;

    while (queuedFrames < mQueueFullFrames) {
        WWMFSampleData sd;
        WWAudioSampleBuffer *asb = nullptr;
        HRG(mReader.ReadFragment(mFromBuff, &readBytes));
        if (readBytes <= 0) {
            break;
        }

        // リサンプルする。
        HRG(mResampler.Resample(mFromBuff, (int)readBytes, &sd));

        // サンプルキューに追加。
        asb = new WWAudioSampleBuffer(
            sd.data,
            mTargetFmt.FrameBytes(),
            sd.bytes);

        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            mSampleQueue.push_back(asb);

            // 利用可能フレーム数を計算。
            for (auto ite = mSampleQueue.begin(); ite != mSampleQueue.end(); ++ite) {
                auto *p = *ite;
                queuedFrames += p->RemainFrames();
            }
        }
        ReleaseMutex(mMutex);

        sd.Forget();
    }

end:
    return hr;
}

HRESULT
WWAudioReadThread::ReadThreadMain(void)
{
    bool stillPlaying = true;
    HANDLE waitArray[2] = { mShutdownEvent, mBufferEvent };
    int nWaitObjects = 2;
    DWORD waitResult;
    HRESULT hr = 0;

    //　いくつかの初期化処理。
    // MTA
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

    assert(mFromBuff == nullptr);
    mFromBuff = new uint8_t[FRAGMENT_BYTES];
    if (mFromBuff == nullptr) {
        hr = E_OUTOFMEMORY;
        goto end;
    }

    assert(mToBuff == nullptr);
    mToBuff = new uint8_t[FRAGMENT_BYTES];
    if (mToBuff == nullptr) {
        hr = E_OUTOFMEMORY;
        goto end;
    }

    while (stillPlaying) {
        assert(waitArray[0] != nullptr);
        assert(waitArray[1] != nullptr);
        waitResult = WaitForMultipleObjects(nWaitObjects, waitArray, FALSE, INFINITE);
        
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            switch (waitResult) {
            case WAIT_OBJECT_0 + 0: // m_shutdownEvent
                stillPlaying = false;
                break;
            case WAIT_OBJECT_0 + 1: //< mBufferEvent
                hr = Read1();
                break;
            default:
                assert(0);
                break;
            }
        }
        ReleaseMutex(mMutex);

        if (FAILED(hr)) {
            mThreadErcd = hr;
            goto end;
        }
    }

end:
    dprintf("WWSpatialAudioUser::RenderMain() end. hr=%08x\n", hr);

    delete[] mToBuff;
    mToBuff = nullptr;

    delete[] mFromBuff;
    mFromBuff = nullptr;

    CoUninitialize();
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

WWAudioReadThread::~WWAudioReadThread(void)
{
    Term();
}

HRESULT
WWAudioReadThread::Init(const wchar_t *path, const WWMFPcmFormat &wantFmt)
{
    dprintf("WWAudioReadThread::Init()\n");
    HRESULT hr = S_OK;
    WAVEFORMATEXTENSIBLE origFmt;

    mTargetFmt = wantFmt;

    // 音声ファイルを読み出すmReaderを起動。
    // ファイルのPCMフォーマットが判明する。
    HRG(mReader.Start(path, &origFmt));
    mFileFmt.Set(origFmt);

    // リサンプラーを起動。file fmtをtargt fmtに変換する。
    HRG(mResampler.Initialize(mFileFmt, mTargetFmt, RESAMPLE_QUALITY));

    assert(!mMutex);
    mMutex = CreateMutex(nullptr, FALSE, nullptr);
    CHK(mMutex);

    assert(!mShutdownEvent);
    mShutdownEvent = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(mShutdownEvent);

    assert(!mBufferEvent);
    mBufferEvent = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(mBufferEvent);

    assert(nullptr == mReadThread);
    mReadThread = CreateThread(nullptr, 0, ReadThreadEntry, this, 0, nullptr);
    if (nullptr == mReadThread) {
        printf("E: WWAudioReadThread::Init() CreateThread failed\n");
        hr = E_FAIL;
    }

end:
    return hr;
}

void
WWAudioReadThread::Term(void)
{
    if (mReadThread) {
        assert(mShutdownEvent != nullptr);
        SetEvent(mShutdownEvent);

        WaitForSingleObject(mReadThread, INFINITE);
        dprintf("D: %s:%d CloseHandle(%p)\n", __FILE__, __LINE__, mReadThread);
        if (mReadThread) {
            CloseHandle(mReadThread);
        }
        mReadThread = nullptr;
    }

    if (nullptr != mShutdownEvent) {
        dprintf("D: %s:%d CloseHandle(%p)\n", __FILE__, __LINE__, mShutdownEvent);
        CloseHandle(mShutdownEvent);
        mShutdownEvent = nullptr;
    }

    if (nullptr != mBufferEvent) {
        dprintf("D: %s:%d CloseHandle(%p)\n", __FILE__, __LINE__, mBufferEvent);
        CloseHandle(mBufferEvent);
        mBufferEvent = nullptr;
    }

    if (mMutex) {
        CloseHandle(mMutex);
        mMutex = nullptr;
    }

    mResampler.Finalize();
    mReader.End();
}

HRESULT
WWAudioReadThread::Seek(int64_t pos)
{
    HRESULT hr = S_OK;

    assert(mMutex);
    WaitForSingleObject(mMutex, INFINITE);
    {
        hr = mReader.SeekToFrame(pos);
    }
    ReleaseMutex(mMutex);
    return hr;
}

HRESULT
WWAudioReadThread::GetNextPcm(unsigned char *data_return, int64_t *dataBytes_inout)
{
    HRESULT hr = S_OK;
    int64_t queuedFrames = 0;
    int64_t remainBytes = *dataBytes_inout;
    int64_t pos = 0;

    assert((*dataBytes_inout) % mTargetFmt.FrameBytes() == 0);

    assert(mMutex);
    WaitForSingleObject(mMutex, INFINITE);
    {   // この中はgotoしてはいけません。

        // コピーする。
        while (0 < remainBytes) {
            // キューの先頭からサンプル値を取り出す。
            WWAudioSampleBuffer *p = mSampleQueue.front();
            if (p == nullptr) {
                break;
            }

            // copyBytes : コピーするバイト数を確定する。
            int64_t copyBytes = remainBytes;
            assert(remainBytes % mTargetFmt.FrameBytes() == 0);

            if (copyBytes < *dataBytes_inout) {
                copyBytes = *dataBytes_inout;
            }
            if (p->RemainBytes() < copyBytes) {
                copyBytes = p->RemainBytes();
            }

            p->CopyTo(&data_return[pos], copyBytes);

            // 位置を進めます。
            pos += copyBytes;
            remainBytes -= copyBytes;

            if (p->RemainBytes() == 0) {
                // キューのアイテム1個を消費したので消す。
                p->Release();
                mSampleQueue.pop_front();
            }
        }

        // コピー終了。コピーできたバイト数をセット。
        *dataBytes_inout = pos;

        // 利用可能フレーム数を計算。
        for (auto ite = mSampleQueue.begin(); ite != mSampleQueue.end(); ++ite) {
            auto *p = *ite;
            queuedFrames += p->RemainFrames();
        }
    }
    ReleaseMutex(mMutex);

    if (mQueueLowFrames < queuedFrames) {
        // 利用可能フレーム数が閾値を下回ったので読み出しスレッドを起動。
        assert(mBufferEvent != nullptr);
        SetEvent(mBufferEvent);
    }

    return hr;
}
