#pragma once

// 日本語 UTF-8

#include <Windows.h>
#include <mmsystem.h>
#include <MMReg.h>
#include <stdint.h>

/// PCMデータの用途。
enum WWPcmDataContentType {
    WWPcmDataContentMusicData,
    WWPcmDataContentSilenceForTrailing,
    WWPcmDataContentSilenceForPause,
    WWPcmDataContentSilenceForEnding,
    WWPcmDataContentSplice,
};
const char *
WWPcmDataContentTypeToStr(WWPcmDataContentType w);

/// サンプルフォーマット。WasapiCS.SampleFormatTypeと一致させる。
/// すぐ下の関数群もしっかり作って下さい。
enum WWPcmDataSampleFormatType {
    WWPcmDataSampleFormatUnknown = -1,

    WWPcmDataSampleFormatSint16,
    WWPcmDataSampleFormatSint24,
    WWPcmDataSampleFormatSint32V24,
    WWPcmDataSampleFormatSint32,
    WWPcmDataSampleFormatSfloat,

    WWPcmDataSampleFormatSdouble,

    WWPcmDataSampleFormatNUM
};
const char *
WWPcmDataSampleFormatTypeToStr(WWPcmDataSampleFormatType w);
int WWPcmDataSampleFormatTypeToBitsPerSample(WWPcmDataSampleFormatType t);
int WWPcmDataSampleFormatTypeToBytesPerSample(WWPcmDataSampleFormatType t);
int WWPcmDataSampleFormatTypeToValidBitsPerSample(WWPcmDataSampleFormatType t);
bool WWPcmDataSampleFormatTypeIsFloat(WWPcmDataSampleFormatType t);
bool WWPcmDataSampleFormatTypeIsInt(WWPcmDataSampleFormatType t);

WWPcmDataSampleFormatType
WWPcmDataSampleFormatTypeGenerate(int bitsPerSample, int validBitsPerSample, GUID subFormat);

/// PCMデータか、DoPデータか。
enum WWStreamType {
    WWStreamUnknown = -1,
    WWStreamPcm = 0,
    WWStreamDop,

    WWStreamNUM
};

struct WWPcmFormat {
    int                       sampleRate;
    WWPcmDataSampleFormatType sampleFormat;
    int                       numChannels;
    DWORD                     dwChannelMask;
    WWStreamType              streamType;

    void Set(int aSampleRate, WWPcmDataSampleFormatType aSampleFormat, int aNumChannels,
            DWORD aDwChannelMask, WWStreamType aStreamType) {
        this->sampleRate    = aSampleRate;
        this->sampleFormat  = aSampleFormat;
        this->numChannels   = aNumChannels;
        this->dwChannelMask = aDwChannelMask;
        this->streamType    = aStreamType;
    }

    void Clear(void) {
        sampleFormat = WWPcmDataSampleFormatUnknown;
        sampleRate = 0;
        numChannels = 0;
        dwChannelMask = 0;
        streamType = WWStreamUnknown;
    }

    int BytesPerFrame(void) const {
        return numChannels * WWPcmDataSampleFormatTypeToBitsPerSample(sampleFormat) / 8;
    }
};

/*
 * play
 *   pcmData->posFrame: playing position
 *   pcmData->nFrames: total frames to play (frame == sample point)
 * record
 *   pcmData->posFrame: available recorded frame num
 *   pcmData->nFrames: recording buffer size
 */
class WWPcmData {
public:
    WWPcmData(void) {
        mNext         = nullptr;

        mId           = 0;
        mSampleFormat = WWPcmDataSampleFormatUnknown;
        mStreamType   = WWStreamPcm;
        mContentType  = WWPcmDataContentMusicData;
        mChannels    = 0;
        mBytesPerFrame = 0;

        mFilledFrames  = 0;
        mFrames       = 0;
        mPosFrame      = 0;

        mStream        = nullptr;
    }

    ~WWPcmData(void) {
        // ここでstreamをfreeする必要はない。
        // streamがnullptrでなくても問題ない！
        // メモリリークしないように呼び出し側が気をつける。
    }

    /// @param bytesPerFrame 1フレームのバイト数。
    ///     (1サンプル1チャンネルのバイト数×チャンネル数)
    bool Init(int id, WWPcmDataSampleFormatType sampleFormat, int nChannels,
        int64_t nFrames, int bytesPerFrame, WWPcmDataContentType aContentType,
        WWStreamType aStreamType);
    void Term(void);

    void Forget(void) {
        mStream = nullptr;
    }

    void CopyFrom(WWPcmData *rhs);

    /** create splice data from the two adjacent sample data */
    int UpdateSpliceDataWithStraightLine(
        const WWPcmData &fromPcmData, int64_t fromPosFrame,
        const WWPcmData &toPcmData,   int64_t toPosFrame);

    /** @return sample count need to advance */
    int CreateCrossfadeData(
        const WWPcmData &fromPcmData, int64_t fromPosFrame,
        const WWPcmData &toPcmData,   int64_t toPosFrame);

    int64_t AvailableFrames(void) const {
        return mFrames - mPosFrame;
    }

    /// @return retrieved data bytes
    int GetBufferData(int64_t fromBytes, int wantBytes, BYTE *data_return);

    /// FillBuffer api.
    /// FillBufferStart() and FillBufferAddSampleData() several times and FillBufferEnd()
    void FillBufferStart(void);

    /// @param data sampleData
    /// @param bytes data bytes
    /// @return added sample bytes. 0 if satistifed and no sample data is consumed
    int FillBufferAddData(const BYTE *data, int bytes);

    void FillBufferEnd(void);

    /// get float sample min/max for volume correction
    void FindSampleValueMinMax(float *minValue_return, float *maxValue_return);
    bool ScaleSampleValue(float scale);

    void SetStreamType(WWStreamType t) {
        mStreamType = t;
    }

    void FillDopSilentData(void);

    void DopToPcmFast(void);
    void PcmToDopFast(void);

    void CheckDopMarker(void);

    /// advances specified frames. follows link list
    /// @return current pcmData after nFrame
    static WWPcmData *AdvanceFrames(WWPcmData *pcmData, int64_t nFrames);

    WWPcmDataSampleFormatType SampleFormat(void) const {
        return mSampleFormat;
    }

    WWStreamType StreamType(void) const {
        return mStreamType;
    }

    WWPcmDataContentType ContentType(void) const {
        return mContentType;
    }

    int Channels(void) const {
        return mChannels;
    }

    WWPcmData *Next(void) const {
        return mNext;
    }

    int Id(void) const {
        return mId;
    }

    int64_t Frames(void) const {
        return mFrames;
    }

    int64_t PosFrame(void) const {
        return mPosFrame;
    }

    void SetPosFrame(int64_t n) {
        mPosFrame = n;
    }

    void SetNext(WWPcmData *n) {
        mNext = n;
    }

    BYTE *Stream(void) {
        return mStream;
    }
    
    const BYTE *Stream(void) const {
        return mStream;
    }

    int BytesPerFrame(void) const {
        return mBytesPerFrame;
    }

private:
    WWPcmData *mNext;

    int       mId;
    WWPcmDataSampleFormatType mSampleFormat;
    WWStreamType              mStreamType;
    WWPcmDataContentType      mContentType;
    int       mChannels;
    int       mBytesPerFrame;

    /// used by FillBufferAddData()
    int64_t   mFilledFrames;
    int64_t   mFrames;
    int64_t   mPosFrame;

    BYTE      *mStream;

    /** get sample value on posFrame.
     * 24 bit signed int value is returned when Sint32V24
     */
    int GetSampleValueInt(int ch, int64_t posFrame) const;
    float GetSampleValueFloat(int ch, int64_t posFrame) const;
    int GetSampleValueAsInt24(int ch, int64_t posFrame) const;
    float GetSampleValueAsFloat(int ch, int64_t posFrame) const;

    bool SetSampleValueInt(int ch, int64_t posFrame, int v);
    bool SetSampleValueFloat(int ch, int64_t posFrame, float v);
    bool SetSampleValueAsInt24(int ch, int64_t posFrame, int v);
    bool SetSampleValueAsFloat(int ch, int64_t posFrame, float v);

    /** create splice data from the two adjacent sample data */
    int UpdateSpliceDataWithStraightLinePcm(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame);

    /** @return sample count need to advance */
    int CreateCrossfadeDataPcm(
        const WWPcmData &fromPcm, int64_t fromPosFrame,
        const WWPcmData &toPcm,   int64_t toPosFrame);

    /** create splice data from the two adjacent sample data */
    int UpdateSpliceDataWithStraightLineDop(
        const WWPcmData &fromDop, int64_t fromPosFrame,
        const WWPcmData &toDop,   int64_t toPosFrame);

    /** @return sample count need to advance */
    int CreateCrossfadeDataDop(
        const WWPcmData &fromDop, int64_t fromPosFrame,
        const WWPcmData &toDop,   int64_t toPosFrame);
};

