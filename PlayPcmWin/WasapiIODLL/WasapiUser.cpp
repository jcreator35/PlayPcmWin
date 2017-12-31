// 日本語 UTF-8
// WASAPIの機能を使って音を出したり録音したりするWasapiUserクラス。

#include "WasapiUser.h"
#include "WWUtil.h"
#include <assert.h>
#include <strsafe.h>
#include <mmsystem.h>
#include <malloc.h>
#include <stdint.h>

#define FOOTER_SEND_FRAME_NUM                   (2)
#define PERIODS_PER_BUFFER_ON_TIMER_DRIVEN_MODE (4)

// define: レンダーバッファ上で再生データを作る
// undef : 一旦スタック上にて再生データを作ってからレンダーバッファにコピーする
#define CREATE_PLAYPCM_ON_RENDER_BUFFER

// DoPマーカーが正しく付いているかチェックする。
#define CHECK_DOP_MARKER

static AUDCLNT_SHAREMODE
WWShareModeToAudClientShareMode(WWShareMode sm)
{
    switch (sm) {
    case WWSMShared:
        return AUDCLNT_SHAREMODE_SHARED;
    case WWSMExclusive:
        return AUDCLNT_SHAREMODE_EXCLUSIVE;
    default:
        assert(0);
        return AUDCLNT_SHAREMODE_EXCLUSIVE;
    }
}

static void
PcmFormatToWfex(const WWPcmFormat &pcmFormat, WAVEFORMATEXTENSIBLE *wfex)
{
    wfex->Format.wFormatTag           = WAVE_FORMAT_EXTENSIBLE;
    wfex->Format.nChannels            = (WORD)pcmFormat.numChannels;
    wfex->Format.nSamplesPerSec       = pcmFormat.sampleRate;
    wfex->Format.wBitsPerSample       = (WORD)WWPcmDataSampleFormatTypeToBitsPerSample(pcmFormat.sampleFormat);
    wfex->Format.cbSize               = 22;
    wfex->Samples.wValidBitsPerSample = (WORD)WWPcmDataSampleFormatTypeToValidBitsPerSample(pcmFormat.sampleFormat);
    wfex->dwChannelMask               = pcmFormat.dwChannelMask;

    if (WWPcmDataSampleFormatTypeIsInt(pcmFormat.sampleFormat)) {
        wfex->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
    } else {
        wfex->SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
    }

    // あとは計算で決まる。
    wfex->Format.nBlockAlign          = (WORD)((wfex->Format.wBitsPerSample / 8) * wfex->Format.nChannels);
    wfex->Format.nAvgBytesPerSec      = wfex->Format.nSamplesPerSec * wfex->Format.nBlockAlign;
}

static void
WfexToPcmFormat(const WAVEFORMATEXTENSIBLE *wfex, WWPcmFormat &pcmFormat)
{
    pcmFormat.Set(
            wfex->Format.nSamplesPerSec,
            WWPcmDataSampleFormatTypeGenerate(wfex->Format.wBitsPerSample,
            wfex->Samples.wValidBitsPerSample,
            wfex->SubFormat),
            wfex->Format.nChannels,
            wfex->dwChannelMask,
            WWStreamUnknown);
}

static EDataFlow
WWDeviceTypeToEDataFlow(WWDeviceType t)
{
    switch (t) {
    case WWDTPlay:
        return eRender;
    case WWDTRec:
        return eCapture;
    default:
        assert(0);
        return eRender;
    }
}

WasapiUser::WasapiUser(void)
{
    m_shutdownEvent          = nullptr;
    m_audioSamplesReadyEvent = nullptr;
    m_deviceToUse            = nullptr;
    m_audioClient            = nullptr;
    m_bufferFrameNum         = 0;

    m_pcmFormat.Clear();
    m_deviceFormat.Clear();

    m_dataFeedMode    = WWDFMEventDriven;
    m_shareMode       = WWSMExclusive;
    m_latencyMillisec = 0;
    m_renderClient    = nullptr;
    m_captureClient   = nullptr;

    m_thread              = nullptr;
    m_mutex               = nullptr;
    m_coInitializeSuccess = false;
    m_footerNeedSendCount = 0;
    m_dataFlow            = eRender;

    m_glitchCount     = 0;
    m_footerCount     = 0;
    m_captureCallback = nullptr;
    m_endpointVolume = nullptr;
}

WasapiUser::~WasapiUser(void)
{
    assert(!m_deviceToUse);
}

HRESULT
WasapiUser::Init(void)
{
    HRESULT hr = S_OK;
    
    dprintf("D: %s()\n", __FUNCTION__);

    assert(!m_deviceToUse);

    hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (S_OK == hr) {
        m_coInitializeSuccess = true;
    } else {
        // Managed applicationから呼び出すと0x80010106が起こる。
        dprintf("D: WasapiUser::Init() CoInitializeEx() failed %08x\n", hr);
        hr = S_OK;
    }

    assert(!m_mutex);
    m_mutex = CreateMutex(nullptr, FALSE, nullptr);

    m_audioFilterSequencer.Init();

    return hr;
}

void
WasapiUser::Term(void)
{
    dprintf("D: %s() m_deviceToUse=%p m_mutex=%p\n", __FUNCTION__, m_deviceToUse, m_mutex);

    m_captureCallback = nullptr;

    m_audioFilterSequencer.Term();

    SafeRelease(&m_deviceToUse);

    if (m_mutex) {
        CloseHandle(m_mutex);
        m_mutex = nullptr;
    }

    if (m_coInitializeSuccess) {
        CoUninitialize();
    }
}

HRESULT
WasapiUser::GetMixFormat(IMMDevice *device, WWPcmFormat *mixFormat_return)
{
    HRESULT hr;
    WAVEFORMATEX *waveFormat = nullptr;
    IAudioClient *audioClient = nullptr;

    HRG(device->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&audioClient));

    assert(!waveFormat);
    HRG(audioClient->GetMixFormat(&waveFormat));
    assert(waveFormat);

    WAVEFORMATEXTENSIBLE * wfex = (WAVEFORMATEXTENSIBLE*)waveFormat;

    dprintf("original Mix Format:\n");
    WWWaveFormatDebug(waveFormat);
    WWWFEXDebug(wfex);

    WfexToPcmFormat(wfex, *mixFormat_return);

end:
    SafeRelease(&device);
    SafeRelease(&audioClient);

    if (waveFormat) {
        CoTaskMemFree(waveFormat);
        waveFormat = nullptr;
    }

    return hr;
}

HRESULT
WasapiUser::GetDevicePeriod(IMMDevice *device, int64_t *defaultPeriod, int64_t *minimumPeriod)
{
    HRESULT hr;

    IAudioClient *audioClient = nullptr;

    HRG(device->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&audioClient));

    HRG(audioClient->GetDevicePeriod(defaultPeriod, minimumPeriod));

end:
    SafeRelease(&device);
    SafeRelease(&audioClient);

    return hr;
}

int
WasapiUser::GetVolumeParams(WWVolumeParams *volumeParams_return)
{
    HRESULT hr = S_OK;

    if (nullptr == m_endpointVolume) {
        return E_FAIL;
    }

    HRG(m_endpointVolume->GetVolumeRange(
            &volumeParams_return->levelMinDB,
            &volumeParams_return->levelMaxDB,
            &volumeParams_return->volumeIncrementDB));

    DWORD dwHardwareSupport;
    HRG(m_endpointVolume->QueryHardwareSupport(&dwHardwareSupport));
    volumeParams_return->hardwareSupport = dwHardwareSupport;

    HRG(m_endpointVolume->GetMasterVolumeLevel(&volumeParams_return->defaultLevel));

    dprintf("WasapiUser::GetVolumeParams() levelMinDb=%f levelMaxDb=%f volumeIncrementDb=%f defaultLevel=%f hardwareSupport=0x%x\n",
        volumeParams_return->levelMinDB, volumeParams_return->levelMaxDB,
        volumeParams_return->volumeIncrementDB,
        volumeParams_return->defaultLevel, volumeParams_return->hardwareSupport);

end:
    return hr;
}

int
WasapiUser::SetMasterVolumeLevelInDb(float db)
{
    HRESULT hr = S_OK;

    if (nullptr == m_endpointVolume) {
        return E_FAIL;
    }

    HRG(m_endpointVolume->SetMasterVolumeLevel(db, nullptr));

end:
    return hr;
}

HRESULT
WasapiUser::InspectDevice(IMMDevice *device, const WWPcmFormat &pcmFormat)
{
    HRESULT hr;
    WAVEFORMATEX *waveFormat = nullptr;

    HRG(device->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&m_audioClient));

    assert(!waveFormat);
    HRG(m_audioClient->GetMixFormat(&waveFormat));
    assert(waveFormat);

    WAVEFORMATEXTENSIBLE * wfex = (WAVEFORMATEXTENSIBLE*)waveFormat;

    dprintf("original Mix Format:\n");
    WWWaveFormatDebug(waveFormat);
    WWWFEXDebug(wfex);

    if (waveFormat->wFormatTag != WAVE_FORMAT_EXTENSIBLE) {
        dprintf("E: unsupported device ! mixformat == 0x%08x\n", waveFormat->wFormatTag);
        hr = E_FAIL;
        goto end;
    }

    PcmFormatToWfex(pcmFormat, wfex);

    dprintf("preferred Format:\n");
    WWWaveFormatDebug(waveFormat);
    WWWFEXDebug(wfex);

    hr = m_audioClient->IsFormatSupported(AUDCLNT_SHAREMODE_EXCLUSIVE, waveFormat, nullptr);
    dprintf("IsFormatSupported=%08x\n", hr);

end:
    SafeRelease(&device);
    SafeRelease(&m_audioClient);

    if (waveFormat) {
        CoTaskMemFree(waveFormat);
        waveFormat = nullptr;
    }

    return hr;
}

HRESULT
WasapiUser::Setup(IMMDevice *device, WWDeviceType deviceType, const WWPcmFormat &pcmFormat,
        WWShareMode sm, WWDataFeedMode dfm, int latencyMillisec, bool isFormatSupportedCall)
{
    HRESULT      hr          = 0;
    WAVEFORMATEX *waveFormat = nullptr;

    m_shareMode = sm;
    m_dataFeedMode = dfm;
    m_latencyMillisec = latencyMillisec;
    m_dataFlow = WWDeviceTypeToEDataFlow(deviceType);

    auto audClientSm = WWShareModeToAudClientShareMode(sm);

    dprintf("D: %s(%d %s %d)\n", __FUNCTION__, pcmFormat.sampleRate, WWPcmDataSampleFormatTypeToStr(pcmFormat.sampleFormat), pcmFormat.numChannels);
    m_pcmFormat = pcmFormat;

    m_pcmStream.SetStreamType(m_pcmFormat.streamType);

    m_audioSamplesReadyEvent = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(m_audioSamplesReadyEvent);

    assert(!m_deviceToUse);
    m_deviceToUse = device;

    assert(!m_audioClient);
    HRG(m_deviceToUse->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&m_audioClient));

    assert(!waveFormat);
    HRG(m_audioClient->GetMixFormat(&waveFormat));
    assert(waveFormat);

    WAVEFORMATEXTENSIBLE * wfex = (WAVEFORMATEXTENSIBLE*)waveFormat;

    dprintf("original Mix Format:\n");
    WWWaveFormatDebug(waveFormat);
    WWWFEXDebug(wfex);

    assert(waveFormat->wFormatTag == WAVE_FORMAT_EXTENSIBLE);

    if (WWSMExclusive == m_shareMode) {
        // exclusive mode specific task
        // on exclusive mode, sampleRate, bitsPerSample can be changed on most devices.
        // also nChannels can be changed on some audio devices.

        PcmFormatToWfex(m_pcmFormat, wfex);

        dprintf("preferred Format:\n");
        WWWaveFormatDebug(waveFormat);
        WWWFEXDebug(wfex);

        if (isFormatSupportedCall) {
            // 20150907:
            // On iFi nano iDSD, IAudioClient::IsFormatSupported(705600Hz) returns false
            // but IAudioClient::Initialize(705600Hz) succeeds and play 705600Hz PCM smoothly
            // therefore following line is commented out.
            // 20160811:
            // Some realtek HD Audio device driver accepts IAudioClient::Initialize(24bit) but
            // IAudioClient::IsFormatSupported(24bit) returns false
            // when playing 24bit, it produces large static noise so I think the following line is necessary
            HRG(m_audioClient->IsFormatSupported(audClientSm, waveFormat, nullptr));
        }
    } else {
        // shared mode specific task
        // on shared mode, wBitsPerSample, nSamplesPerSec, wValidBitsPerSample and subFormat are fixed.
        // numChannels and dwChannelMask can be changed on some devices.

        // 32bit float is used to represent sample value on wasapi shared
        assert(wfex->Format.wBitsPerSample == 32);
        assert(wfex->Samples.wValidBitsPerSample == 32);
        assert(wfex->SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT);

        // sample rate cannot be changed. use MixFormat sample rate. m_pcmFormat.sampleRate is not used.
        // On shared mode, after this Setup() call, caller must change sample rate to m_deviceFormat.sampleRate
        // assert(wfex->Format.nSamplesPerSec == m_pcmFormat.sampleRate);

        // try changing channel count
        wfex->Format.nChannels       = (WORD)m_pcmFormat.numChannels;
        wfex->Format.nBlockAlign     = (WORD)((wfex->Format.wBitsPerSample / 8) * wfex->Format.nChannels);
        wfex->Format.nAvgBytesPerSec = wfex->Format.nSamplesPerSec*wfex->Format.nBlockAlign;
        wfex->dwChannelMask          = m_pcmFormat.dwChannelMask;
    }

    DWORD streamFlags      = 0;
    int   periodsPerBuffer = 1;
    switch (m_dataFeedMode) {
    case WWDFMTimerDriven:
        streamFlags      = AUDCLNT_STREAMFLAGS_NOPERSIST;
        periodsPerBuffer = PERIODS_PER_BUFFER_ON_TIMER_DRIVEN_MODE;
        break;
    case WWDFMEventDriven:
        streamFlags      = AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_NOPERSIST;
        periodsPerBuffer = 1;
        break;
    default:
        assert(0);
        break;
    }

    REFERENCE_TIME bufferPeriodicity = m_latencyMillisec * 10000;
    REFERENCE_TIME bufferDuration    = bufferPeriodicity * periodsPerBuffer;

    m_deviceFormat.sampleRate    = waveFormat->nSamplesPerSec;
    m_deviceFormat.numChannels   = waveFormat->nChannels;
    m_deviceFormat.dwChannelMask = wfex->dwChannelMask;

    if (WWSMExclusive == m_shareMode) {
        // exclusive mode specific task.
        m_deviceFormat.sampleFormat  = m_pcmFormat.sampleFormat;
    } else {
        // shared mode specific task.
        m_deviceFormat.sampleFormat = WWPcmDataSampleFormatSfloat;

        if (WWDFMEventDriven == m_dataFeedMode) {
            // shared mode event driven specific task.
            bufferPeriodicity = 0;
        }
    }

    hr = m_audioClient->Initialize(audClientSm, streamFlags, bufferDuration, bufferPeriodicity, waveFormat, nullptr);
    if (hr == AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED) {
        HRG(m_audioClient->GetBufferSize(&m_bufferFrameNum));

        SafeRelease(&m_audioClient);

        bufferPeriodicity = (REFERENCE_TIME)(
            10000.0 *                         // (REFERENCE_TIME(100ns) / ms) *
            1000 *                            // (ms / s) *
            m_bufferFrameNum /                // frames /
            waveFormat->nSamplesPerSec +      // (frames / s)
            0.5);
        bufferDuration = bufferPeriodicity * periodsPerBuffer;

        HRG(m_deviceToUse->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, nullptr, (void**)&m_audioClient));

        hr = m_audioClient->Initialize(audClientSm, streamFlags, bufferDuration, bufferPeriodicity, waveFormat, nullptr);
    }
    if (FAILED(hr)) {
        dprintf("E: audioClient->Initialize failed 0x%08x\n", hr);
        goto end;
    }

    HRG(m_audioClient->GetBufferSize(&m_bufferFrameNum));
    dprintf("m_audioClient->GetBufferSize() rv=%u\n", m_bufferFrameNum);

    if (WWDFMEventDriven == m_dataFeedMode) {
        HRG(m_audioClient->SetEventHandle(m_audioSamplesReadyEvent));
    }

    switch (m_dataFlow) {
    case eRender:
        HRG(m_audioClient->GetService(IID_PPV_ARGS(&m_renderClient)));
        m_pcmStream.PrepareSilenceBuffers(m_latencyMillisec, m_deviceFormat.sampleFormat, m_deviceFormat.sampleRate, m_deviceFormat.numChannels, m_deviceFormat.BytesPerFrame());
        break;
    case eCapture:
        HRG(m_audioClient->GetService(IID_PPV_ARGS(&m_captureClient)));
        HRG(m_deviceToUse->Activate(__uuidof(IAudioEndpointVolume), CLSCTX_INPROC_SERVER, nullptr, (void**)&m_endpointVolume));
        assert(m_endpointVolume);
        break;
    default:
        assert(0);
        break;
    }

end:
    if (waveFormat) {
        CoTaskMemFree(waveFormat);
        waveFormat = nullptr;
    }

    return hr;
}

bool
WasapiUser::IsResampleNeeded(void) const
{
    if (WWSMExclusive == m_shareMode) {
        return false;
    }

    if (m_deviceFormat.sampleRate != m_pcmFormat.sampleRate ||
            m_deviceFormat.numChannels != m_pcmFormat.numChannels ||
            m_deviceFormat.dwChannelMask != m_pcmFormat.dwChannelMask ||
            WWPcmDataSampleFormatSfloat != m_pcmFormat.sampleFormat) {
        return true;
    }
    return false;
}

void
WasapiUser::UpdatePcmDataFormat(const WWPcmFormat &fmt)
{
    assert(WWSMShared == m_shareMode);
    m_pcmFormat = fmt;

    m_pcmStream.SetStreamType(fmt.streamType);
}

void
WasapiUser::Unsetup(void)
{
    dprintf("D: %s() ASRE=%p CC=%p RC=%p AC=%p\n", __FUNCTION__, m_audioSamplesReadyEvent, m_captureClient, m_renderClient, m_audioClient);

    if (m_audioSamplesReadyEvent) {
        CloseHandle(m_audioSamplesReadyEvent);
        m_audioSamplesReadyEvent = nullptr;
    }

    m_pcmStream.ReleaseBuffers();

    SafeRelease(&m_endpointVolume);
    SafeRelease(&m_deviceToUse);
    SafeRelease(&m_captureClient);
    SafeRelease(&m_renderClient);
    SafeRelease(&m_audioClient);
}

HRESULT
WasapiUser::Start(void)
{
    HRESULT hr      = 0;
    BYTE    *pData  = nullptr;
    UINT32  nFrames = 0;
    DWORD   flags   = 0;

    dprintf("D: %s()\n", __FUNCTION__);

    HRG(m_audioClient->Reset());

    assert(!m_shutdownEvent);
    m_shutdownEvent = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(m_shutdownEvent);

    switch (m_dataFlow) {
    case eRender:
        {
            WWPcmData *pcm = m_pcmStream.GetPcm(WWPDUNowPlaying);
            assert(pcm);

            assert(nullptr == m_thread);
            m_thread = CreateThread(nullptr, 0, RenderEntry, this, 0, nullptr);
            assert(m_thread);

            nFrames = m_bufferFrameNum;
            if (WWDFMTimerDriven == m_dataFeedMode || WWSMShared == m_shareMode) {
                // 排他タイマー駆動の場合、パッド計算必要。
                // 共有モードの場合タイマー駆動でもイベント駆動でもパッドが必要。
                // RenderSharedEventDrivenのWASAPIRenderer.cpp参照。

                UINT32 padding = 0; //< frame now using
                HRG(m_audioClient->GetCurrentPadding(&padding));
                nFrames = m_bufferFrameNum - padding;
            }

            if (0 <= nFrames) {
                assert(m_renderClient);
                HRG(m_renderClient->GetBuffer(nFrames, &pData));
                memset(pData, 0, nFrames * m_deviceFormat.BytesPerFrame());
                HRG(m_renderClient->ReleaseBuffer(nFrames, 0));
            }

            m_footerCount = 0;

            m_audioFilterSequencer.UpdateSampleFormat(m_pcmFormat.sampleRate,
                    pcm->SampleFormat(), pcm->StreamType(), pcm->Channels());
        }
        break;

    case eCapture:
        assert(m_captureCallback);
        m_thread = CreateThread(nullptr, 0, CaptureEntry, this, 0, nullptr);
        assert(m_thread);

        hr = m_captureClient->GetBuffer(&pData, &nFrames, &flags, nullptr, nullptr);
        if (SUCCEEDED(hr)) {
            // if succeeded, release buffer pData
            m_captureClient->ReleaseBuffer(nFrames);
            pData = nullptr;
        }

        hr = S_OK;
        m_glitchCount = 0;
        break;

    default:
        assert(0);
        break;
    }

    assert(m_audioClient);
    HRG(m_audioClient->Start());

end:
    return hr;
}

HRESULT
WasapiUser::Stop(void)
{
    HRESULT hr = S_OK;
    BOOL b;
    DWORD dw = S_OK;

    dprintf("D: %s() AC=%p SE=%p T=%p\n", __FUNCTION__, m_audioClient, m_shutdownEvent, m_thread);

    // ポーズ中の場合、ポーズを解除。
    m_pcmStream.SetPauseResumePcmData(nullptr);

    if (nullptr != m_audioClient) {
        hr = m_audioClient->Stop();
        if (FAILED(hr)) {
            dprintf("E: %s m_audioClient->Stop() failed 0x%x\n", __FUNCTION__, hr);
        }
    }

    if (nullptr != m_shutdownEvent) {
        SetEvent(m_shutdownEvent);
    }

    if (nullptr != m_thread) {
        WaitForSingleObject(m_thread, INFINITE);

        b = GetExitCodeThread(m_thread, &dw);
        if (b && SUCCEEDED(hr)) {
            hr = dw;
            dprintf("D: Thread exit code = 0x%x\n", hr);
        }

        dprintf("D: %s:%d CloseHandle(%p)\n", __FILE__, __LINE__, m_thread);
        if (m_thread) {
            CloseHandle(m_thread);
        }
        m_thread = nullptr;
    }

    if (nullptr != m_shutdownEvent) {
        dprintf("D: %s:%d CloseHandle(%p)\n", __FILE__, __LINE__, m_shutdownEvent);
        CloseHandle(m_shutdownEvent);
        m_shutdownEvent = nullptr;
    }

    return hr;
}

HRESULT
WasapiUser::Pause(void)
{
    // HRESULT hr = S_OK;
    bool pauseDataSetSucceeded = false;

    assert(m_mutex);
    WaitForSingleObject(m_mutex, INFINITE);
    {
        WWPcmData *nowPlaying = m_pcmStream.GetPcm(WWPDUNowPlaying);
        if (nowPlaying && nowPlaying->ContentType() == WWPcmDataContentMusicData) {
            // 通常データを再生中の場合ポーズが可能。
            // m_nowPlayingPcmDataをpauseBuffer(フェードアウトするPCMデータ)に差し替え、
            // 再生が終わるまでブロッキングで待つ。
            pauseDataSetSucceeded = true;
            m_pcmStream.Paused(nowPlaying);
        }
        if (nowPlaying && nowPlaying->ContentType() == WWPcmDataContentSilenceForTrailing) {
            // 再生開始前無音を再生中。ポーズが可能。
            pauseDataSetSucceeded = true;
            m_pcmStream.Paused(nowPlaying->Next());
        }
    }
    ReleaseMutex(m_mutex);

    if (pauseDataSetSucceeded) {
        // ここで再生一時停止までブロックする。
        WWPcmData *nowPlayingPcmData = nullptr;
        do {
            assert(m_mutex);
            WaitForSingleObject(m_mutex, INFINITE);
            {
                nowPlayingPcmData = m_pcmStream.GetPcm(WWPDUNowPlaying);
            }
            ReleaseMutex(m_mutex);

            Sleep(100);
        } while (nowPlayingPcmData != nullptr);
        //再生一時停止状態はnowPlayingPcmData==nullptrで、再生スレッドは無音を送出し続ける。
    } else {
        dprintf("%s pauseDataSet failed\n", __FUNCTION__);
    }

//end:
    return (pauseDataSetSucceeded) ? S_OK : E_FAIL;
}

HRESULT
WasapiUser::Unpause(void)
{
    if (m_pcmStream.GetPcm(WWPDUPauseResumeToPlay) == nullptr) {
        // ポーズ中ではないのにUnpause()が呼び出された。
        return E_FAIL;
    }

    WWPcmData *restartBuffer = m_pcmStream.UnpausePrepare();

    assert(m_mutex);
    WaitForSingleObject(m_mutex, INFINITE);
    {
        m_pcmStream.UpdateNowPlaying(restartBuffer);
    }
    ReleaseMutex(m_mutex);

    m_pcmStream.UnpauseDone();
    return S_OK;
}

bool
WasapiUser::Run(int millisec)
{
    DWORD rv = WaitForSingleObject(m_thread, millisec);
    if (rv == WAIT_TIMEOUT) {
        Sleep(10);
        return false;
    }

    return true;
}

void
WasapiUser::UpdatePlayPcmData(WWPcmData &pcmData)
{
    if (m_thread != nullptr) {
        UpdatePlayPcmDataWhenPlaying(pcmData);
    } else {
        m_pcmStream.UpdateStartPcm(&pcmData);
    }
}

void
WasapiUser::UpdatePlayPcmDataWhenPlaying(WWPcmData &pcmData)
{
    dprintf("D: %s(%d)\n", __FUNCTION__, pcmData.Id());

    assert(m_mutex);
    WaitForSingleObject(m_mutex, INFINITE);
    {
        WWPcmData *nowPlaying = m_pcmStream.GetPcm(WWPDUNowPlaying);
        if (nowPlaying) {
            WWPcmData *splice = m_pcmStream.GetPcm(WWPDUSplice);
            // m_nowPlayingPcmDataをpcmDataに移動する。
            // Issue3: いきなり移動するとブチッと言うのでsplice bufferを経由してなめらかにつなげる。
            int advance = splice->CreateCrossfadeData(*nowPlaying, nowPlaying->PosFrame(), pcmData, pcmData.PosFrame());

            if (nowPlaying != &pcmData) {
                // 別の再生曲に移動した場合、それまで再生していた曲は頭出ししておく。
                nowPlaying->SetPosFrame(0);
            }

            splice->SetNext(WWPcmData::AdvanceFrames(&pcmData, advance));
            m_pcmStream.UpdateNowPlaying(splice);
        } else {
            // 一時停止中。
            WWPcmData *pauseResumePcm = m_pcmStream.GetPcm(WWPDUPauseResumeToPlay);
            if (pauseResumePcm != &pcmData) {
                // 別の再生曲に移動した場合、それまで再生していた曲は頭出ししておく。
                pauseResumePcm->SetPosFrame(0);
                m_pcmStream.UpdatePauseResume(&pcmData);

                // 再生シークをしたあと再生一時停止し再生曲を変更し再生再開すると
                // 一瞬再生曲表示が再生シークした曲になる問題の修正ｗ
                m_pcmStream.GetPcm(WWPDUSplice)->SetNext(nullptr);
            }
        }
    }
    ReleaseMutex(m_mutex);
}

bool
WasapiUser::SetPosFrame(int64_t v)
{
    if (m_dataFlow != eRender) {
        assert(0);
        return false;
    }

    if (v < 0) {
        return false;
    }

    if (WWStreamDop == m_pcmStream.StreamType()) {
        // 必ず2の倍数の位置にジャンプする。
        v &= ~(1LL);
    }

    bool result = false;

    assert(m_mutex);
    WaitForSingleObject(m_mutex, INFINITE);
    {
        WWPcmData *nowPlaying = m_pcmStream.GetPcm(WWPDUNowPlaying);
        if (nowPlaying &&
                nowPlaying->ContentType() == WWPcmDataContentMusicData && v < nowPlaying->Frames()) {
            WWPcmData *splice = m_pcmStream.GetPcm(WWPDUSplice);
            // 再生中。
            // nowPlaying->posFrameをvに移動する。
            // Issue3: いきなり移動するとブチッと言うのでsplice bufferを経由してなめらかにつなげる。
            int advance = splice->CreateCrossfadeData(*nowPlaying, nowPlaying->PosFrame(), *nowPlaying, v);

            // 移動先は、nowPlaying上の位置v + クロスフェードのためにadvanceフレーム進んだ位置となる。
            nowPlaying->SetPosFrame(v);
            WWPcmData *toPcm = WWPcmData::AdvanceFrames(nowPlaying, advance);
            splice->SetNext(toPcm);

            m_pcmStream.UpdateNowPlaying(splice);

#ifdef CHECK_DOP_MARKER
            splice->CheckDopMarker();
#endif // CHECK_DOP_MARKER

            result = true;
        } else {
            WWPcmData *pauseResumePcm = m_pcmStream.GetPcm(WWPDUPauseResumeToPlay);
            if (pauseResumePcm && v < pauseResumePcm->Frames()) {
                // pause中。Pause再開後に再生されるPCMの再生位置を更新する。
                pauseResumePcm->SetPosFrame(v);
                result = true;
            }
        }
    }
    ReleaseMutex(m_mutex);

    return result;
}

void
WasapiUser::MutexWait(void) {
    WaitForSingleObject(m_mutex, INFINITE);
}

void
WasapiUser::MutexRelease(void) {
    ReleaseMutex(m_mutex);
}

/////////////////////////////////////////////////////////////////////////////////
// 再生スレッド

/// 再生スレッドの入り口。
/// @param lpThreadParameter WasapiUserインスタンスのポインタが渡ってくる。
DWORD
WasapiUser::RenderEntry(LPVOID lpThreadParameter)
{
    WasapiUser* self = (WasapiUser*)lpThreadParameter;

    return self->RenderMain();
}

/// PCMデータをwantFramesフレームだけpData_returnに戻す。
/// @return 実際にpData_returnに書き込んだフレーム数。
int
WasapiUser::CreateWritableFrames(BYTE *pData_return, int wantFrames)
{
    int       pos      = 0;
    WWPcmData *pcmData = m_pcmStream.GetPcm(WWPDUNowPlaying);

    while (nullptr != pcmData && 0 < wantFrames) {
        int copyFrames = wantFrames;
        if (pcmData->Frames() <= pcmData->PosFrame() + wantFrames) {
            // pcmDataが持っているフレーム数よりも要求フレーム数が多い。
            copyFrames = (int)(pcmData->Frames() - pcmData->PosFrame());
        }

        dprintf("pcmData=%p next=%p posFrame/nframes=%lld/%lld copyFrames=%d\n",
                pcmData, pcmData->Next(), pcmData->PosFrame(), pcmData->Frames(), copyFrames);

        CopyMemory(&pData_return[pos*m_deviceFormat.BytesPerFrame()],
            &(pcmData->Stream()[pcmData->PosFrame() * m_deviceFormat.BytesPerFrame()]),
            copyFrames * m_deviceFormat.BytesPerFrame());

        pos        += copyFrames;
        wantFrames -= copyFrames;
        pcmData->SetPosFrame(pcmData->PosFrame() + copyFrames);

        if (pcmData->Frames() <= pcmData->PosFrame()) {
            // pcmDataの最後まで来た。
            // このpcmDataの再生位置は巻き戻して、次のpcmDataの先頭をポイントする。
            pcmData->SetPosFrame(0);
            pcmData = pcmData->Next();
        }
    }

    m_pcmStream.UpdateNowPlaying(pcmData);

    return pos;
}

/// WASAPIデバイスにPCMデータを送れるだけ送る。
HRESULT
WasapiUser::AudioSamplesSendProc(bool &result)
{
    result             = true;
    BYTE    *to        = nullptr;
    HRESULT hr         = 0;
    int     copyFrames = 0;
    int     writableFrames = 0;

    WaitForSingleObject(m_mutex, INFINITE);

    writableFrames = m_bufferFrameNum;
    if (WWDFMTimerDriven == m_dataFeedMode || WWSMShared == m_shareMode) {
        // 共有モードの場合イベント駆動でもパッドが必要になる。
        // RenderSharedEventDrivenのWASAPIRenderer.cpp参照。

        UINT32 padding = 0; //< frame num now using

        assert(m_audioClient);
        HRGR(m_audioClient->GetCurrentPadding(&padding));

        writableFrames = m_bufferFrameNum - padding;

        // dprintf("m_bufferFrameNum=%d padding=%d writableFrames=%d\n", m_bufferFrameNum, padding, writableFrames);
        if (writableFrames <= 0) {
            goto end;
        }
    }

    assert(m_renderClient);
    HRGR(m_renderClient->GetBuffer(writableFrames, &to));
    assert(to);

    copyFrames = CreateWritableFrames(to, writableFrames);

    if (m_audioFilterSequencer.IsAvailable()) {
        // エフェクトを掛ける
        m_audioFilterSequencer.ProcessSamples(to, copyFrames*m_deviceFormat.BytesPerFrame());
    }

    if (0 < writableFrames - copyFrames) {
        memset(&to[copyFrames*m_deviceFormat.BytesPerFrame()], 0, (writableFrames - copyFrames)*m_deviceFormat.BytesPerFrame());
        // dprintf("fc=%d bs=%d cb=%d memset %d bytes\n", m_footerCount, m_bufferFrameNum, copyFrames, (m_bufferFrameNum - copyFrames)*m_deviceFormat.BytesPerFrame());
    }

    HRGR(m_renderClient->ReleaseBuffer(writableFrames, 0));
    to = nullptr;

    if (nullptr == m_pcmStream.GetPcm(WWPDUNowPlaying)) {
        ++m_footerCount;
        if (m_footerNeedSendCount < m_footerCount) {
            // PCMを全て送信完了。
            if (nullptr != m_pcmStream.GetPcm(WWPDUPauseResumeToPlay)) {
                // ポーズ中。スレッドを回し続ける。
            } else {
                // スレッドを停止する。
                result = false;
            }
        }
    }

end:
    if (FAILED(hr)) {
        result = false;
    }

    ReleaseMutex(m_mutex);
    return hr;
}

/// 再生スレッド メイン。
/// イベントやタイマーによって起き、PCMデータを送って、寝る。
/// というのを繰り返す。
DWORD
WasapiUser::RenderMain(void)
{
    bool    stillPlaying   = true;
    HANDLE  waitArray[2]   = {m_shutdownEvent, m_audioSamplesReadyEvent};
    int     waitArrayCount;
    DWORD   timeoutMillisec;
    DWORD   waitResult;
    HRESULT hr             = 0;
    
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

    HRG(m_timerResolution.Setup());
    m_threadCharacteristics.Setup();

    if (m_dataFeedMode == WWDFMTimerDriven) {
        waitArrayCount        = 1;
        m_footerNeedSendCount = FOOTER_SEND_FRAME_NUM * 2;
        timeoutMillisec       = m_latencyMillisec     / 2;
    } else {
        waitArrayCount        = 2;
        m_footerNeedSendCount = FOOTER_SEND_FRAME_NUM;
        timeoutMillisec       = INFINITE;
    }

    // dprintf("D: %s() waitArrayCount=%d m_shutdownEvent=%p m_audioSamplesReadyEvent=%p\n", __FUNCTION__, waitArrayCount, m_shutdownEvent, m_audioSamplesReadyEvent);

    while (stillPlaying) {
        waitResult = WaitForMultipleObjects(
            waitArrayCount, waitArray, FALSE, timeoutMillisec);
        switch (waitResult) {
        case WAIT_OBJECT_0 + 0:     // m_shutdownEvent
            // シャットダウン要求によって起きた場合。
            dprintf("D: %s() shutdown event flagged\n", __FUNCTION__);
            stillPlaying = false;
            break;
        case WAIT_OBJECT_0 + 1:     // m_audioSamplesReadyEvent
            // イベント駆動モードの時だけ起こる。
            hr = AudioSamplesSendProc(stillPlaying);
            break;
        case WAIT_TIMEOUT:
            // タイマー駆動モードの時だけ起こる。
            hr = AudioSamplesSendProc(stillPlaying);
            break;
        default:
            break;
        }
    }

end:
    m_threadCharacteristics.Unsetup();
    m_timerResolution.Unsetup();

    CoUninitialize();
    return hr;
}

//////////////////////////////////////////////////////////////////////////////
// 録音スレッド

DWORD
WasapiUser::CaptureEntry(LPVOID lpThreadParameter)
{
    WasapiUser* self = (WasapiUser*)lpThreadParameter;
    return self->CaptureMain();
}

DWORD
WasapiUser::CaptureMain(void)
{
    bool    stillRecording   = true;
    HANDLE  waitArray[2]   = {m_shutdownEvent, m_audioSamplesReadyEvent};
    int     waitArrayCount;
    DWORD   timeoutMillisec;
    DWORD   waitResult;
    HRESULT hr             = 0;
    
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

    HRG(m_timerResolution.Setup());
    m_threadCharacteristics.Setup();

    if (m_dataFeedMode == WWDFMTimerDriven) {
        waitArrayCount  = 1;
        timeoutMillisec = m_latencyMillisec / 2;
    } else {
        waitArrayCount  = 2;
        timeoutMillisec = INFINITE;
    }

    while (stillRecording) {
        waitResult = WaitForMultipleObjects(waitArrayCount, waitArray, FALSE, timeoutMillisec);
        switch (waitResult) {
        case WAIT_OBJECT_0 + 0:     // m_shutdownEvent
            stillRecording = false;
            break;
        case WAIT_OBJECT_0 + 1:     // m_audioSamplesReadyEvent
            // only in EventDriven mode
            hr = AudioSamplesRecvProc(stillRecording);
            break;
        case WAIT_TIMEOUT:
            // only in TimerDriven mode
            hr = AudioSamplesRecvProc(stillRecording);
            break;
        default:
            break;
        }
    }

end:
    m_threadCharacteristics.Unsetup();
    m_timerResolution.Unsetup();

    CoUninitialize();
    return hr;
}

HRESULT
WasapiUser::AudioSamplesRecvProc(bool &result)
{
    result     = true;
    UINT32  packetLength = 0;
    UINT32  numFramesAvailable = 0;
    DWORD   flags      = 0;
    BYTE    *pData     = nullptr;
    HRESULT hr         = 0;
    UINT64  devicePosition = 0;

    WaitForSingleObject(m_mutex, INFINITE);

    HRG(m_captureClient->GetNextPacketSize(&packetLength));

    if (packetLength == 0) {
        goto end;
    }
        
    numFramesAvailable = packetLength;
    flags = 0;

    HRG(m_captureClient->GetBuffer(&pData, &numFramesAvailable, &flags, &devicePosition, nullptr));

    if (flags & AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY) {
        ++m_glitchCount;
    }

    if (m_captureCallback != nullptr) {
        // 都度コールバックを呼ぶ
        m_captureCallback(pData, numFramesAvailable * m_deviceFormat.BytesPerFrame());
        HRG(m_captureClient->ReleaseBuffer(numFramesAvailable));
        goto end;
    }

end:
    ReleaseMutex(m_mutex);
    return hr;
}
