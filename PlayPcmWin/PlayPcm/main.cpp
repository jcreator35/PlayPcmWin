#include "WasapiWrap.h"
#include "WWUtil.h"
#include "WWWavReader.h"
#include "WWDsfReader.h"
#include "WWDsdiffReader.h"
#include "WWPrivilegeControl.h"

#include <stdio.h>
#include <Windows.h>
#include <stdlib.h>
#include <crtdbg.h>
#include <assert.h>

#define LATENCY_MILLISEC_DEFAULT (100)
#define READ_LINE_BYTES          (256)

static void
PrintUsage(void)
{
    printf(
        "PlayPcm version 1.0.6\n"
        "Usage:\n"
        "    PlayPcm\n"
        "        Print this message and enumerate all available devices\n"
        "\n"
        "    PlayPcm -d deviceId\n"
        "        Test specified device\n"
        "\n"
        "    PlayPcm -d deviceId [-l latencyInMillisec] [-uselargememory] input_pcm_file_name\n"
        "        Play pcm file on deviceId device\n"
        "        Example:\n"
        "            PlayPcm -d 1 C:\\audio\\music.wav\n"
        "            PlayPcm -d 1 C:\\audio\\music.dsf\n"
        "            PlayPcm -d 1 C:\\audio\\music.dff\n"
        );
}

static HRESULT
GetIntValueFromConsole(const char *prompt, int from, int to, int *value_return)
{
    *value_return = 0;

    printf("%s (%d to %d) ? ", prompt, from, to);
    fflush(stdout);

    char s[READ_LINE_BYTES];
    char *result = fgets(s, READ_LINE_BYTES-1, stdin);
    if (nullptr == result) {
        return E_INVALIDARG;
    }

    char *p = nullptr;
    errno = 0;
    int v = (int)strtol(s, &p, 10);
    if (errno != 0 || p == s) {
        printf("E: malformed input...\n");
        return E_INVALIDARG;
    }
    if (v < from || to < v) {
        printf("E: value is out of range.\n");
        return E_INVALIDARG;
    }

    *value_return = v;

    return S_OK;
}

static HRESULT
PrintDeviceList(void)
{
    HRESULT hr;
    WasapiWrap ww;

    HRR(ww.Init());
    HRG(ww.DoDeviceEnumeration());

    printf("Device list:\n");
    for (int i=0; i<ww.GetDeviceCount(); ++i) {
        wchar_t namew[WW_DEVICE_NAME_COUNT];
        ww.GetDeviceName(i, namew, sizeof namew);

        char    namec[WW_DEVICE_NAME_COUNT];
        memset(namec, 0, sizeof namec);
        WideCharToMultiByte(CP_ACP, 0, namew, -1, namec, sizeof namec-1, nullptr, nullptr);
        printf("    deviceId=%d: %s\n", i, namec);
    }
    printf("\n");

end:
    ww.Term();
    return hr;
}

struct Settings {
    int deviceId;
    int latencyMillisec;
    const char *path;
    WWPcmDataStreamAllocType allocType;

    Settings(void) : deviceId(-1), latencyMillisec(LATENCY_MILLISEC_DEFAULT), path(nullptr), allocType(WWPDSA_Normal) {
    }
};

static HRESULT
Run(const Settings &settings, WWPcmData &pcm)
{
    HRESULT hr;
    WasapiWrap ww;

    HRR(ww.Init());
    HRG(ww.DoDeviceEnumeration());
    HRG(ww.ChooseDevice(settings.deviceId));

    WWSetupArg setupArg;
    setupArg.Set(pcm.bitsPerSample,pcm.validBitsPerSample, pcm.nSamplesPerSec, pcm.nChannels, settings.latencyMillisec);
    HRG(ww.Setup(setupArg));
    ww.SetOutputData(pcm);
    ww.Start();

    while (!ww.Run(1000)) {
        printf("%d / %d\n", ww.GetPosFrame(), ww.GetTotalFrameNum());
    }
    hr = S_OK;

end:
    ww.Stop();
    ww.Unsetup();
    ww.Term();
    return hr;
}

static HRESULT
Test(int deviceId)
{
    HRESULT hr;
    WasapiWrap ww;

    HRR(ww.Init());
    HRG(ww.DoDeviceEnumeration());
    HRG(ww.ChooseDevice(deviceId));

    ww.PrintMixFormat();
    WWInspectArg inspectArg;

    inspectArg.Set(16, 16, 44100, 2);
    ww.Inspect(inspectArg);

    inspectArg.Set(16, 16, 48000, 2);
    ww.Inspect(inspectArg);

    inspectArg.Set(24, 24, 176400, 2);
    ww.Inspect(inspectArg);

    inspectArg.Set(32, 24, 176400, 2);
    ww.Inspect(inspectArg);

end:
    ww.Unsetup();
    ww.Term();
    return hr;
}

static WWBitsPerSampleType
InspectDeviceBitsPerSample(int deviceId)
{
    HRESULT hr;
    WWBitsPerSampleType deviceBitsPerSample = WWBpsNone;
    WasapiWrap ww;

    HRG(ww.Init());
    HRG(ww.DoDeviceEnumeration());
    HRG(ww.ChooseDevice(deviceId));

    WWInspectArg inspectArg;

    inspectArg.Set(32, 24, 176400, 2);
    if (SUCCEEDED(ww.Inspect(inspectArg))) {
        deviceBitsPerSample = WWBps32v24;
    }

    inspectArg.Set(24, 24, 176400, 2);
    if (SUCCEEDED(ww.Inspect(inspectArg))) {
        deviceBitsPerSample = WWBps24;
    }

end:
    ww.Unsetup();
    ww.Term();
    return deviceBitsPerSample;
}

enum CommandlineOptionType {
    COT_OTHER = -1,
    COT_DEVICE,
    COT_LATENCY,
    COT_LARGEMEM,

    COT_NUM
};

const char *gCommandLineStrArray[] = {
    "-d",
    "-l",
    "-uselargememory",
};

static CommandlineOptionType
StringToCommandlineOptionType(const char *s)
{
    for (int i=0; i<COT_NUM; ++i) {
        if (0 == strcmp(s, gCommandLineStrArray[i])) {
            return (CommandlineOptionType)i;
        }
    }
    return COT_OTHER;
}

/// @return false: not need to continue program, true: continue program execution
static bool
ParseCommandline(int argc, char *argv[], Settings &settings_return)
{
    for (int i=1; i<argc; ++i) {
        CommandlineOptionType cot = StringToCommandlineOptionType(argv[i]);
        switch (cot) {
        case COT_OTHER:
            if (i != argc-1 || settings_return.deviceId < 0) {
                // filepath must be the last argument
                PrintUsage();
                PrintDeviceList();
                return false;
            }
            settings_return.path = argv[i];
            return true;
        case COT_DEVICE:
            settings_return.deviceId = atoi(argv[i+1]);
            ++i;
            break;
        case COT_LATENCY:
            settings_return.latencyMillisec = atoi(argv[i+1]);
            ++i;
            break;
        case COT_LARGEMEM:
            settings_return.allocType = WWPDSA_LargeMemory;
            break;
        default:
            assert(false);
            break;
        }
    }

    if (0 <= settings_return.deviceId && settings_return.path == nullptr) {
        Test(settings_return.deviceId);
        return false;
    }

    // error
    PrintUsage();
    PrintDeviceList();
    return false;
}

int
main(int argc, char *argv[])
{
    WWPcmData *pcmData = nullptr;
    Settings settings;
    WWBitsPerSampleType bitsPerSampleType = WWBpsNone;
    WWPrivilegeControl pc;

    if (!ParseCommandline(argc, argv, settings)) {
        return 0;
    }

    if (settings.allocType == WWPDSA_LargeMemory) {
        if (!pc.Init()) {
            printf("Error: WWPrivilegeControl::Init()\n");
            goto end;
        }
        if (!pc.SetPrivilege(TEXT("SeLockMemoryPrivilege"), TRUE)) {
            printf("Error: Failed to acquire SeLockMemoryPrivilege. You need to assign <Lock pages in memory> privilege to your account first using secpol.msc and logoff/logon\n");
            goto end;
        }
        printf("use MEM_LARGE_PAGES. page size = %d bytes\n", (int)GetLargePageMinimum());
    }

    bitsPerSampleType = InspectDeviceBitsPerSample(settings.deviceId);

    settings.path = argv[argc-1];
    pcmData = WWReadWavFile(settings.path, settings.allocType);
    if (nullptr == pcmData) {
        pcmData = WWReadDsfFile(settings.path, bitsPerSampleType, settings.allocType);
        if (nullptr == pcmData) {
            pcmData = WWReadDsdiffFile(settings.path, bitsPerSampleType, settings.allocType);
            if (nullptr == pcmData) {
                printf("E: read file failed %s\n", settings.path);
                goto end;
            }
        }
    }

    switch (settings.allocType) {
    case WWPDSA_Normal:
        printf("using normal memory...##############################\n");
        break;
    case WWPDSA_LargeMemory:
        printf("using large memory...###############################\n");
        break;
    default:
        assert(false);
        break;
    }

    HRESULT hr = Run(settings, *pcmData);
    if (FAILED(hr)) {
        printf("E: Run failed (%08x)\n", hr);
    }

    if (nullptr != pcmData) {
        pcmData->Term();
        delete pcmData;
        pcmData = nullptr;
    }

end:
    pc.Term();

#ifdef _DEBUG
    _CrtDumpMemoryLeaks();
#endif
    return 0;
}

