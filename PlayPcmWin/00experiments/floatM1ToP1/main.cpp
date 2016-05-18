#include <stdio.h>

int main(void)
{
    FILE *fp = fopen("output.bin", "wb");

    float m1 = -1.0f;
    float p1 = 1.0f;

    int normalCount = 0;
    int subNormalCount = 0;

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

    printf("subnormal=%d normal=%d total=%d\n",
            subNormalCount, normalCount, normalCount+subNormalCount);

    fclose(fp);

    return 0;
}
