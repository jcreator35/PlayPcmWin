// 日本語
#pragma once
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
#include "WWDynAudioObjectListTemplate.h"
#include "WWDeviceInf.h"
#include "WWUtil.h"
#include "WWGuidToStr.h"
#include "WWPrintDeviceProp.h"
#include <functiondiscoverykeys.h>

template <typename T_RenderStream, typename T_DynAudioObject>
class WWSpatialAudioUserTemplate {
public:
    virtual HRESULT Init(void) {
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

        mDynObjectList.ReleaseAll();

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
        SafeRelease(&mDeviceToUse);
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

        HRR(devEnum->EnumAudioEndpoints(
            eRender, DEVICE_STATE_ACTIVE, &mDeviceCollection));

        UINT nDevices = 0;
        HRG(mDeviceCollection->GetCount(&nDevices));

        for (UINT i = 0; i < nDevices; ++i) {
            wchar_t name[WW_DEVICE_NAME_COUNT];
            HRG(WWDeviceNameGet(mDeviceCollection, i, name, sizeof name));
            mDeviceInf.push_back(WWDeviceInf(i, name));
        }

    end:
        SafeRelease(&devEnum);
        return hr;
    }

    int GetDeviceCount(void) {
        assert(mDeviceCollection);
        return (int)mDeviceInf.size();
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
    HRESULT ChooseDevice(int id) {
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

    /// @param dasc [inout] 成功するとdasc.idxにユニークな番号が書き込まれる。
    HRESULT AddStream(T_DynAudioObject &dasc) {
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
            mDynObjectList.mDynAudioObjectList.push_back(dasc);
        }
        ReleaseMutex(mMutex);

    end:
        return hr;
    }

    /// @param dascIdx dasc.idxを渡す。
    /// @param x 右が+ (左が-) 単位メートル
    /// @param y 上が+ (下が-) 単位メートル
    /// @param z 後ろが+ (前は-)。単位メートル。
    /// @param volume 0～1
    bool SetPosVolume(int dascIdx, float x, float y, float z, float volume) {
        bool rv = true;
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        do { // この中はgoto 不可。
            auto * dasc = mDynObjectList.Find(dascIdx);
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

    void DeactivateAudioStream(void) {
        mDynObjectList.ReleaseAll();
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

protected:
    bool mComInit = false;
    std::vector<WWDeviceInf> mDeviceInf;
    IMMDeviceCollection *mDeviceCollection = nullptr;
    IMMDevice *mDeviceToUse = nullptr;

    UINT mMaxDynamicObjectCount = 0;
    int mNextDynStreamIdx = 0;

    ISpatialAudioClient *mSAClient = nullptr;
    T_RenderStream *mSAORStream = nullptr;
    WWDynAudioObjectListTemplate<T_DynAudioObject> mDynObjectList;
    HANDLE mRenderThread = nullptr;

    WAVEFORMATEXTENSIBLE mUseFmt = { 0 };
    HANDLE mMutex = nullptr;
    HANDLE mShutdownEvent = nullptr;
    HANDLE mBufferEvent = nullptr;
    int    mPlayStreamCount = 0;
};

