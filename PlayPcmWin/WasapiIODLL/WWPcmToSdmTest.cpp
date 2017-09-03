// 日本語

#include "WWCicDownsampler.h"
#include "WWHalfbandFilterDownsampler.h"
#include "WWSdmToPcm.h"
#include "WWCicUpsampler.h"
#include "WWHalfbandFilterUpsampler.h"
#include "WWPcmToSdm.h"

#define _USE_MATH_DEFINES
#include <math.h>

int
WWPcmToSdmTest(void)
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
#if 0
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
#if 0
    WWCicUpsampler cu;

    int inputFr = 44100;
    int outputFr = 44100 * 16;

    float scale = 0.999969482421875f;

    printf("Frequency(Hz),CIC4_16xUpsampler\n");

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (1000)
        float inPcm[IN_COUNT];
        float outPcm[IN_COUNT*16];

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos((float)omega);
            inPcm[i] = vIn;

            cu.Filter(inPcm[i], &outPcm[i*16]);

            if (3 <= i) {
                accIn += abs(vIn);
            }

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        double accOut = 0;
        for (int i=3*16; i<IN_COUNT*16; ++i) {
            accOut += abs(outPcm[i]);
        }

        printf("%d, %f\n", freq,
            20.0*log10(accOut/accIn/16));
    }
#endif
#if 0
    WWHalfbandFilterUpsampler hb(47);

    const int inputFr = 44100;
    const int upsampleScale = 2;
    const int outputFr = inputFr * upsampleScale;

    float scale = 0.999969482421875f;

    printf("Frequency(Hz),HBUpsampler\n");

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (1000)
        float inPcm[IN_COUNT];
        float outPcm[IN_COUNT*2];

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos((float)omega);
            inPcm[i] = vIn;

            hb.Filter(&inPcm[i], 1, &outPcm[i*upsampleScale]);

            if (hb.FilterDelay()/2 <= i) {
                accIn += abs(vIn);
            }

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        double accOut = 0;
        for (int i=hb.FilterDelay(); i<IN_COUNT*upsampleScale; ++i) {
            accOut += abs(outPcm[i]);
        }

        printf("%d, %f\n", freq,
            20.0*log10(accOut/accIn/upsampleScale));
    }
#endif

#if 0
    WWPcmToSdm ps;

    const int inputFr = 44100;
    const int upsampleScale = 64;
    const int outputFr = inputFr * upsampleScale;

    float scale = 0.5;

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (1000)
        float inPcm[IN_COUNT];

        ps.Start(IN_COUNT*64);

        double omega = 0;
        double accIn = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos((float)omega);
            inPcm[i] = vIn;

            ps.AddInputSamples(inPcm, 1);

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        ps.Drain();
        ps.End();
    }
#endif

#if 1
    WWPcmToSdm ps;
    WWSdmToPcm sp;

    const int inputFr = 44100;
    //const int upsampleScale = 64;
    //const int outputFr = inputFr * upsampleScale;

    float scale = 0.5;

    for (int freq=0; freq < inputFr/2; freq += inputFr/2000) {
        double omegaDelta = (double)freq / inputFr * 2.0 * M_PI;

#define IN_COUNT (1000)
        float inPcm[IN_COUNT];

        ps.Start(IN_COUNT*64);
        sp.Start(IN_COUNT);

        double omega = 0;
        for (int i=0; i<IN_COUNT; ++i) {
            float vIn = scale * cos((float)omega);
            inPcm[i] = vIn;

            ps.AddInputSamples(inPcm, 1);

            omega += omegaDelta;
            if (1.0 * M_PI <= omega) {
                omega -= 2.0 * M_PI;
            }
        }

        ps.Drain();

        const uint16_t *sdm = ps.GetOutputSdm();
        for (int i=0; i<IN_COUNT*64/16; ++i) {
            sp.AddInputSamples(sdm[i]);
        }

        sp.Drain();

        //const float *pcm = sp.GetOutputPcm();

        sp.End();

        ps.End();
    }
#endif

    return 0;
}
