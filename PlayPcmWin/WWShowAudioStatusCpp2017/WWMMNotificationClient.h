#pragma once
// 日本語 UTF-8

#include <Windows.h>
#include <MMDeviceAPI.h>

class IWWDeviceStateCallback {
public:
    virtual HRESULT
    OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState) = 0;
};

struct WWMMNotificationClient : public IMMNotificationClient
{
public:
    WWMMNotificationClient(void) : m_cRef(1), m_pCallback(nullptr) { }
    ~WWMMNotificationClient(void) { m_pCallback = nullptr; }
    void SetCallback(IWWDeviceStateCallback *pCallback);
    ULONG STDMETHODCALLTYPE AddRef(void);
    ULONG STDMETHODCALLTYPE Release(void);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, VOID **ppvInterface);
    HRESULT STDMETHODCALLTYPE OnDefaultDeviceChanged(EDataFlow flow, ERole role, LPCWSTR pwstrDeviceId);
    HRESULT STDMETHODCALLTYPE OnDeviceAdded(LPCWSTR pwstrDeviceId);
    HRESULT STDMETHODCALLTYPE OnDeviceRemoved(LPCWSTR pwstrDeviceId);
    HRESULT STDMETHODCALLTYPE OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState);
    HRESULT STDMETHODCALLTYPE OnPropertyValueChanged(LPCWSTR pwstrDeviceId, const PROPERTYKEY key);

    IWWDeviceStateCallback *GetCallbackPtr(void) { return m_pCallback; }
private:
    LONG m_cRef;
    IWWDeviceStateCallback *m_pCallback;
};



