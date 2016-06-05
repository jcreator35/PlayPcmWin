// 日本語UTF-8

#include <stdio.h>
#include <stdint.h>
#include <xmmintrin.h> //< _MM_SET_FLUSH_ZERO_MODE
#include <set>
#include <float.h>
#include <cmath>
#include <assert.h>

static const int S16_SUP = 32768;
static const int S24_SUP = 8388608;
static const int64_t S32_SUP = 2147483648LL;
static const float RECIP_S16SUP = 1.0f / S16_SUP;
static const float RECIP_S24SUP = 1.0f / S24_SUP;
static const float RECIP_S32SUP = 1.0f / S32_SUP;

// subnormal number flush to zero
static void
EnableFtz(void)
{
    _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);
}

static bool
Int16ToFloatToInt16(void)
{
    std::set<float> floatSet;
    for (int v = -S16_SUP; v<S16_SUP; ++v) {
        float f = v * RECIP_S16SUP;
        floatSet.insert(f);
    }
    printf("Int16 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        int v = (int)(S16_SUP * (*ite));
        intSet.insert(v);
    }
    printf("Int16 to Float to Int16: %lld elements\n", (int64_t)intSet.size());

    return true;
}

static bool
Int24ToFloatToInt24(void)
{
    std::set<float> floatSet;
    for (int v = -S24_SUP; v<S24_SUP; ++v) {
        float f = v * RECIP_S24SUP;
        floatSet.insert(f);
    }
    printf("Int24 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        int v = (int)(S24_SUP * (*ite));
        intSet.insert(v);
    }
    printf("Int24 to Float to Int24: %lld elements\n", (int64_t)intSet.size());

    return true;
}

static bool
Int32ToFloatToInt32(void)
{
    std::set<float> floatSet;
    for (int64_t v = -S32_SUP; v<S32_SUP; ++v) {
        float f = v * RECIP_S32SUP;
        const int *fi = (int*)&f;
        auto r = floatSet.insert(f);
#if 0
        if (r.second) {
            printf("Succeeded to insert %lld:%.17e %x\n", v, f, *fi);
        } else {
            printf("Failed    to insert %lld:%.17e %x\n", v, f, *fi);
        }
#endif
    }
    printf("Int32 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int64_t> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        float f = *ite;
        const int *fi = (int*)&f;
        int64_t v = (int64_t)(S32_SUP * f);
        auto r = intSet.insert(v);
        if (r.second) {
            //printf("Succeeded to insert %lld:%.17e %x\n", v, f, *fi);
        } else {
            printf("Failed    to insert %lld:%.17e %x\n", v, f, *fi);
        }
    }
    printf("Int32 to Float to Int32: %lld elements\n", (int64_t)intSet.size());

    return true;
}

static bool
Float32ToInt32(void)
{
    uint32_t fi;

    // fiを介して間接的に書き換えるので一応volatileをつけておく。
    volatile float *f = (volatile float*)&fi;

    int histogram[1024];
    memset((void*)histogram, 0, sizeof histogram);

    std::set<int64_t> intSet;
    for (fi = 0; fi != 0xffffffff; ++fi) {
        if (-1.0f <= *f && *f < 1.0f) {
            int64_t v = (int64_t)(S32_SUP * *f);
            auto r= intSet.insert(v);
#if 1
            if (r.second) {
                printf("Succeeded to insert %lld:%.17e %x\n", v, *f, fi);
            } else {
                printf("Failed    to insert %lld:%.17e %x\n", v, *f, fi);
            }
#endif
            int h = (int)((double)*f * 256.0 + 512);
            ++histogram[h];
        }
    }
    printf("Float to Int32: %lld elements\n", (int64_t)intSet.size());

    for (int i=0; i<1024; ++i) {
        if (histogram[i]) {
            printf("%d %d\n", i, histogram[i]);
        }
    }

    return true;
}

enum FpClassType {
    FpClass_SNAN,
    FpClass_QNAN,
    FpClass_NINF,
    FpClass_NN,
    FpClass_ND,

    FpClass_NZ,
    FpClass_PZ,
    FpClass_PD,
    FpClass_PN,
    FpClass_PINF,

    FpClass_NUM
};

#if 1
// experiment 1
static bool
FloatCount(void)
{
    int64_t counter[FpClass_NUM];
    memset(counter, 0, sizeof counter);

    int v = 0;
    float *f = (float*)&v;
    for (v = 0; v != 0xffffffff; ++v) {
        auto c = _fpclass(*f);
        switch (c) {
        case _FPCLASS_SNAN:
            ++counter[FpClass_SNAN];
            break;
        case _FPCLASS_QNAN:
            ++counter[FpClass_QNAN];
            break;
        case _FPCLASS_NINF:
            ++counter[FpClass_NINF];
            break;
        case _FPCLASS_NN:
            ++counter[FpClass_NN];
            break;
        case _FPCLASS_ND:
            ++counter[FpClass_ND];
            break;

        case _FPCLASS_NZ:
            ++counter[FpClass_NZ];
            break;
        case _FPCLASS_PZ:
            ++counter[FpClass_PZ];
            break;
        case _FPCLASS_PD:
            ++counter[FpClass_PD];
            break;
        case _FPCLASS_PN:
            ++counter[FpClass_PN];
            break;
        case _FPCLASS_PINF:
            ++counter[FpClass_PINF];
            break;
        default:
            assert(0);
            break;
        }
    }

    printf("Signaling NAN=%lld, Quiet NAN=%lld, Negative INF=%lld, Negative normal nonzero=%lld, Negative denormalized=%lld\n",
        counter[FpClass_SNAN], counter[FpClass_QNAN],
        counter[FpClass_NINF], counter[FpClass_NN],
        counter[FpClass_ND]);
    printf("Negative Zero=%lld, Positive Zero=%lld, Positive denormalized=%lld, Positive normalized nonzero=%lld, Positive INF=%lld\n",
        counter[FpClass_NZ], counter[FpClass_PZ],
        counter[FpClass_PD], counter[FpClass_PN],
        counter[FpClass_PINF]);

    return true;
}
#endif

int main(void)
{
#if 1
    // experiment 1
    FloatCount();
#else
    // experiment 2
    
    Float32ToInt32();
    Int16ToFloatToInt16();
    Int24ToFloatToInt24();
    Int32ToFloatToInt32();

    EnableFtz();
    printf("FTZ enabled. subnormal flushes to zero.\n");

    Int16ToFloatToInt16();
    Int24ToFloatToInt24();
    Int32ToFloatToInt32();
#endif

    return 0;
}
