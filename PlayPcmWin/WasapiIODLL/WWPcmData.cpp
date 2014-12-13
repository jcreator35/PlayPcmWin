// 日本語 UTF-8

#include "WWPcmData.h"
#include "WWUtil.h"
#include <assert.h>
#include <malloc.h>
#include <stdint.h>
#include <float.h>

const char *
WWPcmDataContentTypeToStr(WWPcmDataContentType w)
{
    switch (w) {
    case WWPcmDataContentSilenceForTrailing: return "SilenceForTrailing";
    case WWPcmDataContentSilenceForPause: return "SilenceForPause";
    case WWPcmDataContentSilenceForEnding: return "SilenceForEnding";
    case WWPcmDataContentMusicData: return "PcmData";
    case WWPcmDataContentSplice:  return "Splice";
    default: return "unknown";
    }
}

const char *
WWPcmDataSampleFormatTypeToStr(WWPcmDataSampleFormatType w)
{
    switch (w) {
    case WWPcmDataSampleFormatSint16: return "Sint16";
    case WWPcmDataSampleFormatSint24: return "Sint24";
    case WWPcmDataSampleFormatSint32V24: return "Sint32V24";
    case WWPcmDataSampleFormatSint32: return "Sint32";
    case WWPcmDataSampleFormatSfloat: return "Sfloat";
    default: return "unknown";
    }
}

WWPcmDataSampleFormatType
WWPcmDataSampleFormatTypeGenerate(int bitsPerSample, int validBitsPerSample, GUID subFormat)
{
    if (subFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT) {
        if (bitsPerSample == 32 &&
            validBitsPerSample == 32) {
            return WWPcmDataSampleFormatSfloat;
        }
        return WWPcmDataSampleFormatUnknown;
    }
    
    if (subFormat == KSDATAFORMAT_SUBTYPE_PCM) {
        switch (bitsPerSample) {
        case 16:
            if (validBitsPerSample == 16) {
                return WWPcmDataSampleFormatSint16;
            }
            break;
        case 24:
            if (validBitsPerSample == 24) {
                return WWPcmDataSampleFormatSint24;
            }
            break;
        case 32:
            if (validBitsPerSample == 24) {
                return WWPcmDataSampleFormatSint32V24;
            }
            if (validBitsPerSample == 32) {
                return WWPcmDataSampleFormatSint32;
            }
            break;
        default:
            break;
        }
        return WWPcmDataSampleFormatUnknown;
    }

    return WWPcmDataSampleFormatUnknown;
}

int
WWPcmDataSampleFormatTypeToBitsPerSample(WWPcmDataSampleFormatType t)
{
    static const int result[WWPcmDataSampleFormatNUM]
        = { 16, 24, 32, 32, 32 };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return -1;
    }
    return result[t];
}

int
WWPcmDataSampleFormatTypeToValidBitsPerSample(WWPcmDataSampleFormatType t)
{
    static const int result[WWPcmDataSampleFormatNUM]
        = { 16, 24, 24, 32, 32 };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return -1;
    }
    return result[t];
}

bool
WWPcmDataSampleFormatTypeIsFloat(WWPcmDataSampleFormatType t)
{
    static const bool result[WWPcmDataSampleFormatNUM]
        = { false, false, false, false, true };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return false;
    }
    return result[t];
}

bool
WWPcmDataSampleFormatTypeIsInt(WWPcmDataSampleFormatType t)
{
    static const bool result[WWPcmDataSampleFormatNUM]
        = { true, true, true, true, false };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return false;
    }
    return result[t];
}

void
WWPcmData::Term(void)
{
    dprintf("D: %s() stream=%p\n", __FUNCTION__, stream);

    free(stream);
    stream = NULL;
}

void
WWPcmData::CopyFrom(WWPcmData *rhs)
{
    *this = *rhs;

    next = NULL;

    int64_t bytes = nFrames * bytesPerFrame;
    assert(0 < bytes);

    stream = (BYTE*)malloc(bytes);
    CopyMemory(stream, rhs->stream, bytes);
}

bool
WWPcmData::Init(
        int aId, WWPcmDataSampleFormatType asampleFormat, int anChannels,
        int64_t anFrames, int aframeBytes,
        WWPcmDataContentType acontentType)
{
    assert(stream == NULL);

    id           = aId;
    sampleFormat = asampleFormat;
    contentType  = acontentType;
    next         = NULL;
    posFrame     = 0;
    nChannels    = anChannels;
    // メモリ確保に成功してからフレーム数をセットする。
    nFrames       = 0;
    bytesPerFrame = aframeBytes;
    stream        = NULL;

    int64_t bytes = anFrames * aframeBytes;
    if (bytes < 0) {
        return false;
    }
#ifdef _X86_
    if (0x7fffffffL < bytes) {
        // cannot alloc 2GB buffer on 32bit build
        return false;
    }
#endif

    BYTE *p = (BYTE *)malloc(bytes);
    if (NULL == p) {
        // 失敗…
        return false;
    }

    ZeroMemory(p, bytes);
    nFrames = anFrames;
    stream = p;

    return true;
}

int
WWPcmData::GetSampleValueInt(int ch, int64_t posFrame) const
{
    assert(sampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < nChannels);

    if (posFrame < 0 ||
        nFrames <= posFrame) {
        return 0;
    }

    int result = 0;
    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            short *p = (short*)(&stream[2 * (nChannels * posFrame + ch)]);
            result = *p;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            // bus error回避。x86にはbus error無いけど一応。
            unsigned char *p =
                (unsigned char*)(&stream[3 * (nChannels * posFrame + ch)]);

            result =
                (((unsigned int)p[0])<<8) +
                (((unsigned int)p[1])<<16) +
                (((unsigned int)p[2])<<24);
            result /= 256;
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            int *p = (int*)(&stream[4 * (nChannels * posFrame + ch)]);
            result = ((*p)/256);
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&stream[4 * (nChannels * posFrame + ch)]);
            result = *p;
        }
        break;
    default:
        assert(0);
        break;
    }

    return result;
}

float
WWPcmData::GetSampleValueFloat(int ch, int64_t posFrame) const
{
    assert(sampleFormat == WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < nChannels);

    if (posFrame < 0 ||
        nFrames <= posFrame) {
        return 0;
    }

    float *p = (float *)(&stream[4 * (nChannels * posFrame + ch)]);
    return *p;
}

float
WWPcmData::GetSampleValueAsFloat(int ch, int64_t posFrame) const
{
    float result = 0.0f;

    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint16:
        result = GetSampleValueInt(ch, posFrame) * (1.0f / 32768.0f);
        break;
    case WWPcmDataSampleFormatSint24:
    case WWPcmDataSampleFormatSint32V24:
        result = GetSampleValueInt(ch, posFrame) * (1.0f / 8388608.0f);
        break;
    case WWPcmDataSampleFormatSint32:
        result = GetSampleValueInt(ch, posFrame) * (1.0f / 2147483648.0f);
        break;
    case WWPcmDataSampleFormatSfloat:
        result = GetSampleValueFloat(ch, posFrame);
        break;
    default:
        assert(0);
        break;
    }
    return result;
}

bool
WWPcmData::SetSampleValueInt(int ch, int64_t posFrame, int value)
{
    assert(sampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < nChannels);

    if (posFrame < 0 ||
        nFrames <= posFrame) {
        return false;
    }

    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            short *p =
                (short*)(&stream[2 * (nChannels * posFrame + ch)]);
            *p = (short)value;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            // bus error回避。x86にはbus error無いけど一応。
            unsigned char *p =
                (unsigned char*)(&stream[3 * (nChannels * posFrame + ch)]);
            p[0] = (unsigned char)(value & 0xff);
            p[1] = (unsigned char)((value>>8) & 0xff);
            p[2] = (unsigned char)((value>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            unsigned char *p =
                (unsigned char*)(&stream[4 * (nChannels * posFrame + ch)]);
            p[0] = 0;
            p[1] = (unsigned char)(value & 0xff);
            p[2] = (unsigned char)((value>>8) & 0xff);
            p[3] = (unsigned char)((value>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&stream[4 * (nChannels * posFrame + ch)]);
            *p = value;
        }
        break;
    default:
        assert(0);
        break;
    }

    return true;
}

bool
WWPcmData::SetSampleValueFloat(int ch, int64_t posFrame, float value)
{
    assert(sampleFormat == WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < nChannels);

    if (posFrame < 0 ||
        nFrames <= posFrame) {
        return false;
    }

    float *p = (float *)(&stream[4 * (nChannels * posFrame + ch)]);
    *p = value;
    return true;
}

bool
WWPcmData::SetSampleValueAsFloat(int ch, int64_t posFrame, float value)
{
    bool result = false;

    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint16:
        result = SetSampleValueInt(ch, posFrame, (int)(value * 32768.0f));
        break;
    case WWPcmDataSampleFormatSint24:
    case WWPcmDataSampleFormatSint32V24:
        result = SetSampleValueInt(ch, posFrame, (int)(value * 8388608.0f));
        break;
    case WWPcmDataSampleFormatSint32:
        result = SetSampleValueInt(ch, posFrame, (int)(value * 2147483648.0f));
        break;
    case WWPcmDataSampleFormatSfloat:
        result = SetSampleValueFloat(ch, posFrame, value);
        break;
    default:
        assert(0);
        break;
    }
    return result;
}

struct PcmSpliceInfoFloat {
    float dydx;
    float y;
};

struct PcmSpliceInfoInt {
    int deltaX;
    int error;
    int ystep;
    int deltaError;
    int deltaErrorDirection;
    int y;
};

int
WWPcmData::UpdateSpliceDataWithStraightLinePcm(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    assert(0 < nFrames && nFrames <= 0x7fffffff);

    switch (fromPcm.sampleFormat) {
    case WWPcmDataSampleFormatSfloat:
        {
            // floatは、簡単。
            PcmSpliceInfoFloat *p =
                (PcmSpliceInfoFloat*)_malloca(nChannels * sizeof(PcmSpliceInfoFloat));
            assert(p);

            for (int ch=0; ch<nChannels; ++ch) {
                float y0 = fromPcm.GetSampleValueFloat(ch, fromPosFrame);
                float y1 = toPcm.GetSampleValueFloat(ch, toPosFrame);
                p[ch].dydx = (y1 - y0)/(nFrames);
                p[ch].y = y0;
            }

            for (int x=0; x<nFrames; ++x) {
                for (int ch=0; ch<nChannels; ++ch) {
                    SetSampleValueFloat(ch, x, p[ch].y);
                    p[ch].y += p[ch].dydx;
                }
            }

            _freea(p);
            p = NULL;
        }
        break;
    default:
        {
            // Bresenham's line algorithm的な物
            PcmSpliceInfoInt *p =
                (PcmSpliceInfoInt*)_malloca(nChannels * sizeof(PcmSpliceInfoInt));
            assert(p);

            for (int ch=0; ch<nChannels; ++ch) {
                int y0 = fromPcm.GetSampleValueInt(ch, fromPosFrame);
                int y1 = toPcm.GetSampleValueInt(ch, toPosFrame);
                p[ch].deltaX = (int)nFrames;
                p[ch].error  = p[ch].deltaX/2;
                p[ch].ystep  = ((int64_t)y1 - y0)/p[ch].deltaX;
                p[ch].deltaError = abs(y1 - y0) - abs(p[ch].ystep * p[ch].deltaX);
                p[ch].deltaErrorDirection = (y1-y0) >= 0 ? 1 : -1;
                p[ch].y = y0;
            }

            for (int x=0; x<(int)nFrames; ++x) {
                for (int ch=0; ch<nChannels; ++ch) {
                    SetSampleValueInt(ch, x, p[ch].y);
                    // printf("(%d %d)", x, y);
                    p[ch].y += p[ch].ystep;
                    p[ch].error -= p[ch].deltaError;
                    if (p[ch].error < 0) {
                        p[ch].y += p[ch].deltaErrorDirection;
                        p[ch].error += p[ch].deltaX;
                    }
                }
            }
            // printf("\n");

            _freea(p);
            p = NULL;
        }
        break;
    }

    posFrame = 0;

    return 0;
}

int
WWPcmData::CreateCrossfadeDataPcm(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    assert(0 < nFrames && nFrames <= 0x7fffffff);

    for (int ch=0; ch<nChannels; ++ch) {
        const WWPcmData *pcm0 = &fromPcm;
        int64_t pcm0Pos = fromPosFrame;

        const WWPcmData *pcm1 = &toPcm;
        int64_t pcm1Pos = toPosFrame;

        for (int x=0; x<nFrames; ++x) {
            float ratio = (float)x / nFrames;

            float y0 = pcm0->GetSampleValueAsFloat(ch, pcm0Pos);
            float y1 = pcm1->GetSampleValueAsFloat(ch, pcm1Pos);

            SetSampleValueAsFloat(ch, x, y0 * (1.0f - ratio) + y1 * ratio);

            ++pcm0Pos;
            if (pcm0->nFrames <= pcm0Pos && NULL != pcm0->next) {
                pcm0 = pcm0->next;
                pcm0Pos = 0;
            }

            ++pcm1Pos;
            if (pcm1->nFrames <= pcm1Pos && NULL != pcm1->next) {
                pcm1 = pcm1->next;
                pcm1Pos = 0;
            }
        }
    }

    posFrame = 0;

    // クロスフェードのPCMデータは2GBもない(assertでチェックしている)。intにキャストする。
    return (int)nFrames; 
}

int
WWPcmData::GetBufferData(int64_t fromBytes, int wantBytes, BYTE *data_return)
{
    assert(data_return);
    assert(0 <= fromBytes);

    if (wantBytes <= 0 || nFrames <= fromBytes/bytesPerFrame) {
        return 0;
    }

    int copyFrames = wantBytes/bytesPerFrame;
    if (nFrames < (fromBytes/bytesPerFrame + copyFrames)) {
        copyFrames = (int)(nFrames - fromBytes/bytesPerFrame);
    }

    if (copyFrames <= 0) {
        // wantBytes is smaller than bytesPerFrame
        assert(0);
        return 0;
    }

    memcpy(data_return, &stream[fromBytes], copyFrames * bytesPerFrame);
    return copyFrames * bytesPerFrame;
}

void
WWPcmData::FillBufferStart(void)
{
    filledFrames = 0;
}

int
WWPcmData::FillBufferAddData(const BYTE *buff, int bytes)
{
    assert(buff);
    assert(0 <= bytes);

    int copyFrames = bytes / bytesPerFrame;
    if (nFrames - filledFrames < copyFrames) {
        copyFrames = (int)(nFrames - filledFrames);
    }

    if (copyFrames <= 0) {
        return 0;
    }

    memcpy(&stream[filledFrames*bytesPerFrame], buff, copyFrames * bytesPerFrame);
    filledFrames += copyFrames;
    return copyFrames * bytesPerFrame;
}

void
WWPcmData::FillBufferEnd(void)
{
    nFrames = filledFrames;
}

void
WWPcmData::FindSampleValueMinMax(float *minValue_return, float *maxValue_return)
{
    assert(sampleFormat == WWPcmDataSampleFormatSfloat);

    *minValue_return = 0.0f;
    *maxValue_return = 0.0f;
    if (0 == nFrames) {
        return;
    }

    float minValue = FLT_MAX;
    float maxValue = FLT_MIN;

    float *p = (float *)stream;
    for (int i=0; i<nFrames * nChannels; ++i) {
        float v = p[i];
        if (v < minValue) {
            minValue = v;
        }
        if (maxValue < v) {
            maxValue = v;
        }
    }

    *minValue_return = minValue;
    *maxValue_return = maxValue;
}

void
WWPcmData::ScaleSampleValue(float scale)
{
    assert(sampleFormat == WWPcmDataSampleFormatSfloat);

    float *p = (float *)stream;
    for (int i=0; i<nFrames * nChannels; ++i) {
        p[i] = p[i] * scale;
    }
}

void
WWPcmData::FillDopSilentData(void)
{
    int64_t writePos = 0;

    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                stream[writePos+0] = 0;
                stream[writePos+1] = 0x69;
                stream[writePos+2] = 0x69;
                stream[writePos+3] = (i&1) ? 0xfa : 0x05;
                writePos += 4;
            }
        }
        streamType = WWStreamDop;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                stream[writePos+0] = 0x69;
                stream[writePos+1] = 0x69;
                stream[writePos+2] = (i&1) ? 0xfa : 0x05;
                writePos += 3;
            }
        }
        streamType = WWStreamDop;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }
}

static const unsigned char gBitsSetTable256[256] = 
{
#   define B2(n) n,     n+1,     n+1,     n+2
#   define B4(n) B2(n), B2(n+1), B2(n+1), B2(n+2)
#   define B6(n) B4(n), B4(n+1), B4(n+1), B4(n+2)
    B6(0), B6(1), B6(1), B6(2)
};
#undef B6
#undef B4
#undef B2

/// @param availableBits 0以上64以下の整数。
/// @return -1.0 to 1.0f
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

/// @param availableBits 8以上64以下の8の倍数である必要がある。
/// @return -128 to +127
static int8_t
DsdStreamToAmplitudeInt8(uint64_t v, int availableBits)
{
    assert(0 < availableBits && (availableBits&7)==0);
    int bitCount = 0;

    for (int i=0; i<availableBits/8; ++i) {
        bitCount += gBitsSetTable256[v&0xff];
        v >>= 8;
    }
    if (availableBits <= bitCount) {
        bitCount = availableBits-1;
    }

    return (int8_t)((bitCount-availableBits/2) * (256/availableBits));
}

struct SmallDsdStreamInfo {
    uint64_t dsdStream;
    uint32_t availableBits;

    SmallDsdStreamInfo(void) {
        dsdStream = 0;
        availableBits = 0;
    }
};

/// 非常に音が悪いDop DSD→ PCM変換。
void
WWPcmData::DopToPcm(void)
{
    SmallDsdStreamInfo *dsdStreams = new SmallDsdStreamInfo[nChannels];
    if (NULL == dsdStreams) {
        assert(0);
        return;
    }

    int64_t pos = 0;
    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                p->dsdStream <<= 16;
                p->dsdStream += (stream[pos+2] << 8) + stream[pos+1];
                p->availableBits += 16;
                if (64 < p->availableBits) {
                    p->availableBits = 64;
                }

                int8_t pcmValue = DsdStreamToAmplitudeInt8(p->dsdStream, p->availableBits);

                stream[pos+0] = 0;
                stream[pos+1] = 0;
                stream[pos+2] = 0;
                stream[pos+3] = (BYTE)pcmValue;
                pos += 4;
            }
        }
        streamType = WWStreamPcm;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                p->dsdStream <<= 16;
                p->dsdStream += (stream[pos+1] << 8) + stream[pos];
                p->availableBits += 16;
                if (64 < p->availableBits) {
                    p->availableBits = 64;
                }

                int8_t pcmValue = DsdStreamToAmplitudeInt8(p->dsdStream, p->availableBits);

                stream[pos+0] = 0;
                stream[pos+1] = 0;
                stream[pos+2] = (BYTE)pcmValue;
                pos += 3;
            }
        }
        streamType = WWStreamPcm;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }

    delete [] dsdStreams;
    dsdStreams = NULL;
}

void
WWPcmData::CheckDopMarker(void)
{
    assert((nFrames&1)==0);
    int64_t pos = 0;
    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                assert(stream[pos+3] == ((i&1)?0xfa:0x05));
                pos += 4;
            }
        }
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                assert(stream[pos+2] == ((i&1)?0xfa:0x05));
                pos += 3;
            }
        }
        break;
    default:
        break;
    }
}

/// 非常に音が悪いPCM→Dop DSD変換。
void
WWPcmData::PcmToDop(void)
{
    int64_t pos = 0;
    SmallDsdStreamInfo *dsdStreams = new SmallDsdStreamInfo[nChannels];
    if (NULL == dsdStreams) {
        assert(0);
        return;
    }

    switch (sampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                short sv = (stream[pos+3]<<8) + stream[pos+2];

                float targetV = sv / 32768.0f;
                for (int c=0; c<16; ++c) {
                    uint32_t ampBits = p->availableBits;
                    if (64 == p->availableBits) {
                        // 今作っている16ビットのDSDデータをp->dsdStreamに詰めると
                        // 64ビットのデータのうち古いデータ16ビットが押し出されて消えるのでAmplitudeの計算から除外する。
                        ampBits = 48+c;
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
                unsigned short dsdValue16 = (unsigned short)p->dsdStream;

                stream[pos+0] = 0;
                stream[pos+1] = (BYTE)(0xff & dsdValue16);
                stream[pos+2] = (BYTE)(0xff & (dsdValue16>>8));
                stream[pos+3] = (i&1) ? 0xfa : 0x05;
                pos += 4;
            }
        }
        streamType = WWStreamDop;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<nFrames; ++i) {
            for (int ch=0; ch<nChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                short sv = (stream[pos+2]<<8) + stream[pos+1];

                float targetV = sv / 32768.0f;
                for (int c=0; c<16; ++c) {
                    uint32_t ampBits = p->availableBits;
                    if (64 == p->availableBits) {
                        // 今作っている16ビットのDSDデータをp->dsdStreamに詰めると
                        // 64ビットのデータのうち古いデータ16ビットが押し出されて消えるのでAmplitudeの計算から除外する。
                        ampBits = 48+c;
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
                unsigned short dsdValue16 = (unsigned short)p->dsdStream;

                stream[pos+0] = (BYTE)(0xff & dsdValue16);
                stream[pos+1] = (BYTE)(0xff & (dsdValue16>>8));
                stream[pos+2] = (i&1) ? 0xfa : 0x05;
                pos += 3;
            }
        }
        streamType = WWStreamDop;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }

    delete [] dsdStreams;
    dsdStreams = NULL;
}

static void
CopyStream(const WWPcmData &from, int64_t fromPosFrame, int64_t numFrames, WWPcmData &to)
{
    assert(from.bytesPerFrame == to.bytesPerFrame);

    int64_t copyFrames = numFrames;
    if (from.nFrames - fromPosFrame < copyFrames) {
        copyFrames = from.nFrames - fromPosFrame;
        if (copyFrames < 0) {
            copyFrames = 0;
        }
    }
    if (to.nFrames < copyFrames) {
        copyFrames = to.nFrames;
    }

    if (0 < copyFrames) {
        memcpy(to.stream, &from.stream[from.bytesPerFrame * fromPosFrame],
            from.bytesPerFrame * copyFrames);
    }
}

#define SPLICE_READ_FRAME_NUM (4)

int
WWPcmData::UpdateSpliceDataWithStraightLineDop(
        const WWPcmData &fromDop, int64_t fromPosFrame,
        const WWPcmData &toDop,   int64_t toPosFrame)
{
    WWPcmData fromPcm;
    WWPcmData toPcm;

    fromPcm.Init(-1, sampleFormat, nChannels, SPLICE_READ_FRAME_NUM, bytesPerFrame, contentType);
    fromPcm.FillDopSilentData();
    CopyStream(fromDop, fromPosFrame, SPLICE_READ_FRAME_NUM, fromPcm);
    fromPcm.DopToPcm();

    toPcm.Init(  -1, sampleFormat, nChannels, SPLICE_READ_FRAME_NUM, bytesPerFrame, contentType);
    toPcm.FillDopSilentData();
    CopyStream(toDop,   toPosFrame,   SPLICE_READ_FRAME_NUM, toPcm);
    toPcm.DopToPcm();

    int sampleCount = UpdateSpliceDataWithStraightLinePcm(
            fromPcm, SPLICE_READ_FRAME_NUM-1,
            toPcm,   SPLICE_READ_FRAME_NUM-1);

    PcmToDop();

    toPcm.Term();
    fromPcm.Term();

    posFrame = 0;

    return sampleCount;
}

int
WWPcmData::UpdateSpliceDataWithStraightLine(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    switch (streamType) {
    case WWStreamPcm:
        return UpdateSpliceDataWithStraightLinePcm(fromPcm, fromPosFrame, toPcm, toPosFrame);
    case WWStreamDop:
        return UpdateSpliceDataWithStraightLineDop(fromPcm, fromPosFrame, toPcm, toPosFrame);
    default:
        assert(0);
        return 0;
    }
}

// クロスフェードデータを作る。
// this->posFrameの頭出しもする。
// @return クロスフェードデータのためにtoDopのtoPosFrameから消費したフレーム数。
int
WWPcmData::CreateCrossfadeData(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    switch (streamType) {
    case WWStreamPcm:
        return CreateCrossfadeDataPcm(fromPcm, fromPosFrame, toPcm, toPosFrame);
    case WWStreamDop:
        return UpdateSpliceDataWithStraightLineDop(fromPcm, fromPosFrame, toPcm, toPosFrame);
    default:
        assert(0);
        return 0;
    }
}

WWPcmData *
WWPcmData::AdvanceFrames(WWPcmData *pcmData, int64_t skipFrames)
{
    while (0 < skipFrames) {
        int64_t advance = skipFrames;
        if (pcmData->AvailableFrames() <= advance) {
            advance = pcmData->AvailableFrames();

            // 頭出ししておく。
            pcmData->posFrame = 0;

            pcmData = pcmData->next;

            pcmData->posFrame = 0;
        } else {
            pcmData->posFrame += advance;
        }

        skipFrames -= advance;
    }
    return pcmData;
}