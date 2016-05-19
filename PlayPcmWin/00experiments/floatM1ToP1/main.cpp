#include <stdio.h>
#include <stdint.h>
#include <xmmintrin.h> //< _MM_SET_FLUSH_ZERO_MODE
#include <set>

static const float RECIP_32768 = 1.0f / 32768.0f;
static const float RECIP_8388608 = 1.0f / 8388608.0f;
static const float RECIP_2147483648 = 1.0f / 2147483648.0f;

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
    for (int v=-32768; v<32768; ++v) {
        float f = v * RECIP_32768;
        floatSet.insert(f);
    }
    printf("Int16 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        int v = (int)(32768.0f * (*ite));
        intSet.insert(v);
    }
    printf("Int16 to Float to Int16: %lld elements\n", (int64_t)intSet.size());

    return true;
}

static bool
Int24ToFloatToInt24(void)
{
    std::set<float> floatSet;
    for (int v=-8388608; v<8388608; ++v) {
        float f = v * RECIP_8388608;
        floatSet.insert(f);
    }
    printf("Int24 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        int v = (int)(8388608.0f * (*ite));
        intSet.insert(v);
    }
    printf("Int24 to Float to Int24: %lld elements\n", (int64_t)intSet.size());

    return true;
}

static bool
Int32ToFloatToInt32(void)
{
    std::set<float> floatSet;
    for (int64_t v=-2147483648; v<2147483648; ++v) {
        float f = v * RECIP_2147483648;
        floatSet.insert(f);
    }
    printf("Int32 to Float: %lld elements\n", (int64_t)floatSet.size());

    std::set<int> intSet;
    for (auto ite = floatSet.begin(); ite != floatSet.end(); ++ite) {
        int v = (int)(2147483648 * (*ite));
        intSet.insert(v);
    }
    printf("Int32 to Float to Int32: %lld elements\n", (int64_t)intSet.size());

    return true;
}

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

int main(void)
{
    // EnableFtz();
    //FloatCount();
    Int16ToFloatToInt16();
    Int24ToFloatToInt24();
    Int32ToFloatToInt32();

    return 0;
}
