// 日本語UTF-8

#include "stdafx.h"
#include <stdio.h>
#include <stdlib.h>
#include "FLAC/stream_decoder.h"
#include "Flac2Wav.h"

// x86 CPUにしか対応してない。
// x64やビッグエンディアンには対応してない。

/// Flacコールバックに渡すclientData引数。
struct FlacDecodeArgs {
    FLAC__uint64 totalSamples;
    FILE         *fout;
    int          sampleRate;
    int          channels;
    int          bitsPerSample;
    Flac2WavResultType errorCode;
};

#define RG(x,v)                                   \
{                                                 \
    rv = x;                                       \
    if (v != rv) {                                \
        goto end;                                 \
    }                                             \
}                                                 \

static bool
WriteWaveHeader(
    FlacDecodeArgs *args)
{
    bool result = false;
    size_t rv;
    unsigned int totalSize
        = (unsigned int)(args->totalSamples * args->channels * (args->bitsPerSample/8));
    int iv;
    short sv;

    RG(fwrite("RIFF", 1, 4, args->fout),4);
    
    iv = totalSize + 36;
    RG(fwrite(&iv, 1, 4, args->fout), 4);

    RG(fwrite("WAVEfmt ", 1, 8, args->fout), 8);

    iv=16;
    RG(fwrite(&iv, 1, 4, args->fout), 4);

    sv=1;
    RG(fwrite(&sv, 1, 2, args->fout), 2);

    sv = args->channels;
    RG(fwrite(&sv, 1, 2, args->fout), 2);

    iv=args->sampleRate;
    RG(fwrite(&iv, 1, 4, args->fout), 4);

    iv=args->sampleRate * args->channels * (args->bitsPerSample/8);
    RG(fwrite(&iv, 1, 4, args->fout), 4);

    sv = args->channels * (args->bitsPerSample/8);
    RG(fwrite(&sv, 1, 2, args->fout), 2);

    sv = args->bitsPerSample;
    RG(fwrite(&sv, 1, 2, args->fout), 2);

    RG(fwrite("data", 1, 4, args->fout), 4);

    iv=totalSize;
    RG(fwrite(&iv, 1, 4, args->fout), 4);

    result =true;
end:
    return result;
}

////////////////////////////////////////////////////////////////////////
// FLACデコーダーコールバック

static FLAC__StreamDecoderWriteStatus
WriteCallback(const FLAC__StreamDecoder *decoder,
    const FLAC__Frame *frame, const FLAC__int32 * const buffer[],
    void *clientData)
{
    FlacDecodeArgs *args = (FlacDecodeArgs*)clientData;
    size_t i;

    if(args->totalSamples == 0) {
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }
    if(args->channels != 2
        || (args->bitsPerSample != 16
         && args->bitsPerSample != 24)) {
        return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
    }

    if(frame->header.number.sample_number == 0) {
        // 最初のデータが来た。WAVEヘッダを出力する。
        if (!WriteWaveHeader(args)) {
            return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
        }
    }

    if (args->bitsPerSample == 16) {
        for(i = 0; i < frame->header.blocksize; i++) {
            if (2 != fwrite(&buffer[0][i], 1, 2, args->fout)) {
                return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
            }
            if (2 != fwrite(&buffer[1][i], 1, 2, args->fout)) {
                return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
            }
        }
    }

    if (args->bitsPerSample == 24) {
        for(i = 0; i < frame->header.blocksize; i++) {
            if (3 != fwrite(&buffer[0][i], 1, 3, args->fout)) {
                return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
            }
            if (3 != fwrite(&buffer[1][i], 1, 3, args->fout)) {
                return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
            }
        }
    }

    return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
}

static void
MetadataCallback(const FLAC__StreamDecoder *decoder,
    const FLAC__StreamMetadata *metadata, void *clientData)
{
    FlacDecodeArgs *args = (FlacDecodeArgs*)clientData;

    if(metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
        args->totalSamples  = metadata->data.stream_info.total_samples;
        args->sampleRate    = metadata->data.stream_info.sample_rate;
        args->channels      = metadata->data.stream_info.channels;
        args->bitsPerSample = metadata->data.stream_info.bits_per_sample;
    }
}

static void
ErrorCallback(const FLAC__StreamDecoder *decoder,
    FLAC__StreamDecoderErrorStatus status, void *clientData)
{
    FlacDecodeArgs *args = (FlacDecodeArgs*)clientData;

    switch (status) {
    case FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC:
        args->errorCode = F2WRT_LostSync;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER:
        args->errorCode = F2WRT_BadHeader;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH:
        args->errorCode = F2WRT_FrameCrcMismatch;
        break;
    case FLAC__STREAM_DECODER_ERROR_STATUS_UNPARSEABLE_STREAM:
        args->errorCode = F2WRT_Unparseable;
        break;
    default:
        args->errorCode = F2WRT_OtherError;
        break;
    }
};

extern "C" __declspec(dllexport)
int __stdcall
Flac2Wav(const char *fromFlacPath, const char *toWavPath)
{
    int result = F2WRT_Success;
    FLAC__bool ok = true;
    FLAC__StreamDecoder *decoder = NULL;
    FLAC__StreamDecoderInitStatus init_status;
    FlacDecodeArgs args;

    memset(&args, 0, sizeof args);
    args.errorCode = F2WRT_OtherError;

    args.fout = fopen(toWavPath, "wb");
    if (args.fout == NULL) {
        result = F2WRT_WriteOpenFailed;
        goto end;
    }

    decoder = FLAC__stream_decoder_new();
    if(decoder == NULL) {
        result = F2WRT_FlacStreamDecoderNewFailed;
        goto end;
    }

    FLAC__stream_decoder_set_md5_checking(decoder, true);

    init_status = FLAC__stream_decoder_init_file(
        decoder, fromFlacPath, WriteCallback, MetadataCallback, ErrorCallback, &args);
    if(init_status != FLAC__STREAM_DECODER_INIT_STATUS_OK) {
        result = F2WRT_FlacStreamDecoderInitFailed;
        goto end;
    }

    ok = FLAC__stream_decoder_process_until_end_of_stream(decoder);
    if (!ok) {
        result = args.errorCode;
        goto end;
    }

    result = F2WRT_Success;
end:
    if (NULL != decoder) {
        FLAC__stream_decoder_delete(decoder);
        decoder = NULL;
    }
    if (NULL != args.fout) {
        fclose(args.fout);
        args.fout = NULL;
    }

    return result;
}

