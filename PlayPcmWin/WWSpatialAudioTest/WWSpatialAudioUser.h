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
            mSAClient(nullptr) { }
    HRESULT Init(void);
    void Term(void);

    HRESULT DoDeviceEnumeration(void);
    int GetDeviceCount(void);
    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes);

    // when unchoosing device, call ChooseDevice(-1)
    HRESULT ChooseDevice(int id);

    HRESULT PrintDeviceProperties(int id);

    HRESULT PrintDeviceTopo(int id);

private:
    bool mComInit;
    std::vector<WWDeviceInf> mDeviceInf;
    IMMDeviceCollection *mDeviceCollection;
    IMMDevice *mDeviceToUse;

    ISpatialAudioClient *mSAClient;
    std::set<IConnector *> mConnSet;
    std::set<IDeviceTopology *>mTopoSet;

    HRESULT PrintDeviceTopo1(int layer, IDeviceTopology *topo);
    HRESULT PrintConnector(int layer, IConnector *conn);
    HRESULT PrintPart(int layer, IPart *part);
    HRESULT PrintSubunit(int layer, ISubunit *subUnit);
    HRESULT PrintAudioMute(int layer, IAudioMute *am);
    HRESULT PrintAudioVolumeLevel(int layer, IAudioVolumeLevel *avl);
    HRESULT PrintAudioPeakMeter(int layer, IAudioPeakMeter *apm);
    HRESULT PrintAudioAutoGainControl(int layer, IAudioAutoGainControl *agc);
    HRESULT PrintAudioBass(int layer, IAudioBass *ab);
    HRESULT PrintAudioChannelConfig(int layer, IAudioChannelConfig *acc);
    HRESULT PrintAudioInputSelector(int layer, IAudioInputSelector *ais);
    HRESULT PrintAudioLoudness(int layer, IAudioLoudness *al);
    HRESULT PrintAudioMidrange(int layer, IAudioMidrange *amid);
    HRESULT PrintAudioOutputSelector(int layer, IAudioOutputSelector *aos);
    HRESULT PrintAudioTreble(int layer, IAudioTreble *atre);
    HRESULT PrintKsJackDesc(int layer, IKsJackDescription *jd);
    HRESULT PrintKsFormatSupport(int layer, IKsFormatSupport *fs);
    HRESULT PrintControlInterface(int layer, int id, IControlInterface *ci);
};
