#include <stdio.h>
#include <string.h>
#include <stdlib.h> //< atoi()
#include <assert.h>

typedef struct Pcm {
    float *buff;
    int   count;
} Pcm;

int Pcm_Bytes(Pcm *pcm)
{
    assert(pcm);
    return pcm->count * sizeof(float);
}

static void PrintUsage(const char *programName)
{
    printf("%s bitDepth maxMagnitude outputFloatFilePath\n",
            programName);
}

/* @retval 0 é∏îs
 * @retval 0à»äO ê¨å˜
 */
static int GeneratePcm(int bitDepth, int maxMagnitude, Pcm *pcm)
{
    int i;

    assert(pcm);
    assert(0<bitDepth);
    assert(bitDepth<28);
    assert(0<maxMagnitude);

    pcm->count = 2<<(bitDepth-1);
    pcm->buff = malloc(Pcm_Bytes(pcm));
    if (pcm->buff == NULL) {
        pcm->count =0;
        printf("E: could not allocate memory\n");
        return 0;
    }

    for (i=0; i<pcm->count; ++i) {
        int original = maxMagnitude * (i - pcm->count/2);
        pcm->buff[i] = (float)(original) / ((float)pcm->count/2);
    }

    return 1;
}

/* @retval 0 é∏îs
 * @retval 0à»äO ê¨å˜
 */
static int CheckFloatRetainsOriginalIntValues(Pcm *pcm, int bitDepth, int maxMagnitude)
{
    int i;

    for (i=0; i<pcm->count; ++i) {
        int original = maxMagnitude * (i - pcm->count/2);
        int recovered = (int)(pcm->buff[i] * pcm->count/2);

        if (original != recovered) {
            printf("PCM is different! original value=%d, recovered value=%d\n",
                    original, recovered);
            return 1;
        }
    }

    return 1;
}

/* @retval 0à»äO é∏îs
 * @retval 0 ê¨å˜
 */
int main(int argc, char *argv[])
{
    int rv = 1;
    int bitDepth;
    int maxMagnitude;
    const char *outputFloatFilePath = NULL;
    Pcm pcm = {0};
    FILE *fpw = NULL;
    errno_t ercd;
    size_t sz = 0;

    if (argc != 4) {
        PrintUsage(argv[0]);
        return 1;
    }

    bitDepth = atoi(argv[1]);
    if (bitDepth <= 0) {
        printf("E: bitdepth must be greater than 0\n");
        PrintUsage(argv[0]);
        return 1;
    }

    maxMagnitude = atoi(argv[2]);
    if (maxMagnitude <= 0 || 28 <= maxMagnitude) {
        printf("E: maxMagnitude must be greater than 0 and smaller than 28\n");
        PrintUsage(argv[0]);
        return 1;
    }

    if (!GeneratePcm(bitDepth, maxMagnitude, &pcm)) {
        PrintUsage(argv[0]);
        return 1;
    }

    ercd = fopen_s(&fpw, argv[3], "wb");
    if (ercd != 0 || NULL == fpw) {
        printf("E: file open error %d %s\n", ercd, argv[3]);
        goto end;
    }

    sz = fwrite(pcm.buff, 1, Pcm_Bytes(&pcm), fpw);
    if (sz != Pcm_Bytes(&pcm)) {
        printf("E: fwrite(%s, ...) failed\n", argv[3]);
        PrintUsage(argv[0]);
        goto end;
    }

    if (!CheckFloatRetainsOriginalIntValues(&pcm, bitDepth, maxMagnitude)) {
        printf("Float buffer does not retains all integer data of bitdepth %d\n", bitDepth);
    } else {
        printf("Float buffer retains all integer data of bitdepth %d\n", bitDepth);
    }

    rv = 0;
end:
    free(pcm.buff);
    pcm.buff = NULL;
    pcm.count = 0;

    fclose(fpw);
    fpw = NULL;

    return rv;
}
