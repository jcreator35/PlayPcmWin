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
#include "../WasapiIODLL/WWUtil.h"

HRESULT
WWMFUtilConfigureAudioTypeToUncompressedPcm(
    IMFSourceReader *pReader);
