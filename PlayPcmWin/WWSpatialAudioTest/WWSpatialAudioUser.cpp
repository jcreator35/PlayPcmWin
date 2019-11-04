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

static HRESULT
DeviceNameGet(
    IMMDeviceCollection *dc, UINT id, wchar_t *name, size_t nameBytes)
{
    HRESULT hr = 0;

    IMMDevice *device = nullptr;
    LPWSTR deviceId = nullptr;
    IPropertyStore *ps = nullptr;
    PROPVARIANT pv;

    assert(dc);
    assert(name);

    name[0] = 0;

    assert(0 < nameBytes);

    PropVariantInit(&pv);

    HRR(dc->Item(id, &device));
    HRR(device->GetId(&deviceId));
    HRR(device->OpenPropertyStore(STGM_READ, &ps));

    HRG(ps->GetValue(PKEY_Device_FriendlyName, &pv));
    SafeRelease(&ps);

    wcsncpy_s(name, nameBytes / sizeof name[0], pv.pwszVal, _TRUNCATE);

end:
    PropVariantClear(&pv);
    CoTaskMemFree(deviceId);
    SafeRelease(&ps);
    return hr;
}

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

    HRG(mSAORStream->BeginUpdatingAudioObjects(&availableDyn, &frameCountPerBuffer));
    for (auto ite = mSAObjects.mDynAudioStreamsList.begin(); ite != mSAObjects.mDynAudioStreamsList.end(); ++ite) {
        auto &dyn = *ite;
        BYTE *buffer = nullptr;
        UINT32 bufferLength = 0;
        bool bEnd = false;
        if (dyn.buffer == nullptr) {
            continue;
        }

        if (dyn.sao == nullptr) {
            HRG(mSAORStream->ActivateSpatialAudioObject(AudioObjectType_Dynamic, &dyn.sao));
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
            HRG(dyn.sao->SetVolume(dyn.volume));
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
    HANDLE waitArray[2] = { mShutdownEvent, mEvent };
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
            case WAIT_OBJECT_0 + 1: //< mEvent
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
WWSpatialAudioUser::Init(void)
{
    HRESULT hr = 0;

    // ComInitializeする。
    assert(mComInit == false);
    hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (SUCCEEDED(hr)) {
        mComInit = true;
    } else {
        mComInit = false;
    }
    
    assert(!mMutex);
    mMutex = CreateMutex(
        nullptr, FALSE, nullptr);

    assert(!mShutdownEvent);
    mShutdownEvent = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
    if (nullptr == mShutdownEvent) {
        printf("E: WWSpatialAudioUser::Init() CreateEvent failed\n");
        hr = E_FAIL;
        goto end;
    }

    assert(!mEvent);
    mEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (nullptr == mEvent) {
        printf("E: WWSpatialAudioUser::Init() CreateEvent failed\n");
        hr = E_FAIL;
        goto end;
    }

    mRenderThread = CreateThread(nullptr, 0, RenderEntry, this, 0, nullptr);
    assert(mRenderThread);

end:
    return S_OK;
}

void
WWSpatialAudioUser::Term(void)
{
    if (mSAORStream) {
        mSAORStream->Stop();
        mSAORStream->Reset();
        SafeRelease(&mSAORStream);
    }

    mSAObjects.ReleaseAll();

    if (mShutdownEvent) {
        SetEvent(mShutdownEvent);
    }
    if (mRenderThread) {
        SetEvent(mRenderThread);
        WaitForSingleObject(mRenderThread, INFINITE);
        CloseHandle(mRenderThread);
        mRenderThread = nullptr;
    }

    if (mEvent != nullptr) {
        CloseHandle(mEvent);
        mEvent = nullptr;
    }

    if (mShutdownEvent) {
        CloseHandle(mShutdownEvent);
        mShutdownEvent = nullptr;
    }

    if (mMutex) {
        CloseHandle(mMutex);
        mMutex = nullptr;
    }

    mDeviceInf.clear();
    SafeRelease(&mDeviceCollection);
    SafeRelease(&mDeviceToUse);
    SafeRelease(&mSAClient);

    if (mComInit) {
        CoUninitialize();
        mComInit = false;
    }
}

HRESULT
WWSpatialAudioUser::DoDeviceEnumeration(void)
{
    HRESULT hr = 0;
    IMMDeviceEnumerator *devEnum = nullptr;

    HRR(CoCreateInstance(__uuidof(MMDeviceEnumerator),
        nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&devEnum)));

    HRR(devEnum->EnumAudioEndpoints(
        eRender, DEVICE_STATE_ACTIVE, &mDeviceCollection));

    UINT nDevices = 0;
    HRG(mDeviceCollection->GetCount(&nDevices));

    for (UINT i = 0; i < nDevices; ++i) {
        wchar_t name[WW_DEVICE_NAME_COUNT];
        HRG(DeviceNameGet(mDeviceCollection, i, name, sizeof name));
        mDeviceInf.push_back(WWDeviceInf(i, name));
    }

end:
    SafeRelease(&devEnum);
    return hr;
}

int
WWSpatialAudioUser::GetDeviceCount(void)
{
    assert(mDeviceCollection);
    return (int)mDeviceInf.size();
}

bool
WWSpatialAudioUser::GetDeviceName(int id, LPWSTR name, size_t nameBytes)
{
    assert(name);
    memset(name, 0, nameBytes);
    if (id < 0 || mDeviceInf.size() <= (unsigned int)id) {
        assert(0);
        return false;
    }
    wcsncpy_s(name, nameBytes / sizeof name[0], mDeviceInf[id].name, _TRUNCATE);
    return true;
}

HRESULT
WWSpatialAudioUser::ChooseDevice(int id)
{
    HRESULT hr = 0;

    // アクティベーションの設定値pv。
    auto p = reinterpret_cast<SpatialAudioClientActivationParams *>(
        CoTaskMemAlloc(sizeof(SpatialAudioClientActivationParams)));
    if (nullptr == p) {
        throw new std::bad_alloc();
    }
    memset(p, 0, sizeof *p);
    p->majorVersion = 1;
    PROPVARIANT pv;
    PropVariantInit(&pv);
    pv.vt = VT_BLOB;
    pv.blob.cbSize = sizeof(*p);
    pv.blob.pBlobData = reinterpret_cast<BYTE *>(p);

    if (id < 0) {
        // Unchoose device
        if (mSAClient) {
            SafeRelease(&mSAClient);
        }
    } else {
        // Choose device
        assert(!mDeviceToUse);
        HRG(mDeviceCollection->Item(id, &mDeviceToUse));

        assert(mDeviceToUse);
        assert(!mSAClient);

        HRG(mDeviceToUse->Activate(
            __uuidof(ISpatialAudioClient), CLSCTX_INPROC_SERVER, &pv, (void**)&mSAClient));
        assert(mSAClient);

        HRG(mSAClient->GetMaxDynamicObjectCount(&mMaxDynamicObjectCount));

        if (mMaxDynamicObjectCount == 0) {
            printf("    Spatial audio is not enabled\n");
            goto end;
        }
        printf("    Spatial audio is enabled\n");
        printf("    MaxDynamicObjectCount=%u\n", mMaxDynamicObjectCount);

        //WWPrintStaticAudioObjectTypeFlags(mSAClient);

        // prepare WFEX
        {
            int numChannels = 1;
            int sampleRate = 48000;
            int bitsPerSample = 32;
            int byteRate = sampleRate * numChannels * bitsPerSample / 8;
            int blockAlign = numChannels * bitsPerSample / 8;

            mUseFmt.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
            mUseFmt.Format.nChannels = numChannels;
            mUseFmt.Format.nSamplesPerSec = sampleRate;
            mUseFmt.Format.nAvgBytesPerSec = byteRate;
            mUseFmt.Format.nBlockAlign = blockAlign;
            mUseFmt.Format.wBitsPerSample = bitsPerSample;
            mUseFmt.Format.cbSize = 22;
            mUseFmt.Samples.wValidBitsPerSample = bitsPerSample;
            mUseFmt.dwChannelMask = 0;
            mUseFmt.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
            printf("  %dHz %dbit %dch container=%dbit %s\n",
                (int)mUseFmt.Format.nSamplesPerSec, (int)mUseFmt.Samples.wValidBitsPerSample,
                (int)mUseFmt.Format.nChannels, (int)mUseFmt.Format.wBitsPerSample,
                WWGuidToKsDataFormatStr(&mUseFmt.SubFormat));
        }
    }

end:
    SafeRelease(&mDeviceToUse);
    PropVariantClear(&pv);
    return hr;
}

HRESULT
WWSpatialAudioUser::ActivateAudioStream(int dynObjectCount)
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
    
    // 1つもスタティックなオブジェクトが無い。Dynamicにするとエラーが起きた。
    p.StaticObjectTypeMask = AudioObjectType_None;
    p.MinDynamicObjectCount = 0;
    p.MaxDynamicObjectCount = dynObjectCount;
    p.Category = AudioCategory_SoundEffects;
    p.EventHandle = mEvent;
    p.NotifyObject = nullptr;

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

void
WWSpatialAudioUser::DeactivateAudioStream(void)
{
    mSAObjects.ReleaseAll();
}

HRESULT 
WWSpatialAudioUser::AddStream(WWDynamicAudioStreamChannel &dasc)
{
    HRESULT hr = S_OK;
    if (dasc.bufferBytes == 0 || dasc.buffer == nullptr) {
        printf("E: WWSpatialAudioUser::AddStream data err\n");
        hr = E_FAIL;
        goto end;
    }

    if (dasc.sao != nullptr) {
        printf("E: WWSpatialAudioUser::AddStream sao should be nullptr\n");
        hr = E_FAIL;
        goto end;
    }

    dasc.idx = mNextDynStreamIdx;
    ++mNextDynStreamIdx;

    assert(mMutex);
    WaitForSingleObject(mMutex, INFINITE);
    {   // この中はgoto 不可。
        mSAObjects.mDynAudioStreamsList.push_back(dasc);
    }
    ReleaseMutex(mMutex);

end:
    return hr;
}

/// @param dascIdx dasc.idxを渡す。
/// @param x 右が+ 単位メートル
/// @param y 上が+ 単位メートル
/// @param z 後ろが+ (前は-)。単位メートル。
bool
WWSpatialAudioUser::SetPosVolume(int dascIdx, float x, float y, float z, float volume)
{
    bool rv = true;
    assert(mMutex);
    WaitForSingleObject(mMutex, INFINITE);
    do { // この中はgoto 不可。
        auto * dasc = mSAObjects.Find(dascIdx);
        if (dasc == nullptr) {
            rv = false;
            break;
        }

        dasc->SetPos3D(x, y, z);
        dasc->volume = volume;
    } while (false);
    ReleaseMutex(mMutex);
    return rv;
}

int
WWSpatialAudioUser::PlayStreamCount(void) {
    int r = 0;

    WaitForSingleObject(mMutex, INFINITE);
    {   // この中はgoto 不可。
        r = mPlayStreamCount;
    }
    ReleaseMutex(mMutex);

    return r;
}