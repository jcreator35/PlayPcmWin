// 日本語 UTF-8

#pragma once

#ifdef WWSPATIALAUDIOUSER_EXPORTS
#define WWSPATIALAUDIOUSER_API extern "C"  __declspec(dllexport)
#else
#define WWSPATIALAUDIOUSER_API extern "C"  __declspec(dllimport)
#endif

#include "WWSpatialAudioUser.h"
#include "WWSpatialAudioDeviceProperty.h"
#include "WWPlayStatus.h"

/// 新たに実体を作成。
/// @return instanceId 0以上の番号。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserInit(void);

/// 実体を削除する。
/// @param instanceId 実体のID番号。Initで戻る値。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserTerm(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserDoEnumeration(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceCount(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetDeviceProperty(
    int instanceId, int devIdx,
    WWSpatialAudioDeviceProperty &sadp_r);

/// @param staticObjectTypeMask AudioObjectType
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserChooseDevice(
    int instanceId, int devIdx, int maxDynObjectCount, int staticObjectTypeMask);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserClearAllPcm(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmBegin(
    int instanceId, int ch, int64_t numSamples);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmFragment(
    int instanceId, int ch, int64_t startSamplePos, int sampleCount, float * samples);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPcmEnd(
    int instanceId, int ch, int audioObjectType);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserStart(
    int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserStop(
    int instanceId);

/// HRESULTが戻る。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetThreadErcd(
    int instanceId);

/// WWTrackEnumが戻る。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetPlayingTrackNr(
    int instanceId, int ch, int *trackNr_r);

/// 全てのチャンネルの再生位置を変更。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetPosFrame(
    int instanceId, int64_t frame);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserGetPlayStatus(int instanceId, int ch, WWPlayStatus *pos_return);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserRewind(
    int instanceId);


/// Immediately change playing PCM
/// @param trackEnum WWTrackEnum
/// @param changeTrackMethod WWChangeTrackMethod
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioUserSetCurrentPcm(
    int instanceId, int trackEnum, int changeTrackMethod);

