#include "stdafx.h"
#include "WWFlacRW.h"
#include <Windows.h>
#include <map>
#include <stdint.h>
#include <assert.h>
#include <vector>
#include <stdio.h>

#include "FLAC/metadata.h"
#include "FLAC/stream_decoder.h"
#include "FLAC/stream_encoder.h"

#define dprintf(x, ...) printf(x, __VA_ARGS__)

/// wchar_t�̕�����
#define FLACDECODE_MAX_STRSZ (256)

/// �摜�T�C�Y���� 100MB
#define FLACDECODE_IMAGE_BYTES_MAX (100 * 1024 * 1024) 

/// �ő�g���b�N��
#define FLACDECODE_TRACK_MAX (256)

/// �ő�g���b�N�C���f�b�N�X��
#define FLACDECODE_TRACK_IDX_MAX (99)

/// �R�����g������ 1024��
#define FLACDECODE_COMMENT_MAX (1024)

#define FLACENCODE_READFRAMES (4096)

struct FlacCuesheetIndexInfo {
    int64_t offsetSamples;
    int number;
} ;

struct FlacCuesheetTrackInfo {
    int64_t offsetSamples;
    int trackNumber;

    char isrc[13];

    /** The track type: 0 for audio, 1 for non-audio. */
    bool isAudio;

    /** �܂�������Ċ����B */
    bool preEmphasis;

    std::vector<FlacCuesheetIndexInfo> indices;
};

struct FlacDecodeInfo {
    wchar_t path[MAX_PATH];

    FlacRWResultType errorCode;
    FLAC__StreamDecoder *decoder;

    int          id;
    int          sampleRate;
    int          channels;
    int          bitsPerSample;

    int64_t      totalSamples;
    int64_t      totalBytesPerChannel;

    int minFrameSize;
    int minBlockSize;
    int maxFrameSize;
    int maxBlockSize;

    /// 1�̃u���b�N�ɉ��T���v��(frame)�f�[�^�������Ă��邩�B
    int          numFramesPerBlock;

    uint8_t           **buffPerChannel;
    int               retrievedFrames;

    char titleStr[WWFLAC_TEXT_STRSZ];
    char artistStr[WWFLAC_TEXT_STRSZ];
    char albumStr[WWFLAC_TEXT_STRSZ];
    char albumArtistStr[WWFLAC_TEXT_STRSZ];
    char genreStr[WWFLAC_TEXT_STRSZ];

    char dateStr[WWFLAC_TEXT_STRSZ];
    char trackNumberStr[WWFLAC_TEXT_STRSZ];
    char discNumberStr[WWFLAC_TEXT_STRSZ];
    char pictureMimeTypeStr[WWFLAC_TEXT_STRSZ];
    char pictureDescriptionStr[WWFLAC_TEXT_STRSZ];

    uint8_t md5sum[WWFLAC_MD5SUM_BYTES];

    int               pictureBytes;
    uint8_t           *pictureData;

    std::vector<FlacCuesheetTrackInfo> cueSheetTracks;

    FlacDecodeInfo(void) {
        Clear();
    }

    void Clear(void) {
        memset(path, 0, sizeof path);

        errorCode = FRT_OtherError;
        decoder = NULL;

        id = -1;
        sampleRate = 0;
        channels = 0;
        bitsPerSample = 0;

        totalSamples = 0;
        totalBytesPerChannel = 0;

        minFrameSize = 0;
        minBlockSize = 0;
        maxFrameSize = 0;
        maxBlockSize = 0;

        numFramesPerBlock = 0;

        buffPerChannel = NULL;
        retrievedFrames = 0;

        memset(titleStr,       0, sizeof titleStr);
        memset(artistStr,      0, sizeof artistStr);
        memset(albumStr,       0, sizeof albumStr);
        memset(albumArtistStr, 0, sizeof albumArtistStr);
        memset(genreStr,       0, sizeof genreStr);

        memset(dateStr,        0, sizeof dateStr);
        memset(trackNumberStr, 0, sizeof trackNumberStr);
        memset(discNumberStr,  0, sizeof discNumberStr);
        memset(pictureMimeTypeStr,    0, sizeof pictureMimeTypeStr);
        memset(pictureDescriptionStr,    0, sizeof pictureDescriptionStr);

        memset(md5sum,         0, sizeof md5sum);

        pictureBytes = 0;
        pictureData = NULL;
        cueSheetTracks.clear();
    }
   
    ~FlacDecodeInfo(void) {
        if (buffPerChannel) {
            for (int ch=0; ch<channels; ++ch) {
                delete [] buffPerChannel[ch];
                buffPerChannel[ch] = NULL;
            }
            delete [] buffPerChannel;
            buffPerChannel = NULL;
        }

        delete [] pictureData;
        pictureData = NULL;
    }

    static int nextId;
};

int FlacDecodeInfo::nextId;

/// ���u�̎��́B�O���[�o���ϐ��B
static std::map<int, FlacDecodeInfo*> g_flacDecodeInfoMap;

/////////////////////////////////////////////////////////////////////////////////////////////

template <typename T>
static T *
FlacTInfoNew(std::map<int, T*> &storage)
{
    T * fdi = new T();
    if (NULL == fdi) {
        return NULL;
    }

    fdi->id = T::nextId;
    storage[T::nextId] = fdi;
    ++T::nextId;

    return fdi;
}

template <typename T>
static void
FlacTInfoDelete(std::map<int, T*> &storage, T *fdi)
{
    if (NULL == fdi) {
        return;
    }

    storage.erase(fdi->id);
    delete fdi;
    fdi = NULL; // ����܂�Ӗ��Ȃ����A�ꉞ
}

template <typename T>
static T *
FlacTInfoFindById(std::map<int, T*> &storage, int id)
{
    std::map<int, T*>::iterator ite
        = storage.find(id);
    if (ite == storage.end()) {
        return NULL;
    }
    return ite->second;
}

/////////////////////////////////////////////////////////////////////////////////////////////

static FLAC__StreamDecoderWriteStatus
WriteCallback(const FLAC__StreamDecoder *decoder,
        const FLAC__Frame *frame, const FLAC__int32 * const buffer[],
        void *clientData)
{
    FlacDecodeInfo *fdi = (FlacDecodeInfo*)clientData;
    (void)decoder;

    if(fdi->totalSamples == 0) {
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    if(frame->header.number.sample_number == 0) {
        fdi->numFramesPerBlock = frame->header.blocksize;

        // �ŏ��̃f�[�^�������B
    }

    if (fdi->errorCode != FRT_Success) {
        // �f�R�[�h�G���[���N�����B
        dprintf("%s decode error %d. set commandCompleteEvent\n", __FUNCTION__, fdi->errorCode);
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    // �f�[�^�������B�u���b�N���� frame->header.blocksize
    if (fdi->numFramesPerBlock != (int)frame->header.blocksize) {
        // dprintf(fdi->logFP, "%s fdi->numFramesPerBlock changed %d to %d\n", __FUNCTION__, fdi->numFramesPerBlock, frame->header.blocksize);
        fdi->numFramesPerBlock = frame->header.blocksize;
    }

    if ((fdi->totalSamples - fdi->retrievedFrames) < fdi->numFramesPerBlock) {
        fdi->errorCode = FRT_RecvBufferSizeInsufficient;
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    {
        int bytesPerSample = fdi->bitsPerSample / 8;
        int bytesPerFrame  = bytesPerSample * fdi->channels;

        for (int ch = 0; ch < fdi->channels; ++ch) {
            int64_t writePos=fdi->retrievedFrames*bytesPerSample;
            for (int offs=0; offs<fdi->numFramesPerBlock; ++offs) {
                memcpy(&fdi->buffPerChannel[ch][writePos], &buffer[ch][offs], bytesPerSample);
                writePos += bytesPerSample;
            }
        }
    }
    fdi->retrievedFrames += fdi->numFramesPerBlock;
    return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
}

#define VC  metadata->data.vorbis_comment
#define PIC metadata->data.picture
#define CUE metadata->data.cue_sheet
#define STRCPY_COMMENT(X, Y) \
    if (0 == _strnicmp(X, (const char *)(&VC.comments[i].entry[0]), strlen(X))) { strncpy_s(fdi->Y, (const char *)(&VC.comments[i].entry[strlen(X)]), FLACDECODE_MAX_STRSZ-1); }

static void
MetadataCallback(const FLAC__StreamDecoder *decoder,
        const FLAC__StreamMetadata *metadata, void *clientData)
{
    (void)decoder;

    FlacDecodeInfo *fdi = (FlacDecodeInfo*)clientData;

    dprintf("%s type=%d\n", __FUNCTION__, metadata->type);

    if(metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
        fdi->totalSamples  = metadata->data.stream_info.total_samples;
        fdi->sampleRate    = metadata->data.stream_info.sample_rate;
        fdi->channels      = metadata->data.stream_info.channels;
        fdi->bitsPerSample = metadata->data.stream_info.bits_per_sample;
        fdi->minFrameSize  = metadata->data.stream_info.min_framesize;
        fdi->minBlockSize  = metadata->data.stream_info.min_blocksize;
        fdi->maxFrameSize  = metadata->data.stream_info.max_framesize;
        fdi->maxBlockSize  = metadata->data.stream_info.max_blocksize;
        memcpy(fdi->md5sum, metadata->data.stream_info.md5sum, WWFLAC_MD5SUM_BYTES);
        assert(!fdi->buffPerChannel);
        fdi->totalBytesPerChannel = fdi->totalSamples * (fdi->bitsPerSample/ 8);
        fdi->buffPerChannel = new uint8_t*[fdi->channels];
        if (fdi->buffPerChannel == NULL) {
            dprintf("memory exhausted");
            fdi->errorCode = FRT_MemoryExhausted;
            return;
        }
        memset(fdi->buffPerChannel, 0, sizeof(uint8_t*)*fdi->channels);

        for (int ch=0; ch<fdi->channels; ++ch) {
            fdi->buffPerChannel[ch] = new uint8_t[fdi->totalBytesPerChannel];
            if (fdi->buffPerChannel[ch] == NULL) {
                for (int i=0; i<fdi->channels; ++i) {
                    delete [] fdi->buffPerChannel[i];
                    fdi->buffPerChannel[i] = NULL;
                }
                delete [] fdi->buffPerChannel;
                fdi->buffPerChannel = NULL;
                dprintf("memory exhausted");
                fdi->errorCode = FRT_MemoryExhausted;
                return;
            }
        }
    }

    if (metadata->type == FLAC__METADATA_TYPE_VORBIS_COMMENT) {
        dprintf("vendorstr=\"%s\" %d num=%u\n\n",
            (const char *)VC.vendor_string.entry,
            VC.vendor_string.length,
            VC.num_comments);

        // �ȏ���1024���Ȃ����낤�B�������[�v�h�~�B
        int num_comments = (FLACDECODE_COMMENT_MAX < VC.num_comments) ? FLACDECODE_COMMENT_MAX : VC.num_comments;

        for (int i=0; i<num_comments; ++i) {
            dprintf("entry=\"%s\" length=%d\n\n",
                (const char *)(VC.comments[i].entry),
                VC.comments[i].length);
            STRCPY_COMMENT("TITLE=", titleStr);
            STRCPY_COMMENT("ALBUM=", albumStr);
            STRCPY_COMMENT("ARTIST=", artistStr);
            STRCPY_COMMENT("ALBUMARTIST=", albumArtistStr);
            STRCPY_COMMENT("GENRE=", genreStr);

            STRCPY_COMMENT("DATE=", dateStr);
            STRCPY_COMMENT("TRACKNUMBER=", trackNumberStr);
            STRCPY_COMMENT("DISCNUMBER=", discNumberStr);
        }
    }

    if (metadata->type == FLAC__METADATA_TYPE_PICTURE) {
        dprintf("picture bytes=%d\n", PIC.data_length);

        strncpy_s(fdi->pictureMimeTypeStr, PIC.mime_type, sizeof fdi->pictureMimeTypeStr -1);
        strncpy_s(fdi->pictureDescriptionStr, (const char*)PIC.description, sizeof fdi->pictureDescriptionStr -1);

        if (0 == fdi->pictureBytes &&
            PIC.data && 0 < PIC.data_length && PIC.data_length <= FLACDECODE_IMAGE_BYTES_MAX) {
            // store first picture data

            fdi->pictureBytes = PIC.data_length;

            assert(NULL == fdi->pictureData);
            fdi->pictureData = new uint8_t[fdi->pictureBytes];
            if (fdi->pictureData == NULL) {
                fdi->pictureBytes = 0;
                dprintf("memory exhausted");
                fdi->errorCode = FRT_MemoryExhausted;
                return;
            }

            memcpy(fdi->pictureData, PIC.data, fdi->pictureBytes);
        }
    }

    if (metadata->type == FLAC__METADATA_TYPE_CUESHEET && CUE.tracks != NULL) {
        dprintf("cuesheet num tracks=%d\n", CUE.num_tracks);

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

            dprintf("  trackNr=%d offsSamples=%lld isAudio=%d preEmph=%d numIdx=%d isrc=%02x%02x%02x%02x %02x%02x%02x%02x %02x%02x%02x%02x\n",
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

                    dprintf("    idxNr=%d offsSamples=%lld\n", idxInfo.number, idxInfo.offsetSamples);
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

    dprintf("%s status=%d\n", __FUNCTION__, status);

    switch (status) {
    case FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC:
        fdi->errorCode = FRT_LostSync;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER:
        fdi->errorCode = FRT_BadHeader;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH:
        fdi->errorCode = FRT_FrameCrcMismatch;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_UNPARSEABLE_STREAM:
        fdi->errorCode = FRT_Unparseable;
        break;
    default:
        fdi->errorCode = FRT_OtherError;
        break;
    }

    if (fdi->errorCode != FRT_Success) {
        /* �G���[���N�����B */
    }
};

/////////////////////////////////////////////////////////////////////////////////////////////

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_DecodeAll(const wchar_t *path)
{
    FLAC__bool                    ok = true;
    FILE *fp = NULL;
    errno_t ercd;
    FLAC__StreamDecoderInitStatus initStatus = FLAC__STREAM_DECODER_INIT_STATUS_ERROR_OPENING_FILE;

    FlacDecodeInfo *fdi = FlacTInfoNew<FlacDecodeInfo>(g_flacDecodeInfoMap);
    if (NULL == fdi) {
        return FRT_OtherError;
    }

    fdi->errorCode = FRT_Success;

    fdi->decoder = FLAC__stream_decoder_new();
    if(fdi->decoder == NULL) {
        fdi->errorCode = FRT_FlacStreamDecoderNewFailed;
        dprintf("%s Flac decode error %d. set complete event.\n",
                __FUNCTION__, fdi->errorCode);
        goto end;
    }

    wcsncpy_s(fdi->path, path, (sizeof fdi->path)/2-1);

    FLAC__stream_decoder_set_md5_checking(fdi->decoder, true);
    
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_STREAMINFO);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_VORBIS_COMMENT);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_PICTURE);
    FLAC__stream_decoder_set_metadata_respond(fdi->decoder, FLAC__METADATA_TYPE_CUESHEET);

    // Windows�ł́A���̕��@�Ńt�@�C�����J���Ȃ���΂Ȃ�ʁB
    ercd = _wfopen_s(&fp, fdi->path, L"rb");
    if (ercd != 0 || NULL == fp) {
        fdi->errorCode = FRT_FileOpenError;
        goto end;
    }

    initStatus = FLAC__stream_decoder_init_FILE(
            fdi->decoder, fp, WriteCallback, MetadataCallback, ErrorCallback, fdi);

    // FLAC__stream_decoder_finish()��fclose���Ă����̂ŁA�Y���B
    fp = NULL;

    if(initStatus != FLAC__STREAM_DECODER_INIT_STATUS_OK) {
        fdi->errorCode = FRT_FlacStreamDecoderInitFailed;
        dprintf("%s Flac decode error %d. set complete event.\n",
                __FUNCTION__, fdi->errorCode);
        goto end;
    }

    fdi->errorCode = FRT_Success;
    ok = FLAC__stream_decoder_process_until_end_of_metadata(fdi->decoder);
    if (!ok) {
        if (fdi->errorCode == FRT_Success) {
            fdi->errorCode = FRT_DecorderProcessFailed;
        }
        dprintf("%s Flac metadata process error fdi->errorCode=%d\n",
                __FUNCTION__, fdi->errorCode);
        goto end;
    }

    ok = FLAC__stream_decoder_process_until_end_of_stream(fdi->decoder);
    if (!ok) {
        if (fdi->errorCode == FRT_Success) {
                fdi->errorCode = FRT_DecorderProcessFailed;
        }
        dprintf("%s Flac decode error fdi->errorCode=%d\n",
                __FUNCTION__, fdi->errorCode);
        goto end;
    }

    fdi->errorCode = FRT_Completed;

end:
    if (fdi->errorCode < 0) {
        if (NULL != fdi->decoder) {
            if (initStatus == FLAC__STREAM_DECODER_INIT_STATUS_OK) {
                FLAC__stream_decoder_finish(fdi->decoder);
            }
            FLAC__stream_decoder_delete(fdi->decoder);
            fdi->decoder = NULL;
        }

        int result = fdi->errorCode;
        FlacTInfoDelete<FlacDecodeInfo>(g_flacDecodeInfoMap, fdi);
        fdi = NULL;

        return result;
    }

    return fdi->id;
}

#define UTF8TOMB(X) MultiByteToWideChar(CP_UTF8, 0, fdi->X, -1, metaReturn.X, sizeof metaReturn.X-1)

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_GetDecodedMetadata(int id, WWFlacMetadata &metaReturn)
{
    FlacDecodeInfo *fdi = FlacTInfoFindById<FlacDecodeInfo>(g_flacDecodeInfoMap, id);
    if (NULL == fdi) {
        return FRT_IdNotFound;
    }

    metaReturn.sampleRate = fdi->sampleRate;
    metaReturn.channels = fdi->channels;
    metaReturn.bitsPerSample = fdi->bitsPerSample;
    metaReturn.pictureBytes = fdi->pictureBytes;
    metaReturn.totalSamples = fdi->totalSamples;

    UTF8TOMB(titleStr);
    UTF8TOMB(artistStr);
    UTF8TOMB(albumStr);
    UTF8TOMB(albumArtistStr);
    UTF8TOMB(genreStr);

    UTF8TOMB(dateStr);
    UTF8TOMB(trackNumberStr);
    UTF8TOMB(discNumberStr);
    UTF8TOMB(pictureMimeTypeStr);
    UTF8TOMB(pictureDescriptionStr);

    memcpy(metaReturn.md5sum, fdi->md5sum, sizeof metaReturn.md5sum);

    return FRT_Success;
}

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_GetDecodedPicture(int id, uint8_t * pictureReturn, int pictureBytes)
{
    if (NULL == pictureReturn) {
        return FRT_BadParams;
    }

    FlacDecodeInfo *fdi = FlacTInfoFindById<FlacDecodeInfo>(g_flacDecodeInfoMap, id);
    if (NULL == fdi) {
        return FRT_IdNotFound;
    }

    if (pictureBytes != fdi->pictureBytes) {
        return FRT_BadParams;
    }

    memcpy(pictureReturn, fdi->pictureData, pictureBytes);
    return FRT_Success;
}

extern "C" __declspec(dllexport)
int64_t __stdcall
WWFlacRW_GetDecodedPcmBytes(int id, int channel, int64_t startBytes, uint8_t * pcmReturn, int64_t pcmBytes)
{
    if (NULL == pcmReturn || pcmBytes <= 0) {
        return FRT_BadParams;
    }

    FlacDecodeInfo *fdi = FlacTInfoFindById<FlacDecodeInfo>(g_flacDecodeInfoMap, id);
    if (NULL == fdi) {
        return FRT_IdNotFound;
    }

    if (fdi->channels <= channel){
        return FRT_OtherError;
    }

    if (fdi->totalBytesPerChannel <= startBytes) {
        return FRT_RecvBufferSizeInsufficient;
    }

    int64_t copyBytes = pcmBytes;
    if (fdi->totalBytesPerChannel < startBytes + pcmBytes) {
        copyBytes = fdi->totalBytesPerChannel - startBytes;
    }

    memcpy(pcmReturn, &fdi->buffPerChannel[channel][startBytes], copyBytes);
    return copyBytes;
}

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_GetDecodedPictureBytes(int id, uint8_t * pictureReturn, int pictureBytes)
{
    if (NULL == pictureReturn || pictureBytes <= 0) {
        return FRT_BadParams;
    }

    FlacDecodeInfo *fdi = FlacTInfoFindById<FlacDecodeInfo>(g_flacDecodeInfoMap, id);
    if (NULL == fdi) {
        return FRT_IdNotFound;
    }

    if (pictureBytes != fdi->pictureBytes) {
        return FRT_BufferSizeMismatch;
    }

    memcpy(pictureReturn, fdi->pictureData, pictureBytes);
    return pictureBytes;
}

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_DecodeEnd(int id)
{
    FlacDecodeInfo *fdi = FlacTInfoFindById<FlacDecodeInfo>(g_flacDecodeInfoMap, id);
    if (NULL == fdi) {
        return FRT_OtherError;
    }

    int ercd = fdi->errorCode;
    FlacTInfoDelete<FlacDecodeInfo>(g_flacDecodeInfoMap, fdi);
    fdi = NULL;

    return ercd;
}

////////////////////////////////////////////////////////////////////////////////////////////////

enum FlacMetaType {
    FMT_VorbisComment,
    FMT_Picture,
    FMT_NUM
};

struct FlacEncodeInfo {
    wchar_t path[MAX_PATH];

    FlacRWResultType errorCode;
    FLAC__StreamEncoder *encoder;
    FLAC__StreamMetadata *flacMetaArray[FMT_NUM];
    int                  flacMetaCount;


    int          id;
    int          sampleRate;
    int          channels;
    int          bitsPerSample;

    int64_t      totalSamples;
    int64_t      totalBytesPerChannel;

    uint8_t           **buffPerChannel;

    char titleStr[WWFLAC_TEXT_STRSZ];
    char artistStr[WWFLAC_TEXT_STRSZ];
    char albumStr[WWFLAC_TEXT_STRSZ];
    char albumArtistStr[WWFLAC_TEXT_STRSZ];
    char genreStr[WWFLAC_TEXT_STRSZ];

    char dateStr[WWFLAC_TEXT_STRSZ];
    char trackNumberStr[WWFLAC_TEXT_STRSZ];
    char discNumberStr[WWFLAC_TEXT_STRSZ];
    char pictureMimeTypeStr[WWFLAC_TEXT_STRSZ];
    char pictureDescriptionStr[WWFLAC_TEXT_STRSZ];

    int               pictureBytes;
    uint8_t           *pictureData;

    std::vector<FlacCuesheetTrackInfo> cueSheetTracks;

    FlacEncodeInfo(void) {
        Clear();
    }

    void Clear(void) {
        memset(path, 0, sizeof path);

        errorCode = FRT_OtherError;
        encoder = NULL;

        for (int i=0; i<FMT_NUM; ++i) {
            flacMetaArray[i] = NULL;
        }

        id = -1;
        sampleRate = 0;
        channels = 0;
        bitsPerSample = 0;

        totalSamples = 0;
        totalBytesPerChannel = 0;

        buffPerChannel = NULL;

        memset(titleStr,       0, sizeof titleStr);
        memset(artistStr,      0, sizeof artistStr);
        memset(albumStr,       0, sizeof albumStr);
        memset(albumArtistStr, 0, sizeof albumArtistStr);
        memset(genreStr,       0, sizeof genreStr);

        memset(dateStr,        0, sizeof dateStr);
        memset(trackNumberStr, 0, sizeof trackNumberStr);
        memset(discNumberStr,  0, sizeof discNumberStr);
        memset(pictureMimeTypeStr,    0, sizeof pictureMimeTypeStr);
        memset(pictureDescriptionStr,    0, sizeof pictureDescriptionStr);

        pictureBytes = 0;
        pictureData = NULL;

        flacMetaCount = 0;

        cueSheetTracks.clear();
    }
   
    ~FlacEncodeInfo(void) {
        if (buffPerChannel) {
            for (int ch=0; ch<channels; ++ch) {
                delete [] buffPerChannel[ch];
                buffPerChannel[ch] = NULL;
            }
            delete [] buffPerChannel;
            buffPerChannel = NULL;
        }

        delete [] pictureData;
        pictureData = NULL;
    }

    static int nextId;
};

int FlacEncodeInfo::nextId = 0x40000000;

/// ���u�̎��́B�O���[�o���ϐ��B
static std::map<int, FlacEncodeInfo*> g_flacEncodeInfoMap;


static void
ProgressCallback(const FLAC__StreamEncoder *encoder, FLAC__uint64 bytesWritten, FLAC__uint64 samplesWritten,
        unsigned framesWritten, unsigned totalFramesEstimate, void *clientData)
{
    (void)encoder;
    FlacEncodeInfo *fei = (FlacEncodeInfo*)clientData;

    // dprintf("wrote %llu bytes, %llu/%llu samples, %u/%u frames\n", bytesWritten, samplesWritten, fei->totalSamples, framesWritten, totalFramesEstimate);
}

static void
DeleteFlacMetaArray(FlacEncodeInfo *fei)
{
    for (int i=0; i<FMT_NUM; ++i) {
        FLAC__metadata_object_delete(fei->flacMetaArray[i]);
        fei->flacMetaArray[i] = NULL;
    }
}

#define ADD_TAG(V, S)                                                                                                  \
    if(strlen(fei->V) &&                                                                                               \
            FLAC__metadata_object_vorbiscomment_entry_from_name_value_pair(&entry, S, fei->V) &&                       \
            FLAC__metadata_object_vorbiscomment_append_comment(fei->flacMetaArray[FMT_VorbisComment], entry, false)) { \
    }

#define WCTOUTF8(X) WideCharToMultiByte(CP_UTF8, 0, meta.X, -1, fei->X, sizeof fei->X-1,  NULL, NULL)

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_EncodeInit(const WWFlacMetadata &meta)
{
    FLAC__bool                    ok = true;
    FLAC__StreamMetadata_VorbisComment_Entry entry;

    FlacEncodeInfo *fei = FlacTInfoNew<FlacEncodeInfo>(g_flacEncodeInfoMap);
    if (NULL == fei) {
        return FRT_OtherError;
    }
    
    fei->errorCode = FRT_Success;

    fei->sampleRate = meta.sampleRate;
    fei->channels = meta.channels;
    fei->bitsPerSample = meta.bitsPerSample;
    fei->totalSamples = meta.totalSamples;
    fei->totalBytesPerChannel = meta.totalSamples * fei->bitsPerSample/8;
    fei->pictureBytes = meta.pictureBytes;

    assert(NULL == fei->buffPerChannel);
    fei->buffPerChannel = new uint8_t*[fei->channels];
    if (NULL == fei->buffPerChannel) {
        return FRT_MemoryExhausted;
    }
    memset(fei->buffPerChannel, 0, sizeof(uint8_t*)*fei->channels);
    
    WCTOUTF8(titleStr);
    WCTOUTF8(artistStr);
    WCTOUTF8(albumStr);
    WCTOUTF8(albumArtistStr);
    WCTOUTF8(genreStr);

    WCTOUTF8(dateStr);
    WCTOUTF8(trackNumberStr);
    WCTOUTF8(discNumberStr);
    WCTOUTF8(pictureMimeTypeStr);
    WCTOUTF8(pictureDescriptionStr);

    if((fei->encoder = FLAC__stream_encoder_new()) == NULL) {
        dprintf("FLAC__stream_encoder_new failed\n");
        fei->errorCode = FRT_OtherError;
        goto end;
    }

    ok &= FLAC__stream_encoder_set_verify(fei->encoder, true);
    ok &= FLAC__stream_encoder_set_compression_level(fei->encoder, 5);
    ok &= FLAC__stream_encoder_set_channels(fei->encoder, fei->channels);
    ok &= FLAC__stream_encoder_set_bits_per_sample(fei->encoder, fei->bitsPerSample);
    ok &= FLAC__stream_encoder_set_sample_rate(fei->encoder, fei->sampleRate);
    ok &= FLAC__stream_encoder_set_total_samples_estimate(fei->encoder, fei->totalSamples);

    if(!ok) {
        dprintf("FLAC__stream_encoder_set_??? failed\n");
        fei->errorCode = FRT_OtherError;
        goto end;
    }

    if((fei->flacMetaArray[FMT_VorbisComment] = FLAC__metadata_object_new(FLAC__METADATA_TYPE_VORBIS_COMMENT)) == NULL) {
        dprintf("FLAC__metadata_object_new vorbis comment failed\n");
        fei->errorCode = FRT_OtherError;
        goto end;
    }
    if((fei->flacMetaArray[FMT_Picture] = FLAC__metadata_object_new(FLAC__METADATA_TYPE_PICTURE)) == NULL) {
        dprintf("FLAC__metadata_object_new picture failed\n");
        fei->errorCode = FRT_OtherError;
        goto end;
    }

    fei->flacMetaCount = 1;

    ADD_TAG(titleStr,       "TITLE");
    ADD_TAG(artistStr,      "ARTIST");
    ADD_TAG(albumStr,       "ALBUM");
    ADD_TAG(albumArtistStr, "ALBUMARTIST");
    ADD_TAG(genreStr,       "GENRE");

    ADD_TAG(dateStr,        "DATE");
    ADD_TAG(trackNumberStr, "TRACKNUMBER");
    ADD_TAG(discNumberStr,  "DISCNUMBER");

end:
    if (fei->errorCode < 0) {
        if (NULL != fei->encoder) {
            FLAC__stream_encoder_delete(fei->encoder);
            fei->encoder = NULL;
        }

        DeleteFlacMetaArray(fei);

        int result = fei->errorCode;
        FlacTInfoDelete<FlacEncodeInfo>(g_flacEncodeInfoMap, fei);
        fei = NULL;

        return result;
    }

    return fei->id;
}

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_EncodeSetPicture(int id, const uint8_t * pictureData, int pictureBytes)
{
    if (NULL == pictureData || pictureBytes <= 0) {
        dprintf("%s parameter pictureData or pictureBytes error\n", __FUNCTION__);
        return FRT_BadParams;
    }

    FlacEncodeInfo *fei = FlacTInfoFindById<FlacEncodeInfo>(g_flacEncodeInfoMap, id);
    if (NULL == fei) {
        return FRT_IdNotFound;
    }

    if (fei->pictureData != NULL) {
        dprintf("%s already has picture data!\n", __FUNCTION__);
        return FRT_BadParams;
    }

    assert(fei->flacMetaArray[FMT_Picture]);

    if (0 < fei->pictureBytes && 0 != fei->pictureMimeTypeStr[0]) {
        // copy==false�ɂ���ƊJ�����ɃN���b�V������
        if (!FLAC__metadata_object_picture_set_mime_type(fei->flacMetaArray[FMT_Picture], fei->pictureMimeTypeStr, true)) {
            dprintf("FLAC__metadata_object_picture_set_mime_type failed\n");
            fei->errorCode = FRT_OtherError;
            goto end;
        }
    }

    if (0 < fei->pictureBytes && 0 != fei->pictureDescriptionStr[0]) {
        if (!FLAC__metadata_object_picture_set_description(fei->flacMetaArray[FMT_Picture], (FLAC__byte*)fei->pictureDescriptionStr, true)) {
            dprintf("FLAC__metadata_object_picture_set_description failed\n");
            fei->errorCode = FRT_OtherError;
            goto end;
        }
    }

    fei->pictureBytes = pictureBytes;
    fei->pictureData = new uint8_t[fei->pictureBytes];
    if (NULL == fei->pictureData) {
        dprintf("%s could not alloc picture data!\n", __FUNCTION__);
        fei->errorCode = FRT_MemoryExhausted;
        goto end;
    }

    memcpy(fei->pictureData, pictureData, fei->pictureBytes);
    if (!FLAC__metadata_object_picture_set_data(fei->flacMetaArray[FMT_Picture], fei->pictureData, fei->pictureBytes, true)) {
        fei->errorCode = FRT_OtherError;
        goto end;
    }

    fei->flacMetaCount = 2;

end:

    return fei->errorCode;
}

extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_EncodeAddPcm(int id, int channel, const uint8_t * pcmData, int64_t pcmBytes)
{
    if (NULL == pcmData || pcmBytes <= 0 || channel < 0) {
        dprintf("%s parameter pcmData or pcmBytes or channel error\n", __FUNCTION__);
        return FRT_BadParams;
    }

    FlacEncodeInfo *fei = FlacTInfoFindById<FlacEncodeInfo>(g_flacEncodeInfoMap, id);
    if (NULL == fei) {
        return FRT_IdNotFound;
    }

    if (fei->channels <= channel) {
        dprintf("%s parameter channel too large\n", __FUNCTION__);
        return FRT_BadParams;
    }

    if (fei->totalBytesPerChannel != pcmBytes) {
        dprintf("%s parameter pcmBytes mismatch. must be %lld\n", __FUNCTION__, fei->totalBytesPerChannel);
        return FRT_BadParams;
    }

    if (fei->buffPerChannel[channel]) {
        dprintf("%s channel %d already has data!\n", __FUNCTION__, fei->channels);
        return FRT_BadParams;
    }

    fei->buffPerChannel[channel] = new uint8_t[pcmBytes];
    if (NULL == fei->buffPerChannel[channel]) {
        dprintf("%s could not allocm memory\n", __FUNCTION__);
        return FRT_MemoryExhausted;
    }

    memcpy(fei->buffPerChannel[channel], pcmData, pcmBytes);
    return FRT_Success;
}


/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_EncodeRun(int id, const wchar_t *path)
{
    FILE *fp = NULL;
    errno_t ercd;
    int64_t left;
    int64_t readPos;
    int64_t writePos;
    FLAC__bool ok = true;
    FLAC__int32 *pcm = NULL;

    if (NULL == path || wcslen(path) == 0) {
        return FRT_BadParams;
    }

    FLAC__StreamEncoderInitStatus initStatus = FLAC__STREAM_ENCODER_INIT_STATUS_ENCODER_ERROR;

    FlacEncodeInfo *fei = FlacTInfoFindById<FlacEncodeInfo>(g_flacEncodeInfoMap, id);
    if (NULL == fei) {
        return FRT_IdNotFound;
    }

    if (0 < fei->pictureBytes && fei->pictureData == NULL) {
        dprintf("%s picture data is not set yet.\n", __FUNCTION__);
        return FRT_DataNotReady;
    }

    assert(fei->buffPerChannel);

    ok = FLAC__stream_encoder_set_metadata(fei->encoder, &fei->flacMetaArray[0], fei->flacMetaCount);
    if(!ok) {
        dprintf("FLAC__stream_encoder_set_metadata failed\n");
        fei->errorCode = FRT_OtherError;
        goto end;
    }

    for (int ch=0; ch<fei->channels; ++ch) {
        if (fei->buffPerChannel[ch] == NULL){
            dprintf("%s pcm buffer is not set yet.\n", __FUNCTION__);
            return FRT_DataNotReady;
        }
    }

    if (fei->bitsPerSample != 16 && fei->bitsPerSample != 24) {
        return FRT_InvalidBitsPerSample;
    }

    pcm = new FLAC__int32[FLACENCODE_READFRAMES * fei->channels];
    if (pcm == NULL) {
        return FRT_MemoryExhausted;
    }

    // Windows�ł́A���̕��@�Ńt�@�C�����J���Ȃ���΂Ȃ�ʁB
    wcsncpy_s(fei->path, path, (sizeof fei->path)/2-1);
    ercd = _wfopen_s(&fp, fei->path, L"wb");
    if (ercd != 0 || NULL == fp) {
        fei->errorCode = FRT_FileOpenError;
        goto end;
    }

    initStatus = FLAC__stream_encoder_init_FILE(fei->encoder, fp, ProgressCallback, fei);
    if(initStatus != FLAC__STREAM_ENCODER_INIT_STATUS_OK) {
        dprintf("FLAC__stream_encoder_init_FILE failed %s\n", FLAC__StreamEncoderInitStatusString[initStatus]);
        switch (initStatus) {
        case FLAC__STREAM_ENCODER_INIT_STATUS_ENCODER_ERROR:
            {
                FLAC__StreamDecoderState state = FLAC__stream_encoder_get_verify_decoder_state(fei->encoder);
                dprintf("decoderState=%d\n", state);
            }
            fei->errorCode = FRT_EncoderError;
            goto end;
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_NUMBER_OF_CHANNELS:
            fei->errorCode = FRT_InvalidNumberOfChannels;
            goto end;
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_BITS_PER_SAMPLE:
            fei->errorCode = FRT_InvalidBitsPerSample;
            goto end;
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_SAMPLE_RATE:
            fei->errorCode = FRT_InvalidSampleRate;
            goto end;
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_METADATA:
            fei->errorCode = FRT_InvalidMetadata;
            goto end;
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_CALLBACKS:
        case FLAC__STREAM_ENCODER_INIT_STATUS_ALREADY_INITIALIZED:
        case FLAC__STREAM_ENCODER_INIT_STATUS_UNSUPPORTED_CONTAINER:
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_BLOCK_SIZE:
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_MAX_LPC_ORDER:
        case FLAC__STREAM_ENCODER_INIT_STATUS_INVALID_QLP_COEFF_PRECISION:
        case FLAC__STREAM_ENCODER_INIT_STATUS_BLOCK_SIZE_TOO_SMALL_FOR_LPC_ORDER:
        case FLAC__STREAM_ENCODER_INIT_STATUS_NOT_STREAMABLE:
        default:
            fei->errorCode = FRT_OtherError;
            goto end;
        }
    }
    fp = NULL;

    readPos = 0;
    left = fei->totalSamples;
    while(ok && left) {
        uint32_t need = left>FLACENCODE_READFRAMES ? FLACENCODE_READFRAMES : (unsigned int)left;

        // create interleaved PCM samples to pcm[]
        writePos = 0;
        switch (fei->bitsPerSample) {
        case 16:
            for (uint32_t i=0; i<need; ++i) {
                for (int ch=0; ch<fei->channels;++ch) {
                    uint8_t *p = &fei->buffPerChannel[ch][readPos];
                    int v = (p[0]<<16) + (p[1]<<24);
                    pcm[writePos] = v>>16;
                    ++writePos;
                }
                readPos += 2;
            }
            break;
        case 24:
            for (uint32_t i=0; i<need; ++i) {
                for (int ch=0; ch<fei->channels;++ch) {
                    uint8_t *p = &fei->buffPerChannel[ch][readPos];
                    int v = (p[0]<<8) + (p[1]<<16) + (p[2]<<24);
                    pcm[writePos] = v >> 8;
                    ++writePos;
                }
                readPos += 3;
            }
            break;
        default:
            assert(0);
            break;
        }

        ok = FLAC__stream_encoder_process_interleaved(fei->encoder, pcm, need);
        left -= need;
    }
    if (!ok) {
        dprintf("FLAC__stream_encoder_process_interleaved failed");
        fei->errorCode = FRT_EncoderProcessFailed;
    }

end:
    delete [] pcm;
    pcm = NULL;

    if (NULL != fp) {
        fclose(fp);
        fp = NULL;
    }

    if (NULL != fei->encoder) {
        if (initStatus == FLAC__STREAM_ENCODER_INIT_STATUS_OK) {
            FLAC__stream_encoder_finish(fei->encoder);
        }

        DeleteFlacMetaArray(fei);

        FLAC__stream_encoder_delete(fei->encoder);
        fei->encoder = NULL;
    }

    if (fei->errorCode < 0) {
        int result = fei->errorCode;
        FlacTInfoDelete<FlacEncodeInfo>(g_flacEncodeInfoMap, fei);
        fei = NULL;

        return result;
    }

    return fei->id;
}


/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" __declspec(dllexport)
int __stdcall
WWFlacRW_EncodeEnd(int id)
{
    FlacEncodeInfo *fei = FlacTInfoFindById<FlacEncodeInfo>(g_flacEncodeInfoMap, id);
    if (NULL == fei) {
        return FRT_IdNotFound;
    }

    if (NULL != fei->encoder) {
        FLAC__stream_encoder_delete(fei->encoder);
        fei->encoder = NULL;
    }
    FlacTInfoDelete<FlacEncodeInfo>(g_flacEncodeInfoMap, fei);

    return FRT_Success;
}
