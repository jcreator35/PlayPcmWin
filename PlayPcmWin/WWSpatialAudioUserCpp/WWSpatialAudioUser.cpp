// 日本語
#include "WWSpatialAudioUser.h"
#include <SpatialAudioClient.h>
#include <exception>
#include <mmdeviceapi.h>
#include "WWUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>
#include "WWGuidToStr.h"
#include "WWPrintDeviceProp.h"
#include <assert.h>

// 参考ページ。
// https://msdn.microsoft.com/en-us/windows/desktop/mt809289

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

DWORD
WWSpatialAudioUser::RenderEntry(LPVOID lpThreadParameter)
{
    WWSpatialAudioUser* self = (WWSpatialAudioUser*)lpThreadParameter;
    return self->RenderMain();
}

HRESULT
WWSpatialAudioUser::Render1(void)
{
    HRESULT hr = S_OK;
    mPlayStreamCount = 0;
    UINT32 availableDyn = 0;
    UINT32 frameCountPerBuffer = 0;

    if (nullptr == mSAORStream) {
        return S_OK;
    }

    HRG(mSAORStream->BeginUpdatingAudioObjects(&availableDyn, &frameCountPerBuffer));
    for (auto ite = mAudioObjectListHolder.mAudioObjectList.begin();
            ite != mAudioObjectListHolder.mAudioObjectList.end(); ++ite) {
        auto &ao = *ite;
        BYTE *buffer = nullptr;
        UINT32 buffBytes = 0;
        bool bEnd = false;

        if (ao.sao == nullptr) {
            HRG(mSAORStream->ActivateSpatialAudioObject(ao.aot, &ao.sao));
        }

        HRG(ao.sao->GetBuffer(&buffer, &buffBytes));

        bEnd = ao.CopyNextPcmTo(buffer, buffBytes);

        if (bEnd) {
            // 送るデータが無いので無音をセットした。
        } else {
            ++mPlayStreamCount;

            if (ao.aot == AudioObjectType_Dynamic) {
                HRG(ao.sao->SetPosition(ao.posX, ao.posY, ao.posZ));
            }
            HRG(ao.sao->SetVolume(ao.volume));
        }
    }

    HRG(mSAORStream->EndUpdatingAudioObjects());

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::RenderMain(void)
{
    bool stillPlaying = true;
    HANDLE waitArray[2] = { mShutdownEvent, mBufferEvent };
    int nWaitObjects = 2;
    DWORD waitResult;
    HRESULT hr = 0;

    assert(waitArray[0]);
    assert(waitArray[1]);

    // MTA
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

    mTimerResolution.Setup();
    mThreadCharacteristics.Setup();

    while (stillPlaying) {
        waitResult = WaitForMultipleObjects(nWaitObjects, waitArray, FALSE, INFINITE);
        
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            switch (waitResult) {
            case WAIT_OBJECT_0 + 0: // m_shutdownEvent
                if (mSAORStream) {
                    mSAORStream->Stop();
                    mSAORStream->Reset();
                }
                stillPlaying = false;
                break;
            case WAIT_OBJECT_0 + 1: //< mBufferEvent
                hr = Render1();
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
    dprintf("WWSpatialAudioUser::RenderMain() end\n");

    mThreadCharacteristics.Unsetup();
    mTimerResolution.Unsetup();

    CoUninitialize();
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

HRESULT
WWSpatialAudioUser::Init(void)
{
    dprintf("WWSpatialAudioUser::Init()\n");
    HRESULT hr = S_OK;

    hr = WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStream, WWAudioObject>::Init();
    if (FAILED(hr)) {
        goto end;
    }

    assert(nullptr == mRenderThread);
    mRenderThread = CreateThread(nullptr, 0, RenderEntry, this, 0, nullptr);
    if (nullptr == mRenderThread) {
        printf("E: WWSpatialAudioUser::Init() CreateThread failed\n");
        hr = E_FAIL;
    }

end:
    return hr;
}

void
WWSpatialAudioUser::Term(void)
{
    WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStream, WWAudioObject>::Term();
}

HRESULT
WWSpatialAudioUser::ActivateAudioStream(int dynObjectCount, int staticObjectTypeMask)
{
    HRESULT hr = S_OK;
    SpatialAudioObjectRenderStreamActivationParams p;
    PROPVARIANT pv;
    PropVariantInit(&pv);

    if (mSAORStream) {
        printf("E: WWSpatialAudioUser::ActivateAudioStream() already activated\n");
        hr = E_FAIL;
        goto end;
    }

    // 念の為。
    HRG(mSAClient->IsAudioObjectFormatSupported((const WAVEFORMATEX*)&mUseFmt));

    p.ObjectFormat = (const WAVEFORMATEX*)&mUseFmt;
    
    // 1つもスタティックなオブジェクトが無いときはNone。Dynamicにするとエラーが起きた。
    p.StaticObjectTypeMask = (AudioObjectType)staticObjectTypeMask;
    p.MinDynamicObjectCount = 0;
    p.MaxDynamicObjectCount = dynObjectCount;
    p.Category = AudioCategory_SoundEffects;
    p.EventHandle = mBufferEvent;
    p.NotifyObject = nullptr;

    pv.vt = VT_BLOB;
    pv.blob.cbSize = sizeof(p);
    pv.blob.pBlobData = reinterpret_cast<BYTE *>(&p);

    HRG(mSAClient->ActivateSpatialAudioStream(&pv, __uuidof(mSAORStream),
        (void**)&mSAORStream));

end:
    // blobの指す先はdelete不可。
    pv.blob.cbSize = 0;
    pv.blob.pBlobData = nullptr;
    PropVariantClear(&pv);

    return hr;
}

