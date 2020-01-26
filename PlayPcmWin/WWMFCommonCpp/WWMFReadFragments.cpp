// 日本語

#include "WWMFReadFragments.h"
#include "WWMFReaderFunctions.h"
#include <stdio.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>

static HRESULT
GetAudioEncodingBitrate(IMFSourceReader *pReader, UINT32 *bitrate_return)
{
    PROPVARIANT var;
    PropVariantInit(&var);
    HRESULT hr = S_OK;

    HRG(pReader->GetPresentationAttribute(
        MF_SOURCE_READER_MEDIASOURCE,
        MF_PD_AUDIO_ENCODING_BITRATE, &var));

    PropVariantToUInt32(var, bitrate_return);

end:
    PropVariantClear(&var);
    return hr;
}

static HRESULT
GetUncompressedPcmAudio(
    IMFSourceReader *pReader,
    IMFMediaType **ppPCMAudio)
{
    HRESULT hr = S_OK;

    assert(pReader);
    *ppPCMAudio = nullptr;
    IMFMediaType *pUncompressedAudioType = nullptr;
    IMFMediaType *pPartialType = nullptr;

    // Create a partial media type that specifies uncompressed PCM audio.
    HRG(MFCreateMediaType(&pPartialType));
    HRG(pPartialType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio));
    HRG(pPartialType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM));

    // Set this type on the source reader. The source reader will
    // load the necessary decoder.
    HRG(pReader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, nullptr, pPartialType));

    // Get the complete uncompressed format.
    HRG(pReader->GetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, &pUncompressedAudioType));

    // Ensure the stream is selected.
    HRG(pReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, TRUE));

    // Return the PCM format to the caller.
    {
        *ppPCMAudio = pUncompressedAudioType;
        (*ppPCMAudio)->AddRef();
    }

end:
    SafeRelease(&pUncompressedAudioType);
    SafeRelease(&pPartialType);
    return hr;
}

HRESULT
WWMFReadFragments::Start(const wchar_t *wszSourceFile, WAVEFORMATEXTENSIBLE *mfext_r)
{
    HRESULT              hr           = S_OK;
    IMFMediaType         *pMTPcmAudio = nullptr;
    UINT32               cbFormat     = 0;
    WAVEFORMATEX         *pWfex       = nullptr;
    WAVEFORMATEXTENSIBLE *pWfext      = nullptr;
    memset(&mMfext, 0, sizeof mMfext);
    
    HRG(MFStartup(MF_VERSION));
    mMFStarted = true;

    HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &mReader));
    HRG(WWMFReaderConfigureAudioTypeToUncompressedPcm(mReader));

    HRG(GetUncompressedPcmAudio(mReader, &pMTPcmAudio));

    HRG(MFCreateWaveFormatExFromMFMediaType(pMTPcmAudio, &pWfex, &cbFormat));
    if (22 <= pWfex->cbSize) {
        // WAVEFORMATEXTENSIBLEが入っていた。
        mMfext = *((WAVEFORMATEXTENSIBLE*)pWfex);
    } else {
        // WAVEFORMATEXが入っていた。
        WAVEFORMATEX *pTo = (WAVEFORMATEX*)&mMfext;
        *pTo = *pWfex;
    }

    if (mfext_r) {
        *mfext_r = mMfext;
    }

end:
    return hr;
}

HRESULT
WWMFReadFragments::SeekToFrame(int64_t &nFrame_inout)
{
    HRESULT hr = S_OK;
    assert(mReader);

    PROPVARIANT pv;
    PropVariantInit(&pv);

    // 100 nanosec = 1tick
    int sampleRate = mMfext.Format.nSamplesPerSec;
    double posSec = (double)nFrame_inout / sampleRate;

    pv.vt= VT_I8;
    //                                      m     μ     n
    pv.hVal.QuadPart = (int64_t)(posSec * 1000 * 1000 * 10);

    HRG(mReader->SetCurrentPosition(
        GUID_NULL, // 100 nanosecond 
        pv));

end:
    PropVariantClear(&pv);

    return hr;
}


HRESULT
WWMFReadFragments::ReadFragment(
        unsigned char *data_return,
        int64_t *dataBytes_inout)
{
    HRESULT hr = S_OK;
    assert(mMFStarted);
    assert(data_return);
    const int64_t cbMaxAudioData = *dataBytes_inout;
    assert(0 < cbMaxAudioData);
    DWORD streamIdx = 0;

    *dataBytes_inout = 0;

    IMFSample *pSample = nullptr;
    IMFMediaBuffer *pBuffer = nullptr;
    DWORD cbBuffer = 0;
    LONGLONG timeStamp;

    // pSampleが1個出てくるまで繰り返す。
    while (true) {
        DWORD dwFlags = 0;
        assert(pSample == nullptr);
        HRB_Quiet(mReader->ReadSample(
            (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM,
            0,
            &streamIdx,
            &dwFlags,
            &timeStamp,
            &pSample));

        if (dwFlags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) {
            dprintf("Type change - not supported by WAVE file format.\n");
            goto end;
        }
        if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM) {
            //dprintf("End of input file.\n");
            goto end;
        }

        if (pSample == nullptr) {
            dprintf("No sample\n");
            continue;
        } else {
            // pSampleが出てきた。
            break;
        }
    }

    do {
        BYTE *pAudioData = nullptr;

        assert(pSample);
        assert(pBuffer == nullptr);
        HRB_Quiet(pSample->ConvertToContiguousBuffer(&pBuffer));

        cbBuffer = 0;
        HRB_Quiet(pBuffer->Lock(&pAudioData, NULL, &cbBuffer));

        if (cbMaxAudioData < cbBuffer) {
            // 十分に大きいサイズを指定して呼んで下さい。
            dprintf("D: %s:%d Result data trimmed! cbMaxAudioData=%lld, cbBuffer=%u\n",
                __FILE__, __LINE__, cbMaxAudioData, cbBuffer);
            cbBuffer = (DWORD)cbMaxAudioData;
        }

        memcpy(&data_return[0], pAudioData, cbBuffer);

        hr = pBuffer->Unlock();
        pAudioData = nullptr;
    } while (false);

end:
    *dataBytes_inout = cbBuffer;

    SafeRelease(&pSample);
    SafeRelease(&pBuffer);

    return hr;
};

void WWMFReadFragments::End(void)
{
    SafeRelease(&mReader);
    if (mMFStarted) {
        MFShutdown();
        mMFStarted =false;
    }
}


