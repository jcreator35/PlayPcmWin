#pragma once

#include <stdint.h>

#ifdef WWFLACRW_EXPORTS
#define WWFLACRW_API __declspec(dllexport)
#else
#define WWFLACRW_API __declspec(dllimport)
#endif

enum FlacRWDecodeType {
    FRDT_All,    ///< メタデータの抽出とPCMデータの抽出をブロッキングですべて行うモード。
    FRDT_Header, ///< メタデータの抽出だけを行う。
    FRDT_One, ///< WWFlacRW_DecodeStreamOne()を使用してフレームを1個ずつ受け取るモード。
};

enum FlacRWResultType {
    /// ヘッダの取得やデータの取得に成功。
    FRT_Success = 0,

    /// ファイルの最後まで行き、codecを完了した。もうデータはない。
    FRT_Completed = 1,

    // 以下、FLACデコードエラー。
    FRT_DataNotReady               = -2,
    FRT_WriteOpenFailed            = -3,
    FRT_FlacStreamDecoderNewFailed = -4,

    FRT_FlacStreamDecoderInitFailed = -5,
    FRT_DecorderProcessFailed       = -6,
    FRT_LostSync                    = -7,
    FRT_BadHeader                   = -8,
    FRT_FrameCrcMismatch            = -9,

    FRT_Unparseable                = -10,
    FRT_NumFrameIsNotAligned       = -11,
    FRT_RecvBufferSizeInsufficient = -12,
    FRT_OtherError                 = -13,

    FRT_FileOpenError              = -14,
    FRT_BufferSizeMismatch         = -15,
    FRT_MemoryExhausted            = -16,
    FRT_EncoderError               = -17,
    FRT_InvalidNumberOfChannels    = -18,
    FRT_InvalidBitsPerSample       = -19,
    FRT_InvalidSampleRate          = -20,
    FRT_InvalidMetadata            = -21,
    FRT_BadParams                  = -22,
    FRT_IdNotFound                 = -23,
    FRT_EncoderProcessFailed       = -24,
    FRT_OutputFileTooLarge         = -25,
    FRT_MD5SignatureDoesNotMatch   = -26,

    /// CRC異常などは無くチェックは正常終了したがMD5の値が入っておらず照合できなかった。
    /// WWFlacRW_CheckIntegrity()が戻すことがある。
    FRT_SuccessButMd5WasNotCalculated = -27,
};

#define WWFLAC_TEXT_STRSZ   (256)
#define WWFLAC_MD5SUM_BYTES (16)

/// 最大トラックインデックス数
#define WWFLAC_TRACK_IDX_NUM (99)

#pragma pack(push, 4)
struct WWFlacMetadata {
    int          sampleRate;
    int          channels;
    int          bitsPerSample;
    int          pictureBytes;

    uint64_t     totalSamples;

    wchar_t titleStr[WWFLAC_TEXT_STRSZ];
    wchar_t artistStr[WWFLAC_TEXT_STRSZ];
    wchar_t albumStr[WWFLAC_TEXT_STRSZ];
    wchar_t albumArtistStr[WWFLAC_TEXT_STRSZ];
    wchar_t genreStr[WWFLAC_TEXT_STRSZ];

    wchar_t dateStr[WWFLAC_TEXT_STRSZ];
    wchar_t trackNumberStr[WWFLAC_TEXT_STRSZ];
    wchar_t discNumberStr[WWFLAC_TEXT_STRSZ];
    wchar_t pictureMimeTypeStr[WWFLAC_TEXT_STRSZ];
    wchar_t pictureDescriptionStr[WWFLAC_TEXT_STRSZ];

    uint8_t md5sum[WWFLAC_MD5SUM_BYTES];
};

struct WWFlacCuesheetTrackIdx {
    int64_t offsetSamples;
    int     number;
    int     pad; // 8バイトアラインする。
};

struct WWFlacCuesheetTrack {
    int64_t offsetSamples;
    int trackNumber;
    int numIdx;
    int isAudio;
    int preEmphasis;

    int trackIdxCount;
    int pad; // 8バイトアラインする。

    WWFlacCuesheetTrackIdx trackIdx[WWFLAC_TRACK_IDX_NUM];
};

#pragma pack(pop)

///////////////////////////////////////////////////////////////////////////////////////////////////
// flac decode

/// FLACヘッダーを読み込んで、フォーマット情報を取得、FRDT_Allの場合さらにすべてのサンプルデータを取得。
/// 中のグローバル変数に貯める。
/// @param frdt FlacFWDecodeType (FRDT_All または FRDT_Header)
/// @param path パス名(UTF-16)
/// @return 0以上: デコーダーId。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_Decode(int frdt, const wchar_t *path);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_DecodeStreamOne(int id, uint8_t *pcmReturn, int pcmBytes);

/// @param skipFrames 先頭からのスキップするサンプル数。
/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_DecodeStreamSkip(int id, int64_t skipFrames);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedMetadata(int id, WWFlacMetadata &metaReturn);

/// @return 0以上: コピーしたバイト数。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedPicture(int id, uint8_t * pictureReturn, int pictureBytes);

/// @return 0以上: コピーしたバイト数。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedPcmBytes(int id, int channel, int64_t startBytes, uint8_t * pcmReturn, int pcmBytes);

/// キューシートのトラック数を戻す。
/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedCuesheetNum(int id);

extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedCuesheetByTrackIdx(int id, int trackIdx, WWFlacCuesheetTrack &trackReturn);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_DecodeEnd(int id);


///////////////////////////////////////////////////////////////////////////////////////////////////
// flac encode

/// @return 0以上: デコーダーId。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeInit(const WWFlacMetadata &meta);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeSetPicture(int id, const uint8_t * pictureData, int pictureBytes);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeSetPcmFragment(int id, int channel, int64_t offs, const uint8_t * pcmData, int copyBytes);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeRun(int id, const wchar_t *path);

/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeEnd(int id);

///////////////////////////////////////////////////////////////////////////////////////////////////
// flac check integrity

/// FLACファイルのintegrity checkを行う。
/// @param path パス名(UTF-16)
/// @return 0以上: 成功。負: エラー。FlacRWResultType参照。
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_CheckIntegrity(const wchar_t *path);

