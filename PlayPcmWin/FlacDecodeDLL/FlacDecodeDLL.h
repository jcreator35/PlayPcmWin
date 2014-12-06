// 日本語UTF-8
#pragma once

#include <stdint.h>
#include <Windows.h>

#ifdef FLACDECODE_EXPORTS
#define FLACDECODE_API __declspec(dllexport)
#else
#define FLACDECODE_API __declspec(dllimport)
#endif

enum FlacDecodeResultType {
    /// ヘッダの取得やデータの取得に成功。
    FDRT_Success = 0,

    /// ファイルの最後まで行き、デコードを完了した。もうデータはない。
    FDRT_Completed = 1,

    // 以下、FLACデコードエラー。
    FDRT_DataNotReady               = -2,
    FDRT_WriteOpenFailed            = -3,
    FDRT_FlacStreamDecoderNewFailed = -4,

    FDRT_FlacStreamDecoderInitFailed = -5,
    FDRT_DecorderProcessFailed       = -6,
    FDRT_LostSync                    = -7,
    FDRT_BadHeader                   = -8,
    FDRT_FrameCrcMismatch            = -9,

    FDRT_Unparseable                = -10,
    FDRT_NumFrameIsNotAligned       = -11,
    FDRT_RecvBufferSizeInsufficient = -12,
    FDRT_OtherError                 = -13,

    FDRT_FileOpenError              = -14,

};

/// FLACヘッダーを読み込んで、フォーマット情報を取得する。
/// 中のグローバル変数に貯める。APIの設計がスレッドセーフになってないので注意。
/// @param skipSamples スキップするサンプル数。0以外の値を指定するとMD5のチェックを行わなくなるので注意。
/// @param fromFlacPath パス名(UTF-16)
/// @return 0以上: デコーダーId。負: エラー。FlacDecodeResultType参照。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_DecodeStart(const wchar_t *fromFlacPath, int64_t skipSamples);

/// FlacDecodeを終了する。(DecodeStartで立てたスレを止めたりする)
/// DecodeStartが失敗を戻しても、成功を戻しても、呼ぶ必要がある。
extern "C" FLACDECODE_API
void __stdcall
FlacDecodeDLL_DecodeEnd(int id);

/// チャンネル数。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetNumOfChannels(int id);

/// 量子化ビット数。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetBitsPerSample(int id);

/// サンプルレート。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetSampleRate(int id);

/// フレーム総数。(ここでフレームとは、全チャンネル分のサンプルデータ1個のこと)
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
int64_t __stdcall
FlacDecodeDLL_GetNumFrames(int id);

/// タイトル文字列(WSTR)。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
bool __stdcall
FlacDecodeDLL_GetTitleStr(int id, LPWSTR name, int nameBytes);

/// アルバム文字列(WSTR)。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
bool __stdcall
FlacDecodeDLL_GetAlbumStr(int id, LPWSTR name, int nameBytes);

/// アーティスト文字列(WSTR)。
/// DecodeStart成功後に呼ぶことができる。
extern "C" FLACDECODE_API
bool __stdcall
FlacDecodeDLL_GetArtistStr(int id, LPWSTR name, int nameBytes);

/// リザルトコード FlacDecodeResultType を取得。
/// ファイルの最後まで行った場合
///   GetLastError==FDRT_Completedで、GetNextPcmDataの戻り値は取得できたフレーム数となる。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetLastResult(int id);

/// ブロックサイズを取得。
/// FlacDecodeDLL_GetNextPcmData()のnumFrameはこのサイズの倍数である必要がある。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetNumFramesPerBlock(int id);


/// 次のPCMデータをnumFrameサンプルだけbuff_returnに詰める。
/// 最後のデータでなくても、numFrameが取得できないこともあるので注意。
/// @return エラーの場合、-1が戻る。0以上の場合、取得できたサンプル数。FDRT_Completedは、正常終了に分類されている。
/// @retval 0 0が戻った場合、取得できたデータが0サンプルであった(成功)。
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetNextPcmData(int id, int numFrame, char *buff_return);

/// 画像データのバイト数
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetPictureBytes(int id);

/// 画像データ
/// @param picture_return [out] ここに書き込まれる。pictureBytes確保して渡す
/// @param offs 元データ列のoffsバイト目からpictureBytesバイトコピーする。
/// @return コピーしたバイト数
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetPictureData(int id, int offs, int pictureBytes, char *picture_return);

/// 埋め込みCUEシートトラックの総数
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(int id);

/// MD5Sum
/// @param md5_return [out] ここに書き込まれる。16バイト確保して渡す
/// @return コピーしたバイト数(16)
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetMD5Sum(int id, char *md5_return);

/// 埋め込みCUEシートトラックtrackIdのトラックナンバーtrackNr
/// @param trackId トラックID (0から連番で付与されている)
/// @return trackNr (1～99 および170) 負の値のとき、trackIdが範囲外
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(int id, int trackId);

/// 埋め込みCUEシートトラックtrackIdのオフセットサンプル位置
extern "C" FLACDECODE_API
int64_t __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(int id, int trackId);

/// 埋め込みCUEシートトラックtrackIdのインデックス総数
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackNumOfIndices(int id, int trackId);

/// 埋め込みCUEシートトラックtrackId、インデックスindexIdのインデックス番号indexNr
/// @param trackId トラックID (0から連番で付与されている)
/// @param indexId インデックスID(0から連番で付与される)
/// @return indexNr (0～99) 負の値のとき、trackIdかindexIdが範囲外
extern "C" FLACDECODE_API
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexNumber(int id, int trackId, int indexId);

/// 埋め込みCUEシートトラックtrackIdのインデックスindexIdのオフセット
/// @param indexId インデックスID(0から連番で付与される)
/// @return インデックスindexIdのトラック先頭からのオフセット位置
extern "C" FLACDECODE_API
int64_t __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexOffsetSamples(int id, int trackId, int indexId);

