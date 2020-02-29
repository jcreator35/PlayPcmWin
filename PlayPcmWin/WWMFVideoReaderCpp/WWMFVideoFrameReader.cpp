// 日本語。
#include "WWMFVideoFrameReader.h"
#include "WWMFVideoReaderCommon.h"
#include "WWCommonUtil.h"
#include <string>

// inspired from Microsoft VideoThumbnail sample.

bool WWMFVideoFrameReader::mStaticInit = false;

HRESULT
WWMFVideoFrameReader::StaticInit(void)
{
    HRESULT hr = S_OK;

    HRG(MFStartup(MF_VERSION, MFSTARTUP_LITE));
    mStaticInit = true;

end:
    return hr;
}

void
WWMFVideoFrameReader::StaticTerm(void)
{
    if (mStaticInit) {
        MFShutdown();
        mStaticInit = false;
    }
}

/*
// MFCReateSourceReaderFromURL後に呼ぶ。 
static HRESULT
GetNativeVideoType(
    IMFSourceReader *pReader,
    IMFMediaType **ppVideo)
{
    HRESULT hr = S_OK;

    assert(pReader);

    *ppVideo = nullptr;

    IMFMediaType *pVideoType = nullptr;
    HRG(pReader->GetNativeMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, &pVideoType));

    {
        *ppVideo = pVideoType;
        (*ppVideo)->AddRef();
    }

end:
    SafeRelease(&pVideoType);
    return hr;
}
*/

HRESULT
WWMFVideoFrameReader::ReadStart(const wchar_t *path)
{
    HRESULT hr = S_OK;
    IMFAttributes *pAttributes = nullptr;
    IMFMediaType *pType = nullptr;

    assert(mReader == nullptr);

    // enable Yuv to RGB conv
    HRG(MFCreateAttributes(&pAttributes, 1));
    HRG(pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, TRUE));

    HRG(MFCreateSourceReaderFromURL(path, pAttributes, &mReader));

    HRG(MFCreateMediaType(&pType));
    HRG(pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video));
    HRG(pType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32));
    HRG(mReader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, nullptr, pType));
    HRG(mReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE));
    HRG(GetVideoFormat(nullptr, &mVideoFmt));

end:
    SafeRelease(&pType);
    return hr;
}

void
WWMFVideoFrameReader::ReadEnd(void)
{
    SafeRelease(&mReader);
}

// ピクセルアスペクト比srcPARの、src.w x src.hピクセルの画像を、
// ピクセルアスペクト比1:1に引き伸ばすときの縦横ピクセル数を戻す。
static RECT
CorrectAspectRatio(const RECT& src, const MFRatio& srcPAR)
{
    RECT rc = { 0, 0, src.right - src.left, src.bottom - src.top };
    if ((srcPAR.Numerator != 1) || (srcPAR.Denominator != 1)) {
        if (srcPAR.Numerator > srcPAR.Denominator) {
            rc.right = MulDiv(rc.right, srcPAR.Numerator, srcPAR.Denominator);
        } else if (srcPAR.Numerator < srcPAR.Denominator) {
            rc.bottom = MulDiv(rc.bottom, srcPAR.Denominator, srcPAR.Numerator);
        }
    }
    return rc;
}

HRESULT
WWMFVideoFrameReader::GetVideoFormat(IMFSample *pSample, WWMFVideoFormat *p)
{
    HRESULT hr = S_OK;
    UINT32  w = 0, h = 0;
    LONG lStride = 0;
    MFRatio par = { 0 , 0 };
    GUID subtype = { 0 };
    IMFMediaType *pType = nullptr;
    BOOL canSeek = FALSE;
    BOOL slowSeek = FALSE;
    MFVIDEOFORMAT *mfvf = nullptr;
    UINT32 bytes = 0;
    LONGLONG timeStamp = 0;

    memset(p, 0, sizeof *p);

    assert(mReader);

    HRG(mReader->GetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, &pType));
    HRG(pType->GetGUID(MF_MT_SUBTYPE, &subtype));
    if (subtype != MFVideoFormat_RGB32) {
        printf("Error: output RGB32 expected\n");
        hr = E_UNEXPECTED;
        goto end;
    }

    HRG(MFGetAttributeRatio(pType, MF_MT_FRAME_RATE, (UINT32*)&p->frameRate.numer, (UINT32*)&p->frameRate.denom));

    HRG(MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &w, &h));
    lStride = (LONG)MFGetAttributeUINT32(pType, MF_MT_DEFAULT_STRIDE, 1);
    if (lStride > 0) {
        // 画像が上から下。
        p->flags |= WW_MF_VIDEO_IMAGE_FMT_TopDown;
    }

    // これは、エラーが起こることがあるが、必須でない情報。
    hr = MFGetAttributeRatio(pType, MF_MT_PIXEL_ASPECT_RATIO, (UINT32*)&par.Numerator, (UINT32*)&par.Denominator);
    if (SUCCEEDED(hr) && (par.Denominator != par.Numerator)) {
        RECT rcSrc = { 0, 0, (LONG)w, (LONG)h };
        RECT rcAspectStretched = CorrectAspectRatio(rcSrc, par);
        p->aspectStretchedWH.w = rcAspectStretched.right;
        p->aspectStretchedWH.h = rcAspectStretched.bottom;

        p->aspectRatio.numer = par.Numerator;
        p->aspectRatio.denom = par.Denominator;
    } else {
        p->aspectStretchedWH.w = w;
        p->aspectStretchedWH.h = h;

        p->aspectRatio.numer = 1;
        p->aspectRatio.denom = 1;
    }

    p->pixelWH.w = w;
    p->pixelWH.h = h;

    HRG(CanSeek(&canSeek, &slowSeek));
    if (canSeek) {
        p->flags |= WW_MF_VIDEO_IMAGE_FMT_CAN_SEEK;
    }
    if (slowSeek) {
        p->flags |= WW_MF_VIDEO_IMAGE_FMT_SLOW_SEEK;
    }

    HRG(GetDuration(&p->duration));

    HRG(MFCreateMFVideoFormatFromMFMediaType(pType, &mfvf, &bytes));
    PrintMFVideoFormat(mfvf);
    p->apertureWH.w = mfvf->videoInfo.GeometricAperture.Area.cx;
    p->apertureWH.h = mfvf->videoInfo.GeometricAperture.Area.cy;
    if (mfvf->videoInfo.NominalRange == MFNominalRange_Wide) {
        p->flags |= WW_MF_VIDEO_IMAGE_FMT_LIMITED_RANGE_16_to_235;
    }

    if (pSample && SUCCEEDED(pSample->GetSampleTime(&timeStamp))) {
        p->timeStamp = (int64_t)timeStamp;
    } else {
        p->timeStamp = -1;
    }

end:
    SafeRelease(&pType);
    return hr;
}

HRESULT
WWMFVideoFrameReader::CanSeek(BOOL *bSeek_return, BOOL *bSlowSeek_return)
{
    HRESULT hr = S_OK;
    ULONG flags = 0;
    
    PROPVARIANT pv;
    PropVariantInit(&pv);

    *bSeek_return = FALSE;
    *bSlowSeek_return = FALSE;

    assert(mReader);

    HRG(mReader->GetPresentationAttribute((DWORD)MF_SOURCE_READER_MEDIASOURCE,
        MF_SOURCE_READER_MEDIASOURCE_CHARACTERISTICS, &pv));
    HRG(PropVariantToUInt32(pv, &flags));

    if (flags & MFMEDIASOURCE_CAN_SEEK) {
        *bSeek_return = TRUE;
    }
    if (flags & MFMEDIASOURCE_HAS_SLOW_SEEK) {
        *bSeek_return = TRUE;
    }

end:
    PropVariantClear(&pv);
    return hr;
}

HRESULT
WWMFVideoFrameReader::GetDuration(int64_t *duration_return)
{
    HRESULT hr = S_OK;

    PROPVARIANT pv;
    PropVariantInit(&pv);

    *duration_return = 0;
    assert(mReader);

    HRG(mReader->GetPresentationAttribute((DWORD)MF_SOURCE_READER_MEDIASOURCE,
        MF_PD_DURATION, &pv));

    {
        assert(pv.vt == VT_UI8);
        *duration_return = pv.hVal.QuadPart;
    }

end:
    PropVariantClear(&pv);
    return hr;
}

HRESULT 
WWMFVideoFrameReader::Seek(int64_t pos)
{
    HRESULT     hr = S_OK;
    PROPVARIANT pv;
    PropVariantInit(&pv);
    bool canSeek = 0 != (mVideoFmt.flags & WW_MF_VIDEO_IMAGE_FMT_CAN_SEEK);

    assert(0 <= pos);
    assert(mReader);

    if (!canSeek) {
        // シーク機能が無い。OK
        return S_OK;
    }
    

    pv.vt = VT_I8;
    pv.hVal.QuadPart = pos;

    HRG(mReader->SetCurrentPosition(GUID_NULL, pv));

end:
    PropVariantClear(&pv);
    return hr;
}

HRESULT
WWMFVideoFrameReader::ReadImage(int64_t posToSeek, uint8_t **ppImg_return,
        int *imgBytes_return, WWMFVideoFormat *vf_return)
{
    HRESULT hr = S_OK;
    DWORD dwFlags = 0;
    BYTE *pBitmapData = nullptr;
    IMFMediaBuffer *pBuffer = nullptr;
    IMFSample *pSample = nullptr;
    DWORD cbBitmapData = 0;
    LONGLONG timeStamp = 0;
    DWORD cSkipped = 0;
    uint8_t *pTo = nullptr;

    assert(ppImg_return);
    assert(imgBytes_return);
    assert(vf_return);

    *ppImg_return = nullptr;
    *imgBytes_return = 0;

    // 1フレームの時間の半分。
    double SEEK_TOLERANCE = 1000LL * 1000 * 10 / 2 / ((double)mVideoFmt.frameRate.numer / mVideoFmt.frameRate.denom);

    if (0 <= posToSeek) {
        HRG(Seek(posToSeek));
    }

    while (true) {
        IMFSample *pSampleTmp = nullptr;

        HRG(mReader->ReadSample((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
            0, nullptr, &dwFlags, nullptr, &pSampleTmp));
        if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM) {
            break;
        }
        if (dwFlags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) {
            HRG(GetVideoFormat(pSampleTmp, &mVideoFmt));
        }
        if (pSampleTmp == nullptr) {
            // 取得できないとき。
            continue;
        }

        // 得られたサンプルをpSampleにセットする。
        SafeRelease(&pSample);
        pSample = pSampleTmp;
        pSample->AddRef();

        if (0 <= posToSeek) {
            if (SUCCEEDED(pSample->GetSampleTime(&timeStamp))) {
                // 得られたサンプルの時刻を調べる。
                if (timeStamp + SEEK_TOLERANCE < posToSeek) {
                    SafeRelease(&pSampleTmp);

                    ++cSkipped;
                    continue;
                }
            }
        }

        SafeRelease(&pSampleTmp);
        break;
    }

    if (!pSample) {
        hr = MF_E_END_OF_STREAM;
        goto end;
    }

    // 画像をppImgReturnにコピーする。
    // 画像フォーマットをvf_returnにセット。
    HRG(pSample->ConvertToContiguousBuffer(&pBuffer));
    HRG(pBuffer->Lock(&pBitmapData, nullptr, &cbBitmapData));
    assert(cbBitmapData == (4 * mVideoFmt.pixelWH.w * mVideoFmt.pixelWH.h));

    pTo = new uint8_t[cbBitmapData];
    if (nullptr == pTo) {
        hr = E_OUTOFMEMORY;
        goto end;
    }
    *ppImg_return = pTo;
    assert(ppImg_return);

    *imgBytes_return = (int)cbBitmapData;
    *vf_return = mVideoFmt;

    // set Alpha to 255
    memcpy(pTo, pBitmapData, cbBitmapData);
    for (uint32_t pos = 3; pos < cbBitmapData; pos += 4) {
        pTo[pos] = 255;
    }

    // swap red and blue
    for (uint32_t pos = 0; pos < cbBitmapData; pos += 4) {
        uint8_t red = pTo[pos + 2];
        uint8_t blue = pTo[pos + 0];
        pTo[pos + 0] = red;
        pTo[pos + 2] = blue;
    }

end:
    if (pBitmapData) {
        pBuffer->Unlock();
        pBitmapData = nullptr;
    }
    SafeRelease(&pBuffer);
    SafeRelease(&pSample);

    return hr;
}


