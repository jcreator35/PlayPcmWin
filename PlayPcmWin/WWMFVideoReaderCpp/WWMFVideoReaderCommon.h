// 日本語。

#pragma once

#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdint.h>
#include <stdio.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>
#include <string>


const char *MFVideoChromaSubsamplingToStr(MFVideoChromaSubsampling t);
const char *MFVideoInterlaceModeToStr(MFVideoInterlaceMode t);
const char *MFVideoTransferFunctionToStr(MFVideoTransferFunction t);
const char *MFVideoTransferMatrixToStr(MFVideoTransferMatrix t);
const char *MFVideoPrimariesToStr(MFVideoPrimaries t);
const char *MFVideoLightingToStr(MFVideoLighting t);
const char *MFNominalRangeToStr(MFNominalRange t);
const std::string MFVideoAreaToStr(const MFVideoArea a);
const std::string MFVideoFlagsToStr(uint64_t t);

void PrintMFVideoFormat(const MFVIDEOFORMAT *p);
