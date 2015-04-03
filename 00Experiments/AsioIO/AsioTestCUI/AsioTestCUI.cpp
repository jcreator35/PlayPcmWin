// AsioTestCUI.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "AsioWrap.h"
#include "asio.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>

static int
GetAnswerNumber(int from, int to)
{
    while (true) {
        printf("(%d ... %d) ? ", from, to);
        fflush(stdout);

        char answer[256];
        memset(answer, 0, sizeof answer);
        fgets(answer, sizeof answer-1, stdin);

        char *end = NULL;
        int n = strtol(answer, &end, 10);
        if (end == 0 || end == answer) {
            continue;
        }

        if (from <= n && n <= to) {
            return n;
        }
    }
}

#define PI (3.14159265358979)

static int *
CreateSineWave(int sampleRate, int hz, int seconds)
{
    int * data = new int[sampleRate * seconds];

    for (int i=0; i<sampleRate * seconds; ++i) {
        data[i] = INT_MAX * sin( 2.0 * PI * hz * i / sampleRate);
    }
    return data;
}

int main(int argc, char *argv[])
{
    AsioWrap_init();
    
    int driverNum = AsioWrap_getDriverNum();
    for (int i=0; i<driverNum; ++i) {
        char s[256];
        AsioWrap_getDriverName(i, s, sizeof s);

        printf("%d %s\n", i, s);
    }
#if 0
    int driverId = GetAnswerNumber(0, driverNum-1);
#else
    int driverId = 1;
#endif
    bool bRv = AsioWrap_loadDriver(driverId);

    printf("load driver result: %s\n", bRv ? "success" : "fail");
    if (!bRv) {
        AsioWrap_term();
        return 0;
    }

#if 0
    printf("44100 44100Hz\n");
    printf("88200 88200Hz\n");
    printf("96000 96000Hz\n");
    printf("192000 192000Hz\n");
    int sampleRate = GetAnswerNumber(44100, 192000);
#else
    int sampleRate = 96000;
#endif

    int rv = AsioWrap_setup(sampleRate);
    if (rv == 0) {
        printf("sample rate set: success\n");
    } else {
        printf("sample rate set: failed %d\n", rv);
        AsioWrap_unloadDriver();
        AsioWrap_term();
        return 0;
    }

#if 0
    printf("440 440Hz sine wave\n");
    printf("1000 1000Hz sine wave\n");
    int hz = GetAnswerNumber(1, 1000*1000);
#else
    int hz = 1056;
#endif

    int seconds = 3;
    int * pcm = CreateSineWave(sampleRate, hz, seconds);

    AsioWrap_setOutput(1, pcm, 96000, false);
    AsioWrap_setOutput(0, pcm, 96000, false);

    rv = AsioWrap_start();
    if (rv == 0) {
        printf("start: success\n");
    } else {
        printf("start: failed %d\n", rv);
        AsioWrap_unsetup();
        AsioWrap_unloadDriver();
        AsioWrap_term();
        return 0;
    }

    while (!AsioWrap_run()) {
        printf("...");
    }

    printf("play finished.\n");

    printf("unsetup...\n");
    AsioWrap_unsetup();

    printf("unload driver...\n");
    AsioWrap_unloadDriver();

    printf("term...\n");
    AsioWrap_term();

    delete[] pcm; pcm = 0;

	return 0;
}

