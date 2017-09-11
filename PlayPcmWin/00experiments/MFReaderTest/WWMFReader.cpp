#include "WWMFReader.h"
#include "WWUtil.h"
#include <assert.h>

// creates uncompressed PCM audio ppPCMAudio from the source reader pReader
static HRESULT
ConfigureAudioStream(
        IMFSourceReader *pReader,
        IMFMediaType **ppPCMAudio)
{
    HRESULT hr = S_OK;

    *ppPCMAudio = nullptr;

    IMFMediaType *pUncompressedAudioType = NULL;
    IMFMediaType *pPartialType = NULL;

    HRG(MFCreateMediaType(&pPartialType));
    HRG(pPartialType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio));
    HRG(pPartialType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM));

    HRG(pReader->SetCurrentMediaType(
            (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, nullptr, pPartialType));

    HRG(pReader->GetCurrentMediaType(
            (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, &pUncompressedAudioType));

    HRG(pReader->SetStreamSelection(
            (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, TRUE));

    // Succeeded! returns pcmAudio
    *ppPCMAudio = pUncompressedAudioType;
    (*ppPCMAudio)->AddRef();

end:
    SafeRelease(&pUncompressedAudioType);
    SafeRelease(&pPartialType);
    return hr;
}

// following code is from the IMFMetadata::GetProperty method manual

static void
DisplayProperty(REFPROPVARIANT var)
{
    switch (var.vt) {
    case VT_BLOB:
        wprintf(L"VT_BLOB. cbSize=%d\n", var.blob.cbSize);
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
DisplayMetadata(IMFMetadata *pMetadata)
{
    PROPVARIANT varNames;
    HRESULT hr;
    
    HRG(pMetadata->GetAllPropertyNames(&varNames));

    for (ULONG i = 0; i < varNames.calpwstr.cElems; i++) {
        wprintf(L"%s\n", varNames.calpwstr.pElems[i]);

        PROPVARIANT varValue;
        hr = pMetadata->GetProperty( varNames.calpwstr.pElems[i], &varValue );
        if (SUCCEEDED(hr)) {
            DisplayProperty(varValue);
            PropVariantClear(&varValue);
        }
    }

    PropVariantClear(&varNames);
end:
    return hr;
}

// following code is from IMFMetadata manual
static HRESULT
GetMetadata(
    IMFMediaSource *pSource, IMFMetadata **ppMetadata)
{
    IMFPresentationDescriptor *pPD = nullptr;
    IMFMetadataProvider *pProvider = nullptr;
    HRESULT hr;
    DWORD dwStream = 0;
    *ppMetadata = nullptr;

    HRG(pSource->CreatePresentationDescriptor(&pPD));

    HRG(MFGetService(
        pSource, MF_METADATA_PROVIDER_SERVICE, IID_PPV_ARGS(&pProvider)));

    HRG(pProvider->GetMFMetadata(pPD, dwStream, 0, ppMetadata));

end:
    SafeRelease(&pPD);
    SafeRelease(&pProvider);
    return hr;
}

// from protectedplayback sample
// this function blocks until mediasource is fully created
static HRESULT
CreateMediaSource(const WCHAR *sURL, IMFMediaSource **ppSource_return)
{
    HRESULT hr = S_OK;
    MF_OBJECT_TYPE ObjectType = MF_OBJECT_INVALID;

    IMFSourceResolver* pSourceResolver = NULL;
    IUnknown* pSource = NULL;
    *ppSource_return = nullptr;

    HRG(MFCreateSourceResolver(&pSourceResolver));

    HRG(pSourceResolver->CreateObjectFromURL(
        sURL, MF_RESOLUTION_MEDIASOURCE, nullptr, &ObjectType, &pSource));

    HRG(pSource->QueryInterface(__uuidof(IMFMediaSource), (void**)ppSource_return));

end:
    SAFE_RELEASE(pSourceResolver);
    SAFE_RELEASE(pSource);
    return hr;
}

WWMFReader::WWMFReader(void)
    : mSource(nullptr), mReader(nullptr), mPcmAudio(nullptr)
{
}

WWMFReader::~WWMFReader(void)
{
    assert(!mReader);
    assert(!mPcmAudio);
    assert(!mSource);
}

HRESULT
WWMFReader::Init(LPCWSTR path)
{
    HRESULT hr;

    HRG(CreateMediaSource(path, &mSource));
    HRG(MFCreateSourceReaderFromMediaSource(mSource, nullptr, &mReader));
    HRG(ConfigureAudioStream(mReader, &mPcmAudio));

end:
    return hr;
}

void
WWMFReader::Term(void)
{
    SafeRelease(&mPcmAudio);
    SafeRelease(&mReader);
    SafeRelease(&mSource);
}

HRESULT
WWMFReader::RetrieveMFMetadata(WWMFMetadata &metaData_return)
{
    HRESULT hr;

    if (!mSource) {
        return E_FAIL;
    }

    IMFMetadata *pMetadata = nullptr;

    HRG(GetMetadata(mSource, &pMetadata));
    HRG(DisplayMetadata(pMetadata));

end:
    SafeRelease(&pMetadata);
    return hr;
}
