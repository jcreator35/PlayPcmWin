#include <stdio.h>
#include <stdint.h>
#include <xmmintrin.h> //< _MM_SET_FLUSH_ZERO_MODE
#include <set>

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
        floatSet.insert(f);
    }
    printf("Int32 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
		int v = (int)(S32_SUP * (*ite));
        intSet.insert(v);
    }
    printf("Int32 to Float to Int32: %lld elements\n", (int64_t)intSet.size());

    return true;
}

#if 0
static bool
FloatCount(void)
{
    FILE *fp = fopen("output.bin", "wb");
    if (fp == nullptr) {
        printf("Error: failed to open file\n");
        return false;
    }

    float m1 = -1.0f;
    float p1 = 1.0f;

    int64_t normalCount = 0;
    int64_t subNormalCount = 0;

    int *v = (int*)&m1;
    while (p1 != m1) {
        fwrite(&v, 1, 4, fp);
        (*v) = *v + 1;

        if (*v & 0x7f800000) {
            ++normalCount;
        } else {
            ++subNormalCount;
        }
    }

    printf("subnormal=%lld normal=%lld total=%lld\n",
            subNormalCount, normalCount, normalCount+subNormalCount);

    fclose(fp);

    return true;
}
#endif

int main(void)
{
#if 0
	// experiment 1
	FloatCount();
#else
	// experiment 2
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
