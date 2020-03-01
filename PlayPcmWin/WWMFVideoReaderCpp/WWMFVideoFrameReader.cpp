// 日本語。
#include "WWMFVideoFrameReader.h"
#include "WWMFVideoReaderCommon.h"
#include "WWCommonUtil.h"
#include <string>

// inspired from Microsoft VideoThumbnail sample.

// 色順表記は、画像1バイトごとに出てくる値の色を表している。
// ひとつだけ1にし、他は0にする。
#define IMAGE_RGBA 0
#define IMAGE_BGRA 1

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

static double
MFOffsetToDouble(const MFOffset &t)
{
    double r = t.value;
    r += t.fract / 65536.0;
    return r;
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
    //PrintMFVideoFormat(mfvf);

    assert(0 == MFOffsetToDouble(mfvf->videoInfo.GeometricAperture.OffsetX));
    assert(0 == MFOffsetToDouble(mfvf->videoInfo.GeometricAperture.OffsetY));

    p->aperture.x = (int32_t)MFOffsetToDouble(mfvf->videoInfo.GeometricAperture.OffsetX);
    p->aperture.y = (int32_t)MFOffsetToDouble(mfvf->videoInfo.GeometricAperture.OffsetY);
    p->aperture.w = mfvf->videoInfo.GeometricAperture.Area.cx;
    p->aperture.h = mfvf->videoInfo.GeometricAperture.Area.cy;
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
WWMFVideoFrameReader::ReadImage(int64_t posToSeek, uint8_t *pImg_io,
        int *imgBytes_io, WWMFVideoFormat *vf_return)
{
    HRESULT hr = S_OK;
    IMFSample *pSample = nullptr;
    IMFMediaBuffer *pBuffer = nullptr;
    BYTE *pBitmapData = nullptr;

    assert(pImg_io);
    assert(0 < imgBytes_io);
    assert(vf_return);

    // 1フレームの時間の半分。
    double SEEK_TOLERANCE = 1000LL * 1000 * 10 / 2 / ((double)mVideoFmt.frameRate.numer / mVideoFmt.frameRate.denom);

    if (0 <= posToSeek) {
        HRG(Seek(posToSeek));
    }

    while (true) {
        DWORD dwFlags = 0;
        DWORD actualStreamIndex = 0;
        LONGLONG timeStamp = 0;
        IMFSample *pSampleTmp = nullptr;

        HRG(mReader->ReadSample((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
            0, &actualStreamIndex, &dwFlags, &timeStamp, &pSampleTmp));
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
        SafeRelease(&pSampleTmp);

        dprintf("time=%f\n", (double)timeStamp / (1000.0 *1000.0 * 10));
        mVideoFmt.timeStamp = timeStamp;
        if (0 <= posToSeek) {
            // 得られたサンプルの時刻を調べる。
            if (timeStamp + SEEK_TOLERANCE < posToSeek) {
                continue;
            }
        }

        break;
    }

    if (!pSample) {
        hr = MF_E_END_OF_STREAM;
        goto end;
    }

    {
        DWORD cbBitmapData = 0;
        DWORD cbOutputBytes = 0;

        // 画像をppImgReturnにコピーする。
        // 画像フォーマットをvf_returnにセット。
        HRG(pSample->ConvertToContiguousBuffer(&pBuffer));
        HRG(pBuffer->Lock(&pBitmapData, nullptr, &cbBitmapData));
        assert(cbBitmapData == (4 * mVideoFmt.pixelWH.w * mVideoFmt.pixelWH.h));

        cbOutputBytes = 4
            * (mVideoFmt.aperture.w - mVideoFmt.aperture.x) 
            * (mVideoFmt.aperture.h - mVideoFmt.aperture.y);
        if (*imgBytes_io < (int)cbOutputBytes) {
            hr = E_OUTOFMEMORY;
            goto end;
        }

        *imgBytes_io = (int)cbOutputBytes;
        *vf_return = mVideoFmt;
    }

    {
        int toPos = 0;
        for (int y = mVideoFmt.aperture.y; y < mVideoFmt.aperture.h; ++y) {
            for (int x = mVideoFmt.aperture.x; x < mVideoFmt.aperture.w; ++x) {
                int fromPos = 4 * (x + y * mVideoFmt.pixelWH.w);
                uint8_t blue  = pBitmapData[fromPos + 0];
                uint8_t green = pBitmapData[fromPos + 1];
                uint8_t red   = pBitmapData[fromPos + 2];
                uint8_t alpha = pBitmapData[fromPos + 3];
#if IMAGE_BGRA
                pImg_io[toPos++] = blue;
                pImg_io[toPos++] = green;
                pImg_io[toPos++] = red;
                pImg_io[toPos++] = 0xff; //< set Alpha to 255
#endif
#if IMAGE_RGBA
                pImg_io[toPos++] = red;
                pImg_io[toPos++] = green;
                pImg_io[toPos++] = blue;
                pImg_io[toPos++] = 0xff; //< set Alpha to 255
#endif
            }
        }
        assert(toPos == *imgBytes_io);
    }

end:
    if (pBitmapData) {
        dprintf("D: pBuffer->Unlock\n");
        pBuffer->Unlock();
        // After Unlock, pBitmapData pointer is no longer valid!
        pBitmapData = nullptr;
    }
    SafeRelease(&pBuffer);
    SafeRelease(&pSample);

    return hr;
}


