#pragma once

// 日本語 UTF-8

#include <Windows.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <MMDeviceAPI.h>
#include "WWPcmData.h"
#include "WWPcmStream.h"
#include "WWTimerResolution.h"
#include "WWThreadCharacteristics.h"
#include "WWTypes.h"

/// @param data captured data
/// @param dataBytes captured data size in bytes
typedef void (__stdcall WWCaptureCallback)(unsigned char *data, int dataBytes);

enum WWDataFeedMode {
    WWDFMEventDriven,
    WWDFMTimerDriven,

    WWDFMNum
};

enum WWShareMode {
    WWSMShared,
    WWSMExclusive,
};

enum WWBitFormatType {
    WWBitFormatUnknown = -1,
    WWBitFormatSint,
    WWBitFormatSfloat,
    WWBitFormatNUM
};

class WasapiUser {
public:
    WasapiUser(void);
    ~WasapiUser(void);

    HRESULT Init(void);
    void    Term(void);

    /// @param bitFormat 0:Int, 1:Float
    /// @return 0 this sampleFormat is supported
    int InspectDevice(IMMDevice *device, WWPcmFormat &pcmFormat);

    /// @param format sampleRate pcm data sample rate. On WASAPI shared mode, device sample rate cannot be changed so
    ///        you need to resample pcm to DeviceSampleRate
    HRESULT Setup(IMMDevice *device, WWDeviceType deviceType, WWPcmFormat &pcmFormat, WWShareMode sm, WWDataFeedMode dfm, int latencyMillisec);

    void Unsetup(void);

    bool IsResampleNeeded(void) const;

    /// if you changed sample format after Setup() call this function...
    void UpdatePcmDataFormat(WWPcmFormat &fmt);

    /// 再生データをpcmDataに切り替える。再生中でも停止中でも再生一時停止中でも可。
    void UpdatePlayPcmData(WWPcmData &pcmData);

    /// 再生位置を移動する。
    bool SetPosFrame(int64_t v);

    /// cb is called when recording buffer is filled
    void RegisterCaptureCallback(WWCaptureCallback cb) {
        m_captureCallback = cb;
    }

    HRESULT Start(void);

    /// 再生スレッドが終了したかどうかを調べる。
    bool Run(int millisec);

    /// 停止。
    void Stop(void);

    /// ポーズ。
    HRESULT Pause(void);

    /// ポーズ解除。
    HRESULT Unpause(void);

    /// 再生PCMデータのミューテックス。
    void MutexWait(void);
    void MutexRelease(void);

    /// Setup後に呼ぶ(Setup()で代入するので)
    void GetPcmFormat(WWPcmFormat &pcmFormat) const { pcmFormat = m_pcmFormat; }

    /// デバイス(ミックスフォーマット)サンプルレート
    /// WASAPI共有の場合、Setup後にGetPcmDataSampleRateとは異なる値になることがある。
    void GetDevicePcmFormat(WWPcmFormat &deviceFormat) const { deviceFormat = m_deviceFormat; }
    EDataFlow GetDataFlow(void) const { return m_dataFlow; }
    int GetEndpointBufferFrameNum(void) const { return m_bufferFrameNum; }
    int64_t GetCaptureGlitchCount(void) const { return m_glitchCount; }

    WWStreamType StreamType(void) const { return m_pcmStream.StreamType(); }
    WWPcmStream &PcmStream(void) { return m_pcmStream; }
    WWTimerResolution &TimerResolution(void) { return m_timerResolution; }
    WWThreadCharacteristics &ThreadCharacteristics(void) { return m_threadCharacteristics; }

private:
    HANDLE       m_shutdownEvent;
    HANDLE       m_audioSamplesReadyEvent;

    IMMDevice    *m_deviceToUse;
    IAudioClient *m_audioClient;

    /// wasapi audio buffer frame size
    UINT32       m_bufferFrameNum;

    /// source data format
    WWPcmFormat m_pcmFormat;

    /// may have different value from m_pcmFormat on wasapi shared mode
    WWPcmFormat m_deviceFormat;

    WWDataFeedMode m_dataFeedMode;
    WWShareMode    m_shareMode;
    DWORD          m_latencyMillisec;

    IAudioRenderClient  *m_renderClient;
    IAudioCaptureClient *m_captureClient;
    HANDLE       m_thread;
    HANDLE       m_mutex;
    bool         m_coInitializeSuccess;
    int          m_footerNeedSendCount;

    EDataFlow    m_dataFlow;
    int64_t      m_glitchCount;
    int          m_footerCount;
    WWCaptureCallback *m_captureCallback;

    WWPcmStream m_pcmStream;
    WWTimerResolution m_timerResolution;
    WWThreadCharacteristics m_threadCharacteristics;

    static DWORD WINAPI RenderEntry(LPVOID lpThreadParameter);
    static DWORD WINAPI CaptureEntry(LPVOID lpThreadParameter);

    DWORD RenderMain(void);
    DWORD CaptureMain(void);

    bool AudioSamplesSendProc(void);
    bool AudioSamplesRecvProc(void);

    /// WASAPIレンダーバッファに詰めるデータを作る。
    int CreateWritableFrames(BYTE *pData_return, int wantFrames);

    /// 再生中(か一時停止中)に再生するPcmDataをセットする。
    /// サンプル値をなめらかに補間する。
    void UpdatePlayPcmDataWhenPlaying(WWPcmData &playPcmData);

    void PrepareBuffers(void);
};

