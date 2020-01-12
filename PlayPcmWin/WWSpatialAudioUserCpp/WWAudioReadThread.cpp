// 日本語
#include "WWAudioReadThread.h"
#include <SpatialAudioClient.h>
#include <exception>
#include <mmdeviceapi.h>
#include "WWSAUtil.h"
#include <functiondiscoverykeys.h>
#include <assert.h>
#include "WWGuidToStr.h"
#include "WWPrintDeviceProp.h"
#include <assert.h>
#include "WWMFReaderMetadata.h"
#include "WWMFReaderFunctions.h"
#include "WWCommonUtil.h"

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

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

static int
WWMFReaderReadHeader(
	const wchar_t *wszSourceFile,
	WWMFReaderMetadata *meta_return)
{
	HRESULT hr = S_OK;

	IMFSourceReader *pReader = nullptr;
	IMFMediaType *pMTPcmAudio = nullptr;
	IMFMetadata *pMetadata = nullptr;
	IMFMediaSource *pMediaSource = nullptr;
	WAVEFORMATEX *pWfex = nullptr;
	WAVEFORMATEXTENSIBLE *pWfext = nullptr;
	UINT32 cbFormat = 0;
    MFTIME hnsDuration = 0;

	memset(meta_return, 0, sizeof(WWMFReaderMetadata));

	// Intialize the Media Foundation platform.
	HRG(MFStartup(MF_VERSION));

	// Create the source reader to read the input file.
	HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &pReader));

    HRG(GetDuration(pReader, &hnsDuration));
    
    HRG(GetUncompressedPcmAudio(pReader, &pMTPcmAudio));

	HRG(MFCreateWaveFormatExFromMFMediaType(pMTPcmAudio, &pWfex, &cbFormat));
	if (22 <= pWfex->cbSize) {
		pWfext = (WAVEFORMATEXTENSIBLE*)pWfex;
	}

	// 収集。
	meta_return->bitRate = 0;
	meta_return->bitsPerSample = pWfex->wBitsPerSample;
	meta_return->numChannels = pWfex->nChannels;
	meta_return->sampleRate = pWfex->nSamplesPerSec;
	meta_return->dwChannelMask = pWfext ? pWfext->dwChannelMask : 0;
    meta_return->numApproxFrames = 
		(int64_t)((double)hnsDuration * meta_return->sampleRate / (1000 * 1000 * 10));

end:
    pWfext = nullptr;

    CoTaskMemFree(pWfex);
	pWfex = nullptr;

	SafeRelease(&pMediaSource);
	SafeRelease(&pMetadata);
	SafeRelease(&pMTPcmAudio);
	SafeRelease(&pReader);
	MFShutdown();

	return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

DWORD
WWAudioReadThread::ReadThreadEntry(LPVOID lpThreadParameter)
{
    WWAudioReadThread* self = (WWAudioReadThread*)lpThreadParameter;
    return self->ReadThreadMain();
}

HRESULT
WWAudioReadThread::Read1(void)
{
    HRESULT hr = S_OK;
    mPlayStreamCount = 0;
    UINT32 availableDyn = 0;
    UINT32 frameCountPerBuffer = 0;

    if (nullptr == mSAORStream) {
        return S_OK;
    }

    HRG(mSAORStream->BeginUpdatingAudioObjects(&availableDyn, &frameCountPerBuffer));
    for (auto ite = mAudioObjectListHolder.mAudioObjectList.begin();
            ite != mAudioObjectListHolder.mAudioObjectList.end(); ++ite) {
        auto &ao = *ite;
        BYTE *buffer = nullptr;
        UINT32 buffBytes = 0;
        bool bEnd = false;

        if (ao.sao == nullptr) {
            HRG(mSAORStream->ActivateSpatialAudioObject(ao.aot, &ao.sao));
        }

        HRG(ao.sao->GetBuffer(&buffer, &buffBytes));

        bEnd = ao.CopyNextPcmTo(buffer, buffBytes);

        if (bEnd) {
            // 送るデータが無いので無音をセットした。
        } else {
            ++mPlayStreamCount;

            if (ao.aot == AudioObjectType_Dynamic) {
                HRG(ao.sao->SetPosition(ao.posX, ao.posY, ao.posZ));
            }
            HRG(ao.sao->SetVolume(ao.volume));
        }
    }

    HRG(mSAORStream->EndUpdatingAudioObjects());

end:
    return hr;
}

HRESULT
WWAudioReadThread::ReadThreadMain(void)
{
    bool stillPlaying = true;
    HANDLE waitArray[2] = { mShutdownEvent, mBufferEvent };
    int nWaitObjects = 2;
    DWORD waitResult;
    HRESULT hr = 0;

    assert(waitArray[0]);
    assert(waitArray[1]);

    // MTA
    HRG(CoInitializeEx(nullptr, COINIT_MULTITHREADED));

    while (stillPlaying) {
        waitResult = WaitForMultipleObjects(nWaitObjects, waitArray, FALSE, INFINITE);
        
        assert(mMutex);
        WaitForSingleObject(mMutex, INFINITE);
        {   // この中はgoto 不可。
            switch (waitResult) {
            case WAIT_OBJECT_0 + 0: // m_shutdownEvent
                if (mSAORStream) {
                    mSAORStream->Stop();
                    mSAORStream->Reset();
                }
                stillPlaying = false;
                break;
            case WAIT_OBJECT_0 + 1: //< mBufferEvent
                hr = Render1();
                break;
            default:
                assert(0);
                break;
            }
        }
        ReleaseMutex(mMutex);

        if (FAILED(hr)) {
            mThreadErcd = hr;
            goto end;
        }
    }

end:
    dprintf("WWSpatialAudioUser::RenderMain() end\n");

    mThreadCharacteristics.Unsetup();
    mTimerResolution.Unsetup();

    CoUninitialize();
    return hr;
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

HRESULT
WWAudioReadThread::Init(const wchar_t *path, const WWMFPcmFormat &wantFmt)
{
    dprintf("WWAudioReadThread::Init()\n");
    HRESULT hr = S_OK;

    mTargetFmt = wantFmt;


    assert(nullptr == mReadThread);
    mReadThread = CreateThread(nullptr, 0, ReadThreadEntry, this, 0, nullptr);
    if (nullptr == mReadThread) {
        printf("E: WWAudioReadThread::Init() CreateThread failed\n");
        hr = E_FAIL;
    }

end:
    return hr;
}

void
WWSpatialAudioUser::Term(void)
{
    WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStream, WWAudioObject>::Term();
}

HRESULT
WWSpatialAudioUser::ActivateAudioStream(int dynObjectCount, int staticObjectTypeMask)
{
    HRESULT hr = S_OK;
    SpatialAudioObjectRenderStreamActivationParams p;
    PROPVARIANT pv;
    PropVariantInit(&pv);

    if (mSAORStream) {
        printf("E: WWSpatialAudioUser::ActivateAudioStream() already activated\n");
        hr = E_FAIL;
        goto end;
    }

    // 念の為。
    HRG(mSAClient->IsAudioObjectFormatSupported((const WAVEFORMATEX*)&mUseFmt));

    p.ObjectFormat = (const WAVEFORMATEX*)&mUseFmt;
    
    // 1つもスタティックなオブジェクトが無いときはNone。Dynamicにするとエラーが起きた。
    p.StaticObjectTypeMask = (AudioObjectType)staticObjectTypeMask;
    p.MinDynamicObjectCount = 0;
    p.MaxDynamicObjectCount = dynObjectCount;
    p.Category = AudioCategory_SoundEffects;
    p.EventHandle = mBufferEvent;
    p.NotifyObject = nullptr;

    pv.vt = VT_BLOB;
    pv.blob.cbSize = sizeof(p);
    pv.blob.pBlobData = reinterpret_cast<BYTE *>(&p);

    HRG(mSAClient->ActivateSpatialAudioStream(&pv, __uuidof(mSAORStream),
        (void**)&mSAORStream));

end:
    // blobの指す先はdelete不可。
    pv.blob.cbSize = 0;
    pv.blob.pBlobData = nullptr;
    PropVariantClear(&pv);

    return hr;
}

#endif // 0
