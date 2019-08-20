// 日本語

#include "WWFilterCpp.h"
#include "WWLoopFilterCRFB.h"
#include "WWZohCompensation.h"
#include "WWIIRFilterParallel.h"
#include "WWIIRFilterSerial.h"
#include <map>

static int gNextIdx = 1;

#define ADD_NEW_INSTANCE(p, m)        \
    int idx = gNextIdx++;             \
    m.insert(std::make_pair(idx, p)); \
    return idx;

#define DESTROY(idx, m)   \
    auto r = m.find(idx); \
    if (r == m.end()) {   \
        return;           \
    }                     \
    auto *p = r->second;  \
    delete p;             \
    m.erase(r);

#define FIND(idx, m)      \
    auto r = m.find(idx); \
    if (r == m.end()) {   \
        return -1;        \
    }                     \
    auto *p = r->second;

// Crfb ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWLoopFilterCRFB<double>* > gIdxFilterMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Build(int order, const double *a, const double *b, const double *g, double gain)
{
    auto *p = new WWLoopFilterCRFB<double>(order, a, b, g, gain);
    ADD_NEW_INSTANCE(p,gIdxFilterMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_Crfb_Destroy(int idx)
{
    DESTROY(idx, gIdxFilterMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Filter(int idx, int n, const double *buffIn, unsigned char *buffOut)
{
    FIND(idx, gIdxFilterMap);

    p->Filter(n, buffIn, (unsigned char*)buffOut);

    return n;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_PrintDelayValues(int idx)
{
    FIND(idx, gIdxFilterMap);

    p->PrintDelayValues();

    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_SetDelayValues(int idx, const double *buff, int count)
{
    FIND(idx, gIdxFilterMap);

    p->SetDelayValues(buff, count);

    return 0;
}


// Zoh Compensation ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWZohCompensation* > gIdxZohCompensationMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Build(void)
{
    auto *p = new WWZohCompensation();
    ADD_NEW_INSTANCE(p, gIdxZohCompensationMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_ZohCompensation_Destroy(int idx)
{
    DESTROY(idx, gIdxZohCompensationMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Filter(int idx, int n, const double *buffIn, double *buffOut)
{
    FIND(idx, gIdxZohCompensationMap);

    p->Filter(n, buffIn, buffOut);

    return n;
}

// IIR Filter ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWIIRFilterSerial* > gIdxIIRSerialMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Build(int nBlocks)
{
    auto *p = new WWIIRFilterSerial(nBlocks);
    ADD_NEW_INSTANCE(p, gIdxIIRSerialMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRSerial_Destroy(int idx)
{
    DESTROY(idx, gIdxIIRSerialMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Add(int idx, int aCount, const double *a, int bCount, const double *b)
{
    FIND(idx, gIdxIIRSerialMap);

    p->Add(aCount, a, bCount, b);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut)
{
    FIND(idx, gIdxIIRSerialMap);

    p->Filter(nIn, buffIn, nOut, buffOut);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_SetParam(int idx, int osr, int decimation)
{
    FIND(idx, gIdxIIRSerialMap);

    p->SetOSR(osr);
    p->SetDecimation(decimation);

    return 0;
}


static std::map<int, WWIIRFilterParallel* > gIdxIIRParallelMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Build(int nBlocks)
{
    auto *p = new WWIIRFilterParallel(nBlocks);
    ADD_NEW_INSTANCE(p, gIdxIIRParallelMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRParallel_Destroy(int idx)
{
    DESTROY(idx, gIdxIIRParallelMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Add(int idx, int aCount, const double *a, int bCount, const double *b)
{
    FIND(idx, gIdxIIRParallelMap);

    p->Add(aCount, a, bCount, b);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut)
{
    FIND(idx, gIdxIIRParallelMap);

    p->Filter(nIn, buffIn, nOut, buffOut);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_SetParam(int idx, int osr, int decimation)
{
    FIND(idx, gIdxIIRParallelMap);

    p->SetOSR(osr);
    p->SetDecimation(decimation);

    return 0;
}

