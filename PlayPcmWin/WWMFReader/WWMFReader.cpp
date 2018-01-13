// “ú–{Œê

#include "WWMFReader.h"
#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <stdio.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>

#include "../WasapiIODLL/WWUtil.h"

#if 0

static void
DisplayProperty(REFPROPVARIANT var)
{
    switch (var.vt) {
    case VT_BLOB:
        wprintf(L"VT_BLOB\n");
        break;

    case VT_BOOL:
        wprintf(L"VT_BOOL: %s\n", (var.boolVal == VARIANT_TRUE ? L"TRUE" : L"FALSE"));
        break;

    case VT_CLSID:
        {
            WCHAR guid[40];
            StringFromGUID2(*var.puuid, guid, 40);
            wprintf(L"VT_CLSID: %s\n", guid);
        }
        break;

    case VT_EMPTY:
        wprintf(L"VT_EMPTY\n");
        break;

    case VT_FILETIME:
        {
            SYSTEMTIME systemTime;
            FileTimeToSystemTime(&var.filetime, &systemTime);

            int cch = GetDateFormatEx(
                LOCALE_NAME_USER_DEFAULT, 0, &systemTime,
                NULL, NULL, 0, NULL);

            PWSTR pwstr = (PWSTR)CoTaskMemAlloc(cch * sizeof(WCHAR));
            if (pwstr)
            {
                GetDateFormatEx(
                    LOCALE_NAME_USER_DEFAULT, 0, &systemTime,
                    NULL, pwstr, cch, NULL);

                wprintf(L"VT_FILETIME: %s\n", pwstr);
                CoTaskMemFree(pwstr);
            }
        }
        break;

    case VT_LPWSTR:
        wprintf(L"VT_LPWSTR: %s\n", var.pwszVal);
        break;

    case VT_UI4:
        wprintf(L"VT_UI4: %d\n", var.ulVal);
        break;

    case VT_UI8:
        wprintf(L"VT_UI8: %I64d\n", var.uhVal.QuadPart);
        break;

    case VT_VECTOR | VT_LPWSTR:
        for (ULONG i = 0; i < var.calpwstr.cElems; i++)
        {
            wprintf(L"VT_VECTOR | VT_LPWSTR: %s;", var.calpwstr.pElems[i]);
        }
        wprintf(L"\n");
        break;

    default:
        wprintf(L"Variant type = %x\n", var.vt);
        break;
    }
}

static HRESULT
DisplayAllMetadata(IMFMetadata *pMetadata)
{
    PROPVARIANT varNames;
    PropVariantInit(&varNames);

    HRESULT hr = S_OK;
    
    HRG(pMetadata->GetAllPropertyNames(&varNames));

    for (ULONG i = 0; i < varNames.calpwstr.cElems; i++) {
        wprintf(L"%s\n", varNames.calpwstr.pElems[i]);

        PROPVARIANT varValue;
        PropVariantInit(&varValue);
        hr = pMetadata->GetProperty( varNames.calpwstr.pElems[i], &varValue );
        if (SUCCEEDED(hr)) {
            DisplayProperty(varValue);
            PropVariantClear(&varValue);
        }
    }

end:
    PropVariantClear(&varNames);
    return hr;
}

#endif

#define GET_STR_META(KEY, MEMBER)               \
    hr = pMetadata->GetProperty(KEY, &var);     \
    if (SUCCEEDED(hr)) {                        \
        if (var.vt == VT_LPWSTR) {              \
            wcscpy_s(meta.MEMBER, var.pwszVal); \
        }                                       \
        PropVariantClear(&var);                 \
    }

static HRESULT
CollectMetadata(IMFMetadata *pMetadata,
        WWMFReaderMetadata &meta)
{
    HRESULT hr = S_OK;
    PROPVARIANT var;

    GET_STR_META(L"Title", title);
    GET_STR_META(L"Author", artist);
    GET_STR_META(L"WM/AlbumTitle", album);
    GET_STR_META(L"WM/Composer", composer);

    return hr;
}

// Inspired from "Windows-classic-samples/Samples/Win7Samples/multimedia/MediaFoundation/Transcode sample"

static HRESULT
CreateMediaSource(
        const WCHAR *sURL,
        IMFMediaSource** ppMediaSource)
{
    assert(sURL);
    assert(ppMediaSource);

    HRESULT hr = S_OK;
    MF_OBJECT_TYPE ObjectType = MF_OBJECT_INVALID;
    IMFSourceResolver* pSourceResolver = nullptr;
    IUnknown* pUnkSource = nullptr;

    HRG(MFCreateSourceResolver(&pSourceResolver));

    HRG(pSourceResolver->CreateObjectFromURL(
            sURL,                       // URL of the source.
            MF_RESOLUTION_MEDIASOURCE,  // Create a source object.
            NULL,                       // Optional property store.
            &ObjectType,                // Receives the created object type. 
            &pUnkSource                 // Receives a pointer to the media source.
            ));

    HRG(pUnkSource->QueryInterface(IID_PPV_ARGS(ppMediaSource)));

end:
    SafeRelease(&pSourceResolver);
    SafeRelease(&pUnkSource);
    return hr;
}

// Inspired from "Windows-classic-samples/Samples/Win7Samples/multimedia/mediafoundation/AudioClip sample"

static HRESULT
ConfigureAudioStreamToUncompressedPcm(
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

static HRESULT
GetDuration(IMFSourceReader *pReader, MFTIME *phnsDuration)
{
    PROPVARIANT var;
    PropVariantInit(&var);
    HRESULT hr = S_OK;
    
    HRG(pReader->GetPresentationAttribute(
            MF_SOURCE_READER_MEDIASOURCE, MF_PD_DURATION, &var));

    PropVariantToInt64(var, phnsDuration);

end:
    PropVariantClear(&var);

    return hr;
}

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

WWMFREADER_API int
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
    UINT32 cbFormat = 0;
    MFTIME hnsDuration = 0;
    DWORD dwStream = 0;
    UINT32 bitrate = 0;

    memset(meta_return, 0, sizeof(WWMFReaderMetadata));

    // Initialize the COM library.
    HRG(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE));

    // Intialize the Media Foundation platform.
    HRG(MFStartup(MF_VERSION));

    // Create the source reader to read the input file.
    HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &pReader));

    GetAudioEncodingBitrate(pReader, &bitrate);

    HRG(ConfigureAudioStreamToUncompressedPcm(pReader, &pMTPcmAudio));

    HRG(MFCreateWaveFormatExFromMFMediaType(pMTPcmAudio, &pWfex, &cbFormat));

    HRG(GetDuration(pReader, &hnsDuration));

    // Get IMFMetadataProvider
    HRG(pReader->GetServiceForStream(MF_SOURCE_READER_MEDIASOURCE,
            MF_METADATA_PROVIDER_SERVICE,
            IID_IMFMetadataProvider,
            (LPVOID*)&pMetaProvider));
    HRG(CreateMediaSource(wszSourceFile, &pMediaSource));
    HRG(pMediaSource->CreatePresentationDescriptor(&pPD));
    HRG(pMetaProvider->GetMFMetadata(pPD, dwStream, 0, &pMetadata));
    //DisplayAllMetadata(pMetadata);

    // ŽûWB
    meta_return->bitRate = bitrate;
    meta_return->bitsPerSample = pWfex->wBitsPerSample;
    meta_return->numChannels = pWfex->nChannels;
    meta_return->sampleRate = pWfex->nSamplesPerSec;
    meta_return->numApproxFrames =
            (int64_t)((double)hnsDuration * meta_return->sampleRate / (1000 * 1000 * 10));
    CollectMetadata(pMetadata, *meta_return);

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
    CoUninitialize();

    return hr;
}

WWMFREADER_API int
WWMFReaderReadData(
        const wchar_t *wszSourceFile,
        unsigned char *data_return,
        int64_t *dataBytes_inout)
{
    HRESULT hr = S_OK;

    IMFSourceReader *pReader = nullptr;
    IMFMediaType *pMTPcmAudio = nullptr;
    IMFSample *pSample = nullptr;
    IMFMediaBuffer *pBuffer = nullptr;
    BYTE *pAudioData = nullptr;
    DWORD dwFlags = 0;
    int64_t cbAudioData = 0;
    DWORD cbBuffer = 0;

    const int64_t cbMaxAudioData = *dataBytes_inout;
    *dataBytes_inout = 0;

    HRG(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE));

    HRG(MFStartup(MF_VERSION));

    HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &pReader));
    HRG(ConfigureAudioStreamToUncompressedPcm(pReader, &pMTPcmAudio));

    while (true) {
        dwFlags = 0;
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
            break;
        }
        if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM) {
            //dprintf("End of input file.\n");
            break;
        }
        if (pSample == nullptr) {
            dprintf("No sample\n");
            continue;
        }
        
        assert(pBuffer == nullptr);
        HRB_Quiet(pSample->ConvertToContiguousBuffer(&pBuffer));

        cbBuffer = 0;
        HRB_Quiet(pBuffer->Lock(&pAudioData, NULL, &cbBuffer));

        if (cbMaxAudioData - cbAudioData < cbBuffer) {
            cbBuffer = (int)(cbMaxAudioData - cbAudioData);
        }

        memcpy(&data_return[cbAudioData], pAudioData, cbBuffer);

        hr = pBuffer->Unlock();
        pAudioData = nullptr;
        if (FAILED(hr)) { break; }

        cbAudioData += cbBuffer;
        if (cbAudioData >= cbMaxAudioData) {
            break;
        }

        SafeRelease(&pSample);
        SafeRelease(&pBuffer);
    }

    *dataBytes_inout = cbAudioData;

end:

    SafeRelease(&pSample);
    SafeRelease(&pBuffer);
    SafeRelease(&pMTPcmAudio);
    SafeRelease(&pReader);
    MFShutdown();
    CoUninitialize();

    return hr;
}
