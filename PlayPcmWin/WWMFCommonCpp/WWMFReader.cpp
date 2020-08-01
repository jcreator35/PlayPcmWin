// 日本語

#include "WWMFReader.h"
#include "WWCommonUtil.h"
#include "WWMFReaderFunctions.h"
#include <list>
#include <stdint.h>
#include <assert.h>

// Inspired from "Windows-classic-samples/Samples/Win7Samples/multimedia/mediafoundation/AudioClip sample"

static HRESULT
CountPcmBytes(IMFSourceReader *pReader, int64_t &bytes_return)
{
    HRESULT hr = S_OK;

    assert(pReader);
    bytes_return = 0;

    IMFSample *pSample = nullptr;

    while (true) {
        DWORD dwFlags = 0;
        assert(pSample == nullptr);
        HRB_Quiet(pReader->ReadSample(
            (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM,
            0,
            NULL,
            &dwFlags,
            NULL,
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

            IMFMediaBuffer *pBuffer = nullptr;
            do {
                DWORD cbBuffer = 0;
                BYTE *pAudioData = nullptr;

                assert(pSample);
                assert(pBuffer == nullptr);
                HRB_Quiet(pSample->ConvertToContiguousBuffer(&pBuffer));

                cbBuffer = 0;
                HRB_Quiet(pBuffer->Lock(&pAudioData, NULL, &cbBuffer));

                hr = pBuffer->Unlock();
                pAudioData = nullptr;

                bytes_return += cbBuffer;

            } while (false);

            SafeRelease(&pBuffer);
            SafeRelease(&pSample);
        }
    }

end:

    SafeRelease(&pSample);

    return hr;
};


int
WWMFReaderReadHeader(
        const wchar_t *wszSourceFile, int flags,
        WWMFReaderMetadata *meta_return)
{
    HRESULT hr = S_OK;

    IMFSourceReader *pReader = nullptr;
    IMFMediaType *pMTPcmAudio = nullptr;
    IMFMetadataProvider * pMetaProvider = nullptr;
    IMFMetadata *pMetadata = nullptr;
    IMFMediaSource *pMediaSource = nullptr;
    IMFPresentationDescriptor *pPD = nullptr;
    WAVEFORMATEX *pWfex = nullptr;
    WAVEFORMATEXTENSIBLE *pWfext = nullptr;
    UINT32 cbFormat = 0;
    MFTIME hnsDuration = 0;
    DWORD dwStream = 0;
    UINT32 bitrate = 0;

    memset(meta_return, 0, sizeof(WWMFReaderMetadata));

    // Intialize the Media Foundation platform.
    HRG(MFStartup(MF_VERSION));

    // Create the source reader to read the input file.
    HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &pReader));

    HRG(WWMFReaderGetUncompressedPcmAudio(pReader, &pMTPcmAudio));

    HRG(MFCreateWaveFormatExFromMFMediaType(pMTPcmAudio, &pWfex, &cbFormat));
    if (22 <= pWfex->cbSize) {
        pWfext = (WAVEFORMATEXTENSIBLE*)pWfex;
    }

    hr = WWMFReaderGetAudioEncodingBitrate(pReader, &bitrate);
    if (FAILED(hr)) {
        // 別に失敗しても良い。失敗するとbitrate == 0。
        hr = S_OK;
    }

    // 曲長さ：成功しても0が戻ることがある。
    HRG(WWMFReaderGetDuration(pReader, &hnsDuration));

    HRG(WWMFReaderCreateMediaSource(wszSourceFile, &pMediaSource));
    HRG(pMediaSource->CreatePresentationDescriptor(&pPD));

    // Get IMFMetadataProvider
    hr = pReader->GetServiceForStream(MF_SOURCE_READER_MEDIASOURCE,
        MF_METADATA_PROVIDER_SERVICE,
        IID_IMFMetadataProvider,
        (LPVOID*)&pMetaProvider);
    if (FAILED(hr)) {
        // メタデータ取得は失敗することがある。
        // 処理を続行する。
        hr = S_OK;
    } else {
        //DisplayAllMetadata(pMetadata);
        HRG(pMetaProvider->GetMFMetadata(pPD, dwStream, 0, &pMetadata));
    }

    // 収集。
    meta_return->bitRate         = bitrate;
    meta_return->bitsPerSample   = pWfex->wBitsPerSample;
    meta_return->numChannels     = pWfex->nChannels;
    meta_return->sampleRate      = pWfex->nSamplesPerSec;
    meta_return->dwChannelMask   = pWfext ? pWfext->dwChannelMask : 0;
    meta_return->numFrames =
            (int64_t)((double)hnsDuration * meta_return->sampleRate / (1000 * 1000 * 10));
    if (pMetadata) {
        WWMFReaderCollectMetadata(pMetadata, *meta_return);
    }

    if ((WWMFREADER_FLAG_RESOLVE_NUM_FRAMES & flags) && hnsDuration == 0) {
        // ヘッダーに長さが書いてない。
        // 全て読んでサイズを確定する。
        int64_t pcmBytes = 0;
        HRG(CountPcmBytes(pReader, pcmBytes));
        meta_return->numFrames = pcmBytes / meta_return->BytesPerFrame();
    }

end:
    CoTaskMemFree(pWfex);
    pWfex = nullptr;

    SafeRelease(&pPD);
    SafeRelease(&pMediaSource);
    SafeRelease(&pMetadata);
    SafeRelease(&pMetaProvider);
    SafeRelease(&pMTPcmAudio);
    SafeRelease(&pReader);
    MFShutdown();

    return hr;
}

int
WWMFReaderGetCoverart(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout)
{
    assert(data_return);

    HRESULT hr = S_OK;

    IMFSourceReader *pReader = nullptr;
    IMFMetadataProvider * pMetaProvider = nullptr;
    IMFMediaSource *pMediaSource = nullptr;
    IMFPresentationDescriptor *pPD = nullptr;
    IMFMetadata *pMetadata = nullptr;

    const int64_t maxDataBytes = *dataBytes_inout;
    *dataBytes_inout = 0;
    
    DWORD dwStream = 0;
    UINT32 cbBlob = 0;
    PROPVARIANT var;
    PropVariantInit(&var);

    // Intialize the Media Foundation platform.
    HRG(MFStartup(MF_VERSION));

    // Create the source reader to read the input file.
    HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &pReader));

    HRG(pReader->GetServiceForStream(MF_SOURCE_READER_MEDIASOURCE,
            MF_METADATA_PROVIDER_SERVICE,
            IID_IMFMetadataProvider,
            (LPVOID*)&pMetaProvider));
    HRG(WWMFReaderCreateMediaSource(wszSourceFile, &pMediaSource));
    HRG(pMediaSource->CreatePresentationDescriptor(&pPD));
    HRG(pMetaProvider->GetMFMetadata(pPD, dwStream, 0, &pMetadata));

    hr = pMetadata->GetProperty(L"WM/Picture", &var);
    if (SUCCEEDED(hr)) {
        int copyBytes = (int)maxDataBytes;
        if ((int)var.blob.cbSize < copyBytes) {
            copyBytes = (int)var.blob.cbSize;
        }
        memcpy(data_return, var.blob.pBlobData, copyBytes);
        *dataBytes_inout = copyBytes;

        PropVariantClear(&var);
    }

end:
    SafeRelease(&pMetadata);
    SafeRelease(&pPD);
    SafeRelease(&pMediaSource);
    SafeRelease(&pMetaProvider);
    SafeRelease(&pReader);
    MFShutdown();

    return hr;
}

