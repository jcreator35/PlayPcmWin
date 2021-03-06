﻿// 日本語。

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
#include <string>
#include "WWMMNotificationClient.h"
#include <endpointvolume.h>

#define WW_SAS_STRING_COUNT (256)

typedef void(__stdcall WWStateChanged)(LPCWSTR deviceIdStr, int dwNewState);

struct WWDeviceParams {
    int id;
    bool isDefaultDevice;
    BOOL mute;
    EDataFlow dataFlow;
    float masterVolumeLevelDecibel;
    float peak;
    wchar_t idStr[WW_SAS_STRING_COUNT];
    wchar_t name[WW_SAS_STRING_COUNT];
};

struct WWMixFormat {
    int sampleRate;
    int numChannels;
    int dwChannelMask;
    int offloadCapable;
    int64_t hnsDevicePeriod;
    int64_t hnsMinDevicePeriod;
};


struct WWSpatialAudioParams {
    int maxDynamicObjectCount;
    int virtualSpeakerMask;
    int sampleRate;
    int maxFrameCount;
};

struct WWAudioVolumeLevel1ch {
    float minL;
    float maxL;
    float stepL;
    float curL;
};

struct WWAudioVolumeLevel {
    std::vector<WWAudioVolumeLevel1ch> lvl;
};

struct WWAudioPeakMeter {
    std::vector<float> curL;
};

struct WWAudioAutoGainControl {
    BOOL bEnabled;
};

struct WWAudioChannelConfig {
    DWORD cc;
};

struct WWAudioInputSelector {
    UINT id;
};

struct WWAudioOutputSelector {
    UINT id;
};

struct WWAudioLoudness {
    BOOL bEnabled;
};

struct WWDeviceTopology {
    std::wstring idStr;
};

struct WWConnector {
    DataFlow df;
    BOOL bConnected;
    ConnectorType ct;
};

struct WWPart {
    PartType pt;
    UINT localId;
    GUID subType;
    std::wstring name;
    std::wstring gid;
};

struct WWAudioMute {
    BOOL mute;
};

struct WWKsJackDescription {
    std::vector<KSJACK_DESCRIPTION> desc;
};

struct WWControlInterface {
    std::wstring name;
    std::wstring iid;
};

struct WWKsFormat {
    int sampleRate;
    int ch[5]; // 1,2,4,6,8
    int containerBitsPerSample;
    int validBitsPerSample;
    int bFloat;
};

struct WWKsFormatSupport {
    KSDATAFORMAT df;
    WWKsFormat preferredFmt;
    std::vector<WWKsFormat> supportFmts;
};

struct WWDeviceNode {
    enum Type {
        T_IDeviceTopology,
        T_IConnector,
        T_IPart,
        T_ISubunit,
        T_IAudioMute,

        T_IAudioVolumeLevel,
        T_IAudioPeakMeter,
        T_IAudioAutoGainControl,
        T_IAudioBass,
        T_IAudioChannelConfig,

        T_IAudioInputSelector,
        T_IAudioLoudness,
        T_IAudioMidrange,
        T_IAudioOutputSelector,
        T_IAudioTreble,

        T_IKsJackDescription,
        T_IKsFormatSupport,
        T_IControlInterface,
        T_Pointer,
    };

    IUnknown *self;
    IUnknown *parent;
    Type type;

    WWDeviceTopology dt;
    WWConnector conn;
    WWPart part;
    WWAudioMute mute;

    // AudioVolumeLevel or AudioBass
    WWAudioVolumeLevel avl;
    WWAudioPeakMeter apm;
    WWAudioAutoGainControl agc;
    WWAudioChannelConfig acc;
    WWAudioInputSelector ais;
    WWAudioLoudness al;
    WWAudioOutputSelector aos;
    WWKsJackDescription jd;
    WWControlInterface ci;
    WWKsFormatSupport fs;
};

struct WWAudioSession {
    int nth;
    std::wstring displayName;
    std::wstring iconPath;
    GUID groupingParam;
    AudioSessionState state;
    DWORD pid;

    std::wstring sessionId;
    std::wstring sessionInstanceId;
    BOOL isSystemSoundsSession;
    std::vector<float> channelPeaks;
    float peak;
    float masterVolume;
    BOOL mute;
};

class WWShowAudioStatus : public IWWDeviceStateCallback {
public:
    WWShowAudioStatus(void)
        : stateChangedCallback(nullptr) , mComInit(false),
        mDeviceEnumerator(nullptr),
        mDeviceCollection(nullptr) { }

    HRESULT Init(void);
    void Term(void);

    HRESULT CreateDeviceList(EDataFlow dataFlow);
    int GetDeviceCount(void);
    WWDeviceParams *GetDeviceParams(int id);
    HRESULT GetMixFormat(int id, WWMixFormat &saf_return);
    HRESULT GetSpatialAudioParams(int id, WWSpatialAudioParams &sap_return);
    void DestroyDeviceList(void);

    HRESULT CreateDeviceNodeList(int id);
    int NumOfDeviceNodes(void) const { return (int)mDeviceNodes.size(); }
    HRESULT GetDeviceNodeNth(int nth, WWDeviceNode &dn_return);
    void ClearDeviceNodeList(void);

    HRESULT CreateAudioSessionList(int id);
    int NumOfAudioSessions(void) const { return (int)mAudioSessions.size(); }
    HRESULT GetAudioSessionNth(int nth, WWAudioSession &as_return);
    void ClearAudioSessionList(void);

    WWStateChanged * stateChangedCallback;

    // implements IWWDeviceStateCallback
    virtual HRESULT OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState) override {
        if (stateChangedCallback) {
            stateChangedCallback(pwstrDeviceId, dwNewState);
        }
        return S_OK;
    }

private:
    bool mComInit;
    std::vector<WWDeviceParams> mDeviceInf;
    IMMDeviceEnumerator *mDeviceEnumerator;
    IMMDeviceCollection *mDeviceCollection;
    WWMMNotificationClient mNotificationClient;

    std::vector<WWDeviceNode> mDeviceNodes;
    EDataFlow mDataFlow;

    std::vector<WWAudioSession> mAudioSessions;

    HRESULT DeviceGetParams(IMMDeviceCollection *dc, UINT id, WWDeviceParams *dInf);


    bool AlreadyHave(IUnknown *p);

    HRESULT CollectDeviceTopo1(IUnknown *parent, IDeviceTopology *topo);

    HRESULT CollectConnector(IUnknown *parent, IConnector *conn);
    HRESULT CollectPart(IUnknown *parent, IPart *part);
    HRESULT CollectSubunit(IUnknown *parent, ISubunit *subUnit);
    HRESULT CollectAudioMute(IUnknown *parent, IAudioMute *am);
    HRESULT CollectAudioVolumeLevel(IUnknown *parent, IAudioVolumeLevel *avl);
    HRESULT CollectAudioPeakMeter(IUnknown *parent, IAudioPeakMeter *apm);
    HRESULT CollectAudioAutoGainControl(IUnknown *parent, IAudioAutoGainControl *agc);
    HRESULT CollectAudioBass(IUnknown *parent, IAudioBass *ab);
    HRESULT CollectAudioChannelConfig(IUnknown *parent, IAudioChannelConfig *acc);
    HRESULT CollectAudioInputSelector(IUnknown *parent, IAudioInputSelector *ais);
    HRESULT CollectAudioLoudness(IUnknown *parent, IAudioLoudness *al);
    HRESULT CollectAudioMidrange(IUnknown *parent, IAudioMidrange *amid);
    HRESULT CollectAudioOutputSelector(IUnknown *parent, IAudioOutputSelector *aos);
    HRESULT CollectAudioTreble(IUnknown *parent, IAudioTreble *atre);
    HRESULT CollectKsJackDesc(IUnknown *parent, IKsJackDescription *jd);
    HRESULT CollectKsFormatSupport(IUnknown *parent, IKsFormatSupport *fs);
    HRESULT CollectControlInterface(IUnknown *parent, int id, IControlInterface *ci);

    HRESULT CollectAudioSession(IAudioSessionEnumerator *ae, int nth);
};
