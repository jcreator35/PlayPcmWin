#include <stdio.h>  // printf
#include <stdint.h> // uint32_t
#include <string.h> // memset
#include <assert.h> // assert
#include <stdlib.h> // _wtoi

/// 2==LR stereo 1==monaural
#define NUM_CHANNELS (1)

static int
WriteLE4(uint32_t val, FILE *fp)
{
    uint8_t buf[4];

    buf[0] = (val & 0xff);
    buf[1] = ((val>>8) & 0xff);
    buf[2] = ((val>>16) & 0xff);
    buf[3] = ((val>>24) & 0xff);

    return fwrite(buf, 1, 4, fp);
}

static int
WriteLE2(uint16_t val, FILE *fp)
{
    uint8_t buf[2];

    buf[0] = (val & 0xff);
    buf[1] = ((val>>8) & 0xff);

    return fwrite(buf, 1, 2, fp);
}

static void
PrintUsage(const wchar_t *argv0)
{
    printf("usage %S sampleRate [16|24|32] numSamples outputWavPath \n", argv0);
}

int
wmain(int argc, wchar_t* argv[])
{
    if (argc != 5) {
        PrintUsage(argv[0]);
        return 1;
    }

    int sampleRate = _wtoi(argv[1]);
    if (sampleRate <= 0) {
        PrintUsage(argv[0]);
        return 1;
    }

    int bitsPerSample = _wtoi(argv[2]);
    if (bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32) {
        PrintUsage(argv[0]);
        return 1;
    }

    int numSamples = _wtoi(argv[3]);
    if (numSamples <= 0) {
        PrintUsage(argv[0]);
        return 1;
    }

    FILE *fp = _wfopen(argv[4], L"wb");
    if (NULL == fp) {
        PrintUsage(argv[0]);
        return 1;
    }

    int numChannels = NUM_CHANNELS;
    int64_t dataBytes = (numSamples * bitsPerSample * numChannels +7)/ 8;
    uint8_t *buf = new uint8_t[dataBytes];
    if (NULL == buf) {
        printf("out of memory\n");
        return 1;
    }


    memset(buf, 0, dataBytes);

    switch (bitsPerSample) {
    case 16:
        {
            int pos = 0;
            for (int i=0; i<(int)numSamples; ++i) {
                uint32_t sampleValue = i;

                for (int ch=0; ch<numChannels; ++ch) {
                    // 1サンプルのデータを書き込む。16bit little endian
                    buf[pos+0] = (uint8_t)sampleValue;
                    buf[pos+1] = (uint8_t)(sampleValue>>8);
                }
                pos += 2 * numChannels;
            }
        }
        break;
    case 24:
        {
            int pos = 0;
            for (int i=0; i<(int)numSamples; ++i) {
                uint32_t sampleValue = i;

                for (int ch=0; ch<numChannels; ++ch) {
                    // 1サンプルのデータを書き込む。24bit little endian
                    buf[pos+0] = (uint8_t)sampleValue;
                    buf[pos+1] = (uint8_t)(sampleValue>>8);
                    buf[pos+2] = (uint8_t)(sampleValue>>16);
                }
                pos += 3 * numChannels;
            }
        }
        break;
    case 32:
        {
            int64_t pos = 0;
            for (uint32_t i=0; i<(uint32_t)numSamples; ++i) {
                uint32_t sampleValue = i;

                for (int ch=0; ch<numChannels; ++ch) {
                    // 1サンプルのデータを書き込む。32bit little endian
                    buf[pos+0] = (uint8_t)sampleValue;
                    buf[pos+1] = (uint8_t)(sampleValue>>8);
                    buf[pos+2] = (uint8_t)(sampleValue>>16);
                    buf[pos+3] = (uint8_t)(sampleValue>>24);
                }
                pos += 4 * numChannels;
            }
        }
        break;
    default:
        assert(0);
        break;
    }

    int riffChunkSize = 36 + dataBytes;

    fwrite("RIFF", 1, 4, fp);
    WriteLE4(riffChunkSize, fp);
    fwrite("WAVE", 1, 4, fp);

    fwrite("fmt ", 1, 4, fp);
    WriteLE4(16, fp);          //< fmt header size
    WriteLE2(1, fp);           //< 1==PCM
    WriteLE2(numChannels, fp);
    WriteLE4(sampleRate, fp);
    WriteLE4((sampleRate * bitsPerSample * numChannels)/8, fp);
    WriteLE2((bitsPerSample * numChannels) / 8, fp);
    WriteLE2(bitsPerSample, fp);
    fwrite("data", 1, 4, fp);
    WriteLE4(dataBytes, fp);
    fwrite(buf, 1, dataBytes, fp);

    delete [] buf;
    fclose(fp);
    fp = NULL;

    return 0;
}

