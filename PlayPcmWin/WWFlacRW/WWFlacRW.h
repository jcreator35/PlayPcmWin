#pragma once

#include <stdint.h>

#ifdef WWFLACRW_EXPORTS
#define WWFLACRW_API __declspec(dllexport)
#else
#define WWFLACRW_API __declspec(dllimport)
#endif

enum FlacRWResultType {
    /// �w�b�_�̎擾��f�[�^�̎擾�ɐ����B
    FRT_Success = 0,

    /// �t�@�C���̍Ō�܂ōs���Acodec�����������B�����f�[�^�͂Ȃ��B
    FRT_Completed = 1,

    // �ȉ��AFLAC�f�R�[�h�G���[�B
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
};

#define WWFLAC_TEXT_STRSZ   (256)
#define WWFLAC_MD5SUM_BYTES (16)

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
#pragma pack(pop)

///////////////////////////////////////////////////////////////////////////////////////////////////
// flac decode

/// FLAC�w�b�_�[��ǂݍ���ŁA�t�H�[�}�b�g�����擾�A���ׂẴT���v���f�[�^���擾�B
/// ���̃O���[�o���ϐ��ɒ��߂�B
/// @param skipSamples �X�L�b�v����T���v�����B0�ȊO�̒l���w�肷���MD5�̃`�F�b�N���s��Ȃ��Ȃ�̂Œ��ӁB
/// @param fromFlacPath �p�X��(UTF-16)
/// @return 0�ȏ�: �f�R�[�_�[Id�B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_DecodeAll(const wchar_t *path);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedMetadata(int id, WWFlacMetadata &metaReturn);

/// @return 0�ȏ�: �R�s�[�����o�C�g���B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_GetDecodedPicture(int id, uint8_t * pictureReturn, int pictureBytes);

/// @return 0�ȏ�: �R�s�[�����o�C�g���B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int64_t __stdcall
WWFlacRW_GetDecodedPcmBytes(int id, int channel, int64_t startBytes, uint8_t * pcmReturn, int64_t pcmBytes);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_DecodeEnd(int id);


///////////////////////////////////////////////////////////////////////////////////////////////////
// flac encode

/// @return 0�ȏ�: �f�R�[�_�[Id�B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeInit(const WWFlacMetadata &meta);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeSetPicture(int id, const uint8_t * pictureData, int pictureBytes);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeAddPcm(int id, int channel, const uint8_t * pcmData, int64_t pcmBytes);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeRun(int id, const wchar_t *path);

/// @return 0�ȏ�: �����B��: �G���[�BFlacRWResultType�Q�ƁB
extern "C" WWFLACRW_API
int __stdcall
WWFlacRW_EncodeEnd(int id);

