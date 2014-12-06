// 日本語UTF-8

#define FLACDECODE_EXPORTS

#include "targetver.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdio.h>
#include <stdlib.h>
#include "FLAC/stream_decoder.h"
#include "FlacDecodeDLL.h"
#include <assert.h>
#include <map>
#include <vector>

// x86 CPUにしか対応してない。
// x64やビッグエンディアンには対応してない。

/// ファイルパス制限
#define FLACDECODE_MAXPATH (1024)

/// メタデータの文字バッファバイト数
#define FLACDECODE_MAX_STRSZ (256)

/// コメント個数制限 1024個
#define FLACDECODE_COMMENT_MAX (1024)

/// 画像サイズ制限 100MB
#define FLACDECODE_IMAGE_BYTES_MAX (100 * 1024 * 1024) 

/// 最大トラック数
#define FLACDECODE_TRACK_MAX (256)

/// 最大トラックインデックス数
#define FLACDECODE_TRACK_IDX_MAX (99)

/// MD5SUMのバイト数
#define FLACDECODE_MD5SUM_BYTES (16)

#ifdef _DEBUG
/*
#  define dprintf1(fp, x, ...) { \
    if (NULL == fp) { \
        printf(x, __VA_ARGS__); \
    } else { \
        fprintf(fp, x, __VA_ARGS__); fflush(fp); \
    } \
}
#  define dprintf(fp, x, ...) { \
    if (NULL == fp) { \
        printf(x, __VA_ARGS__); \
    } else { \
        fprintf(fp, x, __VA_ARGS__); fflush(fp); \
    } \
}
*/
#  define dprintf1(fp, x, ...) printf(x, __VA_ARGS__)
#  define dprintf(fp, x, ...) printf(x, __VA_ARGS__)
//#  define dprintf1(fp, x, ...)
//#  define dprintf(fp, x, ...)
#else
#  define dprintf1(fp, x, ...)
#  define dprintf(fp, x, ...)
#endif

#define CHK(x)                           \
{   if (!x) {                            \
        dprintf(fdi->logFP, "E: %s:%d %s is NULL\n", \
            __FILE__, __LINE__, #x);     \
        return FDRT_OtherError;          \
    }                                    \
}

/// FlacDecodeスレッドへのコマンド。
enum FlacDecodeCommand {
    /// コマンドなし。(コマンド実行後にFlacDecodeがセットする)
    FDC_None,

    /// シャットダウンイベント。
    FDC_Shutdown,

    /// フレーム(サンプルデータ)取得。
    /// 取得するフレーム数
    FDC_GetFrames,
};

struct FlacCuesheetIndexInfo{
    int64_t offsetSamples;
    int number;
};

struct FlacCuesheetTrackInfo {
    int64_t offsetSamples;
    int trackNumber;

    char isrc[13];

    /** The track type: 0 for audio, 1 for non-audio. */
    bool isAudio;

    /** まじかよって感じ。 */
    bool preEmphasis;

    std::vector<FlacCuesheetIndexInfo> indices;
};

/// FlacDecodeの物置。
struct FlacDecodeInfo {
    int          id;
    int          sampleRate;
    int          channels;
    int          bitsPerSample;
    FLAC__uint64 totalFrames;

    int64_t      skipFrames;

    int minFrameSize;
    int minBlockSize;
    int maxFrameSize;
    int maxBlockSize;

    FLAC__StreamDecoder *decoder;

    /// 1個のブロックに何サンプル(frame)データが入っているか。
    int          numFramesPerBlock;

    HANDLE       thread;

    FlacDecodeResultType errorCode;

    FlacDecodeCommand command;
    HANDLE            commandEvent;
    HANDLE            commandCompleteEvent;
    /// コマンドを投入する部分を囲むミューテックス。
    HANDLE            commandMutex;

    char              *buff;
    int               buffFrames;
    int               retrievedFrames;
    FILE              *logFP;

    bool md5Available;
    char md5sum[FLACDECODE_MD5SUM_BYTES];

    wchar_t fromFlacPathUtf16[FLACDECODE_MAXPATH];
    char titleStr[FLACDECODE_MAX_STRSZ];
    char artistStr[FLACDECODE_MAX_STRSZ];
    char albumStr[FLACDECODE_MAX_STRSZ];

    int               pictureBytes;
    char              *pictureData;

    std::vector<FlacCuesheetTrackInfo> cueSheetTracks;

    void Clear(void) {
        sampleRate    = 0;
        channels      = 0;
        bitsPerSample = 0;

        totalFrames = 0;
        skipFrames  = 0;

        minFrameSize = 0;
        minBlockSize = 0;
        maxFrameSize = 0;
        maxBlockSize = 0;

        decoder = NULL;

        numFramesPerBlock     = 0;

        thread        = NULL;

        errorCode     = FDRT_DataNotReady;

        command              = FDC_None;
        commandEvent         = NULL;
        commandCompleteEvent = NULL;
        commandMutex         = NULL;

        buff            = NULL;
        buffFrames      = 0;
        retrievedFrames = 0;
        logFP           = NULL;

        md5Available = false;
        fromFlacPathUtf16[0] = 0;
        titleStr[0]     = 0;
        artistStr[0]    = 0;
        albumStr[0]     = 0;

        pictureBytes      = 0;
        pictureData       = NULL;
    }

    FlacDecodeInfo(void) {
        Clear();
    }

    ~FlacDecodeInfo(void) {
        delete [] pictureData;
        pictureData = NULL;
    }
};

#define RG(x,v)                                   \
{                                                 \
    rv = x;                                       \
    if (v != rv) {                                \
        goto end;                                 \
    }                                             \
}                                                 \

////////////////////////////////////////////////////////////////////////
// FLACデコーダーコールバック

static FLAC__StreamDecoderWriteStatus
WriteCallback1(const FLAC__StreamDecoder *decoder,
    const FLAC__Frame *frame, const FLAC__int32 * const buffer[],
    void *clientData)
{
    FlacDecodeInfo *fdi = (FlacDecodeInfo*)clientData;

    (void)decoder;

    // dprintf(fdi->logFP, "%s fdi->totalFrames=%lld errorCode=%d\n", __FUNCTION__, fdi->totalFrames, fdi->errorCode);
    if(fdi->totalFrames == 0) {
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    // dprintf(fdi->logFP, "%s frame->header.number.sample_number=%d\n", __FUNCTION__, frame->header.number.sample_number);
    if(frame->header.number.sample_number == 0) {
        fdi->numFramesPerBlock = frame->header.blocksize;

        // 最初のデータが来た。ここでいったん待ち状態になる。
        // dprintf(fdi->logFP, "%s first data arrived. numFramesPerBlock=%d. set commandCompleteEvent\n", __FUNCTION__, fdi->numFramesPerBlock);
        SetEvent(fdi->commandCompleteEvent);
        WaitForSingleObject(fdi->commandEvent, INFINITE);

        // 起きた。要因をチェックする。
        // dprintf(fdi->logFP, "%s event received1. %d\n", __FUNCTION__, fdi->command);
        if (fdi->command == FDC_Shutdown) {
            return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
        }
    }

    if (fdi->errorCode != FDRT_Success) {
        // デコードエラーが起きた。ここでいったん待ち状態になる。
        dprintf(fdi->logFP, "%s decode error %d. set commandCompleteEvent\n", __FUNCTION__, fdi->errorCode);
        SetEvent(fdi->commandCompleteEvent);
        WaitForSingleObject(fdi->commandEvent, INFINITE);

        // 起きた。要因をチェックする。どちらにしても続行はできない。
        dprintf(fdi->logFP, "%s event received2. %d\n", __FUNCTION__, fdi->command);
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    // データが来た。ブロック数は frame->header.blocksize
    // dprintf(fdi->logFP, "%s fdi->numFramesPerBlock=%d frame->header.blocksize=%d\n", __FUNCTION__, fdi->numFramesPerBlock, frame->header.blocksize);
    if (fdi->numFramesPerBlock != (int)frame->header.blocksize) {
        // dprintf(fdi->logFP, "%s fdi->numFramesPerBlock changed %d to %d\n", __FUNCTION__, fdi->numFramesPerBlock, frame->header.blocksize);
        fdi->numFramesPerBlock = frame->header.blocksize;
    }

    // dprintf(fdi->logFP, "%s fdi->buffFrames=%d fdi->retrievedFrames=%d fdi->numFramesPerBlock=%d\n", __FUNCTION__, fdi->buffFrames, fdi->retrievedFrames, fdi->numFramesPerBlock);
    if ((fdi->buffFrames - fdi->retrievedFrames) < fdi->numFramesPerBlock) {
        // このブロックを収容する場所がない。データ詰め終わり。
        fdi->errorCode       = FDRT_Success;
        SetEvent(fdi->commandCompleteEvent);
        WaitForSingleObject(fdi->commandEvent, INFINITE);

        // 起きた。要因をチェックする。
        // dprintf(fdi->logFP, "%s event received3. %d fdi->errorCode=%d\n", __FUNCTION__, fdi->command, fdi->errorCode);
        if (fdi->command == FDC_Shutdown) {
            return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
        }

        assert(fdi->retrievedFrames == 0);
        // いったんバッファをフラッシュしたのに、まだ足りない場合はデータが詰められない。
        if ((fdi->buffFrames - fdi->retrievedFrames) < fdi->numFramesPerBlock) {
            fdi->errorCode = FDRT_RecvBufferSizeInsufficient;
            dprintf(fdi->logFP, "D: bufferSize insufficient %d < %d\n", fdi->buffFrames, fdi->numFramesPerBlock);
            return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
        }
        // dprintf(fdi->logFP, "%s fdi->buffFrames=%d fdi->retrievedFrames=%d fdi->numFramesPerBlock=%d\n", __FUNCTION__, fdi->buffFrames, fdi->retrievedFrames, fdi->numFramesPerBlock);
    }

    {
        int bytesPerSample = fdi->bitsPerSample / 8;
        int bytesPerFrame  = bytesPerSample * fdi->channels;

        for(int i = 0; i < fdi->numFramesPerBlock; i++) {
            for (int ch = 0; ch < fdi->channels; ++ch) {
                memcpy(&fdi->buff[(fdi->retrievedFrames + i) * bytesPerFrame + ch * bytesPerSample],
                    &buffer[ch][i], bytesPerSample);
            }
        }
    }

    // dprintf(fdi->logFP, "%s set %d frame. fdi->errorCode=%d set commandCompleteEvent\n", __FUNCTION__, fdi->numFramesPerBlock, fdi->errorCode);
    fdi->retrievedFrames += fdi->numFramesPerBlock;

    return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
}

static FLAC__StreamDecoderWriteStatus
WriteCallback(const FLAC__StreamDecoder *decoder,
    const FLAC__Frame *frame, const FLAC__int32 * const buffer[],
    void *clientData)
{
    FLAC__StreamDecoderWriteStatus rv =
        WriteCallback1(decoder, frame, buffer, clientData);

    if (rv == FLAC__STREAM_DECODER_WRITE_STATUS_ABORT) {
        /* デコード終了 */
    }
    return rv;
}

#define VC  metadata->data.vorbis_comment
#define PIC metadata->data.picture
#define CUE metadata->data.cue_sheet

static void
MetadataCallback(const FLAC__StreamDecoder *decoder,
    const FLAC__StreamMetadata *metadata, void *clientData)
{
    (void)decoder;

    FlacDecodeInfo *fdi = (FlacDecodeInfo*)clientData;

    dprintf(fdi->logFP, "%s type=%d\n", __FUNCTION__, metadata->type);

    if(metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
        fdi->totalFrames  = metadata->data.stream_info.total_samples;
        fdi->sampleRate    = metadata->data.stream_info.sample_rate;
        fdi->channels      = metadata->data.stream_info.channels;
        fdi->bitsPerSample = metadata->data.stream_info.bits_per_sample;
        fdi->minFrameSize  = metadata->data.stream_info.min_framesize;
        fdi->minBlockSize  = metadata->data.stream_info.min_blocksize;
        fdi->maxFrameSize  = metadata->data.stream_info.max_framesize;
        fdi->maxBlockSize  = metadata->data.stream_info.max_blocksize;

        // MD5値が0のときMD5値のメタ情報が利用不可を表すという仕様のようだ。
        fdi->md5Available = !!memcmp(metadata->data.stream_info.md5sum, "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", 16);
        memcpy(fdi->md5sum, metadata->data.stream_info.md5sum, FLACDECODE_MD5SUM_BYTES);
    }

    if (metadata->type == FLAC__METADATA_TYPE_VORBIS_COMMENT) {
        dprintf(fdi->logFP, "vendorstr=\"%s\" %d num=%u\n\n",
            (const char *)VC.vendor_string.entry,
            VC.vendor_string.length,
            VC.num_comments);

        // 曲情報は1024個もないだろう。無限ループ防止。
        int num_comments = (FLACDECODE_COMMENT_MAX < VC.num_comments) ? FLACDECODE_COMMENT_MAX : VC.num_comments;

        for (int i=0; i<num_comments; ++i) {
            dprintf(fdi->logFP, "entry=\"%s\" length=%d\n\n",
                (const char *)(VC.comments[i].entry),
                VC.comments[i].length);
            if (0 == _strnicmp("TITLE=", (const char *)(&VC.comments[i].entry[0]), 6)) {
                strncpy_s(fdi->titleStr, (const char *)(&VC.comments[i].entry[6]), FLACDECODE_MAX_STRSZ-1);
            }
            if (0 == _strnicmp("ALBUM=", (const char *)(&VC.comments[i].entry[0]), 6)) {
                strncpy_s(fdi->albumStr, (const char *)(&VC.comments[i].entry[6]), FLACDECODE_MAX_STRSZ-1);
            }
            if (0 == _strnicmp("ARTIST=", (const char *)(&VC.comments[i].entry[0]), 7)) {
                strncpy_s(fdi->artistStr, (const char *)(&VC.comments[i].entry[7]), FLACDECODE_MAX_STRSZ-1);
            }
        }
    }

    if (metadata->type == FLAC__METADATA_TYPE_PICTURE) {
        dprintf(fdi->logFP, "picture bytes=%d\n", PIC.data_length);

        if (0 == fdi->pictureBytes &&
            PIC.data && 0 < PIC.data_length && PIC.data_length <= FLACDECODE_IMAGE_BYTES_MAX) {
            // store first picture data

            fdi->pictureBytes = PIC.data_length;

            assert(NULL == fdi->pictureData);
            fdi->pictureData = new char[fdi->pictureBytes];
            assert(fdi->pictureData);

            memcpy(fdi->pictureData, PIC.data, fdi->pictureBytes);
        }
    }

    if (metadata->type == FLAC__METADATA_TYPE_CUESHEET && CUE.tracks != NULL) {
        dprintf(fdi->logFP, "cuesheet num tracks=%d\n", CUE.num_tracks);

        fdi->cueSheetTracks.clear();

        uint32_t numOfTracks = CUE.num_tracks;
        if (FLACDECODE_TRACK_MAX < numOfTracks) {
            numOfTracks = FLACDECODE_TRACK_MAX;
        }

        for (int trackId=0; trackId<(int)numOfTracks; ++trackId) {
            FlacCuesheetTrackInfo track;
            FLAC__StreamMetadata_CueSheet_Track *from = &CUE.tracks[trackId];

            track.offsetSamples = from->offset;
            track.isAudio = !from->type;
            track.preEmphasis = !!from->pre_emphasis;
            track.trackNumber = from->number;

            memset(track.isrc, 0, sizeof track.isrc);
            memcpy(track.isrc, from->isrc, sizeof track.isrc-1);

            dprintf(fdi->logFP, "  trackNr=%d offsSamples=%lld isAudio=%d preEmph=%d numIdx=%d isrc=%02x%02x%02x%02x %02x%02x%02x%02x %02x%02x%02x%02x\n",
                track.trackNumber, track.offsetSamples,  (int)track.isAudio, (int)track.preEmphasis, (int)from->num_indices,
                (unsigned int)track.isrc[0], (unsigned int)track.isrc[1], (unsigned int)track.isrc[2], (unsigned int)track.isrc[3],
                (unsigned int)track.isrc[4], (unsigned int)track.isrc[5], (unsigned int)track.isrc[6], (unsigned int)track.isrc[7],
                (unsigned int)track.isrc[8], (unsigned int)track.isrc[9], (unsigned int)track.isrc[10], (unsigned int)track.isrc[11]);

            if (from->indices != NULL) {
                uint32_t numOfIndices = from->num_indices;
                if (FLACDECODE_TRACK_IDX_MAX < numOfIndices) {
                    numOfIndices = FLACDECODE_TRACK_IDX_MAX;
                }

                for (int indexId=0; indexId<(int)numOfIndices; ++indexId) {
                    FlacCuesheetIndexInfo idxInfo;
                    FLAC__StreamMetadata_CueSheet_Index *idxFrom = &from->indices[indexId];
                    idxInfo.number = idxFrom->number;
                    idxInfo.offsetSamples = idxFrom->offset;
                    track.indices.push_back(idxInfo);

                    dprintf(fdi->logFP, "    idxNr=%d offsSamples=%lld\n",
                        idxInfo.number, idxInfo.offsetSamples);
                }
            }
            fdi->cueSheetTracks.push_back(track);
        }
    }
}

static void
ErrorCallback(const FLAC__StreamDecoder *decoder,
    FLAC__StreamDecoderErrorStatus status, void *clientData)
{
    FlacDecodeInfo *fdi = (FlacDecodeInfo*)clientData;

    (void)decoder;

    dprintf(fdi->logFP, "%s status=%d\n", __FUNCTION__, status);

    switch (status) {
    case FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC:
        fdi->errorCode = FDRT_LostSync;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER:
        fdi->errorCode = FDRT_BadHeader;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH:
        fdi->errorCode = FDRT_FrameCrcMismatch;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_UNPARSEABLE_STREAM:
        fdi->errorCode = FDRT_Unparseable;
        break;
    default:
        fdi->errorCode = FDRT_OtherError;
        break;
    }

    if (fdi->errorCode != FDRT_Success) {
        /* エラーが起きた。 */
    }
};

///////////////////////////////////////////////////////////////

// デコードスレッド
static int
DecodeMain(FlacDecodeInfo *fdi)
{
    assert(fdi);

    FLAC__bool                    ok = true;
    FLAC__StreamDecoderInitStatus init_status = FLAC__STREAM_DECODER_INIT_STATUS_ERROR_OPENING_FILE;
    FILE *fp = NULL;
    errno_t ercd;

    fdi->decoder = FLAC__stream_decoder_new();
    if(fdi->decoder == NULL) {
        fdi->errorCode = FDRT_FlacStreamDecoderNewFailed;
        dprintf(fdi->logFP, "%s FLAC__stream_decoder_new error %d. set complete event.\n",
            __FUNCTION__, fdi->errorCode);
        goto end;
    }

    dprintf(fdi->logFP, "%s FLAC_stream_decoder=%p\n", __FUNCTION__, fdi->decoder);

    // MD5チェックはPCMを読み出し終わった後に自分で行うので無効にしておく。
    //FLAC__stream_decoder_set_md5_checking(fdi->decoder, true);

    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_STREAMINFO);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_VORBIS_COMMENT);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_PICTURE);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_CUESHEET);

#if 1
    // Windowsでは、この方法でファイルを開かなければならぬ。
    ercd = _wfopen_s(&fp, fdi->fromFlacPathUtf16, L"rb");
    if (ercd != 0 || NULL == fp) {
        fdi->errorCode = FDRT_FileOpenError;
        goto end;
    }

    init_status = FLAC__stream_decoder_init_FILE(fdi->decoder, fp, WriteCallback, MetadataCallback, ErrorCallback, fdi);

    // FLAC__stream_decoder_finish()がfcloseしてくれるので、忘れる。
    fp = NULL;
#else
    // この方法でファイルを開くと、日本語Windowsで、アクサンテギューとかの付いているファイルが開けなくなる。
    {
        char path[MAX_PATH];
        memset(path, 0, sizeof path);
        WideCharToMultiByte(CP_ACP, 0, fdi->fromFlacPathUtf16, -1, path, sizeof path-1, NULL, false);
        init_status = FLAC__stream_decoder_init_file(
            fdi->decoder, path,
            WriteCallback, MetadataCallback, ErrorCallback, fdi);
    }
#endif
    if(init_status != FLAC__STREAM_DECODER_INIT_STATUS_OK) {
        fdi->errorCode = FDRT_FlacStreamDecoderInitFailed;
        dprintf(fdi->logFP, "%s FLAC__stream_decoder_init_FILE() error %d. set complete event.\n",
            __FUNCTION__, init_status);
        goto end;
    }

    ok = FLAC__stream_decoder_process_until_end_of_metadata(fdi->decoder);
    if (!ok) {
        dprintf(fdi->logFP, "%s Flac metadata process error fdi->errorCode=%d\n",
            __FUNCTION__, fdi->errorCode);

        if (fdi->errorCode == FDRT_Success) {
            fdi->errorCode = FDRT_DecorderProcessFailed;
        }
        dprintf(fdi->logFP, "%s Flac metadata process error %d. set complete event.\n",
            __FUNCTION__, fdi->errorCode);
        goto end;
    }

    dprintf(fdi->logFP, "%s skip frames=%lld\n", __FUNCTION__, fdi->skipFrames);
    if (0 < fdi->skipFrames) {
        ok = FLAC__stream_decoder_seek_absolute(fdi->decoder, fdi->skipFrames);
        if (!ok) {
            dprintf(fdi->logFP, "%s Flac seek error skipFrames=%lld fdi->errorCode=%d\n",
                __FUNCTION__, fdi->skipFrames, fdi->errorCode);
            if (fdi->errorCode == FDRT_Success) {
                fdi->errorCode = FDRT_DecorderProcessFailed;
            }
            dprintf(fdi->logFP, "%s FLAC__stream_decoder_seek_absolute() error %d. set complete event.\n",
                __FUNCTION__, fdi->errorCode);
            goto end;
        }
        // FLAC__stream_decoder_seek_absolute()を呼ぶとMD5チェックフラグが外れる
    } else if (fdi->skipFrames < 0) {
        // メタデータのみの読み出し。
        fdi->errorCode = FDRT_Success;
        goto end;
    }

    ok = FLAC__stream_decoder_process_until_end_of_stream(fdi->decoder);
    if (!ok) {
        if (fdi->errorCode == FDRT_Success) {
            fdi->errorCode = FDRT_DecorderProcessFailed;
        }
        dprintf(fdi->logFP, "%s FLAC__stream_decoder_process_until_end_of_stream() error %d. set complete event.\n",
            __FUNCTION__, fdi->errorCode);
        goto end;
    } else {
        // OK。データがバッファに溜まっていたら最後のイベントを出す。

        if (0 < fdi->retrievedFrames) {
            SetEvent(fdi->commandCompleteEvent);
            WaitForSingleObject(fdi->commandEvent, INFINITE);

            // 起きた。要因をチェックする。
            dprintf(fdi->logFP, "%s event received. %d fdi->errorCode=%d\n",
                __FUNCTION__, fdi->command, fdi->errorCode);
            if (fdi->command == FDC_Shutdown) {
                fdi->errorCode = FDRT_DecorderProcessFailed;
                goto end;
            }
        }
    }

    fdi->errorCode = FDRT_Completed;
end:
    if (NULL != fdi->decoder) {
        if (init_status == FLAC__STREAM_DECODER_INIT_STATUS_OK) {
            FLAC__stream_decoder_finish(fdi->decoder);
        }
        FLAC__stream_decoder_delete(fdi->decoder);
        fdi->decoder = NULL;
    }

    SetEvent(fdi->commandCompleteEvent);

    dprintf(fdi->logFP, "%s end ercd=%d\n", __FUNCTION__, fdi->errorCode);
    return fdi->errorCode;
}

static DWORD WINAPI
DecodeEntry(LPVOID param)
{
    FlacDecodeInfo *fdi = (FlacDecodeInfo*)param;
    DecodeMain(fdi);
    return 0;
}

///////////////////////////////////////////////////////////////

/// 物置の実体。グローバル変数。
static std::map<int, FlacDecodeInfo*> g_flacDecodeInfoMap;

static int g_nextDecoderId = 0;

static FlacDecodeInfo *
FlacDecodeInfoNew(void)
{
    FlacDecodeInfo * fdi = new FlacDecodeInfo();
    if (NULL == fdi) {
        return NULL;
    }

    fdi->id = g_nextDecoderId;
    g_flacDecodeInfoMap[g_nextDecoderId] = fdi;

    ++g_nextDecoderId;
    return fdi;
}

static void
FlacDecodeInfoDelete(FlacDecodeInfo *fdi)
{
    if (NULL == fdi) {
        return;
    }

    g_flacDecodeInfoMap.erase(fdi->id);
    delete fdi;
    fdi = NULL; // あんまり意味ないが、一応
}

static FlacDecodeInfo *
FlacDecodeInfoFindById(int id)
{
    std::map<int, FlacDecodeInfo*>::iterator ite
        = g_flacDecodeInfoMap.find(id);
    if (ite == g_flacDecodeInfoMap.end()) {
        return NULL;
    }
    return ite->second;
}

///////////////////////////////////////////////////////////////

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNumOfChannels(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return 0;
    }

    return fdi->channels;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetBitsPerSample(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return 0;
    }

    return fdi->bitsPerSample;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetSampleRate(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return 0;
    }

    return fdi->sampleRate;
}

extern "C" __declspec(dllexport)
int64_t __stdcall
FlacDecodeDLL_GetNumFrames(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return 0;
    }

    return fdi->totalFrames;
}

extern "C" __declspec(dllexport)
bool __stdcall
FlacDecodeDLL_GetTitleStr(int id, LPWSTR name, int nameBytes)
{
    memset(name, 0, nameBytes);

    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return false;
    }

    MultiByteToWideChar(CP_UTF8, 0, fdi->titleStr, -1, name, nameBytes/2-1);
    return true;
}

extern "C" __declspec(dllexport)
bool __stdcall
FlacDecodeDLL_GetAlbumStr(int id, LPWSTR name, int nameBytes)
{
    memset(name, 0, nameBytes);

    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return false;
    }

    MultiByteToWideChar(CP_UTF8, 0, fdi->albumStr, -1, name, nameBytes/2-1);
    return true;
}

extern "C" __declspec(dllexport)
bool __stdcall
FlacDecodeDLL_GetArtistStr(int id, LPWSTR name, int nameBytes)
{
    memset(name, 0, nameBytes);

    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (fdi->errorCode != FDRT_Success) {
        assert(!"please call FlacDecodeDLL_DecodeStart()");
        return false;
    }

    MultiByteToWideChar(CP_UTF8, 0, fdi->artistStr, -1, name, nameBytes/2-1);
    return true;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetLastResult(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    return fdi->errorCode;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNumFramesPerBlock(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    return fdi->numFramesPerBlock;
}

#ifdef _DEBUG
static FILE *g_fp = NULL;
static void
LogOpen(FlacDecodeInfo *fdi)
{
    assert(fdi);

    LARGE_INTEGER performanceCount;
    QueryPerformanceCounter(&performanceCount);

    char s[256];
    sprintf_s(s, "log%d_%lld.txt", fdi->id, performanceCount.QuadPart);

    errno_t result = fopen_s(&fdi->logFP, s, "wb");
    if (result != 0) {
        fdi->logFP = NULL;
    }
}
static void
LogClose(FlacDecodeInfo *fdi)
{
    assert(fdi);
    if (fdi->logFP) {
        fclose(fdi->logFP);
        fdi->logFP = NULL;
    }
}
#else
#define LogOpen(fdi)
#define LogClose(fdi)
#endif

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_DecodeStart(const wchar_t *fromFlacPath, int64_t skipFrames)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoNew();
    if (NULL == fdi) {
        assert(0);
        return FDRT_OtherError;
    }

    LogOpen(fdi);
    dprintf1(fdi->logFP, "%s started\n", __FUNCTION__);
    dprintf1(fdi->logFP, "%s skipFrames=%lld path=\"%S\"\n",
        __FUNCTION__, skipFrames, fromFlacPath);

    fdi->skipFrames = skipFrames;

    assert(NULL == fdi->commandMutex);
    fdi->commandMutex = CreateMutex(NULL, FALSE, NULL);
    CHK(fdi->commandMutex);

    assert(NULL == fdi->commandEvent);
    fdi->commandEvent = CreateEventEx(NULL, NULL, 0,
        EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(fdi->commandEvent);

    assert(NULL == fdi->commandCompleteEvent);
    fdi->commandCompleteEvent = CreateEventEx(NULL, NULL, 0,
        EVENT_MODIFY_STATE | SYNCHRONIZE);
    CHK(fdi->commandCompleteEvent);

    fdi->errorCode = FDRT_Success;
    wcsncpy_s(fdi->fromFlacPathUtf16, fromFlacPath,
        (sizeof fdi->fromFlacPathUtf16)/2-1);

    fdi->thread
        = CreateThread(NULL, 0, DecodeEntry, fdi, 0, NULL);
    assert(fdi->thread);

    dprintf(fdi->logFP, "%s createThread\n", __FUNCTION__);

    // FlacDecodeスレが動き始める。commandCompleteEventを待つ。
    // FlacDecodeスレは、途中でエラーが起きるか、
    // データの準備ができたらcommandCompleteEventを発行し、commandEventをWaitする。
    WaitForSingleObject(fdi->commandCompleteEvent, INFINITE);
    
    dprintf1(fdi->logFP, "%s commandCompleteEvent. ercd=%d fdi->id=%d\n",
        __FUNCTION__, fdi->errorCode, fdi->id);
    if (fdi->errorCode < 0) {
        goto end;
    }

end:
    if (fdi->errorCode < 0) {
        int ercd = fdi->errorCode;
        FlacDecodeInfoDelete(fdi);
        fdi = NULL;

        return ercd;
    }

    return fdi->id;
}

#define CLOSE_SET_NULL(p) \
if (NULL != p) {          \
    CloseHandle(p);       \
    p = NULL;             \
}

extern "C" __declspec(dllexport)
void __stdcall
FlacDecodeDLL_DecodeEnd(int id)
{
    if (id < 0) {
        assert(0);
        return;
    }

    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    if (NULL == fdi) {
        assert(0);
        return;
    }
    dprintf1(fdi->logFP, "%s started. id=%d\n", __FUNCTION__, id);


    if (fdi->thread) {
        assert(fdi->commandMutex);
        assert(fdi->commandEvent);
        assert(fdi->commandCompleteEvent);

        WaitForSingleObject(fdi->commandMutex, INFINITE);
        fdi->command = FDC_Shutdown;

        dprintf(fdi->logFP, "%s SetEvent and wait to complete FlacDecodeThead\n",
            __FUNCTION__);

        SetEvent(fdi->commandEvent);
        ReleaseMutex(fdi->commandMutex);

        // スレッドが終わるはず。
        WaitForSingleObject(fdi->thread, INFINITE);

        dprintf(fdi->logFP, "%s thread stopped. delete FlacDecodeThead\n",
            __FUNCTION__);
        CLOSE_SET_NULL(fdi->thread);
    }

    CLOSE_SET_NULL(fdi->commandEvent);
    CLOSE_SET_NULL(fdi->commandCompleteEvent);
    CLOSE_SET_NULL(fdi->commandMutex);

    fdi->Clear();

    dprintf1(fdi->logFP, "%s id=%d done.\n", __FUNCTION__, id);
    LogClose(fdi);

    FlacDecodeInfoDelete(fdi);
    fdi = NULL;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetNextPcmData(int id, int numFrame, char *buff_return)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (NULL == fdi->thread) {
        dprintf(fdi->logFP, "%s FlacDecodeThread is not ready.\n",
            __FUNCTION__);
        return FDRT_OtherError;
    }

    assert(fdi->commandMutex);
    assert(fdi->commandEvent);
    assert(fdi->commandCompleteEvent);

    {   // FlacDecodeThreadにGetFramesコマンドを伝える
        WaitForSingleObject(fdi->commandMutex, INFINITE);

        fdi->errorCode    = FDRT_Success;
        fdi->command      = FDC_GetFrames;
        fdi->buff         = buff_return;
        fdi->buffFrames    = numFrame;
        fdi->retrievedFrames = 0;

        dprintf(fdi->logFP, "%s set command.\n", __FUNCTION__);
        SetEvent(fdi->commandEvent);

        ReleaseMutex(fdi->commandMutex);
    }

    dprintf(fdi->logFP, "%s wait for commandCompleteEvent.\n", __FUNCTION__);
    WaitForSingleObject(fdi->commandCompleteEvent, INFINITE);

    dprintf1(fdi->logFP, "%s numFrame=%d retrieved=%d ercd=%d\n",
            __FUNCTION__, numFrame,
            fdi->retrievedFrames, fdi->errorCode);

    if (FDRT_Success   != fdi->errorCode &&
        FDRT_Completed != fdi->errorCode) {
        // エラー終了。
        return -1;
    }
    return fdi->retrievedFrames;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetPictureBytes(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    return fdi->pictureBytes;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetPictureData(int id, int offs, int pictureBytes, char *picture_return)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    int copyBytes = fdi->pictureBytes - offs;

    if (copyBytes <= 0) {
        return 0;
    }

    if (pictureBytes < copyBytes) {
        copyBytes = pictureBytes;
    }

    memcpy(picture_return, &fdi->pictureData[offs], copyBytes);
    return copyBytes;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetNumOfTracks(int id)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    return fdi->cueSheetTracks.size();
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetMD5Sum(int id, char *md5_return)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);

    if (!fdi->md5Available) {
        return 0;
    }

    memcpy(md5_return, fdi->md5sum, FLACDECODE_MD5SUM_BYTES);
    return FLACDECODE_MD5SUM_BYTES;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackNumber(int id, int trackId)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);
    if (trackId < 0 || fdi->cueSheetTracks.size() <= (unsigned int)trackId) {
        return -1;
    }
    return fdi->cueSheetTracks[trackId].trackNumber;
}

extern "C" __declspec(dllexport)
int64_t __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackOffsetSamples(int id, int trackId)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);
    if (trackId < 0 || fdi->cueSheetTracks.size() <= (unsigned int)trackId) {
        return -1;
    }
    return fdi->cueSheetTracks[trackId].offsetSamples;
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackNumOfIndices(int id, int trackId)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);
    if (trackId < 0 || fdi->cueSheetTracks.size() <= (unsigned int)trackId) {
        return -1;
    }
    return fdi->cueSheetTracks[trackId].indices.size();
}

extern "C" __declspec(dllexport)
int __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexNumber(int id, int trackId, int indexId)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);
    if (trackId < 0 || fdi->cueSheetTracks.size() <= (unsigned int)trackId) {
        return -1;
    }

    FlacCuesheetTrackInfo *fct = &fdi->cueSheetTracks[trackId];
    if (indexId < 0 || fct->indices.size() <= (unsigned int)indexId) {
        return -1;
    }

    return fct->indices[indexId].number;
}

extern "C" __declspec(dllexport)
int64_t __stdcall
FlacDecodeDLL_GetEmbeddedCuesheetTrackIndexOffsetSamples(int id, int trackId, int indexId)
{
    FlacDecodeInfo *fdi = FlacDecodeInfoFindById(id);
    assert(fdi);
    if (trackId < 0 || fdi->cueSheetTracks.size() <= (unsigned int)trackId) {
        return -1;
    }

    FlacCuesheetTrackInfo *fct = &fdi->cueSheetTracks[trackId];
    if (indexId < 0 || fct->indices.size() <= (unsigned int)indexId) {
        return -1;
    }

    return fct->indices[indexId].offsetSamples;
}
