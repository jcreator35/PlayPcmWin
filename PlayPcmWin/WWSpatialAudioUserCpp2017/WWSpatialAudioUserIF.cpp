// 日本語 UTF-8

#include "WWSpatialAudioUserIF.h"
#include "WWSpatialAudioUser.h"
#include "WWSpatialAudioDeviceProperty.h"

#include <list>

static std::list<WWSpatialAudioUser*> gInstanceList;

static WWSpatialAudioUser *
FindInstance(int idx)
{
    if (idx < 0 || gInstanceList.size() <= idx) {
        return nullptr;
    }

    auto ite = gInstanceList.begin();
    for (int i = 0; i < idx; ++i) {
        ++ite;
    }

    return *ite;
}

/// 新たに実体を作成。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserInit(void)
{
    auto p = new WWSpatialAudioUser();
    p->Init();

    gInstanceList.push_back(p);
    return (int)gInstanceList.size() - 1;
}

/// 実体を削除する。
/// @param instanceId 実体のID番号。Initで戻る値。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserTerm(int instanceId)
{
    if (instanceId < 0 || gInstanceList.size() <= instanceId) {
        return E_NOTFOUND;
    }

    auto ite = gInstanceList.begin();
    for (int i = 0; i < instanceId; ++i) {
        ++ite;
    }

    auto *p = *ite;
    p->Term();

    delete p;
    p = nullptr;
    *ite = nullptr;

    gInstanceList.erase(ite);

    return S_OK;
}

#define FIND_INSTANCE                   \
    auto *p = FindInstance(instanceId); \
    if (nullptr == p) {                 \
        return E_NOTFOUND;              \
    }


WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserDoEnumeration(int instanceId)
{
    FIND_INSTANCE;
    return p->DoDeviceEnumeration();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceCount(int instanceId)
{
    FIND_INSTANCE;
    return p->GetDeviceCount();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceProperty(int instanceId, int devIdx, WWSpatialAudioDeviceProperty &sadp_r)
{
    FIND_INSTANCE;
    return p->GetDeviceName(devIdx, sadp_r.name, sizeof sadp_r.name);
}

