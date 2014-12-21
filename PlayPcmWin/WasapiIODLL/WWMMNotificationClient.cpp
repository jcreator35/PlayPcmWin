// 日本語 UTF-8

#include "WWMMNotificationClient.h"
#include "WWUtil.h"
#include <assert.h>

WWMMNotificationClient::WWMMNotificationClient(IWWDeviceStateCallback *pCallback)
        : m_cRef(1), m_pCallback(pCallback)
{
}

ULONG STDMETHODCALLTYPE
WWMMNotificationClient::AddRef(void)
{
    return InterlockedIncrement(&m_cRef);
}

ULONG STDMETHODCALLTYPE
WWMMNotificationClient::Release(void)
{
    ULONG ulRef = InterlockedDecrement(&m_cRef);
    if (0 == ulRef) {
        // この構造体はデストラクタを作っても親の構造体にvirtualデストラクタがないので
        // 呼び出し側のポインタの持ち方によっては呼ばれない。ここでnewしたメンバをdeleteする。
        // 現時点でdeleteするものは特にない。
            
        m_pCallback = nullptr;

        delete this;
    }
    return ulRef;
}

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::QueryInterface(REFIID riid, VOID **ppvInterface)
{
    if (IID_IUnknown == riid) {
        AddRef();
        *ppvInterface = (IUnknown*)this;
    }
    else if (__uuidof(IMMNotificationClient) == riid) {
        AddRef();
        *ppvInterface = (IMMNotificationClient*)this;
    } else {
        *ppvInterface = nullptr;
        return E_NOINTERFACE;
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::OnDefaultDeviceChanged(EDataFlow flow, ERole role, LPCWSTR pwstrDeviceId)
{
    dprintf("%s %d %d %S\n", __FUNCTION__, flow, role, pwstrDeviceId);

    (void)flow;
    (void)role;
    (void)pwstrDeviceId;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::OnDeviceAdded(LPCWSTR pwstrDeviceId)
{
    dprintf("%s %S\n", __FUNCTION__, pwstrDeviceId);

    (void)pwstrDeviceId;

    return S_OK;
};

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::OnDeviceRemoved(LPCWSTR pwstrDeviceId)
{
    dprintf("%s %S\n", __FUNCTION__, pwstrDeviceId);

    (void)pwstrDeviceId;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState)
{
    dprintf("%s %S %08x\n", __FUNCTION__, pwstrDeviceId, dwNewState);
    assert(m_pCallback);
    return m_pCallback->OnDeviceStateChanged(pwstrDeviceId, dwNewState);
}

HRESULT STDMETHODCALLTYPE
WWMMNotificationClient::OnPropertyValueChanged(LPCWSTR pwstrDeviceId, const PROPERTYKEY key)
{
    /*
    dprintf("%s %S %08x:%08x:%08x:%08x = %08x\n", __FUNCTION__,
        pwstrDeviceId, key.fmtid.Data1, key.fmtid.Data2, key.fmtid.Data3, key.fmtid.Data4, key.pid);
    */

    (void)pwstrDeviceId;
    (void)key;

    return S_OK;
}
