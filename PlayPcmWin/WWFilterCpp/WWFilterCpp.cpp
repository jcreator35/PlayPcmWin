#include "stdafx.h"
#include "WWFilterCpp.h"
#include "WWLoopFilterCRFB.h"
#include <map>

static std::map<int, WWLoopFilterCRFB* > gIdxFilterMap;
static int gNextIdx = 1;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Build(int order, const double *a, const double *b,
        const double *g, double gain)
{
    int idx = gNextIdx++;

    auto *p = new WWLoopFilterCRFB(order, a, b, g, gain);
    gIdxFilterMap.insert(std::make_pair(idx, p));

    return idx;
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_Crfb_Destroy(int idx)
{
    auto r = gIdxFilterMap.find(idx);
    if (r == gIdxFilterMap.end()) {
        return;
    }

    auto *p = r->second;
    delete p;

    gIdxFilterMap.erase(r);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Filter(int idx, int n, const double *buffIn,
    unsigned char *buffOut)
{
    auto r = gIdxFilterMap.find(idx);
    if (r == gIdxFilterMap.end()) {
        return -1;
    }

    auto *p = r->second;

    p->Filter(n, buffIn, (unsigned char*)buffOut);

    return n;
}
