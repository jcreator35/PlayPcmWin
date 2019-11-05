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

#define TEST_SA
//#define TEST_SA_HRTF

// dynamic moving speaker (or static speaker)
#define TEST_DYNAMIC

static const int MAX_DYN_STREAM = 16;

// Creates full scale white noise
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

#ifdef TEST_SA
static int
Run(void)
{
    HRESULT hr = S_OK;
    WWSpatialAudioUser sa;
    WWDynAudioObject dyn;
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

#ifdef TEST_DYNAMIC
    int staticObjectTypeMask = AudioObjectType_None;
#else
    int staticObjectTypeMask = (AudioObjectType)(AudioObjectType_FrontLeft | AudioObjectType_FrontRight);
#endif
    HRG(sa.ChooseDevice(devNr, MAX_DYN_STREAM, staticObjectTypeMask));
    
    dyn.buffer = (BYTE*)PrepareSound(nBufBytes);
    dyn.bufferBytes = nBufBytes;
    dyn.volume = 1.0f;
#ifdef TEST_DYNAMIC
    dyn.aot = AudioObjectType_Dynamic;
    dyn.SetPos3D(1.0f, 0, 0.0f);
#else
    dyn.aot = AudioObjectType_FrontLeft;
#endif

    HRG(sa.AddStream(dyn));

    HRG(sa.Start());

    for (int i=0; i<(soundSec+2) * 10; ++i) {
        printf("Playing %d %d\n", sa.PlayStreamCount(), i);
        Sleep(100); // wait 0.1 sec

#ifdef TEST_DYNAMIC
        float theta = 2 * 3.141592f * i / (soundSec * 10);
        float x = cos(theta);
        float y = 0;
        float z = -sin(theta);
        float volume = 1.0f;
        sa.SetPosVolume(dyn.idx, x, y, z, volume);
#endif
    }

    sa.Stop();

end:
    sa.Term();
    return hr;
}
#endif

#ifdef TEST_SA_HRTF
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
    HRG(sa.ChooseDevice(devNr, MAX_DYN_STREAM, AudioObjectType_None));

    dyn.buffer = (BYTE*)PrepareSound(nBufBytes);
    dyn.bufferBytes = nBufBytes;
    dyn.SetPos3D(1.0f, 0, 0.0f);
    dyn.volume = 1.0f;

    HRG(sa.AddStream(dyn));

    HRG(sa.Start());

    for (int i = 0; i < (soundSec + 2) * 10; ++i) {
        printf("Playing %d %d\n", sa.PlayStreamCount(), i);
        Sleep(100); // wait 0.1 sec

        float theta = 2 * 3.141592f * i / (soundSec * 10);
        float x = cos(theta);
        float y = 0;
        float z = -sin(theta);
        float volume = 1.0f;
        sa.SetPosVolume(dyn.idx, x, y, z, volume);
    }

    sa.Stop();

end:
    sa.Term();
    return hr;
}
#endif

int
main(void)
{
    // _CrtSetBreakAlloc(35);
    // COM leak cannot be detected by debug heap manager ...
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

#ifdef TEST_SA
    Run();
#endif

#ifdef TEST_SA_HRTF
    RunHrtf();
#endif

    return 0;
}

