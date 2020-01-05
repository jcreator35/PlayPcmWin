// 日本語 UTF-8

#include "WWPcmData.h"
#include "WWCommonUtil.h"
#include <assert.h>
#include <malloc.h>
#include <stdint.h>
#include <float.h>
#include "WWSdmToPcm.h"
#include "WWPcmToSdm.h"
#include <stdint.h>
#include <array>

#define SPLICE_NOISE_SAMPLES   (10)
#define SPLICE_READ_FRAME_NUM  (10)

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
    //assert(0 <= (int)w && (int)w < WWPcmDataSampleFormatNUM);

    switch (w) {
    case WWPcmDataSampleFormatSint16: return "Sint16";
    case WWPcmDataSampleFormatSint24: return "Sint24";
    case WWPcmDataSampleFormatSint32V24: return "Sint32V24";
    case WWPcmDataSampleFormatSint32: return "Sint32";
    case WWPcmDataSampleFormatSfloat: return "Sfloat";
    case WWPcmDataSampleFormatSdouble: return "Sdouble";
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
        if (bitsPerSample == 64 &&
            validBitsPerSample == 64) {
            return WWPcmDataSampleFormatSdouble;
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
        = { 16, 24, 32, 32, 32, 64 };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return -1;
    }
    return result[t];
}

int
WWPcmDataSampleFormatTypeToBytesPerSample(WWPcmDataSampleFormatType t)
{
    static const int result[WWPcmDataSampleFormatNUM]
        = { 2, 3, 4, 4, 4, 8 };

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
        = { 16, 24, 24, 32, 32, 64 };

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
        = { false, false, false, false, true, true };

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
        = { true, true, true, true, false, false };

    if (t < 0 || WWPcmDataSampleFormatNUM <= t) {
        assert(0);
        return false;
    }
    return result[t];
}

int
WWPcmData::GetSampleValueInt(int ch, int64_t posFrame) const
{
    assert(mSampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return 0;
    }

    int result = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            short *p = (short*)(&mStream[2 * (mChannels * posFrame + ch)]);
            result = *p;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            // bus error回避。x86にはbus error無いけど一応。
            unsigned char *p =
                (unsigned char*)(&mStream[3 * (mChannels * posFrame + ch)]);

            result =
                (((unsigned int)p[0])<<8) +
                (((unsigned int)p[1])<<16) +
                (((unsigned int)p[2])<<24);
            result /= 256;
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
            result = ((*p)/256);
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
            result = *p;
        }
        break;
    default:
        assert(0);
        break;
    }

    return result;
}

static float
SaturateForInt24(const float v) {
    if (v < -1.0f) {
        return -1.0f;
    }
    if (8388607.0f / 8388608.0f < v) {
        return 8388607.0f / 8388608.0f;
    }
    return v;
}

int
WWPcmData::GetSampleValueAsInt24(int ch, int64_t posFrame) const
{
    assert(mSampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return 0;
    }

    int result = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            short *p = (short*)(&mStream[2 * (mChannels * posFrame + ch)]);
            result = *p;
            result <<= 8;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[3 * (mChannels * posFrame + ch)]);

            result =
                (((unsigned int)p[0])<<8) +
                (((unsigned int)p[1])<<16) +
                (((unsigned int)p[2])<<24);
            result >>= 8;
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
            result = *p;
            result >>= 8;
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
            result = *p;
            result >>= 8;
        }
        break;
    case WWPcmDataSampleFormatSfloat:
        {
            float *p = (float*)(&mStream[4 * (mChannels * posFrame + ch)]);
            float v = SaturateForInt24(*p);
            result = (int)(8388608.0f * v);
        }
        break;
    case WWPcmDataSampleFormatSdouble:
        {
            double *p = (double*)(&mStream[8 * (mChannels * posFrame + ch)]);
            float v = (float)*p;
            v = SaturateForInt24(v);
            result = (int)(8388608.0f * v);
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
    assert(mSampleFormat == WWPcmDataSampleFormatSfloat
        || mSampleFormat == WWPcmDataSampleFormatSdouble);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return 0;
    }

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSfloat:
        {
            float *p = (float *)(&mStream[4 * (mChannels * posFrame + ch)]);
            return *p;
        }
    case WWPcmDataSampleFormatSdouble:
        {
            double *p = (double *)(&mStream[8 * (mChannels * posFrame + ch)]);
            return (float)(*p);
        }
    default:
        assert(0);
        return 0;
    }
}

float
WWPcmData::GetSampleValueAsFloat(int ch, int64_t posFrame) const
{
    float result = 0.0f;

    switch (mSampleFormat) {
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
    case WWPcmDataSampleFormatSdouble:
        result = GetSampleValueFloat(ch, posFrame);
        break;
    default:
        assert(0);
        break;
    }
    return result;
}

bool
WWPcmData::SetSampleValueInt(int ch, int64_t posFrame, int v)
{
    assert(mSampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return false;
    }

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            short *p =
                (short*)(&mStream[2 * (mChannels * posFrame + ch)]);
            *p = (short)v;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[3 * (mChannels * posFrame + ch)]);
            p[0] = (unsigned char)(v & 0xff);
            p[1] = (unsigned char)((v>>8) & 0xff);
            p[2] = (unsigned char)((v>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[4 * (mChannels * posFrame + ch)]);
            p[0] = 0;
            p[1] = (unsigned char)(v & 0xff);
            p[2] = (unsigned char)((v>>8) & 0xff);
            p[3] = (unsigned char)((v>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
            *p = v;
        }
        break;
    default:
        assert(0);
        break;
    }

    return true;
}

bool
WWPcmData::SetSampleValueAsInt24(int ch, int64_t posFrame, int v)
{
    assert(mSampleFormat != WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return false;
    }

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint16:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[2 * (mChannels * posFrame + ch)]);
            p[0] = (unsigned char)((v>>8) & 0xff);
            p[1] = (unsigned char)((v>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[3 * (mChannels * posFrame + ch)]);
            p[0] = (unsigned char)(v & 0xff);
            p[1] = (unsigned char)((v>>8) & 0xff);
            p[2] = (unsigned char)((v>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[4 * (mChannels * posFrame + ch)]);
            p[0] = 0;
            p[1] = (unsigned char)(v & 0xff);
            p[2] = (unsigned char)((v>>8) & 0xff);
            p[3] = (unsigned char)((v>>16) & 0xff);
        }
        break;
    default:
        assert(0);
        break;
    }

    return true;
}

bool
WWPcmData::SetSampleValueFloat(int ch, int64_t posFrame, float v)
{
    assert(mSampleFormat == WWPcmDataSampleFormatSfloat
        || mSampleFormat == WWPcmDataSampleFormatSdouble);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return false;
    }

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSfloat:
        {
            float *p = (float *)(&mStream[4 * (mChannels * posFrame + ch)]);
            *p = v;
        }
        return true;
    case WWPcmDataSampleFormatSdouble:
        {
            double *p = (double *)(&mStream[8 * (mChannels * posFrame + ch)]);
            *p = v;
        }
        return true;
    default:
        assert(0);
        return false;
    }
}

bool
WWPcmData::SetSampleValueAsFloat(int ch, int64_t posFrame, float v)
{
    bool result = false;

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint16:
        result = SetSampleValueInt(ch, posFrame, (int)(v * 32768.0f));
        break;
    case WWPcmDataSampleFormatSint24:
    case WWPcmDataSampleFormatSint32V24:
        result = SetSampleValueInt(ch, posFrame, (int)(v * 8388608.0f));
        break;
    case WWPcmDataSampleFormatSint32:
        result = SetSampleValueInt(ch, posFrame, (int)(v * 2147483648.0f));
        break;
    case WWPcmDataSampleFormatSfloat:
    case WWPcmDataSampleFormatSdouble:
        result = SetSampleValueFloat(ch, posFrame, v);
        break;
    default:
        assert(0);
        break;
    }
    return result;
}

bool
WWPcmData::ScaleSampleValue(float scale)
{
    if (mStreamType == WWStreamDop) {
        // 未対応: DoPの音量をこのアルゴリズムで変えたら再生が出来なくなる。
        return false;
    }

    if (mSampleFormat == WWPcmDataSampleFormatSfloat) {
        // float

        float *p = (float *)mStream;
        for (int64_t i=0; i<mFrames * mChannels; ++i) {
            p[i] = p[i] * scale;
        }
        return true;
    }
    if (mSampleFormat == WWPcmDataSampleFormatSdouble) {
        // double

        double *p = (double *)mStream;
        for (int64_t i=0; i<mFrames * mChannels; ++i) {
            p[i] = p[i] * scale;
        }
        return true;
    }

    // 整数の場合。
    for (int64_t pos=0; pos<mFrames; ++pos) {
        for (int ch=0; ch<mChannels; ++ch) {
            double v = GetSampleValueInt(ch,pos);
            SetSampleValueInt(ch, pos, (int)(v*scale));
        }
    }
    return true;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

void
WWPcmData::Term(void)
{
    dprintf("D: %s() mStream=%p\n", __FUNCTION__, mStream);

    free(mStream);
    mStream = nullptr;
}

void
WWPcmData::CopyFrom(WWPcmData *rhs)
{
    *this = *rhs;

    mNext = nullptr;

    int64_t bytes = mFrames * mBytesPerFrame;
    assert(0 < bytes);

    mStream = (BYTE*)malloc(bytes);
    CopyMemory(mStream, rhs->mStream, bytes);
}

bool
WWPcmData::Init(
        int aId, WWPcmDataSampleFormatType asampleFormat, int anChannels,
        int64_t anFrames, int aframeBytes,
        WWPcmDataContentType acontentType, WWStreamType aStreamType)
{
    assert(mStream == nullptr);

    mId           = aId;
    mSampleFormat = asampleFormat;
    mContentType  = acontentType;
    mNext         = nullptr;
    mPosFrame     = 0;
    mChannels    = anChannels;
    // メモリ確保に成功してからフレーム数をセットする。
    mFrames       = 0;
    mBytesPerFrame = aframeBytes;
    mStream        = nullptr;
    mStreamType    = aStreamType;

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
    if (nullptr == p) {
        // 失敗…
        return false;
    }

    ZeroMemory(p, bytes);
    mFrames = anFrames;
    mStream = p;

    return true;
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
    assert(0 < mFrames && mFrames <= 0x7fffffff);

    switch (fromPcm.mSampleFormat) {
    case WWPcmDataSampleFormatSfloat:
    case WWPcmDataSampleFormatSdouble:
        {
            // floatは、簡単。
            PcmSpliceInfoFloat *p =
                (PcmSpliceInfoFloat*)_malloca(mChannels * sizeof(PcmSpliceInfoFloat));
            assert(p);

            for (int ch=0; ch<mChannels; ++ch) {
                float y0 = fromPcm.GetSampleValueFloat(ch, fromPosFrame);
                float y1 = toPcm.GetSampleValueFloat(ch, toPosFrame);
                p[ch].dydx = (y1 - y0)/(mFrames);
                p[ch].y = y0;
            }

            for (int x=0; x<mFrames; ++x) {
                for (int ch=0; ch<mChannels; ++ch) {
                    SetSampleValueFloat(ch, x, p[ch].y);
                    p[ch].y += p[ch].dydx;
                }
            }

            _freea(p);
            p = nullptr;
        }
        break;
    default:
        {
            // Bresenham's line algorithm的な物
            PcmSpliceInfoInt *p =
                (PcmSpliceInfoInt*)_malloca(mChannels * sizeof(PcmSpliceInfoInt));
            assert(p);

            for (int ch=0; ch<mChannels; ++ch) {
                int y0 = fromPcm.GetSampleValueInt(ch, fromPosFrame);
                int y1 = toPcm.GetSampleValueInt(ch, toPosFrame);
                p[ch].deltaX = (int)mFrames;
                p[ch].error  = p[ch].deltaX/2;
                p[ch].ystep  = ((int64_t)y1 - y0)/p[ch].deltaX;
                p[ch].deltaError = abs(y1 - y0) - abs(p[ch].ystep * p[ch].deltaX);
                p[ch].deltaErrorDirection = (y1-y0) >= 0 ? 1 : -1;
                p[ch].y = y0;
            }

            for (int x=0; x<(int)mFrames; ++x) {
                for (int ch=0; ch<mChannels; ++ch) {
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
            p = nullptr;
        }
        break;
    }

    mPosFrame = 0;

    return 0;
}

int
WWPcmData::CreateCrossfadeDataPcm(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    assert(0 < mFrames && mFrames <= 0x7fffffff);

    for (int ch=0; ch<mChannels; ++ch) {
        const WWPcmData *pcm0 = &fromPcm;
        int64_t pcm0Pos = fromPosFrame;

        const WWPcmData *pcm1 = &toPcm;
        int64_t pcm1Pos = toPosFrame;

        for (int x=0; x<mFrames; ++x) {
            float ratio = (float)x / mFrames;

            float y0 = pcm0->GetSampleValueAsFloat(ch, pcm0Pos);
            float y1 = pcm1->GetSampleValueAsFloat(ch, pcm1Pos);

            SetSampleValueAsFloat(ch, x, y0 * (1.0f - ratio) + y1 * ratio);

            ++pcm0Pos;
            if (pcm0->mFrames <= pcm0Pos && nullptr != pcm0->mNext) {
                pcm0 = pcm0->mNext;
                pcm0Pos = 0;
            }

            ++pcm1Pos;
            if (pcm1->mFrames <= pcm1Pos && nullptr != pcm1->mNext) {
                pcm1 = pcm1->mNext;
                pcm1Pos = 0;
            }
        }
    }

    mPosFrame = 0;

    // クロスフェードのPCMデータは2GBもない(assertでチェックしている)。intにキャストする。
    return (int)mFrames; 
}

int
WWPcmData::CreateCrossfadeDataDop(
        const WWPcmData &fromDop, int64_t fromPosFrame,
        const WWPcmData &toDop,   int64_t toPosFrame)
{
    assert(0 < mFrames && mFrames <= 0x7fffffff);

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32:
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint24:
        // DoPの処理が可能なフォーマット。
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        return (int)mFrames;
    }

    WWPcmData fromPcm;
    WWPcmData toPcm;

    fromPcm.Init(-1, mSampleFormat, mChannels, mFrames, mBytesPerFrame, mContentType, WWStreamDop);
    toPcm.Init(  -1, mSampleFormat, mChannels, mFrames, mBytesPerFrame, mContentType, WWStreamDop);

    int * firstPart = new int[SPLICE_NOISE_SAMPLES*mChannels];
    int firstPartPos = 0;

    int * lastPart  = new int[SPLICE_NOISE_SAMPLES*mChannels];
    int lastPartPos = 0;

    // サンプルデータを詰める。
    {
        const WWPcmData *pcm0 = &fromDop;
        int64_t pcm0Pos = fromPosFrame;

        const WWPcmData *pcm1 = &toDop;
        int64_t pcm1Pos = toPosFrame;

        for (int x=0; x<(int)fromPcm.Frames(); ++x) {
            for (int ch=0; ch<mChannels; ++ch) {

                int y0 = pcm0->GetSampleValueInt(ch, pcm0Pos);
                fromPcm.SetSampleValueInt(ch, x, y0);
                if (x < SPLICE_NOISE_SAMPLES) {
                    firstPart[firstPartPos++] = y0;
                }

                int y1 = pcm1->GetSampleValueInt(ch, pcm1Pos);
                toPcm.SetSampleValueInt(ch, x, y1);
                if (mFrames-SPLICE_NOISE_SAMPLES <= x) {
                    lastPart[lastPartPos++] = y1;
                }
            }

            ++pcm0Pos;
            if (pcm0->mFrames <= pcm0Pos && nullptr != pcm0->mNext) {
                pcm0 = pcm0->mNext;
                pcm0Pos = 0;
            }

            ++pcm1Pos;
            if (pcm1->mFrames <= pcm1Pos && nullptr != pcm1->mNext) {
                pcm1 = pcm1->mNext;
                pcm1Pos = 0;
            }
        }
    }

    fromPcm.DopToPcmFast();
    toPcm.DopToPcmFast();

    // DopToPcmでfromPcm->mFrames (== toPcm->mFrames)が変化するので、this->mFramesをそれに合わせる。
    mFrames = (int)fromPcm.Frames();

    for (int x=0; x<mFrames; ++x) {
        for (int ch=0; ch<mChannels; ++ch) {
            float ratio = (float)x / mFrames;
            float y0 = fromPcm.GetSampleValueAsFloat(ch, x);
            float y1 = toPcm.GetSampleValueAsFloat(ch, x);
            float y = y0 * (1.0f - ratio) + y1 * ratio;

            SetSampleValueAsFloat(ch, x, y);

            //printf("%d %f * %f + %f * %f = %f\n", x, (1.0f - ratio), y0, ratio, y1, y);
        }
    }

    PcmToDopFast();

    // PcmToDopでmFramesが変化することに注意。

    if (SPLICE_NOISE_SAMPLES * 10 < mFrames) {
        // SPLICE_NOISE_SAMPLESの10倍以上のサンプル数があるとき、両端をオリジナルデータで上書き。
        // Sdm → Pcm → Sdm変換で最初10サンプルが荒れるので。

        firstPartPos = 0;
        for (int x=0; x<SPLICE_NOISE_SAMPLES; ++x) {
            for (int ch=0; ch<mChannels; ++ch) {
                int y = firstPart[firstPartPos++];
                SetSampleValueInt(ch, x, y);
            }
        }

       lastPartPos = 0;
        for (int x=(int)mFrames-SPLICE_NOISE_SAMPLES; x<(int)mFrames; ++x) {
            for (int ch=0; ch<mChannels; ++ch) {
                int y = lastPart[lastPartPos++];
                SetSampleValueInt(ch, x, y);
            }
        }
    }

    delete [] lastPart;
    lastPart = nullptr;

    delete [] firstPart;
    firstPart = nullptr;

    toPcm.Term();
    fromPcm.Term();

    mPosFrame = 0;

    // クロスフェードのPCMデータは2GBもない(assertでチェックしている)。intにキャストする。
    return (int)mFrames; 
}

int
WWPcmData::GetBufferData(int64_t fromBytes, int wantBytes, BYTE *data_return)
{
    assert(data_return);
    assert(0 <= fromBytes);

    if (wantBytes <= 0 || mFrames <= fromBytes/mBytesPerFrame) {
        return 0;
    }

    int copyFrames = wantBytes/mBytesPerFrame;
    if (mFrames < (fromBytes/mBytesPerFrame + copyFrames)) {
        copyFrames = (int)(mFrames - fromBytes/mBytesPerFrame);
    }

    if (copyFrames <= 0) {
        // wantBytes is smaller than bytesPerFrame
        assert(0);
        return 0;
    }

    memcpy(data_return, &mStream[fromBytes], copyFrames * mBytesPerFrame);
    return copyFrames * mBytesPerFrame;
}

void
WWPcmData::FillBufferStart(void)
{
    mFilledFrames = 0;
}

int
WWPcmData::FillBufferAddData(const BYTE *buff, int bytes)
{
    assert(buff);
    assert(0 <= bytes);

    int copyFrames = bytes / mBytesPerFrame;
    if (mFrames - mFilledFrames < copyFrames) {
        copyFrames = (int)(mFrames - mFilledFrames);
    }

    if (copyFrames <= 0) {
        return 0;
    }

    memcpy(&mStream[mFilledFrames*mBytesPerFrame], buff, copyFrames * mBytesPerFrame);
    mFilledFrames += copyFrames;
    return copyFrames * mBytesPerFrame;
}

void
WWPcmData::FillBufferEnd(void)
{
    mFrames = mFilledFrames;
}

void
WWPcmData::FindSampleValueMinMax(float *minValue_return, float *maxValue_return)
{
    assert(mSampleFormat == WWPcmDataSampleFormatSfloat);

    *minValue_return = 0.0f;
    *maxValue_return = 0.0f;
    if (0 == mFrames) {
        return;
    }

    float minValue = FLT_MAX;
    float maxValue = FLT_MIN;

    float *p = (float *)mStream;
    for (int i=0; i<mFrames * mChannels; ++i) {
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
WWPcmData::FillDopSilentData(void)
{
    int64_t writePos = 0;

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                mStream[writePos+0] = 0;
                mStream[writePos+1] = 0x69;
                mStream[writePos+2] = 0x69;
                mStream[writePos+3] = (i&1) ? 0xfa : 0x05;
                writePos += 4;
            }
        }
        mStreamType = WWStreamDop;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                mStream[writePos+0] = 0x69;
                mStream[writePos+1] = 0x69;
                mStream[writePos+2] = (i&1) ? 0xfa : 0x05;
                writePos += 3;
            }
        }
        mStreamType = WWStreamDop;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }
}

static float
SaturateFloat(const float v)
{
    if (v < -1.0f) {
        return -1.0f;
    }
    if (8388607.0f / 8388608.0f < v) {
        return 8388607.0f / 8388608.0f;
    }
    return v;
}

/// Dop DSD→ PCM変換。
void
WWPcmData::DopToPcmFast(void)
{
    WWSdmToPcm *sp = new WWSdmToPcm[mChannels];
    if (nullptr == sp) {
        assert(0);
        return;
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWSdmToPcm *p = &sp[ch];
        p->Start((int)(mFrames/4));
    }

    for (int64_t i=0; i<mFrames; ++i) {
        for (int ch=0; ch<mChannels; ++ch) {
            const int v = GetSampleValueAsInt24(ch, i);
            const uint16_t inSdm = (uint16_t)v;

            WWSdmToPcm *p = &sp[ch];
            p->AddInputSamples(inSdm);
        }
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWSdmToPcm *p = &sp[ch];
        p->Drain();
    }

    // PCMサンプル数が4分の1に減る。
    mFrames /= 4;
    mStreamType = WWStreamPcm;

    for (int64_t i=0; i<mFrames; ++i) {
        for (int ch=0; ch<mChannels; ++ch) {
            const WWSdmToPcm *p = &sp[ch];
            float v=p->GetOutputPcm()[i];
            v = SaturateFloat(v);
            SetSampleValueAsFloat(ch,i,v);
        }
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWSdmToPcm *p = &sp[ch];
        p->End();
    }

    delete [] sp;
    sp = nullptr;
}

void
WWPcmData::PcmToDopFast(void)
{
    WWPcmToSdm *ps = new WWPcmToSdm[mChannels];

    if (nullptr == ps) {
        assert(0);
        return;
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWPcmToSdm *p = &ps[ch];
        p->Start((int)(mFrames*64));
    }

    int64_t pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWPcmToSdm *p = &ps[ch];
                int vI = (mStream[pos+3]<<24)
                        + (mStream[pos+2]<<16)
                        + (mStream[pos+1]<<8);
                float vF = (float)vI / 2147483648.0f;
                p->AddInputSamples(&vF, 1);
                pos += 4;
            }
        }
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWPcmToSdm *p = &ps[ch];
                int vI = (mStream[pos+2]<<24)
                        + (mStream[pos+1]<<16)
                        + (mStream[pos+0]<<8);
                float vF = (float)vI / 2147483648.0f;
                p->AddInputSamples(&vF, 1);
                pos += 3;
            }
        }
        break;
    default:
        assert(0);
        break;
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWPcmToSdm *p = &ps[ch];
        p->Drain();
    }

    pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames*4; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWPcmToSdm *p = &ps[ch];

                const uint16_t v = p->GetOutputSdm()[i];

                mStream[pos+0] = 0;
                mStream[pos+1] = (BYTE)(0xff & v);
                mStream[pos+2] = (BYTE)(0xff & (v>>8));
                mStream[pos+3] = (i&1) ? 0xfa : 0x05;
                pos += 4;
            }
        }
        mFrames *= 4;
        mStreamType = WWStreamDop;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames*4; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWPcmToSdm *p = &ps[ch];

                const uint16_t v = p->GetOutputSdm()[i];

                mStream[pos+0] = (BYTE)(0xff & v);
                mStream[pos+1] = (BYTE)(0xff & (v>>8));
                mStream[pos+2] = (i&1) ? 0xfa : 0x05;
                pos += 3;
            }
        }
        mFrames *= 4;
        mStreamType = WWStreamDop;
        break;
    default:
        assert(0);
        break;
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWPcmToSdm *p = &ps[ch];
        p->End();
    }

    delete [] ps;
    ps = nullptr;
}

void
WWPcmData::CheckDopMarker(void)
{
    if (mStreamType != WWStreamDop) {
        return;
    }

    assert((mFrames&1)==0);
    int64_t pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32:
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                assert(mStream[pos+3] == ((i&1)?0xfa:0x05));
                pos += 4;
            }
        }
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                assert(mStream[pos+2] == ((i&1)?0xfa:0x05));
                pos += 3;
            }
        }
        break;
    default:
        break;
    }
}

static void
CopyStream(const WWPcmData &from, int64_t fromPosFrame, int64_t numFrames, WWPcmData &to)
{
    assert(from.BytesPerFrame() == to.BytesPerFrame());

    int64_t copyFrames = numFrames;
    if (from.Frames() - fromPosFrame < copyFrames) {
        copyFrames = from.Frames() - fromPosFrame;
        if (copyFrames < 0) {
            copyFrames = 0;
        }
    }
    if (to.Frames() < copyFrames) {
        copyFrames = to.Frames();
    }

    if (0 < copyFrames) {
        memcpy(to.Stream(), &(from.Stream()[from.BytesPerFrame() * fromPosFrame]),
            from.BytesPerFrame() * copyFrames);
    }
}

int
WWPcmData::UpdateSpliceDataWithStraightLineDop(
        const WWPcmData &fromDop, int64_t fromPosFrame,
        const WWPcmData &toDop,   int64_t toPosFrame)
{
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32:
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint24:
        // DoPの処理が可能なフォーマット。
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        return (int)mFrames;
    }

    int * firstPart = new int[SPLICE_READ_FRAME_NUM*mChannels];
    int firstPartPos = 0;

    int * lastPart  = new int[SPLICE_READ_FRAME_NUM*mChannels];
    int lastPartPos = 0;

    WWPcmData fromPcm;
    WWPcmData toPcm;

    fromPcm.Init(-1, mSampleFormat, mChannels, SPLICE_READ_FRAME_NUM, mBytesPerFrame, mContentType, WWStreamPcm);
    fromPcm.FillDopSilentData();
    CopyStream(fromDop, fromPosFrame, SPLICE_READ_FRAME_NUM, fromPcm);
    for (int x=0; x<SPLICE_READ_FRAME_NUM; ++x) {
        for (int ch=0; ch<mChannels; ++ch) {
            firstPart[firstPartPos++] = fromPcm.GetSampleValueAsInt24(ch, x);
        }
    }

    toPcm.Init(  -1, mSampleFormat, mChannels, SPLICE_READ_FRAME_NUM, mBytesPerFrame, mContentType, WWStreamPcm);
    toPcm.FillDopSilentData();
    CopyStream(toDop,   toPosFrame,   SPLICE_READ_FRAME_NUM, toPcm);
    for (int x=0; x<SPLICE_READ_FRAME_NUM; ++x) {
        for (int ch=0; ch<mChannels; ++ch) {
            lastPart[lastPartPos++] = toPcm.GetSampleValueAsInt24(ch, x);
        }
    }

    fromPcm.DopToPcmFast();
    toPcm.DopToPcmFast();

    // PcmToDopFast()がmFramesを4倍する。
    // PCMを1/4で作る。
    mFrames /= 4;

    int sampleCount = UpdateSpliceDataWithStraightLinePcm(
            fromPcm, SPLICE_READ_FRAME_NUM-1,
            toPcm,   SPLICE_READ_FRAME_NUM-1);

    PcmToDopFast();


    // Sdm → Pcm → Sdm変換で最初10サンプルが荒れるので。

    firstPartPos = 0;
    for (int x=0; x<SPLICE_READ_FRAME_NUM; ++x) {
        for (int ch=0; ch<mChannels; ++ch) {
            int y = firstPart[firstPartPos++];
            SetSampleValueAsInt24(ch, x, y);
        }
    }

    lastPartPos = 0;
    for (int x=(int)mFrames-SPLICE_READ_FRAME_NUM; x<(int)mFrames; ++x) {
        for (int ch=0; ch<mChannels; ++ch) {
            int y = lastPart[lastPartPos++];
            SetSampleValueAsInt24(ch, x, y);
        }
    }

    delete [] lastPart;
    lastPart = nullptr;

    delete [] firstPart;
    firstPart = nullptr;

    toPcm.Term();
    fromPcm.Term();

    mPosFrame = 0;

    return sampleCount;
}

int
WWPcmData::UpdateSpliceDataWithStraightLine(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame)
{
    switch (fromPcm.StreamType()) {
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
    switch (fromPcm.StreamType()) {
    case WWStreamPcm:
        return CreateCrossfadeDataPcm(fromPcm, fromPosFrame, toPcm, toPosFrame);
    case WWStreamDop:
        return CreateCrossfadeDataDop(fromPcm, fromPosFrame, toPcm, toPosFrame);
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
            pcmData->SetPosFrame(0);

            pcmData = pcmData->mNext;

            pcmData->SetPosFrame(0);
        } else {
            pcmData->SetPosFrame(pcmData->PosFrame() + advance);
        }

        skipFrames -= advance;
    }
    return pcmData;
}
