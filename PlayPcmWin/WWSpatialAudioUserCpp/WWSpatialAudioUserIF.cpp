// 日本語 UTF-8

#include "WWSpatialAudioUserIF.h"
#include "WWSpatialAudioUser.h"
#include "WWSpatialAudioDeviceProperty.h"
#include "WWPcmStore.h"

#include <list>

struct Instances {
    WWSpatialAudioUser * sau = nullptr;
    WWPcmStore *ps = nullptr;
    WWPcmFloat *pcm = nullptr;
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
WWSpatialAudioUserClearAllPcm(int instanceId)
{
    FIND_INSTANCE;
    ps->Clear();
    sau->ClearAllStreams();

    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmBegin(
    int instanceId, int ch, int64_t numSamples)
{
    FIND_INSTANCE;
    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    int trackNr = 0;

    ps->ClearPcmOfSpecifiedChannel(ch);
    inst->pcm = ps->NewSilentPcm(ch, (WWTrackEnum)trackNr, numSamples);
    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmFragment(
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

    assert(inst->pcm); //< SetPcmBegin()を呼ばずにSetPcmFragmentを呼ぶとエラー。


    // sampleCountは正の値。

    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    if (startSamplePos < 0 || inst->pcm->pcm.size() < (uint64_t)(startSamplePos + sampleCount)) {
        return E_INVALIDARG;
    }
    
    memcpy(&inst->pcm->pcm[startSamplePos], samples, sizeof(float)*sampleCount);
    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmEnd(
    int instanceId, int ch, int audioObjectType)
{
    FIND_INSTANCE;
    if (ch < 0 || NUM_CHANNELS <= ch) {
        return E_NOTFOUND;
    }

    WWAudioObject ao;
    ao.Init(ch, *ps, inst->pcm, (AudioObjectType)audioObjectType, 1.0f);
    return sau->AddStream(ao);
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserStart(
    int instanceId)
{
    FIND_INSTANCE;

    return sau->Start();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserStop(
    int instanceId)
{
    FIND_INSTANCE;

    HRESULT hr = sau->Stop();
    if (FAILED(hr)) {
        return hr;
    }

    return hr;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetThreadErcd(
    int instanceId)
{
    FIND_INSTANCE;

    return sau->GetThreadErcd();
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetPlayingTrackNr(
    int instanceId, int ch, int *trackNr_r)
{
    FIND_INSTANCE;

    *trackNr_r = sau->GetPlayingTrackNr(ch);

    return S_OK;
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPosFrame(
    int instanceId, int64_t frame)
{
    FIND_INSTANCE;

    return sau->UpdatePlayPosition(frame);
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetPlayStatus(int instanceId, int ch, WWPlayStatus *pos_return)
{
    FIND_INSTANCE;
    assert(pos_return);

    return sau->GetPlayStatus(ch, *pos_return);
}

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserRewind(
    int instanceId)
{
    FIND_INSTANCE;

    sau->Rewind();
    return S_OK;
}
