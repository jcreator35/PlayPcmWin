#include "WWSpatialAudioUser.h"
#include <SpatialAudioClient.h>
#include <exception>
#include <mmdeviceapi.h>
#include "WWUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>

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
        printf("    Spatial audio is not enabled on this device\n");
        goto end;
    }
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

#if 0
HRESULT
WWSpatialAudioUser::PrintSAMetadata(int id)
{
    HRESULT hr = S_OK;
    ISpatialAudioMetadataReader *samReader = nullptr;
    ISpatialAudioMetadataItems *items = nullptr;

    assert(mDeviceCollection);
    assert(!mDeviceToUse);

    HRG(mDeviceCollection->Item(id, &mDeviceToUse));

    assert(mDeviceToUse);
    assert(!mSAClient);

    HRG(mDeviceToUse->Activate(
        __uuidof(ISpatialAudioMetadataClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&mSAMClient));
    assert(mSAMClient);

    HRG(mSAMClient->ActivateSpatialAudioMetadataReader(&samReader));

    assert(samReader);

    samReader->Open(items);
    samReader->
        samReader->ReadNextItem()
    samReader->Open()

        end:
    SafeRelease(&samReader);
    SafeRelease(&mDeviceToUse);
    SafeRelease(&mSAMClient);
    return hr;
}
#endif
