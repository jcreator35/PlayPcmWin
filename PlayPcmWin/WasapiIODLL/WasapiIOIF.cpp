#include "WasapiIOIF.h"
#include "WasapiUser.h"
#include "WWPlayPcmGroup.h"
#include "WWUtil.h"
#include "WWTimerResolution.h"
#include "WWAudioDeviceEnumerator.h"
#include "WWAudioFilterType.h"
#include "WWAudioFilterPolarityInvert.h"
#include "WWAudioFilterMonauralMix.h"
#include "WWAudioFilterChannelMapping.h"
#include "WWAudioFilterMuteSoloChannel.h"
#include "WWAudioFilterZohNosdacCompensation.h"
#include "WWAudioFilterDelay.h"
#include <assert.h>
#include <map>

class WasapiIO : public IWWDeviceStateCallback {
public:
    WasapiUser     wasapi;
    WWPlayPcmGroup playPcmGroup;
    WWTimerResolution timerResolution;
    WWAudioDeviceEnumerator deviceEnumerator;
    WWStateChanged * stateChangedCallback;
    int            instanceId;
    static int     sNextInstanceId;

    int GetInstanceId(void) const { return instanceId; }

    HRESULT Init(void);
    void Term(void);

    void UpdatePlayRepeat(bool repeat);
    bool AddPcmDataStart(void);
    HRESULT ResampleIfNeeded(int conversionQuality);
    void AddPcmDataEnd(void);

    HRESULT StartPlayback(int wavDataId);
    HRESULT StartRecording(void);

    double ScanPcmMaxAbsAmplitude(void);
    void ScalePcmAmplitude(double scale);

    bool ConnectPcmDataNext(int fromIdx, int toIdx);

    // implements IWWDeviceStateCallback
    virtual HRESULT OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState);
};

int WasapiIO::sNextInstanceId = 0;

HRESULT
WasapiIO::Init(void)
{
    HRESULT hr;
    
    stateChangedCallback = nullptr;
    playPcmGroup.Init();
    deviceEnumerator.Init();
    hr = wasapi.Init();

    deviceEnumerator.RegisterDeviceStateCallback(this);

    if (SUCCEEDED(hr)) {
        instanceId = sNextInstanceId;
        ++sNextInstanceId;
    }

    return hr;
}

void
WasapiIO::Term(void)
{
    stateChangedCallback = nullptr;

    deviceEnumerator.UnregisterDeviceStateCallback(this);

    wasapi.Term();
    deviceEnumerator.Term();
    playPcmGroup.Term();
}

void
WasapiIO::UpdatePlayRepeat(bool repeat)
{
    WWPcmData *first = playPcmGroup.FirstPcmData();
    WWPcmData *last  = playPcmGroup.LastPcmData();

    if (nullptr != first && nullptr != last) {
        playPcmGroup.SetPlayRepeat(repeat);
        wasapi.PcmStream().UpdatePlayRepeat(repeat, first, last);
    }
}

bool
WasapiIO::ConnectPcmDataNext(int fromIdx, int toIdx)
{
    WWPcmData *from = playPcmGroup.FindPcmDataById(fromIdx);
    WWPcmData *to = playPcmGroup.FindPcmDataById(toIdx);

    if (nullptr == from || nullptr == to) {
        return false;
    }

    wasapi.MutexWait();
    from->SetNext(to);
    wasapi.MutexRelease();

    return true;
}

bool
WasapiIO::AddPcmDataStart(void)
{
    WWPcmFormat pcmFormat;
    wasapi.GetPcmFormat(pcmFormat);

    return playPcmGroup.AddPlayPcmDataStart(pcmFormat);
//        sampleRate, sampleFormat, numChannels, dwChannelMask, bytesPerFrame);
}

HRESULT
WasapiIO::ResampleIfNeeded(int conversionQuality)
{
    HRESULT hr;

    if (!wasapi.IsResampleNeeded()) {
        return S_OK;
    }

    WWPcmFormat deviceFormat;
    wasapi.GetDevicePcmFormat(deviceFormat);
    assert(deviceFormat.sampleFormat == WWPcmDataSampleFormatSfloat);

    hr = playPcmGroup.DoResample(deviceFormat, conversionQuality);
    if (FAILED(hr)) {
        return hr;
    }

    wasapi.UpdatePcmDataFormat(deviceFormat);

    return hr;
}

double
WasapiIO::ScanPcmMaxAbsAmplitude(void)
{
    float minResult = FLT_MAX;
    float maxResult = FLT_MIN;

    for (int i=0; i<playPcmGroup.Count(); ++i) {
        WWPcmData *pcm = playPcmGroup.NthPcmData(i);
        assert(pcm);

        float minV, maxV;
        pcm->FindSampleValueMinMax(&minV, &maxV);

        if (minV < minResult) {
            minResult = minV;
        }
        if (maxResult < maxV) {
            maxResult = maxV;
        }
    }
    
    return max(fabsf(maxResult), fabsf(minResult));
}

void
WasapiIO::ScalePcmAmplitude(double scale)
{
    for (int i=0; i<playPcmGroup.Count(); ++i) {
        WWPcmData *pcm = playPcmGroup.NthPcmData(i);
        assert(pcm);

        pcm->ScaleSampleValue((float)scale);
    }
}

void
WasapiIO::AddPcmDataEnd(void)
{
    playPcmGroup.AddPlayPcmDataEnd();

    // リピートなしと仮定してリンクリストをつなげておく。
    UpdatePlayRepeat(false);
}

HRESULT
WasapiIO::StartPlayback(int wavDataId)
{
    WWPcmData *p = playPcmGroup.FindPcmDataById(wavDataId);
    if (nullptr == p) {
        dprintf("%s(%d) PcmData is not found\n",
            __FUNCTION__, wavDataId);
        return E_FAIL;
    }

    wasapi.UpdatePlayPcmData(*p);
    return wasapi.Start();
}

HRESULT
WasapiIO::StartRecording(void)
{
    return wasapi.Start();
}

HRESULT
WasapiIO::OnDeviceStateChanged(LPCWSTR pwstrDeviceId, DWORD dwNewState)
{
    (void)dwNewState;
    // 再生中で、再生しているデバイスの状態が変わったときは
    // DeviceStateChanged()は再生を停止しなければならない
    if (stateChangedCallback) {
        stateChangedCallback(pwstrDeviceId);
    }

    return S_OK;
}

static std::map<int, WasapiIO *> gSelf;

static WasapiIO *
Instance(int id)
{
    if (id < 0) {
        return nullptr;
    }

    std::map<int, WasapiIO *>::iterator ite = gSelf.find(id);
    if (ite == gSelf.end()) {
        return nullptr;
    }

    return ite->second;
}

////////////////////////////////////////////////////////////////////////////////

extern "C" {

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_Init(int *instanceId_return)
{
    HRESULT hr = S_OK;

    WasapiIO * self = new WasapiIO();
    if (self == nullptr) {
        return E_FAIL;
    }

    hr = self->Init();
    if (FAILED(hr)) {
        return hr;
    }

    *instanceId_return = self->GetInstanceId();
    gSelf[*instanceId_return] = self;
    return hr;
}

__declspec(dllexport)
void __stdcall
WasapiIO_Term(int instanceId)
{
    std::map<int, WasapiIO *>::iterator ite = gSelf.find(instanceId);
    if (ite == gSelf.end()) {
        assert(0);
        return;
    }

    ite->second->Term();
    SAFE_DELETE(ite->second);
    gSelf.erase(ite);
}

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_EnumerateDevices(int instanceId, int deviceType)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    WWDeviceType t = (WWDeviceType)deviceType;

    return self->deviceEnumerator.DoDeviceEnumeration(t);
}

__declspec(dllexport)
int __stdcall
WasapiIO_GetDeviceCount(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->deviceEnumerator.GetDeviceCount();
}

__declspec(dllexport)
bool __stdcall
WasapiIO_GetDeviceAttributes(int instanceId, int deviceId, WasapiIoDeviceAttributes &attr_return)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    attr_return.deviceId = deviceId;
    if (!self->deviceEnumerator.GetDeviceName(deviceId, attr_return.name, sizeof attr_return.name)) {
        return false;
    }
    if (!self->deviceEnumerator.GetDeviceIdString(deviceId, attr_return.deviceIdString, sizeof attr_return.deviceIdString)) {
        return false;
    }
    return true;
}

__declspec(dllexport)
int __stdcall
WasapiIO_InspectDevice(int instanceId, int deviceId, const WasapiIoInspectArgs &args)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    WWPcmFormat pcmFormat;
    pcmFormat.Set(args.sampleRate, (WWPcmDataSampleFormatType)args.sampleFormat, args.numChannels, args.dwChannelMask, WWStreamPcm);

    IMMDevice *device = self->deviceEnumerator.GetDevice(deviceId);
    return self->wasapi.InspectDevice(device, pcmFormat);
}

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_Setup(int instanceId, int deviceId, const WasapiIoSetupArgs &args)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->deviceEnumerator.SetUseDeviceId(deviceId);
    IMMDevice *device = self->deviceEnumerator.GetDevice(deviceId);
    assert(device);

    WWPcmFormat pcmFormat;
    pcmFormat.Set(args.sampleRate, (WWPcmDataSampleFormatType)args.sampleFormat, args.numChannels, args.dwChannelMask, (WWStreamType)args.streamType);

    self->wasapi.ThreadCharacteristics().Set((WWMMCSSCallType)args.mmcssCall,
        (WWMMThreadPriorityType) args.mmThreadPriority, (WWSchedulerTaskType)args.schedulerTask);
    self->wasapi.PcmStream().SetZeroFlushMillisec(args.zeroFlushMillisec);
    self->wasapi.TimerResolution().SetTimePeriodHundredNanosec(args.timePeriodHandledNanosec);

    return self->wasapi.Setup(device, (WWDeviceType)args.deviceType, pcmFormat, (WWShareMode)args.shareMode, (WWDataFeedMode)args.dataFeedMode, (DWORD)args.latencyMillisec);
}

__declspec(dllexport)
void __stdcall
WasapiIO_Unsetup(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->wasapi.Unsetup();
    self->deviceEnumerator.SetUseDeviceId(-1);
}

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataStart(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    return self->AddPcmDataStart();
}

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmData(int instanceId, int pcmId, unsigned char *data, int64_t bytes)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->playPcmGroup.AddPlayPcmData(pcmId, data, bytes);
}

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataSetPcmFragment(int instanceId, int pcmId, int64_t posBytes, unsigned char *data, int64_t bytes)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
#ifdef _X86_
    if (0x7fffffffL < posBytes + bytes) {
        // cannot alloc 2GB buffer on 32bit build
        return false;
    }
#endif

    WWPcmData *p = self->playPcmGroup.FindPcmDataById(pcmId);
    if (nullptr == p) {
        return false;
    }

    assert(posBytes + bytes <= p->Frames() * p->BytesPerFrame());

    memcpy(&(p->Stream()[posBytes]), data, bytes);
    return true;
}

__declspec(dllexport)
int __stdcall
WasapiIO_ResampleIfNeeded(int instanceId, int conversionQuality)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->ResampleIfNeeded(conversionQuality);
}

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataEnd(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->AddPcmDataEnd();

    return true;
}

__declspec(dllexport)
void __stdcall
WasapiIO_RemovePlayPcmDataAt(int instanceId, int pcmId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->playPcmGroup.RemoveAt(pcmId);
    self->UpdatePlayRepeat(self->playPcmGroup.GetRepatFlag());
}

__declspec(dllexport)
void __stdcall
WasapiIO_ClearPlayList(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->playPcmGroup.Clear();
}

__declspec(dllexport)
void __stdcall
WasapiIO_SetPlayRepeat(int instanceId, bool b)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->UpdatePlayRepeat(b);
}

__declspec(dllexport)
bool __stdcall
WasapiIO_ConnectPcmDataNext(int instanceId, int fromIdx, int toIdx)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    return self->ConnectPcmDataNext(fromIdx, toIdx);
}

__declspec(dllexport)
int __stdcall
WasapiIO_GetPcmDataId(int instanceId, int usageType)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.PcmStream().GetPcmDataId((WWPcmDataUsageType)usageType);
}

__declspec(dllexport)
void __stdcall
WasapiIO_SetNowPlayingPcmDataId(int instanceId, int pcmId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    WWPcmData *p = nullptr;

    if (pcmId < 0) {
        // jump to end PCM. This leads to playback termination
        p = self->wasapi.PcmStream().GetSilenceBuffer(WWPcmDataContentSilenceForEnding);
    } else {
        p = self->playPcmGroup.FindPcmDataById(pcmId);
    }

    if (nullptr == p) {
        dprintf("%s(%d) PcmData not found\n",
            __FUNCTION__, pcmId);
        return;
    }

    self->wasapi.UpdatePlayPcmData(*p);
}

__declspec(dllexport)
int64_t __stdcall
WasapiIO_GetCaptureGlitchCount(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.GetCaptureGlitchCount();
}

__declspec(dllexport)
void __stdcall
WasapiIO_ResetCaptureGlitchCount(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->wasapi.ResetCaptureGlitchCount();
}

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_StartPlayback(int instanceId, int wavDataId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    return self->StartPlayback(wavDataId);
}

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_StartRecording(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    return self->StartRecording();
}

__declspec(dllexport)
bool __stdcall
WasapiIO_Run(int instanceId, int millisec)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.Run(millisec);
}

__declspec(dllexport)
void __stdcall
WasapiIO_Stop(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->wasapi.Stop();
}

__declspec(dllexport)
int __stdcall
WasapiIO_Pause(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.Pause();
}

__declspec(dllexport)
int __stdcall
WasapiIO_Unpause(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.Unpause();
}

__declspec(dllexport)
bool __stdcall
WasapiIO_GetPlayCursorPosition(int instanceId, int usageType, WasapiIoCursorLocation &pos_return)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    pos_return.posFrame      = self->wasapi.PcmStream().PosFrame(     (WWPcmDataUsageType)usageType);
    pos_return.totalFrameNum = self->wasapi.PcmStream().TotalFrameNum((WWPcmDataUsageType)usageType);
    return true;
}

__declspec(dllexport)
bool __stdcall
WasapiIO_SetPosFrame(int instanceId, int64_t v)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->wasapi.SetPosFrame(v);
}

__declspec(dllexport)
bool __stdcall
WasapiIO_GetSessionStatus(int instanceId, WasapiIoSessionStatus &stat_return)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    WWPcmFormat pcmFmt;
    self->wasapi.GetPcmFormat(pcmFmt);

    WWPcmFormat deviceFmt;
    self->wasapi.GetDevicePcmFormat(deviceFmt);

    stat_return.streamType          = self->wasapi.StreamType();
    stat_return.pcmDataSampleRate   = pcmFmt.sampleRate;
    stat_return.deviceSampleRate    = deviceFmt.sampleRate;
    stat_return.deviceSampleFormat  = deviceFmt.sampleFormat;
    stat_return.deviceNumChannels   = deviceFmt.numChannels;
    stat_return.deviceBytesPerFrame = deviceFmt.BytesPerFrame();
    stat_return.timePeriodHandledNanosec = self->wasapi.TimerResolution().GetTimePeriodHundredNanosec();
    stat_return.bufferFrameNum           = self->wasapi.GetEndpointBufferFrameNum();

    return true;
}

__declspec(dllexport)
void __stdcall
WasapiIO_RegisterStateChangedCallback(int instanceId, WWStateChanged callback)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->stateChangedCallback = callback;
}

__declspec(dllexport)
double __stdcall
WasapiIO_ScanPcmMaxAbsAmplitude(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->ScanPcmMaxAbsAmplitude();
}

__declspec(dllexport)
void __stdcall
WasapiIO_ScalePcmAmplitude(int instanceId, double scale)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    return self->ScalePcmAmplitude(scale);
}

__declspec(dllexport)
void __stdcall
WasapiIO_RegisterCaptureCallback(int instanceId, WWCaptureCallback callback)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);
    self->wasapi.RegisterCaptureCallback(callback);
}

__declspec(dllexport)
void __stdcall
WasapiIO_GetWorkerThreadSetupResult(int instanceId, WasapiIoWorkerThreadSetupResult &result)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    WWThreadCharacteristicsSetupResult r;
    self->wasapi.ThreadCharacteristics().GetThreadCharacteristicsSetupResult(r);
    result.dwmEnableMMCSSResult               = (int)r.dwmEnableMMCSSResult;
    result.avSetMmThreadCharacteristicsResult = (int)r.avSetMmThreadCharacteristicsResult;
    result.avSetMmThreadPriorityResult        = (int)r.avSetMmThreadPriorityResult;
}


__declspec(dllexport)
void __stdcall
WasapiIO_AppendAudioFilter(int instanceId, int audioFilterType, PCWSTR args)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->wasapi.MutexWait();
    {
        switch (audioFilterType) {
        case WWAF_PolarityInvert:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterPolarityInvert());
            break;
        case WWAF_Monaural:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterMonauralMix());
            break;
        case WWAF_ChannelMapping:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterChannelMapping(args));
            break;
        case WWAF_MuteChannel:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterMuteSoloChannel(WWAFMSMode_Mute, args));
            break;
        case WWAF_SoloChannel:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterMuteSoloChannel(WWAFMSMode_Solo, args));
            break;
        case WWAF_ZohNosdacCompensation:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterZohNosdacCompensation());
            break;
        case WWAF_Delay:
            self->wasapi.AudioFilterSequencer().Append(new WWAudioFilterDelay(args));
            break;
        default:
            assert(0);
            return;
        }
    }
    self->wasapi.MutexRelease();
}

__declspec(dllexport)
void __stdcall
WasapiIO_ClearAudioFilter(int instanceId)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    self->wasapi.MutexWait();
    {
        self->wasapi.AudioFilterSequencer().UnregisterAll();
    }
    self->wasapi.MutexRelease();
}

__declspec(dllexport)
int __stdcall
WasapiIO_GetMixFormat(int instanceId, int deviceId, WasapiIoMixFormat &mixFormat_return)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    IMMDevice *device = self->deviceEnumerator.GetDevice(deviceId);
    assert(device);

    WWPcmFormat pcmFormat;
    int hr = self->wasapi.GetMixFormat(device, pcmFormat);
    if (SUCCEEDED(hr)) {
        mixFormat_return.sampleFormat = pcmFormat.sampleFormat;
        mixFormat_return.sampleRate = pcmFormat.sampleRate;
        mixFormat_return.numChannels = pcmFormat.numChannels;
        mixFormat_return.dwChannelMask = pcmFormat.dwChannelMask;
    }

    return hr;
}

__declspec(dllexport)
int __stdcall
WasapiIO_GetVolumeParams(int instanceId, WasapiIoVolumeParams &result_return)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    WWVolumeParams params;
    int rv = self->wasapi.GetVolumeParams(&params);
    if (0 <= rv) {
        // イマイチな感じだが、コピーする。
        result_return.levelMinDB = params.levelMinDB;
        result_return.levelMaxDB = params.levelMaxDB;
        result_return.volumeIncrementDB = params.volumeIncrementDB;
        result_return.defaultLevel = params.defaultLevel;
        result_return.hardwareSupport = params.hardwareSupport;
    }
    return rv;
}

__declspec(dllexport)
int __stdcall
WasapiIO_SetMasterVolumeInDb(int instanceId, float db)
{
    WasapiIO *self = Instance(instanceId);
    assert(self);

    return self->wasapi.SetMasterVolumeLevelInDb(db);
}

}; // extern "C"
