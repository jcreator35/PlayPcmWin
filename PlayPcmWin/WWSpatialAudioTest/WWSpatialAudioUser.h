#pragma once
#include <Windows.h>
#include <MMDeviceAPI.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <vector>
#include <string.h>
#include <SpatialAudioClient.h>
#include <SpatialAudioMetadata.h>

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
    WWSpatialAudioUser(void) : mComInit(false), mDeviceCollection(nullptr), mDeviceToUse(nullptr),
            mSAClient(nullptr), mSAMClient(nullptr) { }
    HRESULT Init(void);
    void Term(void);

    HRESULT DoDeviceEnumeration(void);
    int GetDeviceCount(void);
    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes);

    // when unchoosing device, call ChooseDevice(-1)
    HRESULT ChooseDevice(int id);

    HRESULT PrintDeviceProperties(int id);

private:
    bool mComInit;
    std::vector<WWDeviceInf> mDeviceInf;
    IMMDeviceCollection *mDeviceCollection;
    IMMDevice *mDeviceToUse;

    ISpatialAudioClient *mSAClient;
    ISpatialAudioMetadataClient *mSAMClient;

    HRESULT PrintSAMetadata(int id);
};
