#include <stdio.h>
#include <string.h>
#include <stdlib.h> //< atoi()
#include <assert.h>
#include <xmmintrin.h> //< _MM_SET_FLUSH_ZERO_MODE

static void PrintUsage(const char *programName)
{
    printf("Usage\n"
        "%s -generate32 bitdepth writeFilePath\n"
        "    Generate file that contains 32bit float values\n"
        "%s -convert32 [-ftz] bitdepth readFilePath numerator denominator writeFilePath\n"
        "    read float values from readFilePath and scale sample value by numerator/deniminator and write to writeFilePath\n",
        "%s -add32 readFilePath numerator denominator writeFilePath\n"
        "    read float values from readFilePath and add sample value with numerator/deniminator and write to writeFilePath\n",
        programName, programName);
}

static int Generate32(int argc, char *argv[])
{
    FILE *fpw = NULL;
    int rv = 1;
    float *buff = NULL;
    int bitDepth = 0;
    int floatNum;
    int buffBytes;
    size_t sz = 0;
    int i;
    errno_t ercd;

    if (0 != strcmp(argv[1], "-generate32")) {
        PrintUsage(argv[0]);
        return 1;
    }

    bitDepth = atoi(argv[2]);
    if (bitDepth <= 0 || 28 < bitDepth) {
        printf("E: bitdepth must be larger than 0 and smaller than 29\n");
        PrintUsage(argv[0]);
        return 1;
    }

    floatNum = 2 << (bitDepth-1);
    buffBytes = floatNum * sizeof(float);
    buff = (float *)malloc(floatNum * sizeof(float));
    if (buff == NULL) {
        printf("E: could not allocate memory\n");
        return 1;
    }

    ercd = fopen_s(&fpw, argv[3], "wb");
    if (ercd != 0 || NULL == fpw) {
        printf("E: file open error %d %s\n", ercd, argv[3]);
        return 1;
    }

    for (i=0; i<floatNum; ++i) {
        buff[i] = ((float)(i - floatNum/2)) / (floatNum/2);
    }

    sz = fwrite(buff, 1, buffBytes, fpw);
    if (sz != buffBytes) {
        printf("E: fwrite() failed\n");
        buff = NULL;
        return 1;
    }

    free(buff);
    buff = NULL;

    fclose(fpw);
    fpw = NULL;

    return rv;
}

///////////////////////////////////////////////////////////////////////////////////

static int Convert32(FILE *fpr, float multiplier, FILE *fpw)
{
    int i;
    long fileBytes = 0;
    long buffCount = 0;
    long buffBytes = 0;
    float *buff = NULL;
    size_t sz = 0;

    assert(fpr);
    assert(fpw);

    fseek(fpr, 0, SEEK_END);
    fileBytes = ftell(fpr);
    fseek(fpr, 0, SEEK_SET);
    if (fileBytes <= 0) {
        printf("E: read file size is too small\n");
        return 1;
    }

    buffCount = fileBytes/sizeof(float);
    // buffBytesはファイルサイズを4の倍数に切り捨てた値になる。
    buffBytes = buffCount * sizeof(float);
    buff = (float *)malloc(buffBytes);
    if (buff == NULL) {
        printf("E: could not allocate memory\n");
        return 1;
    }

    sz = fread(buff, 1, buffBytes, fpr);
    if (sz != buffBytes) {
        printf("E: fread() failed\n");
        buff = NULL;
        return 1;
    }

    for (i=0; i<buffCount; ++i) {
        buff[i] *= multiplier;
    }

    sz = fwrite(buff, 1, buffBytes, fpw);
    if (sz != buffBytes) {
        printf("E: fwrite() failed\n");
        buff = NULL;
        return 1;
    }

    free(buff);
    buff = NULL;

    return 0;
}

static int ReadConvertWrite32(int argc, char *argv[])
{
    FILE *fpw = NULL;
    FILE *fpr = NULL;
    errno_t ercd;
    int numerator;
    int denominator;
    float multiplier;
    int rv = 1;
    int ftz = 0;

    if (0 != strcmp(argv[1], "-convert32")) {
        PrintUsage(argv[0]);
        return 1;
    }

    if (0 == strcmp(argv[2], "-ftz")) {
        ftz = 1;
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);
    }

    numerator   = atoi(argv[ftz+4]);
    denominator = atoi(argv[ftz+5]);
    if (numerator == 0 || denominator == 0) {
        printf("E: numerator and denominator must not be zero\n");
        return 1;
    }

    multiplier = (float)numerator / denominator;

    ercd = fopen_s(&fpr, argv[ftz+3], "rb");
    if (ercd != 0 || NULL == fpr) {
        printf("E: file open error %d %s\n", ercd, argv[ftz+3]);
        return 1;
    }

    ercd = fopen_s(&fpw, argv[ftz+6], "wb");
    if (ercd != 0 || NULL == fpw) {
        printf("E: file open error %d %s\n", ercd, argv[ftz+6]);

        fclose(fpr);
        fpr = NULL;
        return 1;
    }

    rv = Convert32(fpr, multiplier, fpw);

    fclose(fpw);
    fpw = NULL;
    fclose(fpr);
    fpr = NULL;

    return rv;
}

/////////////////////////////////////////////////////////////////////////////////////////

static int Add32(FILE *fpr, float add, FILE *fpw)
{
    int i;
    long fileBytes = 0;
    long buffCount = 0;
    long buffBytes = 0;
    float *buff = NULL;
    size_t sz = 0;

    assert(fpr);
    assert(fpw);

    fseek(fpr, 0, SEEK_END);
    fileBytes = ftell(fpr);
    fseek(fpr, 0, SEEK_SET);
    if (fileBytes <= 0) {
        printf("E: read file size is too small\n");
        return 1;
    }

    buffCount = fileBytes/sizeof(float);
    // buffBytesはファイルサイズを4の倍数に切り捨てた値になる。
    buffBytes = buffCount * sizeof(float);
    buff = (float *)malloc(buffBytes);
    if (buff == NULL) {
        printf("E: could not allocate memory\n");
        return 1;
    }

    sz = fread(buff, 1, buffBytes, fpr);
    if (sz != buffBytes) {
        printf("E: fread() failed\n");
        buff = NULL;
        return 1;
    }

    for (i=0; i<buffCount; ++i) {
        buff[i] += add;
    }

    sz = fwrite(buff, 1, buffBytes, fpw);
    if (sz != buffBytes) {
        printf("E: fwrite() failed\n");
        buff = NULL;
        return 1;
    }

    free(buff);
    buff = NULL;

    return 0;
}

static int ReadAddWrite32(int argc, char *argv[])
{
    FILE *fpw = NULL;
    FILE *fpr = NULL;
    errno_t ercd;
    int numerator;
    int denominator;
    float add;
    int rv = 1;
    int ftz = 0;

    if (0 != strcmp(argv[1], "-add32") || argc != 6) {
        PrintUsage(argv[0]);
        return 1;
    }

    numerator   = atoi(argv[3]);
    denominator = atoi(argv[4]);
    if (numerator == 0 || denominator == 0) {
        printf("E: numerator and denominator must not be zero\n");
        return 1;
    }

    add = (float)numerator / denominator;

    ercd = fopen_s(&fpr, argv[2], "rb");
    if (ercd != 0 || NULL == fpr) {
        printf("E: file open error %d %s\n", ercd, argv[ftz+3]);
        return 1;
    }

    ercd = fopen_s(&fpw, argv[5], "wb");
    if (ercd != 0 || NULL == fpw) {
        printf("E: file open error %d %s\n", ercd, argv[ftz+6]);

        fclose(fpr);
        fpr = NULL;
        return 1;
    }

    rv = Add32(fpr, add, fpw);

    fclose(fpw);
    fpw = NULL;
    fclose(fpr);
    fpr = NULL;

    return rv;
}

///////////////////////////////////////////////////////////////////////////////////

int main(int argc, char *argv[])
{
    int rv = 1;
    switch (argc) {
    case 4:
        return Generate32(argc, argv);
        break;
    case 6:
        return ReadAddWrite32(argc, argv);
    case 7:
    case 8:
        return ReadConvertWrite32(argc, argv);
    default:
        PrintUsage(argv[0]);
        break;
    }
    return rv;
}

