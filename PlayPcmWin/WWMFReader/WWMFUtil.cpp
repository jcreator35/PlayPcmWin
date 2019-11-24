// 日本語

#include "WWMFUtil.h"

HRESULT
WWMFUtilConfigureAudioTypeToUncompressedPcm(
    IMFSourceReader *pReader)
{
    HRESULT hr = S_OK;

    assert(pReader);
    IMFMediaType *pPartialType = nullptr;

    // Create a partial media type that specifies uncompressed PCM audio.
    HRG(MFCreateMediaType(&pPartialType));
    HRG(pPartialType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio));
    HRG(pPartialType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM));

    // Set this type on the source reader. The source reader will
    // load the necessary decoder.
    HRG(pReader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, nullptr, pPartialType));

    // Ensure the stream is selected.
    HRG(pReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, TRUE));

end:
    SafeRelease(&pPartialType);
    return hr;
}

