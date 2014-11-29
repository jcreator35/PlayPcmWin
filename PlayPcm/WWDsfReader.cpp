#include "WWDsfReader.h"
#include <stdio.h>
#include <stdint.h>
#include <string.h>

#define DSD_CHUNK_FOURCC  "DSD "
#define FMT_CHUNK_FOURCC  "fmt "
#define DATA_CHUNK_FOURCC "data"

// assumed target platform is little endian...
#define FREAD(to, bytes, fp)               \
    if (fread(to, 1, bytes, fp) < bytes) { \
        return -1;                         \
    }

static const unsigned char gBitReverse[256] = 
{
#   define R2(n)    n,     n + 2*64,     n + 1*64,     n + 3*64
#   define R4(n) R2(n), R2(n + 2*16), R2(n + 1*16), R2(n + 3*16)
#   define R6(n) R4(n), R4(n + 2*4 ), R4(n + 1*4 ), R4(n + 3*4 )
    R6(0), R6(2), R6(1), R6(3)
};

struct DsfDsdChunk {
    uint64_t chunkBytes;
    uint64_t totalFileBytes;
    uint64_t medadataOffset;

    int ReadFromFile(FILE *fp) {
        FREAD(&chunkBytes,     8, fp);
        FREAD(&totalFileBytes, 8, fp);
        FREAD(&medadataOffset, 8, fp);

        if (chunkBytes != 28) {
            printf("DSF DSD chunkBytes!=28 %llu\n", chunkBytes);
            return -1;
        }

        if (0x7fffffff < totalFileBytes) {
            printf("file too large %llu\n", totalFileBytes);
            return -1;
        }

        return 0;
    }
};

struct DsfFmtChunk {
    uint64_t chunkBytes;
    uint32_t formatVersion;
    uint32_t formatId;
    uint32_t channelType;
    uint32_t channelNum;

    uint32_t samplingFrequency;
    uint32_t bitsPerSample;
    uint64_t sampleCount;
    uint32_t blockSizePerChannel;
    uint32_t reserved;

    int ReadFromFile(FILE *fp) {
        FREAD(&chunkBytes,     8, fp);
        FREAD(&formatVersion,  4, fp);
        FREAD(&formatId,       4, fp);
        FREAD(&channelType,    4, fp);
        FREAD(&channelNum,     4, fp);

        FREAD(&samplingFrequency,   4, fp);
        FREAD(&bitsPerSample,       4, fp);
        FREAD(&sampleCount,         8, fp);
        FREAD(&blockSizePerChannel, 4, fp);
        FREAD(&reserved,            4, fp);

        if (chunkBytes != 52) {
            printf("DSF fmt chunkBytes!=52 %llu\n", chunkBytes);
            return -1;
        }

        if (formatVersion != 1) {
            printf("DSF fmt version!=1 %u\n", formatVersion);
            return -1;
        }

        if (formatId != 0) {
            printf("DSF fmt formatId!=0 %u\n", formatId);
            return -1;
        }

        if (channelType != 2) {
            printf("DSF fmt channelType!=2 %u\n", channelType);
            return -1;
        }

        if (channelNum != 2) {
            printf("DSF fmt channelNum!=2 %u\n", channelNum);
            return -1;
        }

        if (samplingFrequency != 2822400) {
            printf("samplingFrequency!=2822400 %u\n", samplingFrequency);
            return -1;
        }

        if (bitsPerSample != 1) {
            printf("DSF fmt bitsPerSample!=1 %u\n", bitsPerSample);
            return -1;
        }

        if (blockSizePerChannel != 4096) {
            printf("blockSizePerChannel!=4096 %u\n", blockSizePerChannel);
            return -1;
        }

        return 0;
    }
};

struct DsfDataChunk {
    uint64_t chunkBytes;

    int ReadFromFile(FILE *fp) {
        FREAD(&chunkBytes,     8, fp);

        if (0x7fffffff < chunkBytes) {
            printf("DsfDataChunk too large %llu\n", chunkBytes);
            return -1;
        }

        return 0;
    }
};

WWPcmData *
WWReadDsfFile(const char *path, WWBitsPerSampleType bitsPerSampleType)
{
    WWPcmData *pcmData = NULL;
    char fourCC[4];
    DsfDsdChunk  dsdChunk;
    DsfFmtChunk  fmtChunk;
    DsfDataChunk dataChunk;
    uint32_t streamBytes;
    uint32_t writePos;
    uint32_t blockNum;
    unsigned char *blockData = NULL;
    int result = -1;

    if (bitsPerSampleType == WWBpsNone) {
        printf("E: device does not support DoP\n");
        return NULL;
    }

    FILE *fp = NULL;
    fopen_s(&fp, path, "rb");
    if (NULL == fp) {
        return NULL;
    }

    if (fread(fourCC, 1, 4, fp) < 4 ||
        0 != memcmp(fourCC, DSD_CHUNK_FOURCC, 4) ||
        dsdChunk.ReadFromFile(fp) < 0) {
        goto end;
    }

    if (fread(fourCC, 1, 4, fp) < 4 ||
        0 != memcmp(fourCC, FMT_CHUNK_FOURCC, 4) ||
        fmtChunk.ReadFromFile(fp) < 0) {
        goto end;
    }

    if (fread(fourCC, 1, 4, fp) < 4 ||
        0 != memcmp(fourCC, DATA_CHUNK_FOURCC, 4) ||
        dataChunk.ReadFromFile(fp) < 0) {
        goto end;
    }

    pcmData = new WWPcmData();
    if (NULL == pcmData) {
        goto end;
    }
    pcmData->Init();

    pcmData->bitsPerSample      = bitsPerSampleType == WWBps32v24 ? 32 : 24;
    pcmData->validBitsPerSample = 24;
    pcmData->nChannels          = fmtChunk.channelNum;

    // DSD 16bit == 1 frame
    pcmData->nFrames        = (int)(fmtChunk.sampleCount/16);
    pcmData->nSamplesPerSec = 176400;
    pcmData->posFrame       = 0;

    streamBytes = (pcmData->bitsPerSample/8) * pcmData->nFrames * pcmData->nChannels;
    pcmData->stream = new unsigned char[streamBytes];
    if (NULL == pcmData->stream) {
        goto end;
    }
    memset(pcmData->stream, 0, streamBytes);

    blockNum = (uint32_t)((dataChunk.chunkBytes-12)/fmtChunk.blockSizePerChannel);
    blockData = new unsigned char[fmtChunk.blockSizePerChannel * fmtChunk.channelNum];
    if (NULL == blockData) {
        goto end;
    }

    writePos = 0;
    for (uint32_t block = 0; block < blockNum; ++block) {
        // data is stored in following order:
        // L channel 4096bytes consecutive data, R channel 4096bytes consecutive data, L channel 4096bytes consecutive data, ...
        //
        // read 4096 x numChannels bytes.
        if (fread(blockData, fmtChunk.blockSizePerChannel * fmtChunk.channelNum, 1, fp) < 1) {
            goto end;
        }

        switch (bitsPerSampleType) {
        case WWBps32v24:
            for (uint32_t i=0; i<fmtChunk.blockSizePerChannel/2; ++i) {
                for (uint32_t ch=0; ch<fmtChunk.channelNum; ++ch) {
                    pcmData->stream[writePos+0] = 0;
                    pcmData->stream[writePos+1] = gBitReverse[blockData[i*2+1 + ch*fmtChunk.blockSizePerChannel]];
                    pcmData->stream[writePos+2] = gBitReverse[blockData[i*2+0 + ch*fmtChunk.blockSizePerChannel]];
                    pcmData->stream[writePos+3] = i & 1 ? 0xfa : 0x05;
                    writePos += 4;
                    if (streamBytes <= writePos) {
                        // recorded sample is ended on part of the way of the block
                        result = 0;
                        goto end;
                    }
                }
            }
            break;
        case WWBps24:
            for (uint32_t i=0; i<fmtChunk.blockSizePerChannel/2; ++i) {
                for (uint32_t ch=0; ch<fmtChunk.channelNum; ++ch) {
                    pcmData->stream[writePos+0] = gBitReverse[blockData[i*2+1 + ch*fmtChunk.blockSizePerChannel]];
                    pcmData->stream[writePos+1] = gBitReverse[blockData[i*2+0 + ch*fmtChunk.blockSizePerChannel]];
                    pcmData->stream[writePos+2] = i & 1 ? 0xfa : 0x05;
                    writePos += 3;
                    if (streamBytes <= writePos) {
                        // recorded sample is ended on part of the way of the block
                        result = 0;
                        goto end;
                    }
                }
            }
            break;
        }
    }

    // coincidentally block size == recorded sample size
    result = 0;
end:
    delete [] blockData;
    blockData = NULL;

    if (result < 0) {
        if (pcmData) {
            pcmData->Term();
            delete pcmData;
            pcmData = NULL;
        }
    }

    fclose(fp);
    return pcmData;
}

