// 日本語

#include "CrossfeedF.h"
#include "CrossfeedD.h"
#include <stdio.h>
#include <assert.h>
#include <string.h>

enum PrecisionType {
    PREC_SINGLEPREC,
    PREC_DOUBLEPREC,
};

int
wmain(int argc, wchar_t *argv[])
{
    if (argc != 5) {
        printf("Usage:\n"
            " %S -F coeffFile inputFile outputFile : use single precision\n"
            " %S -D coeffFile inputFile outputFile : use double precision\n", argv[0], argv[0]);
        return 1;
    }

    PrecisionType prec = PREC_SINGLEPREC;
    if (0 == wcsncmp(L"-D", argv[1], 2)) {
        prec = PREC_DOUBLEPREC;
    }

    const wchar_t *coeffPath = argv[2];
    const wchar_t *fromPath = argv[3];
    const wchar_t *toPath = argv[4];

    int result = 0;

    switch (prec) {
    case PREC_SINGLEPREC:
    default:
        result = WWRunCrossfeedF(coeffPath, fromPath, toPath);
        break;
    case PREC_DOUBLEPREC:
        result = WWRunCrossfeedD(coeffPath, fromPath, toPath);
        break;
    }

    return result;
}