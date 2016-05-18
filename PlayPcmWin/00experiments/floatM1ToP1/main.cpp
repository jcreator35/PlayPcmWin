#include <stdio.h>
#include <stdint.h>

int main(void)
{
    FILE *fp = fopen("output.bin", "wb");
    if (fp == nullptr) {
        printf("Error: failed to open file\n");
        return 1;
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

    return 0;
}
