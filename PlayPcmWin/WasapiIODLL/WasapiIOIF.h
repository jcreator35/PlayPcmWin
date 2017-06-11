#pragma once

#include <Windows.h>
#include "WasapiUser.h"
#include <stdint.h>

extern "C" {

typedef void (__stdcall WWStateChanged)(LPCWSTR deviceIdStr);

/// @param instanceId_return [out] instance id
/// @return 0: success. -1 or less: failed. returns error code HRESULT
__declspec(dllexport)
HRESULT __stdcall
WasapiIO_Init(int *instanceId_return);

/// @param instanceId instanceId returned from WasapiIO_Init
__declspec(dllexport)
void __stdcall
WasapiIO_Term(int instanceId);

/// @param deviceType == WWDeviceType, 0: Play, 1:Rec
__declspec(dllexport)
HRESULT __stdcall
WasapiIO_EnumerateDevices(int instanceId, int deviceType);

__declspec(dllexport)
int __stdcall
WasapiIO_GetDeviceCount(int instanceId);

#define WASAPI_IO_DEVICE_STR_COUNT (256)

#pragma pack(push, 4)
struct WasapiIoDeviceAttributes {
    int   deviceId;
    WCHAR name[WASAPI_IO_DEVICE_STR_COUNT];
    WCHAR deviceIdString[WASAPI_IO_DEVICE_STR_COUNT];
};
#pragma pack(pop)

__declspec(dllexport)
bool __stdcall
WasapiIO_GetDeviceAttributes(int instanceId, int deviceId, WasapiIoDeviceAttributes &attr_return);

#pragma pack(push, 4)
struct WasapiIoMixFormat {
    int sampleRate;
    int sampleFormat;    ///< WWPcmDataSampleFormatType
    int numChannels;
    int dwChannelMask;
};
#pragma pack(pop)

__declspec(dllexport)
int __stdcall
WasapiIO_GetMixFormat(int instanceId, int deviceId, WasapiIoMixFormat &mixFormat_return);

#pragma pack(push, 4)
struct WasapiIoInspectArgs {
    int deviceType;      ///< WWDeviceType, 0: Play, 1: Rec
    int sampleRate;
    int sampleFormat;    ///< WWPcmDataSampleFormatType
    int numChannels;
    int dwChannelMask;
};
#pragma pack(pop)

__declspec(dllexport)
int __stdcall
WasapiIO_InspectDevice(int instanceId, int deviceId, const WasapiIoInspectArgs &args);

#pragma pack(push, 4)
struct WasapiIoSetupArgs {
    int deviceType; ///< WWDeviceType, 0: Play, 1:Rec
    int streamType;
    int sampleRate;
    int sampleFormat;    ///< WWPcmDataSampleFormatType
    int numChannels;

    int dwChannelMask;
    int shareMode;
    int mmcssCall; ///< 0: disable, 1: enable, 2: do not call DwmEnableMMCSS()
    int mmThreadPriority; ///< 0: None, 1: Low, 2: Normal, 3: High, 4: Critical
    int schedulerTask;

    int dataFeedMode;
    int latencyMillisec;
    int timePeriodHandledNanosec;
    int zeroFlushMillisec;
    int isFormatSupportedCall;
};
#pragma pack(pop)

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_Setup(int instanceId, int deviceId, const WasapiIoSetupArgs &args);

__declspec(dllexport)
void __stdcall
WasapiIO_Unsetup(int instanceId);

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataStart(int instanceId);

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmData(int instanceId, int pcmId, unsigned char *data, int64_t bytes);

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataSetPcmFragment(int instanceId, int pcmId, int64_t posBytes, unsigned char *data, int64_t bytes);

/// @return HRESULT
__declspec(dllexport)
int __stdcall
WasapiIO_ResampleIfNeeded(int instanceId, int conversionQuality);

__declspec(dllexport)
bool __stdcall
WasapiIO_AddPlayPcmDataEnd(int instanceId);

__declspec(dllexport)
void __stdcall
WasapiIO_ClearPlayList(int instanceId);

__declspec(dllexport)
void __stdcall
WasapiIO_SetPlayRepeat(int instanceId, bool b);

__declspec(dllexport)
bool __stdcall
WasapiIO_ConnectPcmDataNext(int instanceId, int fromIdx, int toIdx);

__declspec(dllexport)
int __stdcall
WasapiIO_GetPcmDataId(int instanceId, int usageType);

__declspec(dllexport)
void __stdcall
WasapiIO_SetNowPlayingPcmDataId(int instanceId, int pcmId);

__declspec(dllexport)
int64_t __stdcall
WasapiIO_GetCaptureGlitchCount(int instanceId);

__declspec(dllexport)
void __stdcall
WasapiIO_ResetCaptureGlitchCount(int instanceId);

__declspec(dllexport)
HRESULT __stdcall
WasapiIO_Start(int instanceId, int pcmId);

__declspec(dllexport)
bool __stdcall
WasapiIO_Run(int instanceId, int millisec);

__declspec(dllexport)
void __stdcall
WasapiIO_Stop(int instanceId);

__declspec(dllexport)
int __stdcall
WasapiIO_Pause(int instanceId);

__declspec(dllexport)
int __stdcall
WasapiIO_Unpause(int instanceId);

#pragma pack(push, 4)
struct WasapiIoSessionStatus {
    int streamType;
    int pcmDataSampleRate;
    int deviceSampleRate;
    int deviceSampleFormat;
    int deviceBytesPerFrame;
    int deviceNumChannels;
    int timePeriodHandledNanosec;
    int bufferFrameNum;
};
#pragma pack(pop)

__declspec(dllexport)
bool __stdcall
WasapiIO_GetSessionStatus(int instanceId, WasapiIoSessionStatus &stat_return);

#pragma pack(push, 8)
struct WasapiIoCursorLocation {
    int64_t posFrame;
    int64_t totalFrameNum;
};
#pragma pack(pop)

__declspec(dllexport)
bool __stdcall
WasapiIO_GetPlayCursorPosition(int instanceId, int usageType, WasapiIoCursorLocation &pos_return);

__declspec(dllexport)
void __stdcall
WasapiIO_RegisterStateChangedCallback(int instanceId, WWStateChanged callback);

__declspec(dllexport)
int __stdcall
WasapiIO_GetTimePeriodHundredNanosec(int instanceId);

__declspec(dllexport)
double __stdcall
WasapiIO_ScanPcmMaxAbsAmplitude(int instanceId);

__declspec(dllexport)
void __stdcall
WasapiIO_ScalePcmAmplitude(int instanceId, double scale);

__declspec(dllexport)
void __stdcall
WasapiIO_RegisterCaptureCallback(int instanceId, WWCaptureCallback callback);

#pragma pack(push, 4)
struct WasapiIoWorkerThreadSetupResult {
    int dwmEnableMMCSSResult;
    int avSetMmThreadCharacteristicsResult;
    int avSetMmThreadPriorityResult;
};
#pragma pack(pop)

__declspec(dllexport)
void __stdcall
WasapiIO_GetWorkerThreadSetupResult(int instanceId,
    WasapiIoWorkerThreadSetupResult &result_return);

/// @param audioFilterType WWAudioFilterType
__declspec(dllexport)
void __stdcall
WasapiIO_AppendAudioFilter(int instanceId, int audioFilterType, PCWSTR args);

__declspec(dllexport)
void __stdcall
WasapiIO_ClearAudioFilter(int instanceId);

#pragma pack(push, 4)
struct WasapiIoVolumeParams {
    float levelMinDB;
    float levelMaxDB;
    float volumeIncrementDB;
    float defaultLevel;
    /// ENDPOINT_HARDWARE_SUPPORT_VOLUME ==1
    /// ENDPOINT_HARDWARE_SUPPORT_MUTE   ==2
    /// ENDPOINT_HARDWARE_SUPPORT_METER  ==4
    int hardwareSupport;
};
#pragma pack(pop)

__declspec(dllexport)
int __stdcall
WasapiIO_GetVolumeParams(int instanceId, WasapiIoVolumeParams &result_return);

__declspec(dllexport)
int __stdcall
WasapiIO_SetMasterVolumeInDb(int instanceId, float db);


}; // extern "C"
