// 日本語
#include "WWSpatialAudioHrtfUser.h"
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
WWSpatialAudioHrtfUser::RenderEntry(LPVOID lpThreadParameter)
{
    WWSpatialAudioHrtfUser* self = (WWSpatialAudioHrtfUser*)lpThreadParameter;
    return self->RenderMain();
}

HRESULT
WWSpatialAudioHrtfUser::Render1(void)
{
    HRESULT hr = S_OK;
    mPlayStreamCount = 0;
    UINT32 availableDyn = 0;
    UINT32 frameCountPerBuffer = 0;

    HRG(mSAORStream->BeginUpdatingAudioObjects(&availableDyn, &frameCountPerBuffer));
    for (auto ite = mDynObjectList.mDynAudioObjectList.begin(); ite != mDynObjectList.mDynAudioObjectList.end(); ++ite) {
        auto &dyn = *ite;
        BYTE *buffer = nullptr;
        UINT32 bufferLength = 0;
        bool bEnd = false;
        if (dyn.buffer == nullptr) {
            continue;
        }

        if (dyn.sao == nullptr) {
            HRG(mSAORStream->ActivateSpatialAudioObjectForHrtf(AudioObjectType_Dynamic, &dyn.sao));
        }

        HRG(dyn.sao->GetBuffer(&buffer, &bufferLength));

        bEnd = dyn.CopyNextPcmTo(buffer, bufferLength);

        if (bEnd) {
            //printf("SetEndOfStream %d\n", bufferLength / mUseFmt.Format.nBlockAlign);
            HRG(dyn.sao->SetEndOfStream(bufferLength / mUseFmt.Format.nBlockAlign));
            dyn.ReleaseAll();
        } else {
            ++mPlayStreamCount;
            HRG(dyn.sao->SetPosition(dyn.posX, dyn.posY, dyn.posZ));
            HRG(dyn.sao->SetGain(dyn.volume));
            HRG(dyn.sao->SetEnvironment(dyn.env));
            HRG(dyn.sao->SetDistanceDecay(&dyn.dd));
            HRG(dyn.sao->SetDirectivity(&dyn.directivity));
            if (dyn.directivity.Omni.Type != SpatialAudioHrtfDirectivity_OmniDirectional) {
                // 向きがあるので向きを指定。
                // row major 3x3 mat
                SpatialAudioHrtfOrientation o;
                WWQuaternionToRowMajorRotMat(dyn.orientation, o);
                HRG(dyn.sao->SetOrientation(&o));
            }
        }
    }

    HRG(mSAORStream->EndUpdatingAudioObjects());

end:
    return hr;
}

HRESULT
WWSpatialAudioHrtfUser::RenderMain(void)
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
            goto end;
        }
    }

end:
    CoUninitialize();
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

HRESULT
WWSpatialAudioHrtfUser::Init(void)
{
    HRESULT hr = S_OK;

    hr = WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStreamForHrtf, WWDynAudioHrtfObject > ::Init();
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

HRESULT
WWSpatialAudioHrtfUser::ActivateAudioStream(int dynObjectCount)
{
    HRESULT hr = S_OK;
    SpatialAudioHrtfActivationParams p;
    WWDynAudioHrtfObject dyn;
    SpatialAudioHrtfOrientation oriMat;
    PROPVARIANT pv;
    PropVariantInit(&pv);

    if (mSAORStream) {
        printf("E: WWSpatialAudioHrtfUser::ActivateAudioStream() already activated\n");
        hr = E_FAIL;
        goto end;
    }

    // 念の為。
    HRG(mSAClient->IsAudioObjectFormatSupported((const WAVEFORMATEX*)&mUseFmt));

    WWQuaternionToRowMajorRotMat(dyn.orientation, oriMat);

    p.ObjectFormat = (const WAVEFORMATEX*)&mUseFmt;
    
    // 1つもスタティックなオブジェクトが無いときはNone。Dynamicにするとエラーが起きた。
    p.StaticObjectTypeMask = AudioObjectType_None;
    p.MinDynamicObjectCount = 0;
    p.MaxDynamicObjectCount = dynObjectCount;
    p.Category = AudioCategory_SoundEffects;
    p.EventHandle = mBufferEvent;
    p.NotifyObject = nullptr;
    p.DistanceDecay = &dyn.dd;
    p.Directivity = &dyn.directivity;
    p.Environment = &dyn.env;
    p.Orientation = &oriMat;

    pv.vt = VT_BLOB;
    pv.blob.cbSize = sizeof(p);
    pv.blob.pBlobData = reinterpret_cast<BYTE *>(&p);

    HRG(mSAClient->ActivateSpatialAudioStream(&pv, __uuidof(mSAORStream),
        (void**)&mSAORStream));

    mSAORStream->Start();

end:
    // blobの指す先はdelete不可。
    pv.blob.cbSize = 0;
    pv.blob.pBlobData = nullptr;
    PropVariantClear(&pv);

    return hr;
}

