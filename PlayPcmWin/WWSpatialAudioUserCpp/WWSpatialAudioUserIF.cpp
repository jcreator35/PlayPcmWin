// 日本語 UTF-8

#include "WWSpatialAudioUserIF.h"
#include "WWSpatialAudioUser.h"
#include "WWSpatialAudioDeviceProperty.h"
#include "WWPcmStore.h"

#include <list>

struct Instances {
    WWSpatialAudioUser * sau;
    WWPcmStore *ps;
};

static std::list<Instances> gInstanceList;

static Instances *
FindInstance(int idx)
{
    if (idx < 0 || gInstanceList.size() <= idx) {
        return nullptr;
    }

    auto ite = gInstanceList.begin();
    for (int i = 0; i < idx; ++i) {
        ++ite;
    }

    return &(*ite);
}

/// 新たに実体を作成。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserInit(void)
{
    auto p = new WWSpatialAudioUser();
    p->Init();
    auto ps = new WWPcmStore();

    gInstanceList.push_back(Instances{ p,ps });
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

    auto r = *ite;

    r.sau->Term();
    delete r.sau;
    r.sau = nullptr;

    delete r.ps;
    r.ps = nullptr;

    gInstanceList.erase(ite);

    return S_OK;
}

#define FIND_INSTANCE                      \
    auto *inst = FindInstance(instanceId); \
    if (nullptr == inst) {                 \
        return E_NOTFOUND;                 \
    }                                      \
    auto *sau = inst->sau;                 \
    auto *ps  = inst->ps;


WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserDoEnumeration(int instanceId)
{
    FIND_INSTANCE;
    return sau->DoDeviceEnumeration();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceCount(int instanceId)
{
    FIND_INSTANCE;
    return sau->GetDeviceCount();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceProperty(int instanceId, int devIdx, WWSpatialAudioDeviceProperty &sadp_r)
{
    int hr = 0;
    FIND_INSTANCE;

    sadp_r.devIdStr[0] = 0;
    sadp_r.name[0] = 0;

    hr = sau->GetDeviceIdStr(devIdx, sadp_r.devIdStr, sizeof sadp_r.devIdStr);
    if (FAILED(hr)) {
        return hr;
    }
    return sau->GetDeviceName(devIdx, sadp_r.name, sizeof sadp_r.name);
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserChooseDevice(int instanceId, int devIdx, int maxDynObjectCount, int staticObjectTypeMask)
{
    FIND_INSTANCE;
    return sau->ChooseDevice(devIdx, maxDynObjectCount, staticObjectTypeMask);
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioClearAllPcm(int instanceId)
{
    FIND_INSTANCE;
    ps->Clear();
    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioSetPcmBegin(
    int instanceId, int ch, int64_t numSamples)
{
    FIND_INSTANCE;
    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    ps->mPcm[ch].Clear();
    ps->mPcm[ch].pcm.resize(numSamples, 0.0f);
    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioSetPcmFragment(
    int instanceId, int ch, int64_t startSamplePos, int sampleCount, float * samples)
{
    FIND_INSTANCE;
    if (sampleCount < 0) {
        return E_INVALIDARG;
    }
    if (0 == sampleCount) {
        // 何もコピーしない。
        return S_OK;
    }

    // sampleCountは正の値。

    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    if (startSamplePos < 0 || ps->mPcm[ch].pcm.size() <= (uint64_t)(startSamplePos + sampleCount)) {
        return E_INVALIDARG;
    }
    
    memcpy(&ps->mPcm[ch].pcm[startSamplePos], samples, sizeof(float)*sampleCount);
    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioSetPcmEnd(
    int instanceId, int ch)
{
    FIND_INSTANCE;
    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioStart(
    int instanceId)
{
    FIND_INSTANCE;

    return sau->Start();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioStop(
    int instanceId)
{
    FIND_INSTANCE;

}
