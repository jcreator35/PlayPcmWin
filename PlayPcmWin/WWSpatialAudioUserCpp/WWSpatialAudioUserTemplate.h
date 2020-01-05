// 日本語
#pragma once

// 参考ページ。
// https://msdn.microsoft.com/en-us/windows/desktop/mt809289

#include <Windows.h>
#include <MMDeviceAPI.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <vector>
#include <string.h>
#include <SpatialAudioClient.h>
#include <SpatialAudioMetadata.h>
#include <devicetopology.h>
#include <set>
#include <list>
#include "WWAudioObjectListHolder.h"
#include "WWDeviceInf.h"
#include "WWCommonUtil.h"
#include "WWSAUtil.h"
#include "WWGuidToStr.h"
#include "WWPrintDeviceProp.h"
#include <functiondiscoverykeys.h>
#include "WWTrackEnum.h"
#include "WWPlayStatus.h"
#include "WWChangeTrackMethod.h"

/// @param T_RenderStream ISpatialAudioObjectRenderStream または ISpatialAudioObjectRenderStreamForHrtf
/// @param T_AudioObject WWAudioObject または WWAudioHrtfObject
template <typename T_RenderStream, typename T_AudioObject>
class WWSpatialAudioUserTemplate {
public:
    virtual HRESULT Init(void) {
        dprintf("WWSpatialAudioUserTemplate::Init()\n");
        HRESULT hr = 0;

        mThreadErcd = S_OK;

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

        assert(!mBufferEvent);
        mBufferEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
        if (nullptr == mBufferEvent) {
            printf("E: WWSpatialAudioUser::Init() CreateEvent failed\n");
            hr = E_FAIL;
            goto end;
        }

    end:
        return S_OK;
    }

    virtual void Term(void) {
        dprintf("WWSpatialAudioUserTemplate::Term()\n");

        if (mSAORStream) {
            mSAORStream->Stop();
            mSAORStream->Reset();
            SafeRelease(&mSAORStream);
        }

        if (mShutdownEvent) {
            SetEvent(mShutdownEvent);
        }
        if (mRenderThread) {
            SetEvent(mRenderThread);
            WaitForSingleObject(mRenderThread, INFINITE);
            CloseHandle(mRenderThread);
            mRenderThread = nullptr;
        }

        mAudioObjectListHolder.ReleaseAll();

        if (mBufferEvent != nullptr) {
            CloseHandle(mBufferEvent);
            mBufferEvent = nullptr;
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
        SafeRelease(&mSAClient);

        if (mComInit) {
            CoUninitialize();
            mComInit = false;
        }
    }

    HRESULT DoDeviceEnumeration(void) {
        HRESULT hr = 0;
        IMMDeviceEnumerator *devEnum = nullptr;


        HRR(CoCreateInstance(__uuidof(MMDeviceEnumerator),
            nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&devEnum)));

        SafeRelease(&mDeviceCollection);
        HRR(devEnum->EnumAudioEndpoints(
            eRender, DEVICE_STATE_ACTIVE, &mDeviceCollection));

        UINT nDevices = 0;
        HRG(mDeviceCollection->GetCount(&nDevices));

        mDeviceInf.clear();
        for (UINT i = 0; i < nDevices; ++i) {
            wchar_t devIdStr[WW_DEVICE_NAME_COUNT];
            wchar_t name[WW_DEVICE_NAME_COUNT];

            HRG(WWDeviceIdStrGet(mDeviceCollection, i, devIdStr, sizeof devIdStr));
            HRG(WWDeviceNameGet(mDeviceCollection, i, name, sizeof name));
            mDeviceInf.push_back(WWDeviceInf(i, devIdStr, name));
        }

    end:
        SafeRelease(&devEnum);
        return hr;
    }

    int GetDeviceCount(void) {
        assert(mDeviceCollection);
        return (int)mDeviceInf.size();
    }

    bool GetDeviceIdStr(int id, LPWSTR devIdStr, size_t devIdStrBytes) {
        assert(devIdStr);
        memset(devIdStr, 0, devIdStrBytes);
        if (id < 0 || mDeviceInf.size() <= (unsigned int)id) {
            assert(0);
            return false;
        }
        wcsncpy_s(devIdStr, devIdStrBytes / sizeof devIdStr[0], mDeviceInf[id].devIdStr, _TRUNCATE);
        return true;
    }

    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes) {
        assert(name);
        memset(name, 0, nameBytes);
        if (id < 0 || mDeviceInf.size() <= (unsigned int)id) {
            assert(0);
            return false;
        }
        wcsncpy_s(name, nameBytes / sizeof name[0], mDeviceInf[id].name, _TRUNCATE);
        return true;
    }

    // when unchoosing device, call ChooseDevice(-1)
    HRESULT ChooseDevice(int id, int maxDynObjectCount, int staticObjectTypeMask) {
        HRESULT hr = S_OK;
     
        if (id < 0) {
            assert(mMutex);
            WaitForSingleObject(mMutex, INFINITE);
            {   // この中はgoto 不可。
                // Unchoose device
                if (mSAORStream) {
                    SafeRelease(&mSAORStream);
                }

                if (mSAClient) {
                    SafeRelease(&mSAClient);
                }

                // このSA固有のオブジェクト SAOも削除。
                mAudioObjectListHolder.ReleaseSAO();
            }
            ReleaseMutex(mMutex);
        } else {
            // Choose device
            hr = ChooseDevice1(id, maxDynObjectCount, staticObjectTypeMask);
        }
        return hr;
    }

    /// @param maxDynObjectCount 0でも可。AV Receiverの上限値は20程度。ソフトウェア実装は110程度。
    /// @param staticObjectTypeMask  1つもスタティックなオブジェクトが無いときはAudioObjectType_None。Dynamicにするとエラーが起きた。
    virtual HRESULT ActivateAudioStream(int maxDynObjectCount, int staticObjectTypeMask) =0;

    /// @param dasc [inout] 成功するとdasc.idxにユニークな番号が書き込まれる。
    HRESULT AddStream(T_AudioObject &ao) {
        HRESULT hr = S_OK;
        if (ao.pcmCtrl.IsEmpty()) {
            printf("E: WWSpatialAudioUser::AddStream data err\n");
            hr = E_FAIL;
            goto end;
        }

        if (ao.sao != nullptr) {
            printf("E: WWSpatialAudioUser::AddStream sao should be nullptr\n");
            hr = E_FAIL;
            goto end;
        }

        ao.idx = mNextDynStreamIdx;
        ++mNextDynStreamIdx;

        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            mAudioObjectListHolder.mAudioObjectList.push_back(ao);
        }
        ReleaseMutex(mMutex);

    end:
        return hr;
    }

    HRESULT ClearAllStreams(void) {
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {
            mAudioObjectListHolder.ReleaseAll();
        }
        ReleaseMutex(mMutex);

        return S_OK;
    }

    /// @param dascIdx dasc.idxを渡す。
    /// @param x 右が+ (左が-) 単位メートル
    /// @param y 上が+ (下が-) 単位メートル
    /// @param z 後ろが+ (前は-)。単位メートル。
    /// @param volume 0～1
    bool SetDynPosVolume(int dascIdx, float x, float y, float z, float volume) {
        bool rv = true;
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        do { // この中はgoto 不可。
            auto * dasc = mAudioObjectListHolder.Find(dascIdx);
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

    int PlayStreamCount(void) {
        int r = 0;

        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            r = mPlayStreamCount;
        }
        ReleaseMutex(mMutex);

        return r;
    }

    HRESULT Start(void) {
        HRESULT hr = S_OK;
        assert(mSAORStream);
        HRG(mSAORStream->Start());
    end:
        return hr;
    }

    HRESULT Stop(void) {
        HRESULT hr = S_OK;
        assert(mSAORStream);
        HRG(mSAORStream->Stop());
    end:
        return hr;
    }

    void Rewind(void) {
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {
            mAudioObjectListHolder.Rewind();
        }
        ReleaseMutex(mMutex);
    }

    HRESULT GetPlayStatus(int ch, WWPlayStatus &ps_r) {
        HRESULT hr = S_OK;

        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {
            ps_r.trackNr       = mAudioObjectListHolder.GetPlayingTrackNr(ch);
            ps_r.dummy0        = 0;
            ps_r.posFrame      = mAudioObjectListHolder.GetPlayPosition(ch);
            ps_r.totalFrameNum = mAudioObjectListHolder.GetSoundDuration(ch);
        }
        ReleaseMutex(mMutex);

        return hr;
    }

    /// スレッドのエラーコード。
    /// エラーの場合、Term()してからInit()し直す。
    HRESULT GetThreadErcd(void) const {
        return mThreadErcd;
    }

    /// @return WWTrackEnumが戻る。
    int GetPlayingTrackNr(int ch) const {
        return mAudioObjectListHolder.GetPlayingTrackNr(ch);
    }

    HRESULT UpdatePlayPosition(int64_t frame) {
        HRESULT hr = S_OK;

        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {
            hr = mAudioObjectListHolder.UpdatePlayPosition(frame);
        }
        ReleaseMutex(mMutex);
        return hr;
    }

    HRESULT SetCurrentPcm(WWTrackEnum te, WWChangeTrackMethod ctm) {
        HRESULT hr = S_OK;

        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {
            hr = mAudioObjectListHolder.SetCurrentPcm(te, ctm);
        }
        ReleaseMutex(mMutex);
        return hr;
    }

protected:
    bool mComInit = false;
    std::vector<WWDeviceInf> mDeviceInf;
    IMMDeviceCollection *mDeviceCollection = nullptr;

    UINT mMaxDynamicObjectCount = 0;
    AudioObjectType  mNativeStaticObjectTypeFlags = AudioObjectType_None;
    int mNextDynStreamIdx = 0;

    ISpatialAudioClient *mSAClient = nullptr;
    T_RenderStream *mSAORStream = nullptr;
    WWAudioObjectListHolder<T_AudioObject> mAudioObjectListHolder;
    HANDLE mRenderThread = nullptr;

    WAVEFORMATEX mUseFmt = { 0 };
    HANDLE mMutex = nullptr;
    HANDLE mShutdownEvent = nullptr;
    HANDLE mBufferEvent = nullptr;
    int    mPlayStreamCount = 0;
    HRESULT mThreadErcd = S_OK;

    HRESULT ChooseDevice1(int id, int maxDynObjectCount, int staticObjectTypeMask) {
        HRESULT hr = 0;
        IMMDevice *deviceToUse = nullptr;
        PROPVARIANT pv;
        PropVariantInit(&pv);

        // アクティベーションの設定値pv。
        auto p = reinterpret_cast<SpatialAudioClientActivationParams *>(
            CoTaskMemAlloc(sizeof(SpatialAudioClientActivationParams)));
        if (nullptr == p) {
            throw new std::bad_alloc();
        }
        memset(p, 0, sizeof *p);
        p->majorVersion = 1;
        pv.vt = VT_BLOB;
        pv.blob.cbSize = sizeof(*p);
        pv.blob.pBlobData = reinterpret_cast<BYTE *>(p);

        // Choose device
        assert(!deviceToUse);
        assert(mDeviceCollection);

        HRG(mDeviceCollection->Item(id, &deviceToUse));
        assert(deviceToUse);

        assert(!mSAClient);

        HRG(deviceToUse->Activate(
            __uuidof(ISpatialAudioClient), CLSCTX_INPROC_SERVER, &pv, (void**)&mSAClient));
        assert(mSAClient);

        HRG(mSAClient->GetMaxDynamicObjectCount(&mMaxDynamicObjectCount));

        // dynamic objects
        if (mMaxDynamicObjectCount == 0) {
            printf("  Spatial audio is not enabled\n");
            hr = E_UNSUPPORTED_TYPE;
            goto end;
        }
        printf("  Spatial audio is enabled\n");
        printf("  MaxDynamicObjectCount=%u\n", mMaxDynamicObjectCount);

        // static objects
        HRG(mSAClient->GetNativeStaticObjectTypeMask(&mNativeStaticObjectTypeFlags));
        printf("  Native Static Audio Objects : %d ch\n", WWCountNumberOf1s(mNativeStaticObjectTypeFlags));
        WWGetAndPrintStaticAudioObjectProp(mSAClient);

        // prepare WFEX
        {
            int numChannels = 1;
            int sampleRate = 48000;
            int bitsPerSample = 32;
            int byteRate = sampleRate * numChannels * bitsPerSample / 8;
            int blockAlign = numChannels * bitsPerSample / 8;

            mUseFmt.wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
            mUseFmt.nChannels = numChannels;
            mUseFmt.nSamplesPerSec = sampleRate;
            mUseFmt.nAvgBytesPerSec = byteRate;
            mUseFmt.nBlockAlign = blockAlign;
            mUseFmt.wBitsPerSample = bitsPerSample;
            mUseFmt.cbSize = 0;

            printf("  %dHz %dbit %dch x (static:%d + dyn:%d)\n",
                (int)mUseFmt.nSamplesPerSec,
                (int)mUseFmt.wBitsPerSample,
                (int)mUseFmt.nChannels,
                WWCountNumberOf1s(mNativeStaticObjectTypeFlags),
                mMaxDynamicObjectCount);
        }

        HRG(ActivateAudioStream(maxDynObjectCount, staticObjectTypeMask));
    end:
        if (FAILED(hr)) {
            SafeRelease(&mSAClient);
        }
        SafeRelease(&deviceToUse);
        PropVariantClear(&pv);
        return hr;
    }
};

