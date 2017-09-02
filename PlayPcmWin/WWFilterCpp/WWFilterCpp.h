// 日本語

#pragma once

#ifdef WWFILTERCPP_EXPORTS
#define WWFILTERCPP_API __declspec(dllexport)
#else
#define WWFILTERCPP_API __declspec(dllimport)
#endif

// crfb ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

/// @return idx of built instance
extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Build(int order, const double *a, const double *b, const double *g, double gain);

/// @param idx WWFilterCpp_Build returned
extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_Crfb_Destroy(int idx);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Filter(int idx, int n, const double *buffIn, unsigned char *buffOut);

// zoh nosdac compensation ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Build(void);

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_ZohCompensation_Destroy(int idx);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Filter(int idx, int n, const double *buffIn, double *buffOut);

// IIR Filter ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Build(int nBlocks);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Build(int nBlocks);

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRSerial_Destroy(int idx);

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRParallel_Destroy(int idx);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Add(int idx, int aCount, const double *a, int bCount, const double *b);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Add(int idx, int aCount, const double *a, int bCount, const double *b);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_SetParam(int idx, int osr, int decimation);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_SetParam(int idx, int osr, int decimation);

// SdmToPcm conversion ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Test(void);
