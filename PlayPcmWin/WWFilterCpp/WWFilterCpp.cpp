#include "stdafx.h"
#include "WWFilterCpp.h"
#include "WWLoopFilterCRFB.h"
#include "WWZohCompensation.h"
#include "WWIIRFilterParallel.h"
#include "WWIIRFilterSerial.h"
#include "WWCicDownsampler.h"
#include "WWHalfbandFilterDownsampler.h"
#include "WWSdmToPcm.h"
#include <map>
#define _USE_MATH_DEFINES
#include <math.h>

static int gNextIdx = 1;

#define ADD_NEW_INSTANCE(p, m)        \
    int idx = gNextIdx++;             \
    m.insert(std::make_pair(idx, p)); \
    return idx;

#define DESTROY(idx, m)   \
    auto r = m.find(idx); \
    if (r == m.end()) {   \
        return;           \
    }                     \
    auto *p = r->second;  \
    delete p;             \
    m.erase(r);

#define FIND(idx, m)      \
    auto r = m.find(idx); \
    if (r == m.end()) {   \
        return -1;        \
    }                     \
    auto *p = r->second;

// Crfb ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWLoopFilterCRFB* > gIdxFilterMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Build(int order, const double *a, const double *b, const double *g, double gain)
{
    auto *p = new WWLoopFilterCRFB(order, a, b, g, gain);
    ADD_NEW_INSTANCE(p,gIdxFilterMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_Crfb_Destroy(int idx)
{
    DESTROY(idx, gIdxFilterMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Crfb_Filter(int idx, int n, const double *buffIn, unsigned char *buffOut)
{
    FIND(idx, gIdxFilterMap);

    p->Filter(n, buffIn, (unsigned char*)buffOut);

    return n;
}

// Zoh Compensation ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWZohCompensation* > gIdxZohCompensationMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Build(void)
{
    auto *p = new WWZohCompensation();
    ADD_NEW_INSTANCE(p, gIdxZohCompensationMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_ZohCompensation_Destroy(int idx)
{
    DESTROY(idx, gIdxZohCompensationMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_ZohCompensation_Filter(int idx, int n, const double *buffIn, double *buffOut)
{
    FIND(idx, gIdxZohCompensationMap);

    p->Filter(n, buffIn, buffOut);

    return n;
}

// IIR Filter ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

static std::map<int, WWIIRFilterSerial* > gIdxIIRSerialMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Build(int nBlocks)
{
    auto *p = new WWIIRFilterSerial(nBlocks);
    ADD_NEW_INSTANCE(p, gIdxIIRSerialMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRSerial_Destroy(int idx)
{
    DESTROY(idx, gIdxIIRSerialMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Add(int idx, int aCount, const double *a, int bCount, const double *b)
{
    FIND(idx, gIdxIIRSerialMap);

    p->Add(aCount, a, bCount, b);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut)
{
    FIND(idx, gIdxIIRSerialMap);

    p->Filter(nIn, buffIn, nOut, buffOut);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRSerial_SetParam(int idx, int osr, int decimation)
{
    FIND(idx, gIdxIIRSerialMap);

    p->SetOSR(osr);
    p->SetDecimation(decimation);

    return 0;
}


static std::map<int, WWIIRFilterParallel* > gIdxIIRParallelMap;

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Build(int nBlocks)
{
    auto *p = new WWIIRFilterParallel(nBlocks);
    ADD_NEW_INSTANCE(p, gIdxIIRParallelMap);
}

extern "C" WWFILTERCPP_API
void __stdcall
WWFilterCpp_IIRParallel_Destroy(int idx)
{
    DESTROY(idx, gIdxIIRParallelMap);
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Add(int idx, int aCount, const double *a, int bCount, const double *b)
{
    FIND(idx, gIdxIIRParallelMap);

    p->Add(aCount, a, bCount, b);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_Filter(int idx, int nIn, const double *buffIn, int nOut, double *buffOut)
{
    FIND(idx, gIdxIIRParallelMap);

    p->Filter(nIn, buffIn, nOut, buffOut);
    return 0;
}

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_IIRParallel_SetParam(int idx, int osr, int decimation)
{
    FIND(idx, gIdxIIRParallelMap);

    p->SetOSR(osr);
    p->SetDecimation(decimation);

    return 0;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

extern "C" WWFILTERCPP_API
int __stdcall
WWFilterCpp_Test(void)
{
#if 0
    // CICフィルターのテスト。
    // ディレイは2サンプル。
    WWCicDownsampler cic;

    int inputFr = 44100 * 64;
    int outputFr = 44100 * 16;

    float scale = 0.999969482421875f;

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omega = 0;
        double omega1 = (double)freq / inputFr * 2.0 * M_PI;
        float inPcm[16];
        double accIn = 0;
        double accOut = 0;

        for (int i=0; i<1000*16; ++i) {
            float vIn = scale * cos(omega);
            inPcm[i%16] = vIn;

            if (16*2 <= i) {
                accIn += abs(vIn);
            }
            if (15==(i%16)) {
                float outPcm = cic.Process(inPcm);
                //printf("%d, %f\n", i/16, outPcm);

                if (2 <= i) {
                    accOut += abs(outPcm);
                }
            }

            omega += omega1;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        printf("%d, %f\n", freq, 20.0*log10(16*accOut/accIn));
    }
#endif
#if 0
    // Halfbandフィルターのテスト。
    WWHalfbandFilterDownsampler hbf11(11);
    WWHalfbandFilterDownsampler hbf23(23);
    WWHalfbandFilterDownsampler hbf47(47);
    WWHalfbandFilterDownsampler hbf95(95);

    int inputFr = 44100 * 2;
    int outputFr = 44100 * 1;

    float scale = 0.999969482421875f;

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (256)
        float inPcm[IN_COUNT];

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos(omega);
            inPcm[i] = vIn;
            accIn += abs(vIn);

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        float outPcm[IN_COUNT/2];
        double accOut11 = 0;
        double accOut23 = 0;
        double accOut47 = 0;
        double accOut95 = 0;

        hbf11.Start();
        hbf11.Filter(inPcm, IN_COUNT, outPcm);
        hbf11.End();
        for (int i=0; i<IN_COUNT/2; ++i) {
            accOut11 += abs(outPcm[i]);
        }

        hbf23.Start();
        hbf23.Filter(inPcm, IN_COUNT, outPcm);
        hbf23.End();
        for (int i=0; i<IN_COUNT/2; ++i) {
            accOut23 += abs(outPcm[i]);
        }

        hbf47.Start();
        hbf47.Filter(inPcm, IN_COUNT, outPcm);
        hbf47.End();
        for (int i=0; i<IN_COUNT/2; ++i) {
            accOut47 += abs(outPcm[i]);
        }

        hbf95.Start();
        hbf95.Filter(inPcm, IN_COUNT, outPcm);
        hbf95.End();
        for (int i=0; i<IN_COUNT/2; ++i) {
            accOut95 += abs(outPcm[i]);
        }

        printf("%d, %f, %f, %f, %f\n", freq,
            20.0*log10(2*accOut11/accIn),
            20.0*log10(2*accOut23/accIn),
            20.0*log10(2*accOut47/accIn),
            20.0*log10(2*accOut95/accIn));
    }
#endif
#if 0
    // Halfbandフィルター2連のテスト。
    WWHalfbandFilterDownsampler hbf23(23);
    WWHalfbandFilterDownsampler hbf47(47);

    int inputFr = 44100 * 4;
    int outputFr = 44100 * 1;

    float scale = 0.999969482421875f;

    printf("Frequency(Hz),HB23-HB23, HB23-HB47, HB47-HB47\n");

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (256)
        float inPcm[IN_COUNT];
        float tmpPcm[IN_COUNT/2];

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos(omega);
            inPcm[i] = vIn;
            accIn += abs(vIn);

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        float outPcm[IN_COUNT/4];
        double accOut23_23 = 0;
        double accOut23_47 = 0;
        double accOut47_47 = 0;

        // 11サンプルのディレイ。
        hbf23.Start();
        hbf23.Filter(inPcm, IN_COUNT, tmpPcm);
        hbf23.End();
        hbf23.Start();
        hbf23.Filter(tmpPcm, IN_COUNT/2, outPcm);
        hbf23.End();
        for (int i=11; i<IN_COUNT/4; ++i) {
            accOut23_23 += abs(outPcm[i]);
        }

        // 23サンプルのディレイ。
        hbf23.Start();
        hbf23.Filter(inPcm, IN_COUNT, tmpPcm);
        hbf23.End();
        hbf47.Start();
        hbf47.Filter(tmpPcm, IN_COUNT/2, outPcm);
        hbf47.End();
        for (int i=23; i<IN_COUNT/4; ++i) {
            accOut23_47 += abs(outPcm[i]);
        }

        // 26サンプルのディレイ。
        hbf47.Start();
        hbf47.Filter(inPcm, IN_COUNT, tmpPcm);
        hbf47.End();
        hbf47.Start();
        hbf47.Filter(tmpPcm, IN_COUNT/2, outPcm);
        hbf47.End();
        for (int i=26; i<IN_COUNT/4; ++i) {
            accOut47_47 += abs(outPcm[i]);
        }

        printf("%d, %f, %f, %f\n", freq,
            20.0*log10(2*accOut23_23/accIn),
            20.0*log10(2*accOut23_47/accIn),
            20.0*log10(2*accOut47_47/accIn));
    }
#endif
#if 0

    WWCicDownsampler cic;

    // Halfbandフィルター。
    WWHalfbandFilterDownsampler hbf23(23);
    WWHalfbandFilterDownsampler hbf47(47);

    int inputFr = 44100 * 64;
    int outputFr = 44100 * 1;

    float scale = 0.999969482421875f;

    printf("Frequency(Hz),CIC4-HB23-HB47\n");

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (1000*64)
        float inPcm[IN_COUNT];
        float tmpPcm[IN_COUNT/16];
        float tmp2Pcm[IN_COUNT/32];
        float outPcm[IN_COUNT/64];

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos(omega);
            inPcm[i] = vIn;

            if (((i&15)==15)) {
                // (inPcm→tmpPcm 出力から見て)ディレイは3サンプル。16xダウンサンプル。
                tmpPcm[i/16] = cic.Filter(&inPcm[i-15]);
            }

            if (16*25 <= i) {
                accIn += abs(vIn);
            }

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        hbf23.Start();
        hbf23.Filter(tmpPcm, IN_COUNT/16, tmp2Pcm);
        hbf23.End();
        hbf47.Start();
        hbf47.Filter(tmp2Pcm, IN_COUNT/32, outPcm);
        hbf47.End();

        // (inPcm→outPcm 出力から見て)25サンプルのディレイ。4xダウンサンプル。

        double accOut = 0;
        for (int i=25; i<IN_COUNT/64; ++i) {
            accOut += abs(outPcm[i]);
        }

        printf("%d, %f\n", freq,
            20.0*log10(64*accOut/accIn));
    }
#endif
#if 1
    WWSdmToPcm sdmpcm;
    int outSamples = 44100*10;
    sdmpcm.Start(outSamples);
    for (int i=0; i<outSamples*64/16; ++i) {
        sdmpcm.AddInputSamples(0x6969);
    }
    sdmpcm.Drain();
    const float *buf = sdmpcm.GetOutputPcm();
    sdmpcm.End();

#endif
    return 0;
}

