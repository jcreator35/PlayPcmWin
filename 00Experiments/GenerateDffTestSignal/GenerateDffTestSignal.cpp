#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <math.h>

static const float MATH_PI = 3.14159265358979f;

static const char * const OUTPUT_FILENAME = "output.dff";

static const float SIGNAL_FREQUENCY_HZ = 1000;
static const float SIGNAL_AMPLITUDE    = 0.5f;

static const int SAMPLE_RATE      = 2822400;
static const int NUM_CHANNELS     = 2;
static const float OUTPUT_SECONDS =60.0f;

struct SmallDsdStreamInfo {
    uint64_t dsdStream;
    int      availableBits;

    SmallDsdStreamInfo(void) {
        dsdStream = 0;
        availableBits = 0;
    }
};
static const unsigned char gBitsSetTable256[256] = 
{
#define B2(n) n,     n+1,     n+1,     n+2
#define B4(n) B2(n), B2(n+1), B2(n+1), B2(n+2)
#define B6(n) B4(n), B4(n+1), B4(n+1), B4(n+2)
    B6(0), B6(1), B6(1), B6(2)
#undef B6
#undef B4
#undef B2
};

static float
DsdStreamToAmplitudeFloat(uint64_t v, uint32_t availableBits)
{
    v &= 0xFFFFFFFFFFFFFFFFULL >> (64-availableBits);

    const unsigned char * p = (unsigned char *) &v;
    int bitCount = 
        gBitsSetTable256[p[0]] +
        gBitsSetTable256[p[1]] +
        gBitsSetTable256[p[2]] +
        gBitsSetTable256[p[3]] +
        gBitsSetTable256[p[4]] +
        gBitsSetTable256[p[5]] +
        gBitsSetTable256[p[6]] +
        gBitsSetTable256[p[7]];

    return (bitCount-availableBits*0.5f)/(availableBits*0.5f);
}

struct OutputSignalProperty {
    float omega;
    float angleVelocity;
    OutputSignalProperty(void) {
        omega = 0.0f;
        angleVelocity = 0.0f;
    }
};

/// @param nFrames 全チャンネルの8サンプル分の情報を1とする単位。
static int
GenerateDffData(int nFrames, int nChannels, FILE *fp)
{
    int result = -1;
    int64_t pos = 0;
    SmallDsdStreamInfo *dsdStreams = NULL;
    OutputSignalProperty *signals = NULL;

    dsdStreams = new SmallDsdStreamInfo[nChannels];
    if (NULL == dsdStreams) {
        printf("memory exhausted\n");
        goto end;
    }

    signals = new OutputSignalProperty[nChannels];
    if (NULL == signals) {
        printf("memory exhausted\n");
        goto end;
    }

    for (int ch=0; ch<nChannels; ++ch) {
        OutputSignalProperty *s = &signals[ch];

        s->omega         = 0.0f;
        s->angleVelocity = 2.0f * MATH_PI * 8 * SIGNAL_FREQUENCY_HZ / SAMPLE_RATE;
        assert(0.0f <= s->angleVelocity);
    }


    for (int64_t i=0; i<nFrames; ++i) {
        for (int ch=0; ch<nChannels; ++ch) {
            SmallDsdStreamInfo *p = &dsdStreams[ch];
            OutputSignalProperty *s = &signals[ch];

            s->omega += s->angleVelocity;
            while (MATH_PI < s->omega) {
                s->omega -= 2.0f * MATH_PI;
            }

            float targetV = SIGNAL_AMPLITUDE * sinf(s->omega);

            for (int c=0; c<8; ++c) {
                uint32_t ampBits = p->availableBits;
                if (64 == p->availableBits) {
                    // 今作っている8ビットのDSDデータをp->dsdStreamに詰めると
                    // 64ビットのデータのうち古いデータ8ビットが押し出されて消えるのでAmplitudeの計算から除外する。
                    ampBits = 56+c;
                }

                float currentV = DsdStreamToAmplitudeFloat(p->dsdStream, ampBits);
                p->dsdStream <<= 1;
                if (currentV < targetV) {
                    p->dsdStream += 1;
                }

                if (p->availableBits < 64) {
                    ++p->availableBits;
                }
            }
            if (fputc(p->dsdStream&0xff, fp)<0) {
                printf("fputc error\n");
                goto end;
            }
        }
    }

    result = 0;

end:
    delete [] signals;
    signals = NULL;

    delete [] dsdStreams;
    dsdStreams = NULL;

    return result;
}

#define FORM_DSD_FORM_TYPE           "DSD "
#define PROPERTY_CHUNK_PROPERTY_TYPE "SND "
#define COMPRESSION_UNCOMPRESSED     "DSD "

// assumed target platform is little endian...

static const int FOURCC_FRM8 = 0x384d5246; //< "FRM8"
static const int FOURCC_FVER = 0x52455646; //< "FVER"
static const int FOURCC_PROP = 0x504f5250; //< "PROP"
static const int FOURCC_FS   = 0x20205346; //< "FS  "
static const int FOURCC_SND  = 0x20444e53; //< "SND "
static const int FOURCC_CHNL = 0x4c4e4843; //< "CHNL"
static const int FOURCC_CMPR = 0x52504d43; //< "CMPR"
static const int FOURCC_DSD  = 0x20445344; //< "DSD "
static const int FOURCC_ABSS = 0x53534241; //< "ABSS"

// assumed target platform is little endian...
#define FWRITE(ptr, bytes, fp)               \
    if (fwrite(ptr, 1, bytes, fp) < bytes) { \
        return -1;                           \
    }

static uint16_t
Little2ToBig2(uint16_t v)
{
    return (v<<8) |
           (v>>8);
}

static uint32_t
Little4ToBig4(uint32_t v)
{
    return (v >> 24) |
           ((v & 0x00ff0000) >> 8) |
           ((v & 0x0000ff00) << 8) |
           (v << 24);
}

static uint64_t
Little8ToBig8(uint64_t v)
{
    return (v>>56) |
           ((v&0x00ff000000000000)>>40) |
           ((v&0x0000ff0000000000)>>24) |
           ((v&0x000000ff00000000)>>8)  |
           ((v&0x00000000ff000000)<<8)  |
           ((v&0x0000000000ff0000)<<24) |
           ((v&0x000000000000ff00)<<40) |
           (v<<56);
}

static int
WriteBig2(uint16_t v, FILE *fp)
{
    uint16_t tmp = Little2ToBig2(v);
    return fwrite(&tmp, 1, 2, fp);
}

static int
WriteBig4(uint32_t v, FILE *fp)
{
    uint32_t tmp = Little4ToBig4(v);
    return fwrite(&tmp, 1, 4, fp);
}

static int
WriteBig8(uint64_t v, FILE *fp)
{
    uint64_t tmp = Little8ToBig8(v);
    return fwrite(&tmp, 1, 8, fp);
}

#define WRITE_BIG2(v, fp)       \
    if (WriteBig2(v, fp) < 2) { \
        return -1;              \
    }

#define WRITE_BIG4(v, fp)       \
    if (WriteBig4(v, fp) < 4) { \
        return -1;              \
    }

#define WRITE_BIG8(v, fp)       \
    if (WriteBig8(v, fp) < 8) { \
        return -1;              \
    }

struct DsdiffFormDsdChunk {
    uint64_t ckDataSize;

    DsdiffFormDsdChunk(void) {
        ckDataSize = 0;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_FRM8, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        fprintf(fp, "DSD ");
        return 0;
    }
};

struct DsdiffFormVersionChunk {
    uint64_t ckDataSize;
    uint32_t version;

    DsdiffFormVersionChunk(void) {
        ckDataSize = 4;
        version    = 0x01050000;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_FVER, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        WRITE_BIG4(version, fp);
        return 0;
    }
};

struct DsdiffPropertyChunk {
    uint64_t ckDataSize;

    DsdiffPropertyChunk(void) {
        ckDataSize = 0;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_PROP, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        fprintf(fp, "SND ");
        return 0;
    }
};

struct DsdiffSampleRateChunk {
    uint64_t ckDataSize;
    uint32_t sampleRate;

    DsdiffSampleRateChunk(void) {
        ckDataSize = 4;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_FS, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        WRITE_BIG4(sampleRate, fp);
        return 0;
    }
};

struct DsdiffChannelsChunk {
    uint64_t ckDataSize;
    uint16_t numChannels;

    DsdiffChannelsChunk(void) {
        ckDataSize = 0xa;
        numChannels = 2;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_CHNL, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        WRITE_BIG2(numChannels, fp);
        fprintf(fp, "SLFTSRGT");
        return 0;
    }
};

struct DsdiffCompressionTypeChunk {
    uint64_t ckDataSize;

    DsdiffCompressionTypeChunk(void) {
        ckDataSize = 0x14;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_CMPR, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        fprintf(fp, "DSD ");
        const unsigned char count = 0xe;
        FWRITE(&count, 1, fp);
        fprintf(fp, "not compressed");
        const unsigned char term = 0;
        FWRITE(&term, 1, fp);
        return 0;
    }
};

struct DsdiffSoundDataChunk {
    uint64_t ckDataSize;

    DsdiffSoundDataChunk(void) {
        ckDataSize = 0;
    }

    int WriteToFile(FILE *fp) {
        FWRITE(&FOURCC_DSD, 4, fp);
        WRITE_BIG8(ckDataSize, fp);
        return 0;
    }
};

#define HRR(a)         \
    result = a;        \
    if (result < 0) {  \
        return result; \
    }

static int
CreateDsdiffFile(FILE *fp)
{
    int result = 0;

    DsdiffFormDsdChunk     frm8Chunk;
    DsdiffFormVersionChunk fverChunk;
    DsdiffPropertyChunk    propChunk;
    DsdiffSampleRateChunk  fsChunk;
    DsdiffChannelsChunk    chnlChunk;
    DsdiffCompressionTypeChunk cmprChunk;
    DsdiffSoundDataChunk   dataChunk;

    fsChunk.sampleRate = SAMPLE_RATE;
    dataChunk.ckDataSize = NUM_CHANNELS * (SAMPLE_RATE/8) * OUTPUT_SECONDS;
    propChunk.ckDataSize = 4 + (fsChunk.ckDataSize + 12) + (chnlChunk.ckDataSize + 12) + (cmprChunk.ckDataSize + 12);
    frm8Chunk.ckDataSize = 4 + (fverChunk.ckDataSize + 12) + (propChunk.ckDataSize + 12) + (dataChunk.ckDataSize + 12);

    HRR(frm8Chunk.WriteToFile(fp));
    HRR(fverChunk.WriteToFile(fp));
    HRR(propChunk.WriteToFile(fp));
    HRR(fsChunk.WriteToFile(fp));
    HRR(chnlChunk.WriteToFile(fp));
    HRR(cmprChunk.WriteToFile(fp));
    HRR(dataChunk.WriteToFile(fp));

    return GenerateDffData((SAMPLE_RATE/8) * OUTPUT_SECONDS, NUM_CHANNELS, fp);
}

int main(int argc, char* argv[])
{
    FILE *fp = fopen(OUTPUT_FILENAME, "wb");
    if (NULL == fp) {
        printf("could not open %s\n", OUTPUT_FILENAME);
        return 1;
    }

    if (CreateDsdiffFile(fp) < 0) {
        printf("failed\n");
    }

    fclose(fp);

    return 0;
}

