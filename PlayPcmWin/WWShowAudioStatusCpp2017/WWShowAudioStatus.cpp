#include "WWShowAudioStatus.h"
#include <SpatialAudioClient.h>
#include <exception>
#include <mmdeviceapi.h>
#include "WWUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>
#include "WWGuidToStr.h"

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
    SafeRelease(&device);
    PropVariantClear(&pv);
    CoTaskMemFree(deviceId);
    SafeRelease(&ps);
    return hr;
}

static HRESULT
PrintAudioObjectPosition(ISpatialAudioClient *saClient, const char *name, AudioObjectType aot)
{
    HRESULT hr = S_OK;
    float v[3];

    HRG(saClient->GetStaticObjectPosition(aot, &v[0], &v[1], &v[2]));

    printf("    %s (%f %f %f)\n", name, v[0], v[1], v[2]);

end:
    return hr;
}

static HRESULT
PrintStaticAudioObjectTypeFlags(ISpatialAudioClient *saClient)
{
    HRESULT hr = S_OK;
    AudioObjectType f = AudioObjectType_None;
    HRG(saClient->GetNativeStaticObjectTypeMask(&f));

    if (f & AudioObjectType_FrontLeft) { HRG(PrintAudioObjectPosition(saClient, "FrontLeft", AudioObjectType_FrontLeft)); }
    if (f & AudioObjectType_FrontRight) { HRG(PrintAudioObjectPosition(saClient, "FrontRight", AudioObjectType_FrontRight)); }
    if (f & AudioObjectType_FrontCenter) { HRG(PrintAudioObjectPosition(saClient, "FrontCenter", AudioObjectType_FrontCenter)); }
    if (f & AudioObjectType_LowFrequency) { HRG(PrintAudioObjectPosition(saClient, "LowFrequency", AudioObjectType_LowFrequency)); }

    if (f & AudioObjectType_SideLeft) { HRG(PrintAudioObjectPosition(saClient, "SideLeft", AudioObjectType_SideLeft)); }
    if (f & AudioObjectType_SideRight) { HRG(PrintAudioObjectPosition(saClient, "SideRight", AudioObjectType_SideRight)); }
    if (f & AudioObjectType_BackLeft) { HRG(PrintAudioObjectPosition(saClient, "BackLeft", AudioObjectType_BackLeft)); }
    if (f & AudioObjectType_BackRight) { HRG(PrintAudioObjectPosition(saClient, "BackRight", AudioObjectType_BackRight)); }
    if (f & AudioObjectType_TopFrontLeft) { HRG(PrintAudioObjectPosition(saClient, "TopFrontLeft", AudioObjectType_TopFrontLeft)); }

    if (f & AudioObjectType_TopFrontRight) { HRG(PrintAudioObjectPosition(saClient, "TopFrontRight", AudioObjectType_TopFrontRight)); }
    if (f & AudioObjectType_TopBackLeft) { HRG(PrintAudioObjectPosition(saClient, "TopBackLeft", AudioObjectType_TopBackLeft)); }
    if (f & AudioObjectType_TopBackRight) { HRG(PrintAudioObjectPosition(saClient, "TopBackRight", AudioObjectType_TopBackRight)); }
    if (f & AudioObjectType_BottomFrontLeft) { HRG(PrintAudioObjectPosition(saClient, "BottomFrontLeft", AudioObjectType_BottomFrontLeft)); }
    if (f & AudioObjectType_BottomFrontRight) { HRG(PrintAudioObjectPosition(saClient, "BottomFrontRight", AudioObjectType_BottomFrontRight)); }

    if (f & AudioObjectType_BottomBackLeft) { HRG(PrintAudioObjectPosition(saClient, "BottomBackLeft", AudioObjectType_BottomBackLeft)); }
    if (f & AudioObjectType_BottomBackRight) { HRG(PrintAudioObjectPosition(saClient, "BottomBackRight", AudioObjectType_BottomBackRight)); }
    if (f & AudioObjectType_BackCenter) { HRG(PrintAudioObjectPosition(saClient, "BackCenter", AudioObjectType_BackCenter)); }

end:
    return hr;
}

static const char *
GuidToKsDataFormatStr(GUID *guid)
{
    if (KSDATAFORMAT_SUBTYPE_ADPCM == *guid) {
        return "ADPCM";
    }
    if (KSDATAFORMAT_SUBTYPE_ALAW == *guid) {
        return "A-law";
    }
    if (KSDATAFORMAT_SUBTYPE_DRM == *guid) {
        return "DRM - encoded format";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS == *guid) {
        return "Dolby Digital Plus formatted for HDMI output";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL == *guid) {
        return "Dolby Digital Plus formatted for S / PDIF or HDMI output.";
    }
    if (KSDATAFORMAT_SUBTYPE_IEEE_FLOAT == *guid) {
        return "IEEE floating - point audio";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG == *guid) {
        return "MPEG - 1 audio payload";
    }
    if (KSDATAFORMAT_SUBTYPE_MULAW == *guid) {
        return "Myu-law coding";
    }
    if (KSDATAFORMAT_SUBTYPE_PCM == *guid) {
        return "PCM";
    } else {
        return "Unknown";
    }
}

static void
PrintLayerIndent(int layer)
{
    for (int i = 0; i < layer; ++i) {
        printf("  ");
    }
    printf("    ");
}

static const char *
ConnectorTypeToStr(ConnectorType t)
{
    switch (t) {
    case Unknown_Connector: return "Unknown_Connector";
    case Physical_Internal: return "Physical_Internal";
    case Physical_External: return "Physical_External";
    case Software_IO: return "Software_IO";
    case Software_Fixed: return "Software_Fixed";
    case Network: return "Network";
    default: return "Unknown, to be added";
    }
}

static const char *
PartTypeToStr(PartType t)
{
    switch (t) {
    case Connector: return "Connector";
    case Subunit: return "Subunit";
    default: return "Unknown, to be added";
    }
}

static const char *
EPcxConnectionTypeToStr(EPcxConnectionType t)
{
    switch (t) {
    case eConnTypeUnknown: return "Unknown";
    case eConnType3Point5mm: return "3.5mm";
    case  eConnTypeQuarter: return "Quarter";
    case  eConnTypeAtapiInternal: return "AtapiInternal";
    case  eConnTypeRCA: return "RCA";
    case  eConnTypeOptical: return "Optical";
    case  eConnTypeOtherDigital: return "OtherDigital";
    case  eConnTypeOtherAnalog: return "Analog";
    case eConnTypeMultichannelAnalogDIN: return "MultiChannelAnalogDIN";
    case  eConnTypeXlrProfessional: return "XlrProfessional";
    case  eConnTypeRJ11Modem: return "RJ11Model";
    case  eConnTypeCombination: return "Combination";
    default:
        return "Unknown, to be added";
    }
}

static const char *
EPcxGenLocationToStr(EPcxGenLocation t)
{
    switch (t) {
    case eGenLocPrimaryBox: return "PrimaryBox";
    case eGenLocInternal: return "Internal";
    case eGenLocSeparate: return "Separate";
    case eGenLocOther: return "Other";
    default:
        return "Unknown, to be added";
    }
}
static const char *
EPcxGeoLocationToStr(EPcxGeoLocation t)
{
    switch (t) {
    case eGeoLocRear: return "Rear";
    case eGeoLocFront: return "Front";
    case eGeoLocLeft: return "Left";
    case eGeoLocRight: return "Right";
    case eGeoLocTop: return "Top";
    case eGeoLocBottom: return "Bottom";
    case eGeoLocRearPanel: return "RearPanel";
    case eGeoLocRiser: return "Riser";
    case eGeoLocInsideMobileLid: return "InsideMobileLid";
    case eGeoLocDrivebay: return "Drivebay";
    case eGeoLocHDMI: return "HDMI";
    case eGeoLocOutsideMobileLid: return "OutsideMobileLid";
    case eGeoLocATAPI: return "ATAPI";
    case eGeoLocNotApplicable: return "NotApplicable";
    case eGeoLocReserved6: return "Reserved6";
    default:
        return "Unknown, to be added";
    }
}

static const char *
EPxcPortConnectionToStr(EPxcPortConnection t)
{
    switch (t) {
    case     ePortConnJack: return "Jack";
    case ePortConnIntegratedDevice: return "IntegratedDevice";
    case ePortConnBothIntegratedAndJack: return "BothIntegratedAndJack";
    case ePortConnUnknown: return "Unknown";
    default:
        return "Unknown, to be added";
    }
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■


HRESULT
WWShowAudioStatus::Init(void)
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

    stateChangedCallback = nullptr;
    mNotificationClient.SetCallback(this);

    return S_OK;
}



void
WWShowAudioStatus::Term(void)
{
    mNotificationClient.SetCallback(nullptr);
    if (mDeviceEnumerator != nullptr) {
        mDeviceEnumerator->UnregisterEndpointNotificationCallback(&mNotificationClient);
        SafeRelease(&mDeviceEnumerator);
    }

    SafeRelease(&mDeviceCollection);

    if (mComInit) {
        CoUninitialize();
        mComInit = false;
    }
}

void
WWShowAudioStatus::DestroyDeviceList(void)
{
    SafeRelease(&mDeviceCollection);

    if (mDeviceEnumerator != nullptr) {
        mDeviceEnumerator->UnregisterEndpointNotificationCallback(&mNotificationClient);
    }
    SafeRelease(&mDeviceEnumerator);

    mDeviceInf.clear();
}

HRESULT
WWShowAudioStatus::CreateDeviceList(EDataFlow dataFlow)
{
    HRESULT hr = 0;
    IMMDevice *defaultDevice = nullptr;
    LPWSTR pDefaultId = nullptr;
    IMMDevice *device = nullptr;
    LPWSTR pId = nullptr;
    UINT nDevices = 0;

    // create deviceEnumerator
    assert(mDeviceEnumerator == nullptr);
    HRR(CoCreateInstance(__uuidof(MMDeviceEnumerator),
        nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&mDeviceEnumerator)));
    mDeviceEnumerator->RegisterEndpointNotificationCallback(&mNotificationClient);

    mDeviceEnumerator->GetDefaultAudioEndpoint(dataFlow, eMultimedia, &defaultDevice);
    if (nullptr != defaultDevice) {
        defaultDevice->GetId(&pDefaultId);
    }

    // create deviceCollection
    assert(mDeviceCollection == nullptr);
    HRR(mDeviceEnumerator->EnumAudioEndpoints(
        dataFlow, DEVICE_STATE_ACTIVE, &mDeviceCollection));

    HRG(mDeviceCollection->GetCount(&nDevices));

    for (UINT i = 0; i < nDevices; ++i) {
        wchar_t name[WW_DEVICE_NAME_COUNT];
        bool bDefault = false;

        HRG(DeviceNameGet(mDeviceCollection, i, name, sizeof name));

        // default deviceかどうか調べる。
        HRG(mDeviceCollection->Item(i, &device));
        HRG(device->GetId(&pId));
        assert(pDefaultId);
        if (0 == wcsncmp(pDefaultId, pId, 256)) {
            bDefault = true;
        }

        mDeviceInf.push_back(WWDeviceInf(i, bDefault, name));

        CoTaskMemFree(pId);
        pId = nullptr;
        SafeRelease(&device);
    }

end:
    CoTaskMemFree(pId);
    CoTaskMemFree(pDefaultId);
    SafeRelease(&device);
    SafeRelease(&defaultDevice);
    return hr;
}

int
WWShowAudioStatus::GetDeviceCount(void)
{
    assert(mDeviceCollection);
    return (int)mDeviceInf.size();
}

bool
WWShowAudioStatus::GetDeviceName(int id, LPWSTR name, size_t nameBytes)
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

bool
WWShowAudioStatus::IsDefaultDevice(int id)
{
    if (id < 0 || mDeviceInf.size() <= (unsigned int)id) {
        assert(0);
        return false;
    }

    return mDeviceInf[id].isDefaultDevice;
}

HRESULT
WWShowAudioStatus::GetMixFormat(int id, WWMixFormat &saf_return)
{
    HRESULT hr = S_OK;
    IMMDevice *device = nullptr;
    IAudioClient *aClient = nullptr;
    WAVEFORMATEX *wfex = nullptr;
    WAVEFORMATEXTENSIBLE *wfext = nullptr;
    IAudioClient2 *ac2 = nullptr;
    REFERENCE_TIME hnsDevicePeriod = 0;
    REFERENCE_TIME hnsMinDevicePeriod = 0;
    REFERENCE_TIME hnsEventMinBufferDuration = 0;
    REFERENCE_TIME hnsEventMaxBufferDuration = 0;
    REFERENCE_TIME hnsTimerMinBufferDuration = 0;
    REFERENCE_TIME hnsTimerMaxBufferDuration = 0;
    BOOL offloadCapable = FALSE;

    HRG(mDeviceCollection->Item(id, &device));

    assert(device);

    HRG(device->Activate(
        __uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&aClient));
    assert(aClient);

    HRG(aClient->GetMixFormat(&wfex));

    if (22 <= wfex->cbSize) {
        wfext = (WAVEFORMATEXTENSIBLE*)wfex;
        saf_return.dwChannelMask = wfext->dwChannelMask;
    } else {
        saf_return.dwChannelMask = 0;
    }

    saf_return.sampleRate = wfex->nSamplesPerSec;
    saf_return.numChannels = wfex->nChannels;

    HRG(aClient->GetDevicePeriod(&hnsDevicePeriod, &hnsMinDevicePeriod));

    saf_return.hnsDevicePeriod = hnsDevicePeriod;
    saf_return.hnsMinDevicePeriod = hnsMinDevicePeriod;

    HRG(device->Activate(
        __uuidof(IAudioClient2), CLSCTX_INPROC_SERVER, nullptr, (void**)&ac2));
    assert(ac2);

    HRG(ac2->IsOffloadCapable(AudioCategory_Media, &offloadCapable));
    saf_return.offloadCapable = offloadCapable;
    /*
    HRG(ac2->GetBufferSizeLimits(wfex, TRUE, &hnsEventMinBufferDuration, &hnsEventMaxBufferDuration));
    HRG(ac2->GetBufferSizeLimits(wfex, FALSE, &hnsTimerMinBufferDuration, &hnsTimerMaxBufferDuration));

    saf_return.hnsEventMinBufferDuration = hnsEventMinBufferDuration;
    saf_return.hnsEventMaxBufferDuration = hnsEventMaxBufferDuration;
    saf_return.hnsTimerMinBufferDuration = hnsTimerMinBufferDuration;
    saf_return.hnsTimerMaxBufferDuration = hnsTimerMaxBufferDuration;
    */

end:
    SafeRelease(&ac2);
    CoTaskMemFree(wfex);
    SafeRelease(&aClient);
    SafeRelease(&device);
    return hr;
}

HRESULT
WWShowAudioStatus::GetSpatialAudioParams(int id, WWSpatialAudioParams &sap_return)
{
    HRESULT hr = S_OK;
    UINT32 maxDynamicObjectCount = 0;
    IAudioFormatEnumerator *afEnum = nullptr;
    UINT32 afCount = 0;
    IMMDevice *device = nullptr;
    ISpatialAudioClient *saClient = nullptr;
    assert(mDeviceCollection);
    AudioObjectType f = AudioObjectType_None;
    WAVEFORMATEX * wfex = nullptr;

    sap_return.maxDynamicObjectCount = 0;
    sap_return.sampleRate = 0;
    sap_return.maxFrameCount = 0;
    sap_return.virtualSpeakerMask = 0;

    // アクティベーションの設定値。
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

    HRG(mDeviceCollection->Item(id, &device));

    assert(device);

    HRG(device->Activate(__uuidof(ISpatialAudioClient), CLSCTX_INPROC_SERVER,
            &pv, (void**)&saClient));
    assert(saClient);

    HRG(saClient->GetMaxDynamicObjectCount(&maxDynamicObjectCount));
    sap_return.maxDynamicObjectCount = maxDynamicObjectCount;

    if (maxDynamicObjectCount == 0) {
        goto end;
    }

    HRG(saClient->GetNativeStaticObjectTypeMask(&f));
    sap_return.virtualSpeakerMask = (int)f;

    HRG(saClient->GetSupportedAudioObjectFormatEnumerator(&afEnum));

    assert(afEnum);

    HRG(afEnum->GetCount(&afCount));
    if (0 < afCount) {
        UINT32 maxFrameCount = 0;

        HRG(afEnum->GetFormat(0, &wfex));
        assert(wfex);
        sap_return.sampleRate = wfex->nSamplesPerSec;

        HRG(saClient->GetMaxFrameCount(wfex, &maxFrameCount));
        sap_return.maxFrameCount = maxFrameCount;
    }

end:
    CoTaskMemFree(wfex);
    SafeRelease(&afEnum);
    SafeRelease(&device);
    SafeRelease(&saClient);
    PropVariantClear(&pv);
    return hr;
}

HRESULT
WWShowAudioStatus::CreateDeviceNodeList(int id)
{
    HRESULT hr = S_OK;
    IDeviceTopology *devTopo = nullptr;
    IMMDevice *device = nullptr;

    assert(mDeviceNodes.size() == 0);

    assert(mDeviceCollection);

    HRG(mDeviceCollection->Item(id, &device));

    assert(device);

    HRG(device->Activate(
        __uuidof(IDeviceTopology), CLSCTX_INPROC_SERVER, nullptr, (void**)&devTopo));
    assert(devTopo);

    HRG(CollectDeviceTopo1(nullptr, devTopo));

end:
    SafeRelease(&device);
    return hr;
}

void
WWShowAudioStatus::ClearDeviceNodeList(void)
{
    for (auto ite = mDeviceNodes.begin(); ite != mDeviceNodes.end(); ++ite) {
        auto &dn = *ite;
        SafeRelease(&dn.self);
    }
    mDeviceNodes.clear();
}

bool
WWShowAudioStatus::AlreadyHave(IUnknown *p)
{
    for (auto ite = mDeviceNodes.begin(); ite != mDeviceNodes.end(); ++ite) {
        auto &dn = *ite;
        if (dn.self == p) {
            return true;
        }
    }

    return false;
}

#define ADD_DEVICENODE(t)                        \
    WWDeviceNode *pDN = nullptr;                 \
    {                                            \
        WWDeviceNode dn;                         \
        dn.self = self;                          \
        dn.parent = parent;                      \
        assert(self);                            \
        if (AlreadyHave(self)) {                 \
            dn.type = WWDeviceNode::T_Pointer;   \
            mDeviceNodes.push_back(dn);          \
            goto end;                            \
        }                                        \
        dn.type = t;                             \
        mDeviceNodes.push_back(dn);              \
    }                                            \
    pDN = &mDeviceNodes[mDeviceNodes.size() - 1];

HRESULT
WWShowAudioStatus::CollectDeviceTopo1(IUnknown *parent, IDeviceTopology *self)
{
    HRESULT hr = S_OK;
    LPWSTR devIdStr = nullptr;

    UINT nSubunit = 0;
    ISubunit *subUnit = nullptr;

    UINT nConn = 0;
    IConnector *conn = nullptr;

    ADD_DEVICENODE(WWDeviceNode::T_IDeviceTopology);

    HRG(self->GetDeviceId(&devIdStr));
    pDN->dt.idStr = std::wstring(devIdStr);

    //printf("IDeviceTopology::GetDeviceId=%S\n", devIdStr);

    HRG(self->GetSubunitCount(&nSubunit));
    for (UINT i = 0; i < nSubunit; ++i) {
        HRG(self->GetSubunit(i, &subUnit));
        HRG(CollectSubunit(self, subUnit));
    }

    HRG(self->GetConnectorCount(&nConn));
    for (UINT i = 0; i < nConn; ++i) {
        HRG(self->GetConnector(i, &conn));
        HRG(CollectConnector(self, conn));
    }

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectConnector(IUnknown *parent, IConnector *self)
{
    HRESULT hr = S_OK;
    IConnector *conn2 = nullptr;
    IPart *part = nullptr;

    ADD_DEVICENODE(WWDeviceNode::T_IConnector);

    HRG(self->GetDataFlow(&pDN->conn.df));
    HRG(self->IsConnected(&pDN->conn.bConnected));
    HRG(self->GetType(&pDN->conn.ct));

    self->QueryInterface(__uuidof(IPart), (void**)&part);
    if (part) {
        HRG(CollectPart(self, part));
    }

    if (!pDN->conn.bConnected) {
        goto end;
    }

    HRG(self->GetConnectedTo(&conn2));
    HRG(CollectConnector(self, conn2));

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectSubunit(IUnknown *parent, ISubunit *self)
{
    HRESULT hr = S_OK;
    IPart *part = nullptr;

    ADD_DEVICENODE(WWDeviceNode::T_ISubunit);

    HRG(self->QueryInterface(__uuidof(IPart), (void**)&part));

    HRG(CollectPart(self, part));

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectPart(IUnknown *parent, IPart *self)
{
    HRESULT hr = S_OK;
    LPWSTR name = nullptr;
    IDeviceTopology *partTopo = nullptr;
    IAudioMute *am = nullptr;
    IAudioVolumeLevel *avl = nullptr;
    IAudioPeakMeter *apm = nullptr;
    IAudioAutoGainControl *agc = nullptr;
    IAudioBass *ab = nullptr;
    IAudioChannelConfig *acc = nullptr;
    IAudioInputSelector *ais = nullptr;
    IAudioLoudness *al = nullptr;
    IAudioMidrange *amid = nullptr;
    IAudioOutputSelector *aos = nullptr;
    IAudioTreble *atre = nullptr;
    IKsJackDescription *jd = nullptr;
    IControlInterface *ci = nullptr;
    IKsFormatSupport *fs = nullptr;
    LPWSTR gid = nullptr;
    UINT ciCount = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IPart);

    HRG(self->GetPartType(&pDN->part.pt));
    HRG(self->GetSubType(&pDN->part.subType));
    HRG(self->GetName(&name));
    HRG(self->GetGlobalId(&gid));
    HRG(self->GetLocalId(&pDN->part.localId));
    pDN->part.name = std::wstring(name);
    pDN->part.gid = std::wstring(gid);

    HRG(self->GetTopologyObject(&partTopo));
    HRG(CollectDeviceTopo1(self, partTopo));

    HRG(self->GetControlInterfaceCount(&ciCount));
    for (UINT i = 0; i < ciCount; ++i) {
        HRG(self->GetControlInterface(i, &ci));
        HRG(CollectControlInterface(self, i, ci));
    }

    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioMute), (void**)&am);
    if (am) {
        HRG(CollectAudioMute(self, am));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioVolumeLevel), (void**)&avl);
    if (avl) {
        HRG(CollectAudioVolumeLevel(self, avl));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioPeakMeter), (void**)&apm);
    if (apm) {
        HRG(CollectAudioPeakMeter(self, apm));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioAutoGainControl), (void**)&agc);
    if (agc) {
        HRG(CollectAudioAutoGainControl(self, agc));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioBass), (void**)&ab);
    if (ab) {
        HRG(CollectAudioBass(self, ab));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioChannelConfig), (void**)&acc);
    if (acc) {
        HRG(CollectAudioChannelConfig(self, acc));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioInputSelector), (void**)&ais);
    if (ais) {
        HRG(CollectAudioInputSelector(self, ais));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioLoudness), (void**)&al);
    if (al) {
        HRG(CollectAudioLoudness(self, al));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioMidrange), (void**)&amid);
    if (amid) {
        HRG(CollectAudioMidrange(self, amid));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioOutputSelector), (void**)&aos);
    if (aos) {
        HRG(CollectAudioOutputSelector(self, aos));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioTreble), (void**)&atre);
    if (atre) {
        HRG(CollectAudioTreble(self, atre));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IKsJackDescription), (void**)&jd);
    if (jd) {
        HRG(CollectKsJackDesc(self, jd));
    }
    self->Activate(CLSCTX_INPROC_SERVER, __uuidof(IKsFormatSupport), (void**)&fs);
    if (fs) {
        HRG(CollectKsFormatSupport(self, fs));
    }

end:
    CoTaskMemFree(gid);
    CoTaskMemFree(name);
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioMute(IUnknown *parent, IAudioMute *self)
{
    HRESULT hr = S_OK;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioMute);

    HRG(self->GetMute(&pDN->mute.mute));

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioAutoGainControl(IUnknown *parent, IAudioAutoGainControl *self)
{
    HRESULT hr = S_OK;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioAutoGainControl);

    HRG(self->GetEnabled(&pDN->agc.bEnabled));

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioPeakMeter(IUnknown *parent, IAudioPeakMeter *self)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float curL = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioPeakMeter);

    HRG(self->GetChannelCount(&nCh));
    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(self->GetLevel(ch, &curL));
        pDN->apm.curL.push_back(curL);
    }

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioVolumeLevel(IUnknown *parent, IAudioVolumeLevel *self)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioVolumeLevel);

    HRG(self->GetChannelCount(&nCh));
    for (UINT ch = 0; ch < nCh; ++ch) {
        WWAudioVolumeLevel1ch lvl;
        HRG(self->GetLevelRange(ch, &lvl.minL, &lvl.maxL, &lvl.stepL));
        HRG(self->GetLevel(ch, &lvl.curL));
        pDN->avl.lvl.push_back(lvl);
    }

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioBass(IUnknown *parent, IAudioBass *self)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioBass);

    HRG(self->GetChannelCount(&nCh));
    for (UINT ch = 0; ch < nCh; ++ch) {
        WWAudioVolumeLevel1ch lvl;
        HRG(self->GetLevelRange(ch, &lvl.minL, &lvl.maxL, &lvl.stepL));
        HRG(self->GetLevel(ch, &lvl.curL));
        pDN->avl.lvl.push_back(lvl);
    }

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectAudioMidrange(IUnknown *parent, IAudioMidrange *self)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioMidrange);

    HRG(self->GetChannelCount(&nCh));
    for (UINT ch = 0; ch < nCh; ++ch) {
        WWAudioVolumeLevel1ch lvl;
        HRG(self->GetLevelRange(ch, &lvl.minL, &lvl.maxL, &lvl.stepL));
        HRG(self->GetLevel(ch, &lvl.curL));
        pDN->avl.lvl.push_back(lvl);
    }

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectAudioTreble(IUnknown *parent, IAudioTreble *self)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioMidrange);

    HRG(self->GetChannelCount(&nCh));
    for (UINT ch = 0; ch < nCh; ++ch) {
        WWAudioVolumeLevel1ch lvl;
        HRG(self->GetLevelRange(ch, &lvl.minL, &lvl.maxL, &lvl.stepL));
        HRG(self->GetLevel(ch, &lvl.curL));
        pDN->avl.lvl.push_back(lvl);
    }

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioChannelConfig(IUnknown *parent, IAudioChannelConfig *self)
{
    HRESULT hr = S_OK;

    ADD_DEVICENODE(WWDeviceNode::T_IAudioChannelConfig);

    HRG(self->GetChannelConfig(&pDN->acc.cc));

end:
    return hr;
}

HRESULT
WWShowAudioStatus::CollectAudioInputSelector(IUnknown *parent, IAudioInputSelector *self)
{
    HRESULT hr = S_OK;
    ADD_DEVICENODE(WWDeviceNode::T_IAudioInputSelector);
    HRG(self->GetSelection(&pDN->ais.id));

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectAudioLoudness(IUnknown *parent, IAudioLoudness *self)
{
    HRESULT hr = S_OK;
    ADD_DEVICENODE(WWDeviceNode::T_IAudioLoudness);
    HRG(self->GetEnabled(&pDN->al.bEnabled));

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectAudioOutputSelector(IUnknown *parent, IAudioOutputSelector *self)
{
    HRESULT hr = S_OK;
    ADD_DEVICENODE(WWDeviceNode::T_IAudioOutputSelector);
    HRG(self->GetSelection(&pDN->aos.id));

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectKsJackDesc(IUnknown *parent, IKsJackDescription *self)
{
    HRESULT hr = S_OK;
    UINT n = 0;
    KSJACK_DESCRIPTION desc{ 0 };

    ADD_DEVICENODE(WWDeviceNode::T_IKsJackDescription);

    HRG(self->GetJackCount(&n));
    for (UINT i = 0; i < n; ++i) {
        HRG(self->GetJackDescription(i, &desc));
        pDN->jd.desc.push_back(desc);
    }

end:
    return hr;
}

HRESULT WWShowAudioStatus::CollectControlInterface(IUnknown *parent, int id, IControlInterface *self)
{
    HRESULT hr = S_OK;
    LPWSTR name = nullptr;
    GUID iid;

    ADD_DEVICENODE(WWDeviceNode::T_IControlInterface);

    HRG(self->GetName(&name));
    pDN->ci.name = std::wstring(name);
    HRG(self->GetIID(&iid));
    pDN->ci.iid = WWGuidToStr(iid);

    //printf("name=%S\n", name);

end:
    CoTaskMemFree(name);
    return hr;
}

static const int gSampleRateList[] =
{ 32000 / 4, 44100 / 4, 48000 / 4,
  32000 / 2, 44100 / 2, 48000 / 2,
  32000 * 1, 44100 * 1, 48000 * 1,
  32000 * 2, 44100 * 2, 48000 * 2,
  32000 * 4, 44100 * 4, 48000 * 4,
  32000 * 8, 44100 * 8, 48000 * 8,
};

struct BitsPerSamplePair {
    int valid;
    int container;
    bool bFloat;
};

static const BitsPerSamplePair gBitsPerSamples[] = {
    {8, 8, false},   // 0
    {16, 16, false}, // 1
    {24, 24, false}, // 2
    {24, 32, false}, // 3
    {32, 32, false}, // 4
    {32, 32, true},  // 5
};

struct ChannelParam {
    int ch;
    int dwChanelMask;
};

static const ChannelParam gChannelParams[] = {
    {1, 0},
    {2, 3},
    {4, 0x33},
    {6, 0x3f},
    {8, 0xff},
};

static int NumChannelsToChIdx(int ch) {
    switch (ch) {
    case 1: return 0;
    case 2: return 1;
    case 4: return 2;
    case 6: return 3;
    case 8: return 4;
    default:
        assert(0);
        return 0;
    }
}

HRESULT WWShowAudioStatus::CollectKsFormatSupport(IUnknown *parent, IKsFormatSupport *self)
{
    HRESULT hr = S_OK;
    PKSDATAFORMAT kdf = nullptr;
    uint8_t buff[104];
    PKSDATAFORMAT testDF = nullptr;
    WAVEFORMATEXTENSIBLE *wfext;

    int szKDF = sizeof(KSDATAFORMAT);
    int szWFEX = sizeof(WAVEFORMATEX);
    int szWFEXT = sizeof(WAVEFORMATEXTENSIBLE);

    ADD_DEVICENODE(WWDeviceNode::T_IKsFormatSupport);

    HRG(self->GetDevicePreferredFormat(&kdf));
    pDN->fs.df = *kdf;

    if (KSDATAFORMAT_TYPE_AUDIO == kdf->MajorFormat
            && KSDATAFORMAT_SUBTYPE_PCM == kdf->SubFormat
            && KSDATAFORMAT_SPECIFIER_WAVEFORMATEX == kdf->Specifier) {
        wfext = (WAVEFORMATEXTENSIBLE*)&kdf[1];

        int valid = wfext->Format.wBitsPerSample;
        int container = wfext->Format.wBitsPerSample;

        pDN->fs.preferredFmt.containerBitsPerSample = wfext->Format.wBitsPerSample;
        if (22 <= wfext->Format.cbSize) {
            pDN->fs.preferredFmt.validBitsPerSample = wfext->Samples.wValidBitsPerSample;
            pDN->fs.preferredFmt.bFloat = (wfext->SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT);
        } else {
            pDN->fs.preferredFmt.validBitsPerSample = wfext->Format.wBitsPerSample;
            pDN->fs.preferredFmt.bFloat = 0;
        }
        for (int i = 0; i < sizeof gChannelParams / sizeof gChannelParams[0]; ++i) {
            pDN->fs.preferredFmt.ch[i] = 0;
        }
        pDN->fs.preferredFmt.ch[NumChannelsToChIdx(wfext->Format.nChannels)] = 1;
        pDN->fs.preferredFmt.sampleRate = wfext->Format.nSamplesPerSec;
    }

    testDF = (PKSDATAFORMAT)buff;
    testDF->MajorFormat = KSDATAFORMAT_TYPE_AUDIO;
    testDF->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
    testDF->Specifier = KSDATAFORMAT_SPECIFIER_WAVEFORMATEX;
    testDF->FormatSize = 104;
    testDF->Flags = 0;
    testDF->SampleSize = 0;
    testDF->Reserved = 0;
    testDF->Alignment = 104;
    wfext = (WAVEFORMATEXTENSIBLE*)&buff[sizeof(KSDATAFORMAT)];
    wfext->Format.wFormatTag = 65534;
    wfext->Format.cbSize = 22;

    for (int srIdx = 0; srIdx < sizeof gSampleRateList / sizeof gSampleRateList[0]; ++srIdx) {
        int sr = gSampleRateList[srIdx];
        wfext->Format.nSamplesPerSec = sr;

        for (int bIdx = 0; bIdx < sizeof gBitsPerSamples / sizeof gBitsPerSamples[0]; ++bIdx) {
            auto & bits = gBitsPerSamples[bIdx];
            if (bits.bFloat) {
                wfext->SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
            } else {
                wfext->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
            }

            int supportCnt = 0;
            WWKsFormat fmt;
            
            for (int i = 0; i < sizeof gChannelParams / sizeof gChannelParams[0]; ++i) {
                fmt.ch[i] = 0;
            }
            fmt.bFloat = bits.bFloat;
            fmt.containerBitsPerSample = bits.container;
            fmt.sampleRate = sr;
            fmt.validBitsPerSample = bits.valid;

            for (int chIdx = 0; chIdx < sizeof gChannelParams / sizeof gChannelParams[0]; ++chIdx) {
                BOOL supported = FALSE;
                auto &chs = gChannelParams[chIdx];

                wfext->Format.nChannels = chs.ch;
                wfext->dwChannelMask = chs.dwChanelMask;

                wfext->Format.wBitsPerSample = bits.container;
                wfext->Samples.wValidBitsPerSample = bits.valid;

                wfext->Format.nBlockAlign = (wfext->Format.nChannels * wfext->Format.wBitsPerSample) / 8;
                wfext->Format.nAvgBytesPerSec = wfext->Format.nSamplesPerSec * wfext->Format.nBlockAlign;

                HRG(self->IsFormatSupported(testDF, 104, &supported));
                if (supported) {
                    ++supportCnt;
                    fmt.ch[chIdx] = 1;
                } else {
                    fmt.ch[chIdx] = 0;
                }
            }

            if (0 < supportCnt) {
                pDN->fs.supportFmts.push_back(fmt);
            }
        }
    }

end:
    CoTaskMemFree(kdf);
    return hr;
}

HRESULT WWShowAudioStatus::GetDeviceNodeNth(int nth, WWDeviceNode &dn_return)
{
    if (nth < 0 || mDeviceNodes.size() <= nth) {
        return E_INVALIDARG;
    }

    dn_return = mDeviceNodes[nth];

    return S_OK;
}
