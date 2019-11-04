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
#include "WWSpatialAudioObjects.h"

#define WW_DEVICE_NAME_COUNT (256)

struct WWDeviceInf {
    int id;
    wchar_t name[WW_DEVICE_NAME_COUNT];

    WWDeviceInf(void) {
        id = -1;
        name[0] = 0;
    }

    WWDeviceInf(int id, const wchar_t * name) {
        this->id = id;
        wcsncpy_s(this->name, _countof(this->name), name, _TRUNCATE);
    }
};

class WWSpatialAudioUser {
public:
    HRESULT Init(void);
    void Term(void);

    HRESULT DoDeviceEnumeration(void);
    int GetDeviceCount(void);
    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes);

    // when unchoosing device, call ChooseDevice(-1)
    HRESULT ChooseDevice(int id);

    HRESULT ActivateAudioStream(int maxDynObjectCount);

    /// @param dasc [inout] 成功するとdasc.idxにユニークな番号が書き込まれる。
    HRESULT AddStream(WWDynamicAudioStreamChannel &dasc);

    /// @param dascIdx dasc.idxを渡す。
    /// @param x 右が+ (左が-) 単位メートル
    /// @param y 上が+ (下が-) 単位メートル
    /// @param z 後ろが+ (前は-)。単位メートル。
    /// @param volume 0～1
    bool SetPosVolume(int dascIdx, float x, float y, float z, float volume);

    void DeactivateAudioStream(void);

    int PlayStreamCount(void);

private:
    bool mComInit = false;
    std::vector<WWDeviceInf> mDeviceInf;
    IMMDeviceCollection *mDeviceCollection = nullptr;
    IMMDevice *mDeviceToUse = nullptr;

    UINT mMaxDynamicObjectCount = 0;
    int mNextDynStreamIdx = 0;

    ISpatialAudioClient             *mSAClient = nullptr;
    ISpatialAudioObjectRenderStream *mSAORStream = nullptr;
    WWSpatialAudioObjects            mSAObjects;
    HANDLE mEvent = nullptr;

    WAVEFORMATEXTENSIBLE mUseFmt = { 0 };
    HANDLE mRenderThread = nullptr;
    HANDLE mMutex = nullptr;
    HANDLE mShutdownEvent = nullptr;
    int    mPlayStreamCount = 0;

    static DWORD RenderEntry(LPVOID lpThreadParameter);
    HRESULT RenderMain(void);
    HRESULT Render1(void);
};

