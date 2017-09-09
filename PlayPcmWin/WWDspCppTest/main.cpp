#include <stdio.h>
#include "WWSdmToPcm.h"
#include "WWPcmToSdm.h"
#include <crtdbg.h>
#include <math.h>
#include <stdint.h>

struct Wav {
    uint8_t wavHeader[0x28];
    uint32_t pcmBytes;
    uint8_t *pcm;

    Wav(void) : pcmBytes(0), pcm(nullptr) {
    }

    ~Wav(void) {
        delete [] pcm;
        pcm = nullptr;
    }
};

static bool
ReadWav(const char *path, Wav &wav_r)
{
    wav_r.pcm = nullptr;
    FILE *fp = nullptr;

    errno_t rv = fopen_s(&fp, path, "rb");
    if (rv != 0) {
        printf("failed to read %s\n", path);
        return false;
    }

    fread(wav_r.wavHeader, 0x28, 1, fp);

    fread(&wav_r.pcmBytes, 4, 1, fp);

    wav_r.pcm = new uint8_t[wav_r.pcmBytes];
    if (!wav_r.pcm) {
        printf("memory exhausted\n");
        return false;
    }

    fread(wav_r.pcm, wav_r.pcmBytes, 1, fp);

    fclose(fp);
    return true;
}

static bool
WriteWav(const char *path, const Wav &wav_w, const uint8_t *pcm)
{

    FILE *fp = nullptr;
    errno_t rv = fopen_s(&fp, path, "wb");
    if (rv != 0) {
        printf("failed to write %s\n", path);
        return false;
    }

    fwrite(wav_w.wavHeader, 0x28, 1, fp);

    fwrite(&wav_w.pcmBytes, 4, 1, fp);

    fwrite(pcm, wav_w.pcmBytes, 1, fp);

    fclose(fp);
    return true;
}

static float
ConvertToFloat(uint8_t *p)
{
    int vI = 
        (p[0]<<8) +
        (p[1]<<16) +
        (p[2]<<24);

    float vF = (float)vI / 2147483648.0f;

    return vF;
}

static void
ConvertToInt24(const float vF, uint8_t *pTo)
{
    int vI = (int)(vF * 8388608.0f);

    pTo[0] = 0xff & (vI>>0);
    pTo[1] = 0xff & (vI>>8);
    pTo[2] = 0xff & (vI>>16);
}

static bool
Run(void)
{
    WWPcmToSdm ps[2];
    WWSdmToPcm sp[2];

    Wav wavIn;
    if (!ReadWav("C:/audio/TestSignal4424.wav", wavIn)) {
        printf("ReadWav failed\n");
        return false;
    }

    int totalPcmSamples = wavIn.pcmBytes / 6;

    for (int ch = 0; ch<2; ++ch) {
        ps[ch].Start(totalPcmSamples * 64);
        sp[ch].Start(totalPcmSamples);
    }

    float vMax = 0;
    float vMin = 0;
    int64_t pos = 0;
    for (int64_t i = 0; i<totalPcmSamples; ++i) {
        for (int ch = 0; ch<2; ++ch) {
            float v = ConvertToFloat(&wavIn.pcm[pos]);
            if (vMax < v) {
                vMax = v;
            }
            if (v < vMin) {
                vMin = v;
            }

            v *= 0.5f;

            ps[ch].AddInputSamples(&v, 1);
            pos += 3;
        }
    }

    printf("vMax=%f, vMin=%f\n", vMax, vMin);

    for (int ch = 0; ch<2; ++ch) {
        ps[ch].Drain();
    }

    for (int ch = 0; ch<2; ++ch) {
        const uint16_t *sdm = ps[ch].GetOutputSdm();
        for (int64_t i = 0; i<totalPcmSamples*64/16; ++i) {
            sp[ch].AddInputSamples(*sdm);
            ++sdm;
        }
    }

    for (int ch = 0; ch<2; ++ch) {
        ps[ch].End();
        sp[ch].Drain();
    }

    uint8_t *pcmOut = new uint8_t[wavIn.pcmBytes];
    if (!pcmOut) {
        printf("Memory exhausted\n");
        return false;
    }

    vMax = 0;
    vMin = 0;

    pos = 0;
    for (int64_t i = 0; i<totalPcmSamples; ++i) {
        for (int ch = 0; ch<2; ++ch) {
            const float *p = sp[ch].GetOutputPcm();
            float v = p[i];
            if (vMax < v) {
                vMax = v;
            }
            if (v < vMin) {
                vMin = v;
            }

            if (2147483647.0f / 2147483648.0f < v) {
                v = 2147483647.0f / 2147483648.0f;
            }
            if (v < -1.0f) {
                v = -1.0f;
            }

            ConvertToInt24(v, &pcmOut[pos]);
            pos += 3;
        }
    }

    printf("vMax=%f, vMin=%f\n", vMax, vMin);

    for (int ch = 0; ch<2; ++ch) {
        sp[ch].End();
    }

    return WriteWav("C:/audio/output.wav", wavIn, pcmOut);
}

int main(void)
{
    Run();

#ifndef NDEBUG
    _CrtDumpMemoryLeaks();
#endif
    return 0;
}
