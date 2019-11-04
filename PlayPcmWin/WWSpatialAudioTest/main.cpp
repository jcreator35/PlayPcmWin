// 日本語
#include <stdio.h>
#include "WWSpatialAudioUser.h"
#include "WWSpatialAudioHrtfUser.h"
#include <stdlib.h>
#include <iostream>
#include <string>
#include "WWUtil.h"
#include <math.h>

using namespace std;

static float *
PrepareSound(int bytes)
{
    assert((bytes & 3) == 0);

    int elemCount = bytes / 4;
    float * r = new float[elemCount];
    for (int i = 0; i < elemCount; ++i) {
        r[i] = ((float)rand() / RAND_MAX) *2.0f - 1.0f;
    }

    return r;
}

static int
Run(void)
{
    HRESULT hr = S_OK;
    WWSpatialAudioUser sa;
    WWDynAudioObject das;
    const int soundSec = 8;
    const int nBufBytes = soundSec * 48000 * sizeof(float);
    
    sa.Init();

    sa.DoDeviceEnumeration();

    for (int i = 0; i < sa.GetDeviceCount(); ++i) {
        wchar_t s[256];
        memset(s, 0, sizeof s);
        sa.GetDeviceName(i, s, sizeof s -2);
        printf("%d: %S\n", i, s);
    }

    printf("Enter device number: ");
    char devNrStr[256];
    memset(devNrStr, 0, sizeof devNrStr);
    cin.getline(devNrStr, sizeof devNrStr - 1);

    int devNr = atoi(devNrStr);
    HRG(sa.ChooseDevice(devNr));
    HRG(sa.ActivateAudioStream(32));
    

    das.buffer = (BYTE*)PrepareSound(nBufBytes);
    das.bufferBytes = nBufBytes;
    das.SetPos3D(10.0f, 0, -10.0f);
    das.volume = 1.0f;

    HRG(sa.AddStream(das));

    for (int i=0; i<(soundSec+2) * 10; ++i) {
        printf("Playing %d %d\n", sa.PlayStreamCount(), i);
        Sleep(100);

        float theta = 2 * 3.141592f * i / (soundSec * 10);
        float x = cos(theta);
        float y = 0;
        float z = -sin(theta);
        float volume = 1.0f;
        sa.SetPosVolume(das.idx, x, y, z, volume);
    }

end:
    sa.DeactivateAudioStream();
    sa.Term();
    return hr;
}

static int
RunHrtf(void)
{
    HRESULT hr = S_OK;
    WWSpatialAudioHrtfUser sa;
    WWDynAudioHrtfObject dyn;
    const int soundSec = 8;
    const int nBufBytes = soundSec * 48000 * sizeof(float);

    sa.Init();

    sa.DoDeviceEnumeration();

    for (int i = 0; i < sa.GetDeviceCount(); ++i) {
        wchar_t s[256];
        memset(s, 0, sizeof s);
        sa.GetDeviceName(i, s, sizeof s - 2);
        printf("%d: %S\n", i, s);
    }

    printf("Enter device number: ");
    char devNrStr[256];
    memset(devNrStr, 0, sizeof devNrStr);
    cin.getline(devNrStr, sizeof devNrStr - 1);

    int devNr = atoi(devNrStr);
    HRG(sa.ChooseDevice(devNr));
    HRG(sa.ActivateAudioStream(32));


    dyn.buffer = (BYTE*)PrepareSound(nBufBytes);
    dyn.bufferBytes = nBufBytes;
    dyn.SetPos3D(10.0f, 0, -10.0f);
    dyn.volume = 1.0f;

    HRG(sa.AddStream(dyn));

    for (int i = 0; i < (soundSec + 2) * 10; ++i) {
        printf("Playing %d %d\n", sa.PlayStreamCount(), i);
        Sleep(100);

        float theta = 2 * 3.141592f * i / (soundSec * 10);
        float x = cos(theta);
        float y = 0;
        float z = -sin(theta);
        float volume = 1.0f;
        sa.SetPosVolume(dyn.idx, x, y, z, volume);
    }

end:
    sa.DeactivateAudioStream();
    sa.Term();
    return hr;
}

int
main(void)
{
    // _CrtSetBreakAlloc(35);
    // COM leak cannot be detected by debug heap manager ...
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

#if 1
    RunHrtf();
#else
    Run();
#endif
    return 0;
}

