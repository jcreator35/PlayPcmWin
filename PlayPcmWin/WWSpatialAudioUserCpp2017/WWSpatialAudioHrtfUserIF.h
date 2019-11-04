// 日本語 UTF-8

#pragma once

#ifdef WWSPATIALAUDIOUSER_EXPORTS
#define WWSPATIALAUDIOUSER_API extern "C"  __declspec(dllexport)
#else
#define WWSPATIALAUDIOUSER_API extern "C"  __declspec(dllimport)
#endif

#include "WWSpatialAudioHrtfUser.h"
#include "WWSpatialAudioDeviceProperty.h"

/// 新たに実体を作成。
/// @return instanceId 0以上の番号。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioHrtfUserInit(void);

/// 実体を削除する。
/// @param instanceId 実体のID番号。Initで戻る値。
WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioHrtfUserTerm(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioHrtfUserDoEnumeration(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioHrtfUserGetDeviceCount(int instanceId);

WWSPATIALAUDIOUSER_API int __stdcall
WWSpatialAudioHrtfUserGetDeviceProperty(
    int instanceId, int devIdx,
    WWSpatialAudioDeviceProperty &sadp_r);

