// 日本語 UTF-8

#include "WWPcmData.h"
#include "WWUtil.h"
#include <assert.h>
#include <malloc.h>
#include <stdint.h>
#include <float.h>
#include "WWSdmToPcm.h"
#include "WWPcmToSdm.h"
#include <stdint.h>
#include <array>

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
WWPcmDataSampleFormatTypeToBytesPerSample(WWPcmDataSampleFormatType t)
{
    static const int result[WWPcmDataSampleFormatNUM]
        = { 2, 3, 4, 4, 4 };

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

float
WWPcmData::GetSampleValueFloat(int ch, int64_t posFrame) const
{
    assert(mSampleFormat == WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return 0;
    }

    float *p = (float *)(&mStream[4 * (mChannels * posFrame + ch)]);
    return *p;
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
            *p = (short)value;
        }
        break;
    case WWPcmDataSampleFormatSint24:
        {
            // bus error回避。x86にはbus error無いけど一応。
            unsigned char *p =
                (unsigned char*)(&mStream[3 * (mChannels * posFrame + ch)]);
            p[0] = (unsigned char)(value & 0xff);
            p[1] = (unsigned char)((value>>8) & 0xff);
            p[2] = (unsigned char)((value>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            unsigned char *p =
                (unsigned char*)(&mStream[4 * (mChannels * posFrame + ch)]);
            p[0] = 0;
            p[1] = (unsigned char)(value & 0xff);
            p[2] = (unsigned char)((value>>8) & 0xff);
            p[3] = (unsigned char)((value>>16) & 0xff);
        }
        break;
    case WWPcmDataSampleFormatSint32:
        {
            // bus errorは起きない。
            int *p = (int*)(&mStream[4 * (mChannels * posFrame + ch)]);
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
    assert(mSampleFormat == WWPcmDataSampleFormatSfloat);
    assert(0 <= ch && ch < mChannels);

    if (posFrame < 0 ||
        mFrames <= posFrame) {
        return false;
    }

    float *p = (float *)(&mStream[4 * (mChannels * posFrame + ch)]);
    *p = value;
    return true;
}

bool
WWPcmData::SetSampleValueAsFloat(int ch, int64_t posFrame, float value)
{
    bool result = false;

    switch (mSampleFormat) {
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
    assert(0 < mFrames && mFrames <= 0x7fffffff);

    switch (fromPcm.mSampleFormat) {
    case WWPcmDataSampleFormatSfloat:
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

#define SPLICE_NOISE_SAMPLES (10)

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

    int * first10 = new int[SPLICE_NOISE_SAMPLES*mChannels];
    int first10Pos = 0;

    int * last10  = new int[SPLICE_NOISE_SAMPLES*mChannels];
    int last10Pos = 0;

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
                    first10[first10Pos++] = y0;
                }

                int y1 = pcm1->GetSampleValueInt(ch, pcm1Pos);
                toPcm.SetSampleValueInt(ch, x, y1);
                if (mFrames-SPLICE_NOISE_SAMPLES <= x) {
                    last10[last10Pos++] = y1;
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

            //printf("%d %f %f %f\n", x, y0, y1, y);
        }
    }

    PcmToDopFast();

    // PcmToDopでmFramesが変化することに注意。

    if (SPLICE_NOISE_SAMPLES * 10 < mFrames) {
        // Sdm → Pcm → Sdm変換で最初10サンプルが荒れるので。

        int from10Pos = 0;
        for (int x=0; x<SPLICE_NOISE_SAMPLES; ++x) {
            for (int ch=0; ch<mChannels; ++ch) {
                int y = first10[from10Pos++];
                SetSampleValueInt(ch, x, y);
            }
        }

        int last10Pos = 0;
        for (int x=(int)mFrames-10; x<(int)mFrames; ++x) {
            for (int ch=0; ch<mChannels; ++ch) {
                int y = last10[last10Pos++];
                SetSampleValueInt(ch, x, y);
            }
        }
    }

    delete [] last10;
    last10 = nullptr;

    delete [] first10;
    first10 = nullptr;

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

bool
WWPcmData::ScaleSampleValue(float scale)
{
    if (mStreamType == WWStreamDop) {
        // 未対応: DoPの音量をこのアルゴリズムで変えたら再生が出来なくなる。
        return false;
    }

    if (mSampleFormat == WWPcmDataSampleFormatSfloat) {
        // floatの場合。

        float *p = (float *)mStream;
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



#if 0

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
    SmallDsdStreamInfo *dsdStreams = new SmallDsdStreamInfo[mChannels];
    if (nullptr == dsdStreams) {
        assert(0);
        return;
    }

    int64_t pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32:
    case WWPcmDataSampleFormatSint32V24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                p->dsdStream <<= 16;
                p->dsdStream += (mStream[pos+2] << 8) + mStream[pos+1];
                p->availableBits += 16;
                if (64 < p->availableBits) {
                    p->availableBits = 64;
                }

                int8_t pcmValue = DsdStreamToAmplitudeInt8(p->dsdStream, p->availableBits);

                mStream[pos+0] = 0;
                mStream[pos+1] = 0;
                mStream[pos+2] = 0;
                mStream[pos+3] = (BYTE)pcmValue;
                pos += 4;
            }
        }
        mStreamType = WWStreamPcm;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                p->dsdStream <<= 16;
                p->dsdStream += (mStream[pos+1] << 8) + mStream[pos];
                p->availableBits += 16;
                if (64 < p->availableBits) {
                    p->availableBits = 64;
                }

                int8_t pcmValue = DsdStreamToAmplitudeInt8(p->dsdStream, p->availableBits);

                mStream[pos+0] = 0;
                mStream[pos+1] = 0;
                mStream[pos+2] = (BYTE)pcmValue;
                pos += 3;
            }
        }
        mStreamType = WWStreamPcm;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }

    delete [] dsdStreams;
    dsdStreams = nullptr;
}

/// 非常に音が悪いPCM→Dop DSD変換。
void
WWPcmData::PcmToDop(void)
{
    int64_t pos = 0;
    SmallDsdStreamInfo *dsdStreams = new SmallDsdStreamInfo[mChannels];
    if (nullptr == dsdStreams) {
        assert(0);
        return;
    }

    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                short sv = (mStream[pos+3]<<8) + mStream[pos+2];

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

                mStream[pos+0] = 0;
                mStream[pos+1] = (BYTE)(0xff & dsdValue16);
                mStream[pos+2] = (BYTE)(0xff & (dsdValue16>>8));
                mStream[pos+3] = (i&1) ? 0xfa : 0x05;
                pos += 4;
            }
        }
        mStreamType = WWStreamDop;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                SmallDsdStreamInfo *p = &dsdStreams[ch];

                short sv = (mStream[pos+2]<<8) + mStream[pos+1];

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

                mStream[pos+0] = (BYTE)(0xff & dsdValue16);
                mStream[pos+1] = (BYTE)(0xff & (dsdValue16>>8));
                mStream[pos+2] = (i&1) ? 0xfa : 0x05;
                pos += 3;
            }
        }
        mStreamType = WWStreamDop;
        break;
    default:
        // DoPに対応していないデバイスでDoP再生しようとするとここに来ることがある。何もしない。
        break;
    }

    delete [] dsdStreams;
    dsdStreams = nullptr;
}

#else

static int FloatToInt(float vF)
{
    int vI;
    if (0.9999999995343387126922607421875f < vF) {
        vI = INT32_MAX;
    } else if (vF < -1.0f) {
        vI = INT32_MIN;
    } else {
        vI = (int)(vF * 2147483648.0f);
    }
    return vI;
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

    int64_t pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWSdmToPcm *p = &sp[ch];

                uint16_t inSdm = (mStream[pos+2] << 8) + mStream[pos+1];
                p->AddInputSamples(inSdm);
                pos += 4;
            }
        }
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWSdmToPcm *p = &sp[ch];

                uint16_t inSdm = (mStream[pos+2] << 8) + mStream[pos+1];
                p->AddInputSamples(inSdm);
                pos += 3;
            }
        }
        break;
    default:
        assert(0);
        break;
    }

    for (int ch=0; ch<mChannels; ++ch) {
        WWSdmToPcm *p = &sp[ch];
        p->Drain();
    }

    pos = 0;
    switch (mSampleFormat) {
    case WWPcmDataSampleFormatSint32V24:
    case WWPcmDataSampleFormatSint32:
        for (int64_t i=0; i<mFrames/4; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWSdmToPcm *p = &sp[ch];
                const float vF = p->GetOutputPcm()[i];
                int vI = FloatToInt(vF);

                mStream[pos+0] = 0;
                mStream[pos+1] = (BYTE)(0xff&(vI >> 8));
                mStream[pos+2] = (BYTE)(0xff&(vI >> 16));
                mStream[pos+3] = (BYTE)(0xff&(vI >> 24));
                pos += 4;
            }
        }
        mStreamType = WWStreamPcm;
        mFrames /= 4;
        break;
    case WWPcmDataSampleFormatSint24:
        for (int64_t i=0; i<mFrames/4; ++i) {
            for (int ch=0; ch<mChannels; ++ch) {
                WWSdmToPcm *p = &sp[ch];
                const float vF = p->GetOutputPcm()[i];
                int vI = FloatToInt(vF);

                mStream[pos+0] = (BYTE)(0xff&(vI >> 8));
                mStream[pos+1] = (BYTE)(0xff&(vI >> 16));
                mStream[pos+2] = (BYTE)(0xff&(vI >> 24));
                pos += 3;
            }
        }
        mStreamType = WWStreamPcm;
        mFrames /= 4;
        break;
    default:
        assert(0);
        break;
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

#endif

void
WWPcmData::CheckDopMarker(void)
{
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

#define SPLICE_READ_FRAME_NUM (4)

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

    WWPcmData fromPcm;
    WWPcmData toPcm;

    fromPcm.Init(-1, mSampleFormat, mChannels, SPLICE_READ_FRAME_NUM, mBytesPerFrame, mContentType, WWStreamPcm);
    fromPcm.FillDopSilentData();
    CopyStream(fromDop, fromPosFrame, SPLICE_READ_FRAME_NUM, fromPcm);
    fromPcm.DopToPcmFast();

    toPcm.Init(  -1, mSampleFormat, mChannels, SPLICE_READ_FRAME_NUM, mBytesPerFrame, mContentType, WWStreamPcm);
    toPcm.FillDopSilentData();
    CopyStream(toDop,   toPosFrame,   SPLICE_READ_FRAME_NUM, toPcm);
    toPcm.DopToPcmFast();

    // PcmToDopFast()がmFramesを4倍する。
    // PCMを1/4で作る。
    mFrames /= 4;

    int sampleCount = UpdateSpliceDataWithStraightLinePcm(
            fromPcm, SPLICE_READ_FRAME_NUM-1,
            toPcm,   SPLICE_READ_FRAME_NUM-1);

    PcmToDopFast();

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