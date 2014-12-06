#pragma once
// 日本語 UTF-8

#include "WWTypes.h"
#include "WWMMNotificationClient.h"
#include <Windows.h>
#include <mmdeviceapi.h>
#include <vector>
#include <list>

#define WW_DEVICE_NAME_COUNT (256)
#define WW_DEVICE_IDSTR_COUNT (256)



struct WWDeviceInfo {
    int id;
    wchar_t name[WW_DEVICE_NAME_COUNT];
    wchar_t idStr[WW_DEVICE_IDSTR_COUNT];

    WWDeviceInfo(void) {
        id = -1;
        name[0] = 0;
        idStr[0] = 0;
    }

    WWDeviceInfo(int id, const wchar_t * name, const wchar_t * idStr);
};

class WWAudioDeviceEnumerator
{
public:
    WWAudioDeviceEnumerator(void);
    ~WWAudioDeviceEnumerator(void);

    void Init(void);
    void Term(void);

    HRESULT DoDeviceEnumeration(WWDeviceType t);
    int GetDeviceCount(void);
    bool GetDeviceName(int id, LPWSTR name, size_t nameBytes);
    bool GetDeviceIdString(int id, LPWSTR idStr, size_t idStrBytes);

    /// @return device. call SafeRelease(device) to delete
    IMMDevice *GetDevice(int id);

    void SetUseDeviceId(int id) { m_useDeviceId = id; }
    int  GetUseDeviceId(void) { return m_useDeviceId; }

    void UnregisterDeviceStateCallback(IWWDeviceStateCallback *cb);
    HRESULT RegisterDeviceStateCallback(IWWDeviceStateCallback *cb);

private:
    std::vector<WWDeviceInfo> m_deviceInfo;
    IMMDeviceCollection       *m_deviceCollection;
    IMMDeviceEnumerator *m_deviceEnumerator;
    std::list<WWMMNotificationClient *> m_notificationClientList;
    EDataFlow    m_dataFlow;
    int          m_useDeviceId;

    void ReleaseDeviceList(void);
    HRESULT BuildDeviceList(WWDeviceType t);
};

