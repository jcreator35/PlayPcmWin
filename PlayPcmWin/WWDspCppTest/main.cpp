#include <stdio.h>
#include "WWSdmToPcm.h"
#include "WWPcmToSdm.h"
#include "WWHalfbandFilter.h"
#include <crtdbg.h>
#include <math.h>
#include <stdint.h>

#pragma pack(2)
struct WavHeader {
    char riff[4];
    uint32_t riffChunkSize;
    char wave[4];
    char fmt[4];
    uint32_t fmtChunkSize;
    uint16_t audioFormat;
    uint16_t numCh;
    uint32_t sampleRate;
    uint32_t byteRate;
    uint16_t blockAlign;
    uint16_t bitsPerSample;
    char data[4];
    uint32_t dataChunkSize;
};
#pragma pack()

struct Wav {
    WavHeader header;
    uint8_t *pcm;

    Wav(void) : pcm(nullptr) {
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

    fread(&wav_r.header, 0x2c, 1, fp);

    wav_r.pcm = new uint8_t[wav_r.header.dataChunkSize];
    if (!wav_r.pcm) {
        printf("memory exhausted\n");
        return false;
    }

    fread(wav_r.pcm, wav_r.header.dataChunkSize, 1, fp);

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

    fwrite(&wav_w.header, 0x2c, 1, fp);

    fwrite(pcm, wav_w.header.dataChunkSize, 1, fp);

    fclose(fp);
    return true;
}

static float
ConvertInt32ToFloat(const uint8_t *p)
{
    const int32_t vI = (int32_t)(
        (((uint32_t)p[0])<<0) +
        (((uint32_t)p[1])<<8) +
        (((uint32_t)p[2])<<16) +
        (((uint32_t)p[3])<<24));

    const float vF = (float)vI / 2147483648.0f;

    return vF;
}

static float
ConvertInt24ToFloat(const uint8_t *p)
{
    const int32_t vI = 
        (p[0]<<8) +
        (p[1]<<16) +
        (p[2]<<24);

    const float vF = (float)vI / 2147483648.0f;

    return vF;
}

static float
ConvertInt16ToFloat(const uint8_t *p)
{
    const int32_t vI = 
        (p[0]<<16) +
        (p[1]<<24);

    const float vF = (float)vI / 2147483648.0f;

    return vF;
}

static void
ConvertFloatToInt32(const float vF, uint8_t *pTo)
{
    const int32_t vI = (int32_t)(vF * 2147483648.0f);

    pTo[0] = 0xff & (vI>>0);
    pTo[1] = 0xff & (vI>>8);
    pTo[2] = 0xff & (vI>>16);
    pTo[3] = 0xff & (vI>>24);
}

static void
ConvertFloatToInt24(const float vF, uint8_t *pTo)
{
    const int32_t vI = (int32_t)(vF * 8388608.0f);

    pTo[0] = 0xff & (vI>>0);
    pTo[1] = 0xff & (vI>>8);
    pTo[2] = 0xff & (vI>>16);
}

static void
ConvertFloatToInt16(const float vF, uint8_t *pTo)
{
    int32_t vI = (int32_t)(vF * 32768.0f);

    pTo[0] = 0xff & (vI>>0);
    pTo[1] = 0xff & (vI>>8);
}

struct Upsampler
{
    Upsampler(void) : hu47(47) { }

    void Start(void) {
        hu47.Start();
        //hu23.Start();
    }

    void End(void) {
        hu47.End();
        //hu23.End();
    }

    void Filter(const float inPcm, float *outPcm) {
        //float tmp1[2];
        hu47.Filter(&inPcm, 1, outPcm);
        //hu23.Filter(tmp1, 2, outPcm);
    }

    WWHalfbandFilterUpsampler hu47;
    //WWHalfbandFilterUpsampler hu23;
};

struct Downsampler
{
    Downsampler(void) : ds47(47) { }

    void Start(void) {
        ds47.Start();
        //ds23.Start();
    }

    void End(void) {
        ds47.End();
        //ds23.End();
    }

    void Filter(const float * inPcm, int inPcmCount, float *outPcm) {
#if 0
        assert(inPcmCount == 4);
        float tmp1[2];
        ds47.Filter(inPcm, 4, outPcm);
        ds23.Filter(tmp1, 2, outPcm);
#else
        ds47.Filter(inPcm, 2, outPcm);
#endif
    }

    WWHalfbandFilterDownsampler ds47;
    //WWHalfbandFilterDownsampler ds23;
};

#if 0
static bool
RunUpsampleTest(void)
{
    bool rv;
    Upsampler   us[2];
    Downsampler ds[2];

    WWHalfbandFilter hbL(255), hbR(255);
    WWHalfbandFilter *hb[2] = {&hbL, &hbR};

    Wav wavIn;
    if (!ReadWav("C:/audio/FLATSWEEP/FLATSWEEP_032768.WAV", wavIn)) {
        printf("ReadWav failed\n");
        return false;
    }

    const int upsampleScale = 2;

    const int CH = wavIn.header.numCh;
    const int bytesPerSample = ((wavIn.header.bitsPerSample+7)/8);
    const int bytesPerFrame = bytesPerSample * CH;
    const int inPcmSamples = wavIn.header.dataChunkSize / bytesPerFrame;
    const int outPcmSamples = inPcmSamples * upsampleScale;
    const int audioFormat = wavIn.header.audioFormat;

    for (int ch = 0; ch<CH; ++ch) {
        us[ch].Start();
        ds[ch].Start();
        hb[ch]->Start();
    }

    uint8_t **pcmOutByCh = new uint8_t*[CH];
    for (int ch=0; ch<CH; ++ch) {
        pcmOutByCh[ch] = new uint8_t[bytesPerSample * outPcmSamples];
        if (!pcmOutByCh[ch]) {
            printf("Memory exhausted\n");
            return false;
        }
    }

    int64_t posR = 0;
    int64_t *posWByCh = new int64_t[CH];
    memset(posWByCh, 0, sizeof(int64_t)*CH);

    for (int64_t i = 0; i<inPcmSamples; ++i) {
        for (int ch = 0; ch<CH; ++ch) {
            float v = 0;
            if (audioFormat == 0x3) {
                const float *pF = (const float*)&wavIn.pcm[posR];
                v = *pF;
            } else {
                switch (wavIn.header.bitsPerSample) {
                case 32:
                    v = ConvertInt32ToFloat(&wavIn.pcm[posR]);
                    break;
                case 24:
                    v = ConvertInt24ToFloat(&wavIn.pcm[posR]);
                    break;
                case 16:
                    v = ConvertInt16ToFloat(&wavIn.pcm[posR]);
                    break;
                default:
                    assert(0);
                    break;
                }
            }
            posR += bytesPerSample;

            float tmpPcm[4];
            float vOut = 0;

            //v = 1.0f;
            v *= 0.5f;

#if 0
            hb[ch]->Filter(&v, 1, &vOut);
            {
#endif
#if 1
            us[ch].Filter(v, tmpPcm);
            //ds[ch].Filter(tmpPcm,2,&vOut);
            for (int o=0;o<upsampleScale;++o) {
                vOut = tmpPcm[o];
#endif
#if 0
            tmpPcm[0] = 1.0f;
            tmpPcm[1] = 1.0f;
            ds[ch].Filter(tmpPcm,2,&vOut);
            {
#endif

                if (8388607.0f / 8388608.0f < vOut) {
                    vOut = 8388607.0f / 8388608.0f;
                }
                if (vOut < -1.0f) {
                    vOut = -1.0f;
                }

                // printf("%f\n", vOut);

                if (audioFormat == 0x3) {
                    float *pF = (float*)&pcmOutByCh[ch][posWByCh[ch]];
                    *pF = vOut;
                } else {
                    switch (wavIn.header.bitsPerSample) {
                    case 32:
                        ConvertFloatToInt32(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 24:
                        ConvertFloatToInt24(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 16:
                        ConvertFloatToInt16(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    default:
                        assert(0);
                        break;
                    }
                }
                posWByCh[ch] += bytesPerSample;

                /*
                printf("i=%lld ch=%d o=%d posR=%lld posWByCh[ch]=%lld vOut=%f\n",
                    i, ch, o, posR, posWByCh[ch], vOut);
                */
            }
        }
    }

    delete [] posWByCh;
    posWByCh = nullptr;

    for (int ch = 0; ch<CH; ++ch) {
        hb[ch]->End();
        ds[ch].End();
        us[ch].End();
    }

    uint8_t * pcmOut = new uint8_t[2*wavIn.header.dataChunkSize];
    if (!pcmOut) {
        printf("Memory exhausted\n");
        return false;
    }

    int64_t *posRByCh = new int64_t[CH];
    memset(posRByCh, 0, sizeof(int64_t)*CH);
    int64_t posW = 0;
    for (int64_t i = 0; i<outPcmSamples; ++i) {
        for (int ch = 0; ch<CH; ++ch) {
            for (int b=0; b<bytesPerSample;++b) {
                pcmOut[posW++] = pcmOutByCh[ch][posRByCh[ch]];
                ++posRByCh[ch];
            }
        }
    }

    for (int ch=0; ch<CH; ++ch) {
        delete [] pcmOutByCh[ch];
        pcmOutByCh[ch] = nullptr;
    }
    delete [] pcmOutByCh;
    pcmOutByCh = nullptr;

    wavIn.header.byteRate *= upsampleScale;
    wavIn.header.sampleRate *= upsampleScale;
    wavIn.header.dataChunkSize *= upsampleScale;
    wavIn.header.riffChunkSize = wavIn.header.dataChunkSize + 0x24;
    rv = WriteWav("C:/audio/outputHS.wav", wavIn, pcmOut);

    delete [] pcmOut;
    pcmOut = nullptr;

    return rv;
}
#endif

#if 0
static bool
RunDownsampleTest(void)
{
    bool rv;
    Downsampler ds[2];
    WWHalfbandFilter hbL(7);
    WWHalfbandFilter hbR(7);
    WWHalfbandFilter *hb[] = {&hbL, &hbR};

    Wav wavIn;
    if (!ReadWav("C:/audio/FLATSWEEP/FLATSWEEP_032768.WAV", wavIn)) {
        printf("ReadWav failed\n");
        return false;
    }

    const int downsampleScale = 2;

    const int CH = wavIn.header.numCh;
    const int bytesPerSample = ((wavIn.header.bitsPerSample+7)/8);
    const int bytesPerFrame = bytesPerSample * CH;
    const int inPcmSamples = wavIn.header.dataChunkSize / bytesPerFrame;
    const int outPcmSamples = inPcmSamples / downsampleScale;
    const int audioFormat = wavIn.header.audioFormat;

    for (int ch = 0; ch<CH; ++ch) {
        ds[ch].Start();
        hb[ch]->Start();
    }

    uint8_t **pcmOutByCh = new uint8_t*[CH];
    float **tmpPcmByCh = new float*[CH];
    for (int ch=0; ch<CH; ++ch) {
        pcmOutByCh[ch] = new uint8_t[bytesPerSample * outPcmSamples];
        if (!pcmOutByCh[ch]) {
            printf("Memory exhausted\n");
            return false;
        }

        tmpPcmByCh[ch] = new float[downsampleScale];
    }

    int64_t posR = 0;
    int64_t *posWByCh = new int64_t[CH];
    memset(posWByCh, 0, sizeof(int64_t)*CH);

    for (int64_t i = 0; i<inPcmSamples; ++i) {
        for (int ch = 0; ch<CH; ++ch) {
            float v = 0;
            if (audioFormat == 0x3) {
                const float *pF = (const float*)&wavIn.pcm[posR];
                v = *pF;
            } else {
                switch (wavIn.header.bitsPerSample) {
                case 32:
                    v = ConvertInt32ToFloat(&wavIn.pcm[posR]);
                    break;
                case 24:
                    v = ConvertInt24ToFloat(&wavIn.pcm[posR]);
                    break;
                case 16:
                    v = ConvertInt16ToFloat(&wavIn.pcm[posR]);
                    break;
                default:
                    assert(0);
                    break;
                }
            }
            posR += bytesPerSample;

            //v = 1.0f;
            v *= 0.5f;
#if 0
            float vOut = 0;
            hb[ch]->Filter(&v, 1, &vOut);

            if (0 == (i%downsampleScale)) {
                if (8388607.0f / 8388608.0f < vOut) {
                    vOut = 8388607.0f / 8388608.0f;
                }
                if (vOut < -1.0f) {
                    vOut = -1.0f;
                }

                if (audioFormat == 0x3) {
                    float *pF = (float*)&pcmOutByCh[ch][posWByCh[ch]];
                    *pF = vOut;
                } else {
                    switch (wavIn.header.bitsPerSample) {
                    case 32:
                        ConvertFloatToInt32(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 24:
                        ConvertFloatToInt24(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 16:
                        ConvertFloatToInt16(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    default:
                        assert(0);
                        break;
                    }
                }
                posWByCh[ch] += bytesPerSample;
            }
#else
            tmpPcmByCh[ch][i%downsampleScale] = v;

            if (downsampleScale-1 == (i%downsampleScale)) {
                float vOut = 0;
                ds[ch].Filter(tmpPcmByCh[ch], downsampleScale, &vOut);

                if (8388607.0f / 8388608.0f < vOut) {
                    vOut = 8388607.0f / 8388608.0f;
                }
                if (vOut < -1.0f) {
                    vOut = -1.0f;
                }

                // printf("%f\n", vOut);

                if (audioFormat == 0x3) {
                    float *pF = (float*)&pcmOutByCh[ch][posWByCh[ch]];
                    *pF = vOut;
                } else {
                    switch (wavIn.header.bitsPerSample) {
                    case 32:
                        ConvertFloatToInt32(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 24:
                        ConvertFloatToInt24(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    case 16:
                        ConvertFloatToInt16(vOut, &pcmOutByCh[ch][posWByCh[ch]]);
                        break;
                    default:
                        assert(0);
                        break;
                    }
                }
                posWByCh[ch] += bytesPerSample;

                /*
                printf("i=%lld ch=%d posR=%lld posWByCh[ch]=%lld vOut=%f\n",
                    i, ch, posR, posWByCh[ch], vOut);
                */
            }
#endif
        }
    }

    delete [] posWByCh;
    posWByCh = nullptr;

    for (int ch = 0; ch<CH; ++ch) {
        hb[ch]->End();
        ds[ch].End();
    }

    uint8_t * pcmOut = new uint8_t[wavIn.header.dataChunkSize / downsampleScale];
    if (!pcmOut) {
        printf("Memory exhausted\n");
        return false;
    }

    int64_t *posRByCh = new int64_t[CH];
    memset(posRByCh, 0, sizeof(int64_t)*CH);
    int64_t posW = 0;
    for (int64_t i = 0; i<outPcmSamples; ++i) {
        for (int ch = 0; ch<CH; ++ch) {
            for (int b=0; b<bytesPerSample;++b) {
                pcmOut[posW++] = pcmOutByCh[ch][posRByCh[ch]];
                ++posRByCh[ch];
            }
        }
    }

    for (int ch=0; ch<CH; ++ch) {
        delete [] pcmOutByCh[ch];
        pcmOutByCh[ch] = nullptr;
    }
    delete [] pcmOutByCh;
    pcmOutByCh = nullptr;

    wavIn.header.byteRate /= downsampleScale;
    wavIn.header.sampleRate /= downsampleScale;
    wavIn.header.dataChunkSize /= downsampleScale;
    wavIn.header.riffChunkSize = wavIn.header.dataChunkSize + 0x24;
    rv = WriteWav("C:/audio/outputHS.wav", wavIn, pcmOut);

    delete [] pcmOut;
    pcmOut = nullptr;

    return rv;
}
#endif

#if 1
static bool
RunUpDownTest(void)
{
    bool rv;
    Upsampler us[2];
    Downsampler ds[2];

    Wav wavIn;
    if (!ReadWav("C:/audio/FLATSWEEP/FLATSWEEP_032768.WAV", wavIn)) {
        printf("ReadWav failed\n");
        return false;
    }

    const int CH = wavIn.header.numCh;
    const int bytesPerSample = ((wavIn.header.bitsPerSample+7)/8);
    const int bytesPerFrame = bytesPerSample * CH;
    const int inPcmSamples = wavIn.header.dataChunkSize / bytesPerFrame;
    const int outPcmSamples = inPcmSamples ;
    const int audioFormat = wavIn.header.audioFormat;

    for (int ch = 0; ch<CH; ++ch) {
        ds[ch].Start();
        us[ch].Start();
    }

    uint8_t *pcmOut = new uint8_t[wavIn.header.dataChunkSize];
    if (!pcmOut) {
        printf("Memory exhausted\n");
        return false;
    }

    int64_t pos = 0;

    for (int64_t i = 0; i<inPcmSamples; ++i) {
        for (int ch = 0; ch<CH; ++ch) {
            float v = 0;
            if (audioFormat == 0x3) {
                const float *pF = (const float*)&wavIn.pcm[pos];
                v = *pF;
            } else {
                switch (wavIn.header.bitsPerSample) {
                case 32:
                    v = ConvertInt32ToFloat(&wavIn.pcm[pos]);
                    break;
                case 24:
                    v = ConvertInt24ToFloat(&wavIn.pcm[pos]);
                    break;
                case 16:
                    v = ConvertInt16ToFloat(&wavIn.pcm[pos]);
                    break;
                default:
                    assert(0);
                    break;
                }
            }

            //v = 1.0f;
            v *= 0.5f;

            float pcmTmp[2];
            us[ch].Filter(v, pcmTmp);
            float vOut = 0;
            ds[ch].Filter(pcmTmp, 2, &vOut);

            printf("%f %f\n", v, vOut);

            if (8388607.0f / 8388608.0f < vOut) {
                vOut = 8388607.0f / 8388608.0f;
            }
            if (vOut < -1.0f) {
                vOut = -1.0f;
            }

            if (audioFormat == 0x3) {
                float *pF = (float*)&pcmOut[pos];
                *pF = vOut;
            } else {
                switch (wavIn.header.bitsPerSample) {
                case 32:
                    ConvertFloatToInt32(vOut, &pcmOut[pos]);
                    break;
                case 24:
                    ConvertFloatToInt24(vOut, &pcmOut[pos]);
                    break;
                case 16:
                    ConvertFloatToInt16(vOut, &pcmOut[pos]);
                    break;
                default:
                    assert(0);
                    break;
                }
            }

            pos += bytesPerSample;
        }
    }

    for (int ch = 0; ch<CH; ++ch) {
        ds[ch].End();
        us[ch].End();
    }

    rv = WriteWav("C:/audio/outputHS.wav", wavIn, pcmOut);

    delete [] pcmOut;
    pcmOut = nullptr;

    return rv;
}
#endif

#if 0
static bool
RunAll(void)
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

            if (8388607.0f / 8388608.0f < v) {
                v = 8388607.0f / 8388608.0f;
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
#endif

int main(void)
{
    RunUpDownTest();

#ifndef NDEBUG
    _CrtDumpMemoryLeaks();
#endif
    return 0;
}
