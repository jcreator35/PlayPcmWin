// 日本語。

#define WWSHOWAUDIOSTATUS_EXPORTS
#include "WWShowAudioStatusCppIF.h"

#include "WWUtil.h"
#include <map>
#include <assert.h>
#include "WWMMNotificationClient.h"

static int gNextInstanceId = 100;

std::map<int, WWShowAudioStatus*> gInstanceMap;


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASInit(void)
{
    HRESULT hr = S_OK;
    int instanceId = gNextInstanceId++;

    auto *p = new WWShowAudioStatus();
    HRG(p->Init());

    gInstanceMap[instanceId] = p;
    hr = instanceId;

end:
    return hr;
}

#define FIND_P                                                 \
    WWShowAudioStatus *p = nullptr;                            \
    if (gInstanceMap.find(instanceId) == gInstanceMap.end()) { \
        return E_NOTFOUND;                                     \
    }                                                          \
    p = gInstanceMap[instanceId];

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASTerm(int instanceId)
{
    FIND_P;

    p->Term();
    delete p;

    gInstanceMap.erase(instanceId);
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASCreateDeviceList(int instanceId, int dataFlow)
{
    HRESULT hr = S_OK;
    FIND_P;

    HRG(p->CreateDeviceList((EDataFlow)dataFlow));
end:
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASDestroyDeviceList(int instanceId)
{
    HRESULT hr = S_OK;
    FIND_P;

    p->DestroyDeviceList();
end:
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceCount(int instanceId)
{
    FIND_P;

    return p->GetDeviceCount();
}


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceParams(
    int instanceId,
    int idx,
    WWSASAudioDeviceParams * params_return)
{
    FIND_P;

    if (idx < 0 || p->GetDeviceCount() <= idx) {
        return E_INVALIDARG;
    }

    params_return->id = idx;
    params_return->isDefaultDevice = p->IsDefaultDevice(idx);
    p->GetDeviceName(idx, params_return->name, sizeof params_return->name);
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetMixFormat(
    int instanceId,
    int idx,
    WWMixFormat * saf_return)
{
    FIND_P;

    if (idx < 0 || p->GetDeviceCount() <= idx) {
        return E_INVALIDARG;
    }

    return p->GetMixFormat(idx, *saf_return);
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetSpatialAudioParams(
    int instanceId,
    int idx,
    WWSpatialAudioParams * sap_return)
{
    FIND_P;

    if (idx < 0 || p->GetDeviceCount() <= idx) {
        return E_INVALIDARG;
    }

    return p->GetSpatialAudioParams(idx, *sap_return);
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASCreateDeviceNodeList(
    int instanceId,
    int idx)
{
    FIND_P;

    if (idx < 0 || p->GetDeviceCount() <= idx) {
        return E_INVALIDARG;
    }

    return p->CreateDeviceNodeList(idx);
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceNodeNum(
    int instanceId)
{
    FIND_P;

    return p->NumOfDeviceNodes();
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceNodeNth(
    int instanceId,
    int idx,
    WWDeviceNodeIF *dn_return)
{
    WWDeviceNode dn;
    FIND_P;

    assert(dn_return);
    int hr = p->GetDeviceNodeNth(idx, dn);
    if (FAILED(hr)) {
        return hr;
    }

    dn_return->self = (uint64_t)dn.self;
    dn_return->parent = (uint64_t)dn.parent;
    dn_return->type = (int)dn.type;
    return S_OK;
}


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASClearDeviceNodeList(
    int instanceId)
{
    FIND_P;

    p->ClearDeviceNodeList();
    return S_OK;
}

#define PROLOGUE_PARAMS(t)                 \
    WWDeviceNode dn;                       \
    FIND_P;                                \
    int hr = p->GetDeviceNodeNth(idx, dn); \
    if (FAILED(hr)) {                      \
        return hr;                         \
    }                                      \
    if (dn.type != t) {                    \
        return E_INVALIDARG;               \
    }


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioMuteParams(
    int instanceId,
    int idx,
    WWAudioMuteIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IAudioMute);

    param_return->bEnabled = dn.mute.mute;
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioVolumeLevelParams(
    int instanceId,
    int idx,
    WWAudioVolumeLevelIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IAudioVolumeLevel);

    int nChannels = (int)dn.avl.lvl.size();
    if (WW_NUM_CHANNELS < nChannels) {
        nChannels = WW_NUM_CHANNELS;
    }

    param_return->nChannels = nChannels;
    for (int i = 0; i < nChannels; ++i) {
        param_return->volumeLevels[i] = dn.avl.lvl[i].curL;
    }

    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsJackDescriptionsParams(
    int instanceId,
    int idx,
    WWKsJackDescriptionsIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IKsJackDescription);

    int nChannels = (int)dn.jd.desc.size();
    if (WW_NUM_CHANNELS < nChannels) {
        nChannels = WW_NUM_CHANNELS;
    }

    param_return->nChannels = nChannels;
    for (int i = 0; i < nChannels; ++i) {
        auto &f = dn.jd.desc[i];
        WWKsJackDescriptionIF d;
        d.ChannelMapping = (int)f.ChannelMapping;
        d.Color = (int)f.Color;
        d.ConnectionType = (int)f.ConnectionType;
        d.GenLocation = (int)f.GenLocation;
        d.GeoLocation = (int)f.GeoLocation;
        d.IsConnected = (int)f.IsConnected;
        d.PortConnection = (int)f.PortConnection;
        param_return->desc[i] = d;
    }

    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioInputSelectorParams(
    int instanceId,
    int idx,
    WWAudioInputSelectorIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IAudioInputSelector);

    param_return->id = dn.ais.id;
    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetPartParams(
    int instanceId,
    int idx,
    WWPartIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IPart);

    param_return->partType = (int)dn.part.pt;
    param_return->localId = (int)dn.part.localId;
    wcscpy_s(param_return->name, dn.part.name.c_str());
    wcscpy_s(param_return->gid, dn.part.gid.c_str());

    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetControlInterfaceParams(
    int instanceId,
    int idx,
    WWControlInterfaceIF *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IControlInterface);

    wcscpy_s(param_return->name, dn.ci.name.c_str());
    wcscpy_s(param_return->iid, dn.ci.iid.c_str());

    return S_OK;
}


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatpreferredFmt(
    int instanceId,
    int idx,
    WWKsFormat *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IKsFormatSupport);

    *param_return = dn.fs.preferredFmt;

    return S_OK;
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatSupportedFmtNum(
    int instanceId,
    int idx)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IKsFormatSupport);

    return (int)dn.fs.supportFmts.size();
}

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioChannelConfig(
    int instanceId,
    int idx)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IAudioChannelConfig);

    return (int)dn.acc.cc;
}


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatSupportedFmtNth(
    int instanceId,
    int idx,
    int nth,
    WWKsFormat *param_return)
{
    PROLOGUE_PARAMS(WWDeviceNode::T_IKsFormatSupport);

    if (nth < 0 || dn.fs.supportFmts.size() <= nth) {
        return E_INVALIDARG;
    }

    *param_return = dn.fs.supportFmts[nth];

    return S_OK;
}


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASRegisterStateChangedCallback(int instanceId, WWStateChanged callback)
{
    FIND_P;

    p->stateChangedCallback = callback;
    return S_OK;
}