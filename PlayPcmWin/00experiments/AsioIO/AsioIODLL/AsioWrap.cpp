// AsioWrap.cpp Yamamoto Software Lab.
// ASIO is a trademark and software of Steinberg Media Technologies GmbH.

#include "targetver.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "AsioWrap.h"

#include "asiosys.h"
#include "asio.h"
#include "AsioIOIF.h"
#include <assert.h>
#include <stdio.h>

#define ASIOWRAP_CLOCKSOURCE_NUM (32)
#define ASIOWRAP_OUTPUT_CHANNEL_NUM (256)

static HANDLE s_hEvent;

struct WavData {
    bool Use(void) {
        return !end;
    }

    int samples;
    int pos; /**< next data index to read/write */
    int channelIdx; /* buffer index */
    bool repeat;
    bool end;
    int *data;

    ASIOSampleType sampleType;

    unsigned char *asioBuffers[2];

    void Clear(void);
    void SetInput32(int index, int dataCount);
    void SetInput16(int index, int dataCount);
    void SetOutput32(int index, int dataCount);
    void SetOutput16(int index, int dataCount);

    void AsioBufferDisposed(void) {
        asioBuffers[0] = 0;
        asioBuffers[1] = 0;
    }
};

void WavData::Clear(void)
{
    delete[] data;
    data = NULL;
    
    samples = 0;
    pos = 0;
    channelIdx = -1;
    repeat = false;
    end = true;
}

void WavData::SetInput32(int index, int dataCount)
{
    const int *pInputData = 
        (int*)asioBuffers[index];
    if (data) {
        int count = dataCount;
        if (samples < pos + dataCount) {
            //printf("WavData::SetInput32 last data has come.\n");
            count = samples - pos;
            end = true;
        }

        if (count != 0) {
            memcpy(&data[pos], pInputData, count * 4);
            pos += count;
        }
    } else {
        end = true;
    }
}

void WavData::SetInput16(int index, int dataCount)
{
    const short *pInputData =
        (short*)asioBuffers[index];

    //printf("WavData::SetInput16 dataCount=%d bufferRemains=%d\n", dataCount, samples - pos);

    if (data) {
        int count = dataCount;
        if (samples < pos + dataCount) {
            //printf("WavData::SetInput16 last data has come.\n");
            count = samples - pos;
            end = true;
        }

        if (0 < count) {
            for (int i=0; i<count; ++i) {
                data[pos+i] = pInputData[i]<<16;
            }
            pos += count;
        }
    } else {
        end = true;
    }
}

void WavData::SetOutput32(int index, int dataCount)
{
    int *pOutputData =
        (int*)asioBuffers[index];

    if (!data) {
        end = true;
    }

    if (end) {
        if (pOutputData) {
            memset(pOutputData, 0, dataCount*4);
        }
        return;
    }

    if (repeat) {
        // data is ring buffer. never ends
        int writePos = 0;
        while (0 < dataCount) {
            int count = dataCount;
            if (samples < pos + dataCount) {
                count = samples - pos;
                memcpy(&pOutputData[writePos], &data[pos], count*4);
                pos = 0;
            } else {
                memcpy(&pOutputData[writePos], &data[pos], count*4);
                pos += count;
                if (samples == pos) {
                    pos = 0;
                }
            }
            writePos += count;
            dataCount -= count;
        }
    } else {

        // not repeat.
        int count = dataCount;

        if (samples < pos + dataCount) {
            // pos + dataCount == endpos
            // data is insufficient;
            count = samples - pos;
            end = true;
        } 

        if (0 < count) {
            memcpy(pOutputData, &data[pos], count*4);
            pos += count;
        }

        if (count < dataCount) {
            for (int i=count; i<dataCount; ++i) {
                pOutputData[i] = 0;
            }
        }
    }
}

void WavData::SetOutput16(int index, int dataCount)
{
    short *pOutputData =
        (short*)asioBuffers[index];

    //printf("WavData::SetOutput16 dataCount=%d bufferRemains=%d\n", dataCount, samples - pos);

    if (!data) {
        end = true;
    }

    if (end) {
        if (pOutputData) {
            memset(pOutputData, 0, dataCount*2);
        }
        return;
    }

    if (repeat) {
        // data is ring buffer. never ends
        int writePos = 0;
        while (0 < dataCount) {
            int count = dataCount;
            if (samples < pos + dataCount) {
                count = samples - pos;
                for (int i=0; i<count; ++i) {
                    pOutputData[i+writePos] = data[pos+i]>>16;
                }
                pos = 0;
            } else {
                for (int i=0; i<count; ++i) {
                    pOutputData[i+writePos] = data[pos+i]>>16;
                }
                pos += count;
                if (samples == pos) {
                    pos = 0;
                }
            }
            writePos += count;
            dataCount -= count;
        }
    } else {
        // not repeat.
        int count = dataCount;

        if (samples < pos + dataCount) {
            //printf("WavData::SetOutput16 last data has come.\n");
            // data is insufficient
            count = samples - pos;
            end = true;
        }
        if (0 < count) {
            for (int i=0; i<count; ++i) {
                pOutputData[i] = data[pos+i]>>16;
            }
            pos += count;
        }
        if (count < dataCount) {
            for (int i=count; i<dataCount; ++i) {
                pOutputData[i] = 0;
            }
        }
    }
}

static WavData s_outWavArray[ASIOWRAP_OUTPUT_CHANNEL_NUM];
static WavData s_inWav;
static int g_useOutWavArray;

static void
asioBufferDisposed(void)
{
    s_inWav.AsioBufferDisposed();
    for (int i=0; i<ASIOWRAP_OUTPUT_CHANNEL_NUM; ++i) {
        s_outWavArray[i].AsioBufferDisposed();
    }
}

static int
getAsioDriverNum(void)
{
    return (int)AsioDrvGetNumDev();
}

static bool
getAsioDriverName(int n, char *name_return, int size)
{
    assert(name_return);

    name_return[0] = 0;

    if (AsioDrvGetNumDev() <= n) {
        return false;
    }

    AsioDrvGetDriverName(n, name_return, size);
    return true;
}

/*
static bool
loadAsioDriver(int n)
{
    char name[64];
    name[0] = 0;
    AsioDrvGetDriverName(n, name, 32);

    return AsioDrvLoadDriver(name);
}

static void
unloadAsioDriver(void)
{
    AsioDrvRemoveCurrentDriver();
}
*/

#if NATIVE_INT64
    #define ASIO64toDouble(a)  (a)
#else
    const double twoRaisedTo32 = 4294967296.;
    #define ASIO64toDouble(a) ((a).lo + (a).hi * twoRaisedTo32)
#endif

extern "C" __declspec(dllexport) double __stdcall
AsioTimeStampToDouble(ASIOTimeStamp &a)
{
    return ASIO64toDouble(a);
}

extern "C" __declspec(dllexport) double __stdcall
AsioSamplesToDouble(ASIOSamples &a)
{
    return ASIO64toDouble(a);
}

struct AsioPropertyInfo {
    ASIODriverInfo adi;
    long inputChannels;   /**< デバイスの入力チャンネル総数。*/
    long outputChannels;  /**< デバイスの出力チャンネル総数。*/

    int useInputChannelNum;  /**< 使用する入力チャンネル総数 */
    int useOutputChannelNum; /**< 使用する出力チャンネル総数 */

    long minSize;
    long maxSize;
    long preferredSize;
    long bufferSize;

    long granularity;
    ASIOSampleRate sampleRate; /**< input param: 96000 or 44100 or whatever */

    bool postOutput;

    ASIOTime tInfo;
    ASIOBufferInfo  *bufferInfos;
    ASIOChannelInfo *channelInfos;

    long inputLatency;
    long outputLatency;

    double nanoSeconds;
    double samples;
    double tcSamples;
    long  sysRefTime;

    ASIOClockSource clockSources[ASIOWRAP_CLOCKSOURCE_NUM];
    long numOfClockSources;
};

static AsioPropertyInfo *
asioPropertyInstance(void)
{
    static AsioPropertyInfo ap;
    return &ap;
}

//----------------------------------------------------------------------------------
// ASIO callbacks

ASIOTime *
bufferSwitchTimeInfo(ASIOTime *timeInfo, long index, ASIOBool processNow)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    ap->tInfo = *timeInfo;

    if (timeInfo->timeInfo.flags & kSystemTimeValid) {
        ap->nanoSeconds =
            AsioTimeStampToDouble(timeInfo->timeInfo.systemTime);
    } else {
        ap->nanoSeconds = 0;
    }

    if (timeInfo->timeInfo.flags & kSamplePositionValid) {
        ap->samples =
            AsioSamplesToDouble(timeInfo->timeInfo.samplePosition);
    } else {
        ap->samples = 0;
    }

    if (timeInfo->timeCode.flags & kTcValid) {
        ap->tcSamples =
            AsioSamplesToDouble(timeInfo->timeCode.timeCodeSamples);
    } else {
        ap->tcSamples = 0;
    }

    ap->sysRefTime = GetTickCount();

    long buffSize = ap->bufferSize;

    if (s_inWav.Use()) {
        switch (s_inWav.sampleType) {
        case ASIOSTInt16LSB:
            // Realtek ASIO
            s_inWav.SetInput16(index, buffSize);
            break;
        case ASIOSTInt32LSB:
            // M-AUDIO ASIO
            // Creative ASIO
            s_inWav.SetInput32(index, buffSize);
            break;
        default:
            printf("input sampleType=%d\n", 
                s_inWav.sampleType);
            assert(0);
            break;
        }
    }

    for (int i=0; i<ap->useOutputChannelNum; ++i) {
        WavData &wd = s_outWavArray[i];

        if (wd.Use()) {
            switch (wd.sampleType) {
            case ASIOSTInt16LSB:
                wd.SetOutput16(index, buffSize);
                break;
            case ASIOSTInt32LSB:
                wd.SetOutput32(index, buffSize);
                break;
            default:
                printf("output %d sampleType=%d\n", i, wd.sampleType);
                assert(0);
                break;
            }
        }
    }

    if (ap->postOutput) {
        ASIOOutputReady();
    }

    if (!s_inWav.end) {
        return 0;
    }
    for (int i=0; i<ap->useOutputChannelNum; ++i) {
        if (!s_outWavArray[i].end) {
            return 0;
        }
    }

    printf("\nbufferSwitch in/out data end. SetEvent\n");
    if (s_hEvent) {
        SetEvent(s_hEvent);
    }

    return 0;
}

static void
bufferSwitch(long index, ASIOBool processNow)
{
    ASIOTime  timeInfo;
    memset (&timeInfo, 0, sizeof (timeInfo));

    if(ASIOGetSamplePosition(&timeInfo.timeInfo.samplePosition,
        &timeInfo.timeInfo.systemTime) == ASE_OK) {
        timeInfo.timeInfo.flags = kSystemTimeValid | kSamplePositionValid;
    }

    bufferSwitchTimeInfo (&timeInfo, index, processNow);
}

static void
sampleRateChanged(ASIOSampleRate sRate)
{
    printf("sampleRateChanged(%f)\n", sRate);
}

static long
asioMessages(long selector, long value, void* message, double* opt)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    
    long ret = 0;
    switch(selector) {
    case kAsioSelectorSupported:
        if(value == kAsioResetRequest
        || value == kAsioEngineVersion
        || value == kAsioResyncRequest
        || value == kAsioLatenciesChanged
        )
            ret = 1L;
        break;
    case kAsioResetRequest:
        SetEvent(s_hEvent);
        ret = 1L;
        break;
    case kAsioResyncRequest:
        ret = 1L;
        break;
    case kAsioLatenciesChanged:
        ret = 1L;
        break;
    case kAsioEngineVersion:
        ret = 2L;
        break;
    case kAsioSupportsTimeInfo:
        ret = 1;
        break;
    case kAsioSupportsTimeCode:
        ret = 0;
        break;
    }
    return ret;
}

//----------------------------------------------------------------------------------
// AsioWrap APIs

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_init(void)
{
    printf(__FUNCTION__"()\n");

    AsioDrvInit();
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_term(void)
{
    printf(__FUNCTION__"()\n");

    AsioDrvTerm();
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_getDriverNum(void)
{
    return getAsioDriverNum();
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_getDriverName(int n, char *name_return, int size)
{
    return getAsioDriverName(n, name_return, size);
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_loadDriver(int n)
{
    char name[64];
    getAsioDriverName(n, name, sizeof name-1);
    return AsioDrvLoadDriver(name);
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_unloadDriver(void)
{
    AsioDrvRemoveCurrentDriver();
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_setup(int sampleRate)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    ASIOError rv;

    s_inWav.Clear();
    for (int ch=0; ch<ASIOWRAP_OUTPUT_CHANNEL_NUM; ++ch) {
        s_outWavArray[ch].Clear();
    }

    ap->sampleRate = sampleRate;

    memset(&ap->adi, 0, sizeof ap->adi);
    rv = ASIOInit(&ap->adi);
    if (ASE_OK != rv) {
        printf ("ASIOGetChannels() err %d\n", rv);
        return rv;
    }
    printf ("ASIOInit()\n"
        "  asioVersion:   %d\n"
        "  driverVersion: %d\n"
        "  Name:          %s\n"
        "  ErrorMessage:  %s\n",
        ap->adi.asioVersion, ap->adi.driverVersion,
        ap->adi.name, ap->adi.errorMessage);

    rv = ASIOGetChannels(&ap->inputChannels, &ap->outputChannels);
    if (ASE_OK != rv) {
        printf ("ASIOGetChannels() err %d\n", rv);
        return rv;
    }
    printf ("ASIOGetChannels() inputs=%d outputs=%d\n",
        ap->inputChannels, ap->outputChannels);

    int totalChannels = ap->inputChannels + ap->outputChannels;

    assert(!ap->bufferInfos);
    ap->bufferInfos  = new ASIOBufferInfo[totalChannels];

    assert(!ap->channelInfos);
    ap->channelInfos = new ASIOChannelInfo[totalChannels];

    rv = ASIOGetBufferSize(&ap->minSize, &ap->maxSize,
        &ap->preferredSize, &ap->granularity);
    if (ASE_OK != rv) {
        printf ("ASIOGetBufferSize err %d\n", rv);
        return rv;
    }
    printf ("ASIOGetBufferSize() min=%d max=%d preferred=%d granularity=%d\n",
             ap->minSize, ap->maxSize,
             ap->preferredSize, ap->granularity);
    ap->bufferSize = ap->maxSize;

    rv = ASIOCanSampleRate(ap->sampleRate);
    if (ASE_OK != rv) {
        printf ("ASIOCanSampleRate(sampleRate=%f) failed %d\n",
            ap->sampleRate, rv);
        return rv;
    }

    rv = ASIOSetSampleRate(ap->sampleRate);
    if (ASE_OK != rv) {
        printf ("ASIOSetSampleRate(sampleRate=%f) failed %d\n",
            ap->sampleRate, rv);
        return rv;
    }
    printf ("ASIOSetSampleRate(sampleRate=%f)\n", ap->sampleRate);

    ap->postOutput = true;
    rv = ASIOOutputReady();
    if (ASE_OK != rv) {
        ap->postOutput = false;
    }
    printf ("ASIOOutputReady() %s\n",
        ap->postOutput ? "Supported" : "Not supported");

    for (int i=0; i<totalChannels; i++) {
        if (i < ap->inputChannels) {
            ap->channelInfos[i].channel = i;
            ap->channelInfos[i].isInput = true;
        } else {
            ap->channelInfos[i].channel = i-ap->inputChannels;
            ap->channelInfos[i].isInput = false;
        }

        rv = ASIOGetChannelInfo(&ap->channelInfos[i]);
        if (ASE_OK != rv) {
            printf ("ASIOGetChannelInfo() failed %d\n", rv);
            return rv;
        }
        printf("i=%2d ch=%2d isInput=%d chGroup=%08x type=%2d name=%s\n",
            i,
            ap->channelInfos[i].channel,
            ap->channelInfos[i].isInput,
            ap->channelInfos[i].channelGroup,
            ap->channelInfos[i].type,
            ap->channelInfos[i].name);
    }

    rv = ASIOGetLatencies(&ap->inputLatency, &ap->outputLatency);
    if (ASE_OK != rv) {
        printf ("ASIOGetLatencies() failed %d\n", rv);
        return rv;
    }
    printf ("ASIOGetLatencies() input=%d output=%d\n",
        ap->inputLatency, ap->outputLatency);

    ap->numOfClockSources = ASIOWRAP_CLOCKSOURCE_NUM;
    rv = ASIOGetClockSources(ap->clockSources, &ap->numOfClockSources);
    printf ("ASIOGetClockSources() result=%d numOfClockSources=%d\n",
        rv, ap->numOfClockSources);

    ASIOClockSource *cs = ap->clockSources;
    for (int i=0; i<ap->numOfClockSources; ++i) {
        printf (" idx=%d assocCh=%d assocGrp=%d current=%d name=%s\n",
            cs->index, cs->associatedChannel, cs->associatedGroup, cs->isCurrentSource, cs->name);
    }

    return ASE_OK;
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_unsetup(void)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    ASIOExit();
    printf("ASIOExit()\n");

    s_inWav.Clear();
    for (int i=0; i<ASIOWRAP_OUTPUT_CHANNEL_NUM; ++i) {
        s_outWavArray[i].Clear();
    }
    g_useOutWavArray = 0;

    assert(ap->bufferInfos);
    delete[] ap->bufferInfos;  ap->bufferInfos = 0;

    assert(ap->channelInfos);
    delete[] ap->channelInfos; ap->channelInfos = 0;
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_getInputChannelsNum(void)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    return ap->inputChannels;
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_getOutputChannelsNum(void)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    return ap->outputChannels;
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_getInputChannelName(int n, char *name_return, int size)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    if (n < 0 || ap->inputChannels <= n) {
        assert(0);
    }

    memcpy_s(name_return, size,
        ap->channelInfos[n].name, sizeof ap->channelInfos[0].name);
    return true;
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_getOutputChannelName(int n, char *name_return, int size)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    if (n < 0 || ap->outputChannels <= n) {
        assert(0);
    }

    memcpy_s(name_return, size,
        ap->channelInfos[n + ap->inputChannels].name,
        sizeof ap->channelInfos[0].name);
    return true;
}


extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_setOutput(int ch, int *data, int samples, bool repeat)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    WavData &wd = s_outWavArray[g_useOutWavArray];

    assert(0 <= ch && ch < ASIOWRAP_OUTPUT_CHANNEL_NUM);

    delete[] wd.data;
    wd.data = NULL;

    wd.data = new int[samples];
    memcpy(wd.data, data, samples * 4);
    wd.samples = samples;
    wd.pos = 0;
    wd.channelIdx = ch;
    wd.repeat = repeat;
    wd.end = false;
    wd.sampleType = ap->channelInfos[ap->inputChannels + ch].type;

    ++g_useOutWavArray;

    printf("AsioWrap_setOutput %d useOut=%d\n", ch, g_useOutWavArray);
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_setInput(int inputChannel, int samples)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    delete[] s_inWav.data;
    s_inWav.data = NULL;

    if (samples == 0) {
        s_inWav.samples = 0;
        s_inWav.pos = 0;
        s_inWav.channelIdx = inputChannel;
        s_inWav.end = true;
        s_inWav.sampleType = 0;
    } else {
        s_inWav.data = new int[samples];
        s_inWav.samples = samples;
        s_inWav.pos = 0;
        s_inWav.channelIdx = inputChannel;
        s_inWav.end = false;
        s_inWav.sampleType = ap->channelInfos[inputChannel].type;
    }
    printf("AsioWrap_setInput %d use=%d samples=%d\n", inputChannel, s_inWav.Use(), samples);
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_getRecordedData(int inputChannel, int recordedData_return[], int samples)
{
    assert(s_inWav.data);
    memcpy_s(recordedData_return, samples *4, s_inWav.data, s_inWav.pos *4);
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_start(void)
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    ASIOError rv;

    /* 使用するチャンネルだけASIOバッファを確保する */
    ap->useInputChannelNum = 0;
    ap->useOutputChannelNum = 0;
    ASIOBufferInfo *info = ap->bufferInfos;
    if (s_inWav.Use()) {
        info->isInput = ASIOTrue;
        info->channelNum = s_inWav.channelIdx;
        info->buffers[0] = 0;
        info->buffers[1] = 0;

        printf("input ch=%d \n", info->channelNum);

        ++info;
        ++ap->useInputChannelNum;
    }
    for (int i=0; i<g_useOutWavArray; ++i) {
        WavData &wd = s_outWavArray[i];
        if (wd.Use()) {
            info->isInput = ASIOFalse;
            info->channelNum = wd.channelIdx;
            info->buffers[0] = 0;
            info->buffers[1] = 0;

            printf("output ch=%d \n", info->channelNum);

            ++info;
            ++ap->useOutputChannelNum;
        }
    }

    static ASIOCallbacks asioCallbacks;
    asioCallbacks.bufferSwitch         = &bufferSwitch;
    asioCallbacks.sampleRateDidChange  = &sampleRateChanged;
    asioCallbacks.asioMessage          = &asioMessages;
    asioCallbacks.bufferSwitchTimeInfo = &bufferSwitchTimeInfo;

    printf("calling ASIOCreateBuffers(in=%d out=%d bufsz=%d)\n",
        ap->useInputChannelNum, ap->useOutputChannelNum, ap->bufferSize);
    rv = ASIOCreateBuffers(ap->bufferInfos,
        ap->useInputChannelNum + ap->useOutputChannelNum,
        ap->bufferSize, &asioCallbacks);
    if (ASE_OK != rv) {
        printf ("ASIOCreateBuffers() failed %d\n", rv);
        return rv;
    }
    printf ("ASIOCreateBuffers() success.\n");

    /* ASIOバッファができたので、ポインタをWavInfoに入れる */
    info = ap->bufferInfos;
    if (s_inWav.Use()) {
        WavData &wd = s_inWav;
        wd.asioBuffers[0] = (unsigned char *)info->buffers[0];
        wd.asioBuffers[1] = (unsigned char *)info->buffers[1];

        printf("input Use=%d ch=%d asioBuffer[0]=%p asioBuffer[1]=%p pos=%d samples=%d repeat=%d sampleType=%d data=%p\n",
            wd.Use(), wd.channelIdx, wd.asioBuffers[0], wd.asioBuffers[1], wd.pos, wd.samples, wd.repeat, wd.sampleType, wd.data);

        ++info;
    }
    for (int i=0; i<ap->useOutputChannelNum; ++i) {
        WavData &wd = s_outWavArray[i];
        wd.asioBuffers[0] = (unsigned char *)info->buffers[0];
        wd.asioBuffers[1] = (unsigned char *)info->buffers[1];

        printf("output Use=%d ch=%d asioBuffer[0]=%p asioBuffer[1]=%p pos=%d samples=%d repeat=%d sampleType=%d data=%p\n",
            wd.Use(), wd.channelIdx, wd.asioBuffers[0], wd.asioBuffers[1], wd.pos, wd.samples, wd.repeat, wd.sampleType, wd.data);

        // clear ASIO output buffer
        switch (wd.sampleType) {
        case ASIOSTInt32LSB:
            memset(wd.asioBuffers[0], 0, ap->bufferSize*4);
            memset(wd.asioBuffers[1], 0, ap->bufferSize*4);
            break;
        case ASIOSTInt16LSB:
            memset(wd.asioBuffers[0], 0, ap->bufferSize*2);
            memset(wd.asioBuffers[1], 0, ap->bufferSize*2);
            break;
        default:
            assert(0);
            break;
        }

        ++info;
    }

    assert(!s_hEvent);
    s_hEvent = CreateEvent(NULL, FALSE, FALSE, "AsioWrap");
    printf("\nAsioWrap_start CreateEvent()\n");

    rv = ASIOStart();
    if (rv == ASE_OK) {
        printf("ASIOStart() success.\n\n");
    } else {
        printf("ASIOStart() failed %d\n", rv);
    }
    return rv;
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_run(void)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    assert(s_hEvent);

    printf("AsioWrap_run() WaitForSingleObject() start\n");
    DWORD rv = WaitForSingleObject(s_hEvent, 10000);
    printf("AsioWrap_run() WaitForSingleObject() %x\n", rv);
    if (rv == WAIT_TIMEOUT) {
        return false;
    }

    ASIOError ae;
    ae = ASIOStop();
    printf("ASIOStop() result=%d\n", ae);
    if (rv != ASE_OK) {
        printf("ASIOStop() %d ERROR!!!!!!!!!!!!!!!!!!!!!!!!\n", ae);
    }

    ASIODisposeBuffers();
    printf("ASIODisposeBuffers()\n");

    asioBufferDisposed();

    /* 出力チャンネル情報をクリアする。
     * 入力チャンネル情報はクリアしない。
     */
    for (int ch=0; ch<ASIOWRAP_OUTPUT_CHANNEL_NUM; ++ch) {
        s_outWavArray[ch].Clear();
    }
    g_useOutWavArray = 0;

    CloseHandle(s_hEvent);
    s_hEvent = NULL;
    printf("CloseHandle()\n");
    return true;
}

extern "C" __declspec(dllexport)
void __stdcall
AsioWrap_stop(void)
{
    printf("AsioWrap_stop()\n");
    if (s_hEvent) {
        printf("AsioWrap_stop calling SetEvent()\n");
        SetEvent(s_hEvent);
    }
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_controlPanel(void)
{
    return (int)ASIOControlPanel();
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_getClockSourceNum()
{
    AsioPropertyInfo *ap = asioPropertyInstance();
    return ap->numOfClockSources;
}

extern "C" __declspec(dllexport)
bool __stdcall
AsioWrap_getClockSourceName(int n, char *name_return, int size)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    assert(0 <= n && n < ap->numOfClockSources);

    ASIOClockSource *cs = ap->clockSources;

    sprintf(name_return, "%s id=%d Ac=%d Ag=%d",
        cs[n].name, cs[n].index, cs[n].associatedChannel, cs[n].associatedGroup);
    return true;
}

extern "C" __declspec(dllexport)
int __stdcall
AsioWrap_setClockSource(int idx)
{
    AsioPropertyInfo *ap = asioPropertyInstance();

    assert(0 <= idx && idx < ap->numOfClockSources);

    ASIOClockSource *cs = ap->clockSources;

    printf("AsioWrap_setClockSource(%d) csIdx=%d\n",
        idx, cs[idx].index);

    return (int)ASIOSetClockSource(cs[idx].index);
}
