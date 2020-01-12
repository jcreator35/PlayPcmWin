// 日本語

#include "WWMFReader.h"
#include "WWCommonUtil.h"
#include "WWMFReaderFunctions.h"
#include <list>

// Inspired from "Windows-classic-samples/Samples/Win7Samples/multimedia/mediafoundation/AudioClip sample"





int
WWMFReaderReadHeader(
        const wchar_t *wszSourceFile,
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

    HRG(WWMFReaderGetAudioEncodingBitrate(pReader, &bitrate));

    HRG(WWMFReaderGetUncompressedPcmAudio(pReader, &pMTPcmAudio));

    HRG(MFCreateWaveFormatExFromMFMediaType(pMTPcmAudio, &pWfex, &cbFormat));
    if (22 <= pWfex->cbSize) {
        pWfext = (WAVEFORMATEXTENSIBLE*)pWfex;
    }

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
    meta_return->bitRate = bitrate;
    meta_return->bitsPerSample = pWfex->wBitsPerSample;
    meta_return->numChannels = pWfex->nChannels;
    meta_return->sampleRate = pWfex->nSamplesPerSec;
    meta_return->dwChannelMask = pWfext ? pWfext->dwChannelMask : 0;
    meta_return->numApproxFrames =
            (int64_t)((double)hnsDuration * meta_return->sampleRate / (1000 * 1000 * 10));
    if (pMetadata) {
        WWMFReaderCollectMetadata(pMetadata, *meta_return);
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

