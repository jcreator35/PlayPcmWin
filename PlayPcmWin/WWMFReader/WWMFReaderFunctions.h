// 日本語。

#pragma once

#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <stdio.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>
#include "WWMFReaderMetadata.h"

HRESULT
WWMFReaderGetDuration(IMFSourceReader *pReader, MFTIME *phnsDuration);

HRESULT
WWMFReaderGetAudioEncodingBitrate(IMFSourceReader *pReader, UINT32 *bitrate_return);

HRESULT
WWMFReaderCollectMetadata(IMFMetadata *pMetadata,
    WWMFReaderMetadata &meta);

HRESULT
WWMFReaderConfigureAudioTypeToUncompressedPcm(
    IMFSourceReader *pReader);

HRESULT
WWMFReaderCreateMediaSource(
    const WCHAR *sURL,
    IMFMediaSource** ppMediaSource);

HRESULT
WWMFReaderGetUncompressedPcmAudio(
    IMFSourceReader *pReader,
    IMFMediaType **ppPCMAudio);

