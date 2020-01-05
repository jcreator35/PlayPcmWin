// 日本語 UTF-8

#include "WWAudioDeviceEnumerator.h"
#include "WWWasapiIOUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>
#include <algorithm>
#include "WWCommonUtil.h"

static HRESULT
DeviceNameGet(IMMDeviceCollection *dc, UINT id, wchar_t *name, size_t nameBytes)
{
    HRESULT hr = 0;

    IMMDevice *device  = nullptr;
    IPropertyStore *ps = nullptr;
    PROPVARIANT pv;

    assert(dc);
    assert(name);
    assert(0 < nameBytes);

    name[0] = 0;

    PropVariantInit(&pv);

    HRG(dc->Item(id, &device));
    HRG(device->OpenPropertyStore(STGM_READ, &ps));
    HRG(ps->GetValue(PKEY_Device_FriendlyName, &pv));

    wcsncpy_s(name, nameBytes/2, pv.pwszVal, nameBytes/2 -1);

end:
    PropVariantClear(&pv);
    SafeRelease(&ps);
    SafeRelease(&device);
    return hr;
}

static HRESULT
DeviceIdStringGet(IMMDeviceCollection *dc, UINT id, wchar_t *deviceIdStr, size_t deviceIdStrBytes)
{
    HRESULT hr = 0;

    IMMDevice *device  = nullptr;
    LPWSTR    s        = nullptr;

    assert(dc);
    assert(deviceIdStr);
    assert(0 < deviceIdStrBytes);

    deviceIdStr[0] = 0;

    HRG(dc->Item(id, &device));
    HRG(device->GetId(&s));

    wcsncpy_s(deviceIdStr, deviceIdStrBytes/2, s, deviceIdStrBytes/2 -1);

end:
    CoTaskMemFree(s);
    s = nullptr;
    SafeRelease(&device);
    return hr;
}

WWDeviceInfo::WWDeviceInfo(int id, const wchar_t * name, const wchar_t * idStr)
{
    this->id = id;
    wcsncpy_s(this->name, name, WW_DEVICE_NAME_COUNT-1);
    wcsncpy_s(this->idStr, idStr, WW_DEVICE_IDSTR_COUNT-1);
}

WWAudioDeviceEnumerator::WWAudioDeviceEnumerator(void)
    : m_deviceCollection(nullptr),
      m_deviceEnumerator(nullptr),
      m_dataFlow(eCapture),
      m_useDeviceId(-1)
{
}

WWAudioDeviceEnumerator::~WWAudioDeviceEnumerator(void)
{
    assert(!m_deviceEnumerator);
    assert(!m_deviceCollection);
    assert(m_notificationClientList.size() == 0);
}

void
WWAudioDeviceEnumerator::Init(void)
{
}

void
WWAudioDeviceEnumerator::Term(void)
{
    ReleaseDeviceList();

    assert(m_notificationClientList.size() == 0);
}

HRESULT
WWAudioDeviceEnumerator::RegisterDeviceStateCallback(IWWDeviceStateCallback *cb)
{
    auto *nc = new WWMMNotificationClient(cb);

    m_notificationClientList.push_back(nc);

    if (m_deviceEnumerator) {
        m_deviceEnumerator->UnregisterEndpointNotificationCallback(nc);
    }

    return S_OK;
}

void
WWAudioDeviceEnumerator::UnregisterDeviceStateCallback(IWWDeviceStateCallback *cb)
{
    auto it = std::remove_if(m_notificationClientList.begin(),
            m_notificationClientList.end(),
            [&](WWMMNotificationClient * p) { return p->GetCallbackPtr() == cb; } );
    
    if (it != m_notificationClientList.end()) {
        WWMMNotificationClient *p = *it;

        if (m_deviceEnumerator) {
            m_deviceEnumerator->UnregisterEndpointNotificationCallback(p);
        }

        // WWMMNotificationClientは、参照カウンタ管理されている。
        // 参照カウンタが0になってpがdeleteされることを確認すること。
        p->Release();
    }

    m_notificationClientList.erase(it, m_notificationClientList.end());
}

void
WWAudioDeviceEnumerator::ReleaseDeviceList(void)
{
    m_deviceInfo.clear();
    SafeRelease(&m_deviceCollection);

    if (m_deviceEnumerator) {
        for (auto it = m_notificationClientList.begin();
                it != m_notificationClientList.end(); ++it) {
            WWMMNotificationClient *p = *it;
            m_deviceEnumerator->UnregisterEndpointNotificationCallback(p);
        }
    }
    SafeRelease(&m_deviceEnumerator);

}

HRESULT
WWAudioDeviceEnumerator::BuildDeviceList(WWDeviceType t)
{
    HRESULT hr = 0;

    switch (t) {
    case WWDTPlay:
        m_dataFlow = eRender;
        break;
    case WWDTRec:
        m_dataFlow = eCapture;
        break;
    default:
        assert(0);
        return E_FAIL;
    }

    HRR(CoCreateInstance(__uuidof(MMDeviceEnumerator), nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&m_deviceEnumerator)));

    if (m_deviceEnumerator) {
        for (auto it = m_notificationClientList.begin();
                it != m_notificationClientList.end(); ++it) {
            m_deviceEnumerator->RegisterEndpointNotificationCallback(*it);
        }
    }

    HRR(m_deviceEnumerator->EnumAudioEndpoints(m_dataFlow, DEVICE_STATE_ACTIVE, &m_deviceCollection));

    UINT nDevices = 0;
    HRG(m_deviceCollection->GetCount(&nDevices));

    for (UINT i=0; i<nDevices; ++i) {
        wchar_t name[WW_DEVICE_NAME_COUNT];
        wchar_t idStr[WW_DEVICE_IDSTR_COUNT];
        HRG(DeviceNameGet(m_deviceCollection, i, name, sizeof name));
        HRG(DeviceIdStringGet(m_deviceCollection, i, idStr, sizeof idStr));
        m_deviceInfo.push_back(WWDeviceInfo(i, name, idStr));
    }

end:
    return hr;
}

HRESULT
WWAudioDeviceEnumerator::DoDeviceEnumeration(WWDeviceType t)
{
    dprintf("D: %s() t=%d\n", __FUNCTION__, (int)t);

    ReleaseDeviceList();
    return BuildDeviceList(t);
}

int
WWAudioDeviceEnumerator::GetDeviceCount(void)
{
    assert(m_deviceCollection);
    return (int)m_deviceInfo.size();
}

bool
WWAudioDeviceEnumerator::GetDeviceName(int id, LPWSTR name, size_t nameBytes)
{
    if (id < 0 || (int)m_deviceInfo.size() <= id) {
        return false;
    }

    wcsncpy_s(name, nameBytes/2, m_deviceInfo[id].name, nameBytes/2 -1);
    return true;
}

bool
WWAudioDeviceEnumerator::GetDeviceIdString(int id, LPWSTR idStr, size_t idStrBytes)
{
    if (id < 0 || (int)m_deviceInfo.size() <= id) {
        return false;
    }

    wcsncpy_s(idStr, idStrBytes/2, m_deviceInfo[id].idStr, idStrBytes/2 -1);
    return true;
}

IMMDevice *
WWAudioDeviceEnumerator::GetDevice(int id)
{
    HRESULT   hr      = 0;
    IMMDevice *device = nullptr;

    if (id < 0 || (int)m_deviceInfo.size() <= id) {
        return nullptr;
    }

    if (!m_deviceCollection) {
        return nullptr;
    }

    HRG(m_deviceCollection->Item(id, &device));

end:
    return device;
}
