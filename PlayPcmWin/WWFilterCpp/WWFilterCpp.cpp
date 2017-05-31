#include "stdafx.h"
#include "WWFilterCpp.h"
#include "WWLoopFilterCRFB.h"
#include "WWZohCompensation.h"
#include <map>

static int gNextIdx = 1;

static std::map<int, WWLoopFilterCRFB* > gIdxFilterMap;
static std::map<int, WWZohCompensation* > gIdxZohCompensationMap;

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

// Crfb ¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Build(int order, const double *a, const double *b, const double *g, double gain)
{
    auto *p = new WWLoopFilterCRFB(order, a, b, g, gain);
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

// Zoh Compensation ¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡¡

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


