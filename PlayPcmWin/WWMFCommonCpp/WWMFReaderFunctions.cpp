// 日本語

#include "WWMFReaderFunctions.h"
#include "WWMFReaderMetadata.h"
#include "WWCommonUtil.h"

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
        if (pwstr) {
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
        for (ULONG i = 0; i < var.calpwstr.cElems; i++) {
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
        hr = pMetadata->GetProperty(varNames.calpwstr.pElems[i], &varValue);
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

#define GET_STR_META(KEY, MEMBER)                                      \
    hr = pMetadata->GetProperty(KEY, &var);                            \
    if (SUCCEEDED(hr)) {                                               \
        if (var.vt == VT_LPWSTR) {                                     \
            wcsncpy_s(meta.MEMBER, var.pwszVal, WWMFReaderStrCount-1); \
            meta.MEMBER[WWMFReaderStrCount-1] = 0;                     \
        }                                                              \
        PropVariantClear(&var);                                        \
    }

HRESULT
WWMFReaderCollectMetadata(IMFMetadata *pMetadata,
    WWMFReaderMetadata &meta)
{
    HRESULT hr = S_OK;
    PROPVARIANT var;

    GET_STR_META(L"Title", title);
    GET_STR_META(L"Author", artist);
    GET_STR_META(L"WM/AlbumTitle", album);
    GET_STR_META(L"WM/Composer", composer);

    hr = pMetadata->GetProperty(L"WM/Picture", &var);
    if (SUCCEEDED(hr)) {
        meta.pictureBytes = var.blob.cbSize;
        PropVariantClear(&var);
    }

    return hr;
}

// Inspired from "Windows-classic-samples/Samples/Win7Samples/multimedia/MediaFoundation/Transcode sample"

HRESULT
WWMFReaderCreateMediaSource(
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


HRESULT
WWMFReaderGetDuration(IMFSourceReader *pReader, MFTIME *phnsDuration)
{
    PROPVARIANT var;
    PropVariantInit(&var);
    HRESULT hr = S_OK;

    HRG(pReader->GetPresentationAttribute(
        MF_SOURCE_READER_MEDIASOURCE,
        MF_PD_DURATION, &var));

    PropVariantToInt64(var, phnsDuration);

end:
    PropVariantClear(&var);

    return hr;
}

HRESULT
WWMFReaderGetAudioEncodingBitrate(IMFSourceReader *pReader, UINT32 *bitrate_return)
{
    *bitrate_return = 0;

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

HRESULT
WWMFReaderConfigureAudioTypeToUncompressedPcm(
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

HRESULT
WWMFReaderGetUncompressedPcmAudio(
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
