#ifdef WWFILTERCPP_EXPORTS
#define WWFILTERCPP_API __declspec(dllexport)
#else
#define WWFILTERCPP_API __declspec(dllimport)
#endif

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

// zoh nosdac compensation

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Build(void);

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_ZohCompensation_Destroy(int idx);

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Filter(int idx, int n, const double *buffIn, double *buffOut);

