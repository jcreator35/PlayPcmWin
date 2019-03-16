// 日本語。

#pragma once

#include "targetver.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#ifdef WWSHOWAUDIOSTATUS_EXPORTS
#define WWSHOWAUDIOSTATUS_API extern "C"  __declspec(dllexport)
#else
#define WWSHOWAUDIOSTATUS_API extern "C"  __declspec(dllimport)
#endif

#include "WWShowAudioStatus.h"

#define WW_NUM_CHANNELS (64)


struct WWSASAudioDeviceParams {
    int id;
    int isDefaultDevice;
    wchar_t name[256];
};

struct WWSASPcmFormat {
    int sampleFormat;       ///< WWMFBitFormatType of WWMFResampler.h
    int nChannels;          ///< PCMデータのチャンネル数。
    int bits;               ///< PCMデータ1サンプルあたりのビット数。パッド含む。
    int sampleRate;         ///< 44100等。
    int dwChannelMask;      ///< 2チャンネルステレオのとき3
    int validBitsPerSample; ///< PCMの量子化ビット数。
};

/// インスタンスの番号が戻る。
WWSHOWAUDIOSTATUS_API int __stdcall
WWSASInit(void);

/// 指定インスタンスを消す。
WWSHOWAUDIOSTATUS_API int __stdcall
WWSASTerm(int instanceId);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASRegisterStateChangedCallback(int instanceId, WWStateChanged callback);

// デバイスリスト。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASCreateDeviceList(int instanceId, int dataFlow);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASDestroyDeviceList(int instanceId);

/// AudioRenderデバイスの数を戻す。
WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceCount(int instanceId);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceParams(
    int instanceId,
    int idx,
    WWSASAudioDeviceParams * params_return);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetMixFormat(
    int instanceId,
    int idx,
    WWMixFormat * saf_return);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetSpatialAudioParams(
    int instanceId,
    int idx,
    WWSpatialAudioParams * sap_return);

// デバイスノード。■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

struct WWDeviceNodeIF {
    uint64_t self;
    uint64_t parent;
    int type;
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASCreateDeviceNodeList(
    int instanceId,
    int idx);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceNodeNum(
    int instanceId);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetDeviceNodeNth(
    int instanceId,
    int idx,
    WWDeviceNodeIF *dn_return);


WWSHOWAUDIOSTATUS_API int __stdcall
WWSASClearDeviceNodeList(
    int instanceId);

struct WWAudioMuteIF {
    int bEnabled;
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioMuteParams(
    int instanceId,
    int idx,
    WWAudioMuteIF *param_return);

struct WWAudioVolumeLevelIF {
    int nChannels;
    float volumeLevels[WW_NUM_CHANNELS];
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioVolumeLevelParams(
    int instanceId,
    int idx,
    WWAudioVolumeLevelIF *param_return);

struct WWKsJackDescriptionIF {
    int ChannelMapping;
    int Color;
    int ConnectionType;
    int GeoLocation;
    int GenLocation;
    int PortConnection;
    int IsConnected;
};

struct WWKsJackDescriptionsIF {
    int nChannels;
    WWKsJackDescriptionIF desc[WW_NUM_CHANNELS];
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsJackDescriptionsParams(
    int instanceId,
    int idx,
    WWKsJackDescriptionsIF *param_return);

struct WWAudioInputSelectorIF {
    int id;
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioInputSelectorParams(
    int instanceId,
    int idx,
    WWAudioInputSelectorIF *param_return);

struct WWPartIF {
    int partType;
    int localId;
    wchar_t name[256];
    wchar_t gid[256];
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetPartParams(
    int instanceId,
    int idx,
    WWPartIF *param_return);

struct WWControlInterfaceIF {
    wchar_t name[256];
    wchar_t iid[256];
};

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetControlInterfaceParams(
    int instanceId,
    int idx,
    WWControlInterfaceIF *param_return);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatpreferredFmt(
    int instanceId,
    int idx,
    WWKsFormat *param_return);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatSupportedFmtNum(
    int instanceId,
    int idx);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetKsFormatSupportedFmtNth(
    int instanceId,
    int idx,
    int nth,
    WWKsFormat *param_return);

WWSHOWAUDIOSTATUS_API int __stdcall
WWSASGetAudioChannelConfig(
    int instanceId,
    int idx);

