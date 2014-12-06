#pragma once

// 日本語 UTF-8

#include <Windows.h>
#include <mmsystem.h>
#include <MMReg.h>
#include <stdint.h>

/// PCMデータの用途。
enum WWPcmDataContentType {
    WWPcmDataContentMusicData,
    WWPcmDataContentSilence,
    WWPcmDataContentSplice,
};
const char *
WWPcmDataContentTypeToStr(WWPcmDataContentType w);

/// サンプルフォーマット。
enum WWPcmDataSampleFormatType {
    WWPcmDataSampleFormatUnknown = -1,
    WWPcmDataSampleFormatSint16,
    WWPcmDataSampleFormatSint24,
    WWPcmDataSampleFormatSint32V24,
    WWPcmDataSampleFormatSint32,
    WWPcmDataSampleFormatSfloat,

    WWPcmDataSampleFormatNUM
};
const char *
WWPcmDataSampleFormatTypeToStr(WWPcmDataSampleFormatType w);
int WWPcmDataSampleFormatTypeToBitsPerSample(WWPcmDataSampleFormatType t);
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

    void Set(int sampleRate, WWPcmDataSampleFormatType sampleFormat, int numChannels, DWORD dwChannelMask, WWStreamType streamType) {
        this->sampleRate    = sampleRate;
        this->sampleFormat  = sampleFormat;
        this->numChannels   = numChannels;
        this->dwChannelMask = dwChannelMask;
        this->streamType    = streamType;
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
struct WWPcmData {
    WWPcmData *next;

    int       id;
    WWPcmDataSampleFormatType sampleFormat;
    WWStreamType              streamType;
    WWPcmDataContentType      contentType;
    int       nChannels;
    int       bytesPerFrame;

    /// used by FillBufferAddData()
    int64_t   filledFrames;
    int64_t   nFrames;
    int64_t   posFrame;

    BYTE      *stream;

    WWPcmData(void) {
        next         = NULL;

        id           = 0;
        sampleFormat = WWPcmDataSampleFormatUnknown;
        streamType   = WWStreamPcm;
        contentType  = WWPcmDataContentMusicData;
        nChannels    = 0;
        bytesPerFrame = 0;

        filledFrames  = 0;
        nFrames       = 0;
        posFrame      = 0;

        stream        = NULL;
    }

    ~WWPcmData(void) {
        // ここでstreamをfreeする必要はない。
        // streamがNULLでなくても問題ない！
        // メモリリークしないように呼び出し側が気をつける。
    }

    /// @param bytesPerFrame 1フレームのバイト数。
    ///     (1サンプル1チャンネルのバイト数×チャンネル数)
    bool Init(int id, WWPcmDataSampleFormatType sampleFormat, int nChannels,
        int64_t nFrames, int bytesPerFrame, WWPcmDataContentType aContentType);
    void Term(void);

    void Forget(void) {
        stream = NULL;
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
        return nFrames - posFrame;
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
    void ScaleSampleValue(float scale);

    void SetStreamType(WWStreamType t) {
        streamType = t;
    }

    void FillDopSilentData(void);

    void DopToPcm(void);
    void PcmToDop(void);

    void CheckDopMarker(void);

    /// advances specified frames. follows link list
    /// @return current pcmData after nFrame
    static WWPcmData *AdvanceFrames(WWPcmData *pcmData, int64_t nFrames);

private:
    /** get sample value on posFrame.
     * 24 bit signed int value is returned when Sint32V24
     */
    int GetSampleValueInt(int ch, int64_t posFrame) const;
    float GetSampleValueFloat(int ch, int64_t posFrame) const;

    bool SetSampleValueInt(int ch, int64_t posFrame, int value);
    bool SetSampleValueFloat(int ch, int64_t posFrame, float value);

    float GetSampleValueAsFloat(int ch, int64_t posFrame) const;
    bool SetSampleValueAsFloat(int ch, int64_t posFrame, float value);

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

