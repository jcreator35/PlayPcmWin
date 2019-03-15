#include "WWSpatialAudioUser.h"
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

    return S_OK;
}



void
WWSpatialAudioUser::Term(void)
{
    assert(!mDeviceToUse);

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

    if (id < 0) {
        goto end;
    }

    assert(!mDeviceToUse);


end:
    SafeRelease(&mDeviceCollection);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintDeviceProperties(int id)
{
    HRESULT hr = S_OK;
    UINT32 maxDynamicObjectCount = 0;
    IAudioFormatEnumerator *afEnum = nullptr;
    UINT32 afCount = 0;

    assert(mDeviceCollection);
    assert(!mDeviceToUse);

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

    HRG(mDeviceCollection->Item(id, &mDeviceToUse));

    assert(mDeviceToUse);
    assert(!mSAClient);

    HRG(mDeviceToUse->Activate(
        __uuidof(ISpatialAudioClient), CLSCTX_INPROC_SERVER, &pv, (void**)&mSAClient));
    assert(mSAClient);

    HRG(mSAClient->GetMaxDynamicObjectCount(&maxDynamicObjectCount));

    if (maxDynamicObjectCount == 0) {
        printf("    Spatial audio is not enabled\n");
        goto end;
    }
    printf("    Spatial audio is enabled\n");
    printf("    MaxDynamicObjectCount=%u\n", maxDynamicObjectCount);

    PrintStaticAudioObjectTypeFlags(mSAClient);

    HRG(mSAClient->GetSupportedAudioObjectFormatEnumerator(&afEnum));

    assert(afEnum);

    HRG(afEnum->GetCount(&afCount));
    for (UINT32 i = 0; i < afCount; ++i) {
        UINT32 maxFrameCount = 0;
        WAVEFORMATEX * wfex = nullptr;
        HRG(afEnum->GetFormat(i, &wfex));
        
        assert(wfex);

        HRG(mSAClient->GetMaxFrameCount(wfex, &maxFrameCount));

        if (wfex->wFormatTag != WAVE_FORMAT_EXTENSIBLE) {
            printf("  %d: %dHz %dbit %dch\n",
                i, (int)wfex->nSamplesPerSec, (int)wfex->wBitsPerSample,
                (int)wfex->nChannels);
        } else {
            WAVEFORMATEXTENSIBLE *wfext = (WAVEFORMATEXTENSIBLE *)wfex;
            printf("  %d: %dHz %dbit %dch container=%dbit %s\n",
                i, (int)wfex->nSamplesPerSec, (int)wfext->Samples.wValidBitsPerSample,
                (int)wfex->nChannels, (int)wfex->wBitsPerSample,
                GuidToKsDataFormatStr(&wfext->SubFormat));
        }
    }


end:
    SafeRelease(&afEnum);
    SafeRelease(&mDeviceToUse);
    SafeRelease(&mSAClient);
    PropVariantClear(&pv);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintDeviceTopo(int id)
{
    HRESULT hr = S_OK;
    IDeviceTopology *devTopo = nullptr;
    UINT nConn = 0;
    LPWSTR devIdStr = nullptr;

    mTopoSet.clear();
    mConnSet.clear();

    assert(mDeviceCollection);
    assert(!mDeviceToUse);

    HRG(mDeviceCollection->Item(id, &mDeviceToUse));

    assert(mDeviceToUse);

    HRG(mDeviceToUse->Activate(
        __uuidof(IDeviceTopology), CLSCTX_INPROC_SERVER, nullptr, (void**)&devTopo));
    assert(devTopo);

    HRG(PrintDeviceTopo1(0, devTopo));

end:
    SafeRelease(&devTopo);
    SafeRelease(&mDeviceToUse);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintDeviceTopo1(int layer, IDeviceTopology *topo)
{
    HRESULT hr = S_OK;
    LPWSTR devIdStr = nullptr;

    UINT nSubunit = 0;
    ISubunit *subUnit = nullptr;

    UINT nConn = 0;
    IConnector *conn = nullptr;

    assert(topo);

    if (mTopoSet.find(topo) != mTopoSet.end()) {
        goto end;
    }
    mTopoSet.insert(topo);

    HRG(topo->GetDeviceId(&devIdStr));
    PrintLayerIndent(layer);
    printf("IDeviceTopology[%p]::GetDeviceId()=%S\n", topo, devIdStr);

    HRG(topo->GetSubunitCount(&nSubunit));
    PrintLayerIndent(layer);
    printf("IDeviceTopology[%p]::GetSubunitCount()=%u\n", topo, nSubunit);
    for (UINT i = 0; i < nSubunit; ++i) {
        HRG(topo->GetSubunit(i, &subUnit));
        PrintLayerIndent(layer);
        printf("IDeviceTopology[%p]::GetSubunit(%u)=%p\n", topo, i, subUnit);

        HRG(PrintSubunit(layer + 1, subUnit));

        SafeRelease(&subUnit);
    }

    HRG(topo->GetConnectorCount(&nConn));
    PrintLayerIndent(layer);
    printf("IDeviceTopology[%p]::GetConnectorCount()=%u\n", topo, nConn);
    for (UINT i = 0; i < nConn; ++i) {
        HRG(topo->GetConnector(i, &conn));
        PrintLayerIndent(layer);
        printf("IDeviceTopology[%p]::GetConnector(%d)=%p\n", topo, i, conn);

        HRG(PrintConnector(layer+1, conn));

        SafeRelease(&conn);
    }

end:
    SafeRelease(&conn);
    SafeRelease(&subUnit);
    SafeRelease(&mDeviceToUse);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintConnector(int layer, IConnector *conn)
{
    HRESULT hr = S_OK;
    IConnector *conn2 = nullptr;
    IPart *part = nullptr;
    DataFlow df;
    BOOL bConnected = FALSE;
    ConnectorType ct;

    if (mConnSet.find(conn) != mConnSet.end()) {
        // 既に現れた。
        goto end;
    }
    mConnSet.insert(conn);

    HRG(conn->GetDataFlow(&df));
    PrintLayerIndent(layer);
    printf("IConnector[%p]::GetDataFlow()=%s\n", conn, df==In? "In" : "Out");
    HRG(conn->IsConnected(&bConnected));
    PrintLayerIndent(layer);
    printf("IConnector[%p]::IsConnected()=%d\n", conn, bConnected);
    HRG(conn->GetType(&ct));
    PrintLayerIndent(layer);
    printf("IConnector[%p]::GetType()=%s\n", conn, ConnectorTypeToStr(ct));

    conn->QueryInterface(__uuidof(IPart), (void**)&part);
    if (part) {
        PrintLayerIndent(layer);
        printf("IConnector[%p]::QueryInterface(IPart)=%p\n", conn, part);

        HRG(PrintPart(layer + 1, part));
        SafeRelease(&part);
    }

    if (!bConnected) {
        goto end;
    }

    HRG(conn->GetConnectedTo(&conn2));
    PrintLayerIndent(layer);
    printf("IConnector[%p]::GetConnectedTo()=%p\n", conn, conn2);

    if (mConnSet.find(conn2) != mConnSet.end()) {
        // 既に現れた。
        goto end;
    }
    mConnSet.insert(conn2);

    HRG(conn2->QueryInterface(__uuidof(IPart), (void**)&part));
    PrintLayerIndent(layer);
    printf("IConnector[%p]::QueryInterface(IPart)=%p\n", conn2, part);

    HRG(PrintPart(layer + 1, part));
end:
    SafeRelease(&part);
    SafeRelease(&conn2);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintSubunit(int layer, ISubunit *su)
{
    HRESULT hr = S_OK;
    IPart *part = nullptr;

    HRG(su->QueryInterface(__uuidof(IPart), (void**)&part));
    PrintLayerIndent(layer);
    printf("ISubunit[%p]::QueryInterface(IPart)=%p\n", su, part);

    HRG(PrintPart(layer + 1, part));

end:
    SafeRelease(&part);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintPart(int layer, IPart *part)
{
    HRESULT hr = S_OK;
    PartType pt;
    LPWSTR name = nullptr;
    GUID subType;
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
    UINT ciCount = 0;

    assert(part);

    HRG(part->GetPartType(&pt));
    PrintLayerIndent(layer);
    printf("IPart[%p]::GetPartType()=%s\n", part, PartTypeToStr(pt));

    HRG(part->GetSubType(&subType));
    PrintLayerIndent(layer);
    printf("IPart[%p]::GetSubType()=%s\n", part, WWGuidToStr(subType).c_str());

    HRG(part->GetName(&name));
    PrintLayerIndent(layer);
    printf("IPart[%p]::GetName()=%S\n", part, name);

    HRG(part->GetTopologyObject(&partTopo));
    PrintLayerIndent(layer);
    printf("IPart[%p]::GetTopologyObject()=%p\n", part, partTopo);
    HRG(PrintDeviceTopo1(layer + 1, partTopo));

    HRG(part->GetControlInterfaceCount(&ciCount));
    PrintLayerIndent(layer);
    printf("IPart[%p]::GetControlInterfaceCount()=%u\n", part, ciCount);

    for (UINT i = 0; i < ciCount; ++i) {
        HRG(part->GetControlInterface(i, &ci));
        HRG(PrintControlInterface(layer+1, i, ci));
        SafeRelease(&ci);
    }

    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioMute), (void**)&am);
    if (am) {
        HRG(PrintAudioMute(layer + 1, am));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioVolumeLevel), (void**)&avl);
    if (avl) {
        HRG(PrintAudioVolumeLevel(layer + 1, avl));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioPeakMeter), (void**)&apm);
    if (apm) {
        HRG(PrintAudioPeakMeter(layer + 1, apm));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioAutoGainControl), (void**)&agc);
    if (agc) {
        HRG(PrintAudioAutoGainControl(layer + 1, agc));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioBass), (void**)&ab);
    if (ab) {
        HRG(PrintAudioBass(layer + 1, ab));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioChannelConfig), (void**)&acc);
    if (acc) {
        HRG(PrintAudioChannelConfig(layer + 1, acc));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioInputSelector), (void**)&ais);
    if (ais) {
        HRG(PrintAudioInputSelector(layer + 1, ais));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioLoudness), (void**)&al);
    if (al) {
        HRG(PrintAudioLoudness(layer + 1, al));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioMidrange), (void**)&amid);
    if (amid) {
        HRG(PrintAudioMidrange(layer + 1, amid));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioOutputSelector), (void**)&aos);
    if (aos) {
        HRG(PrintAudioOutputSelector(layer + 1, aos));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IAudioTreble), (void**)&atre);
    if (atre) {
        HRG(PrintAudioTreble(layer + 1, atre));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IKsJackDescription), (void**)&jd);
    if (jd) {
        HRG(PrintKsJackDesc(layer + 1, jd));
    }
    part->Activate(CLSCTX_INPROC_SERVER, __uuidof(IKsFormatSupport), (void**)&fs);
    if (fs) {
        HRG(PrintKsFormatSupport(layer + 1, fs));
    }

end:
    SafeRelease(&fs);
    SafeRelease(&ci);
    SafeRelease(&jd);
    SafeRelease(&atre);
    SafeRelease(&aos);
    SafeRelease(&amid);
    SafeRelease(&al);
    SafeRelease(&ais);
    SafeRelease(&acc);
    SafeRelease(&ab);
    SafeRelease(&agc);
    SafeRelease(&apm);
    SafeRelease(&avl);
    SafeRelease(&am);
    SafeRelease(&partTopo);
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioMute(int layer, IAudioMute *am)
{
    HRESULT hr = S_OK;
    BOOL mute = FALSE;

    assert(am);
    HRG(am->GetMute(&mute));

    PrintLayerIndent(layer);
    printf("IAudioMute[%p]::GetMute()=%d\n", am, mute);

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioVolumeLevel(int layer, IAudioVolumeLevel *avl)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float minL, maxL, stepL, curL;

    assert(avl);

    HRG(avl->GetChannelCount(&nCh));
    PrintLayerIndent(layer);
    printf("IAudioVolumeLevel[%p]::GetChannelCount()=%u\n", avl, nCh);

    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(avl->GetLevelRange(ch, &minL, &maxL, &stepL));
        PrintLayerIndent(layer);
        printf("IAudioVolumeLevel[%p]::GetLevelRange(%u) min=%f max=%f step=%f\n", avl, ch, minL, maxL, stepL);

        HRG(avl->GetLevel(ch, &curL));
        PrintLayerIndent(layer);
        printf("IAudioVolumeLevel[%p]::GetLevel(%u)=%f\n", avl, ch, curL);
    }

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioPeakMeter(int layer, IAudioPeakMeter *apm)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float curL;

    assert(apm);

    HRG(apm->GetChannelCount(&nCh));
    PrintLayerIndent(layer);
    printf("IAudioPeakMeter[%p]::GetChannelCount()=%u\n", apm, nCh);

    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(apm->GetLevel(ch, &curL));
        PrintLayerIndent(layer);
        printf("IAudioPeakMeter[%p]::GetLevel(%u)=%f\n", apm, ch, curL);
    }

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioAutoGainControl(int layer, IAudioAutoGainControl *agc)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    BOOL bEnabled = FALSE;

    assert(agc);

    HRG(agc->GetEnabled(&bEnabled));
    PrintLayerIndent(layer);
    printf("IAudioAutoGainControl[%p]::GetEnabled()=%d\n", agc, bEnabled);

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioBass(int layer, IAudioBass *ab)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float minL, maxL, stepL, curL;

    assert(ab);

    HRG(ab->GetChannelCount(&nCh));
    PrintLayerIndent(layer);
    printf("IAudioBass[%p]::GetChannelCount()=%u\n", ab, nCh);

    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(ab->GetLevelRange(ch, &minL, &maxL, &stepL));
        PrintLayerIndent(layer);
        printf("IAudioBass[%p]::GetLevelRange(%u) min=%f max=%f step=%f\n", ab, ch, minL, maxL, stepL);

        HRG(ab->GetLevel(ch, &curL));
        PrintLayerIndent(layer);
        printf("IAudioBass[%p]::GetLevel(%u)=%f\n", ab, ch, curL);
    }

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioChannelConfig(int layer, IAudioChannelConfig *acc)
{
    HRESULT hr = S_OK;
    DWORD cc = 0;

    assert(acc);


    HRG(acc->GetChannelConfig(&cc));
    PrintLayerIndent(layer);
    printf("IAudioChannelConfig[%p]::GetChannelConfig()=0x%x\n", acc, cc);

end:
    return hr;
}

HRESULT
WWSpatialAudioUser::PrintAudioInputSelector(int layer, IAudioInputSelector *ais)
{
    HRESULT hr = S_OK;
    UINT id = 0;

    assert(ais);

    HRG(ais->GetSelection(&id));
    PrintLayerIndent(layer);
    printf("IAudioInputSelector[%p]::GetSelection()=%u\n", ais, id);

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintAudioLoudness(int layer, IAudioLoudness *al)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    BOOL bEnabled = FALSE;

    assert(al);

    HRG(al->GetEnabled(&bEnabled));
    PrintLayerIndent(layer);
    printf("IAudioLoudness[%p]::GetEnabled()=%d\n", al, bEnabled);

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintAudioMidrange(int layer, IAudioMidrange *p)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float minL, maxL, stepL, curL;

    assert(p);

    HRG(p->GetChannelCount(&nCh));
    PrintLayerIndent(layer);
    printf("IAudioMidrange[%p]::GetChannelCount()=%u\n", p, nCh);

    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(p->GetLevelRange(ch, &minL, &maxL, &stepL));
        PrintLayerIndent(layer);
        printf("IAudioMidrange[%p]::GetLevelRange(%u) min=%f max=%f step=%f\n", p, ch, minL, maxL, stepL);

        HRG(p->GetLevel(ch, &curL));
        PrintLayerIndent(layer);
        printf("IAudioMidrange[%p]::GetLevel(%u)=%f\n", p, ch, curL);
    }

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintAudioOutputSelector(int layer, IAudioOutputSelector *aos)
{
    HRESULT hr = S_OK;
    UINT id = 0;

    assert(aos);

    HRG(aos->GetSelection(&id));
    PrintLayerIndent(layer);
    printf("IAudioOutputSelector[%p]::GetSelection()=%u\n", aos, id);

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintAudioTreble(int layer, IAudioTreble *p)
{
    HRESULT hr = S_OK;
    UINT nCh = 0;
    float minL, maxL, stepL, curL;

    assert(p);

    HRG(p->GetChannelCount(&nCh));
    PrintLayerIndent(layer);
    printf("IAudioTreble[%p]::GetChannelCount()=%u\n", p, nCh);

    for (UINT ch = 0; ch < nCh; ++ch) {
        HRG(p->GetLevelRange(ch, &minL, &maxL, &stepL));
        PrintLayerIndent(layer);
        printf("IAudioTreble[%p]::GetLevelRange(%u) min=%f max=%f step=%f\n", p, ch, minL, maxL, stepL);

        HRG(p->GetLevel(ch, &curL));
        PrintLayerIndent(layer);
        printf("IAudioTreble[%p]::GetLevel(%u)=%f\n", p, ch, curL);
    }

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintKsJackDesc(int layer, IKsJackDescription *jd)
{
    HRESULT hr = S_OK;
    UINT n = 0;
    KSJACK_DESCRIPTION desc{ 0 };

    assert(jd);

    HRG(jd->GetJackCount(&n));
    PrintLayerIndent(layer);
    printf("IKsJackDescription[%p]::GetJackCount()=%u\n", jd, n);
    for (UINT i = 0; i < n; ++i) {
        HRG(jd->GetJackDescription(i, &desc));

        PrintLayerIndent(layer);
        printf("  Jack %d : ChannelMapping=0x%x Color=0x%06x ConnectionType=%s IsConnected = %d\n",
            i, desc.ChannelMapping, desc.Color, EPcxConnectionTypeToStr(desc.ConnectionType), (int)desc.IsConnected);
        PrintLayerIndent(layer);
        printf("            GeoLocation = %s GenLocation = %s PortConnection = %s\n",
            EPcxGeoLocationToStr(desc.GeoLocation), EPcxGenLocationToStr(desc.GenLocation),
            EPxcPortConnectionToStr(desc.PortConnection));
    }

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintControlInterface(int layer, int id, IControlInterface *ci)
{
    HRESULT hr = S_OK;
    LPWSTR name = nullptr;
    GUID guid;
    assert(ci);

    HRG(ci->GetName(&name));
    HRG(ci->GetIID(&guid));

    PrintLayerIndent(layer);
    printf("id=%d IControlInterface[%p]::GetName()=%S\n", id, ci, name);

    PrintLayerIndent(layer);
    printf("id=%d IControlInterface[%p]::GetIID()=%s\n", id, ci, WWGuidToStr(guid).c_str());

    

end:
    return hr;
}

HRESULT WWSpatialAudioUser::PrintKsFormatSupport(int layer, IKsFormatSupport *fs)
{
    HRESULT hr = S_OK;
    PKSDATAFORMAT kdf = nullptr;

    assert(fs);

    HRG(fs->GetDevicePreferredFormat(&kdf));

    PrintLayerIndent(layer);
    printf("IKsFormatSupport[%p]::GetDevicePreferredFormat() FormatSize=%u Flags=%u SampleSize=%u\n", fs, kdf->FormatSize, kdf->Flags, kdf->SampleSize);
    PrintLayerIndent(layer);
    printf("    MajorFormat=%s\n", WWGuidToStr(kdf->MajorFormat).c_str());
    PrintLayerIndent(layer);
    printf("    SubFormat=%s\n", WWGuidToStr(kdf->SubFormat).c_str());
    PrintLayerIndent(layer);
    printf("    Specifier=%s\n", WWGuidToStr(kdf->Specifier).c_str());

    if (KSDATAFORMAT_TYPE_AUDIO == kdf->MajorFormat
            && KSDATAFORMAT_SUBTYPE_PCM == kdf->SubFormat
            && KSDATAFORMAT_SPECIFIER_WAVEFORMATEX == kdf->Specifier) {
        WAVEFORMATEX * wfex = (WAVEFORMATEX*)&kdf[1];
        if (22 <= wfex->cbSize) {
            WAVEFORMATEXTENSIBLE *wfext = (WAVEFORMATEXTENSIBLE*)wfex;
            PrintLayerIndent(layer);
            printf("    %dHz %dbit(%dbit container) %dch\n",
                (int)wfex->nSamplesPerSec, (int)wfext->Samples.wValidBitsPerSample,
                (int)wfex->wBitsPerSample, (int)wfex->nChannels);

        } else {
            PrintLayerIndent(layer);
            printf("    %dHz %dbit %dch\n", (int)wfex->nSamplesPerSec,
                (int)wfex->wBitsPerSample, (int)wfex->nChannels);
        }
    }
end:
    CoTaskMemFree(kdf);
    return hr;
}

