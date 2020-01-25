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

    // MTA
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

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

    mReader.End();
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

    mTargetFmt = wantFmt;

    HRG(mReader.Start(path));

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

