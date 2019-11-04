// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include <mmdeviceapi.h>
#include <devicetopology.h>
#include "WWUtil.h"
#include <set>

HRESULT
WWPrintStaticAudioObjectTypeFlags(ISpatialAudioClient *saClient);
const char *
WWGuidToKsDataFormatStr(GUID *guid);

class WWPrintDeviceProp {
public:
    ~WWPrintDeviceProp(void);

    HRESULT PrintDeviceProperties(IMMDevice *device);

    HRESULT PrintDeviceTopo(IMMDevice *device);

private:
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

    std::set<IConnector *> mConnSet;
    std::set<IDeviceTopology *>mTopoSet;
};
