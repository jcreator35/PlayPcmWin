// 日本語UTF-8

#include "WWPlayPcmGroup.h"
#include "WWMFResampler.h"
#include "WWUtil.h"
#include <assert.h>
#include <stdint.h>
#include <list>

void
WWPlayPcmGroup::Init(void)
{
}

void
WWPlayPcmGroup::Term(void)
{
    Clear();
}

void
WWPlayPcmGroup::Clear(void)
{
    for (size_t i=0; i<m_playPcmDataList.size(); ++i) {
        m_playPcmDataList[i].Term();
    }
    m_playPcmDataList.clear();

    m_pcmFormat.Clear();
}

bool
WWPlayPcmGroup::AddPlayPcmData(int id, BYTE *data, int64_t bytes)
{
#ifdef _X86_
    if (0x7fffffffL < bytes) {
        // cannot alloc 2GB buffer on 32bit build
        dprintf("E: %s(%d, %p, %lld) cannot alloc 2GB buffer on 32bit build\n", __FUNCTION__, id, data, bytes);
        return false;
    }
#endif

    if (0 == bytes) {
        dprintf("E: %s(%d, %p, %lld) arg check failed\n", __FUNCTION__, id, data, bytes);
        return false;
    }

    WWPcmData pcmData;
    if (!pcmData.Init(id, m_pcmFormat.sampleFormat, m_pcmFormat.numChannels,
            bytes/m_pcmFormat.BytesPerFrame(),
            m_pcmFormat.BytesPerFrame(), WWPcmDataContentMusicData, m_pcmFormat.streamType)) {
        dprintf("E: %s(%d, %p, %lld) malloc failed\n", __FUNCTION__, id, data, bytes);
        return false;
    }

    if (nullptr != data) {
        CopyMemory(pcmData.Stream(), data, (bytes/m_pcmFormat.BytesPerFrame()) * m_pcmFormat.BytesPerFrame());
    }
    m_playPcmDataList.push_back(pcmData);
    return true;
}

bool
WWPlayPcmGroup::AddPlayPcmDataStart(WWPcmFormat &pf)
{
    assert(m_playPcmDataList.size() == 0);
    assert(1 <= pf.numChannels);
    assert(1 <= pf.sampleRate);

    m_pcmFormat = pf;

    return true;
}

void
WWPlayPcmGroup::AddPlayPcmDataEnd(void)
{
    PlayPcmDataListDebug();
}

void
WWPlayPcmGroup::RemoveAt(int id)
{
    assert(0 <= id && (uint32_t)id < m_playPcmDataList.size());

    WWPcmData *pcmData = &m_playPcmDataList[id];
    pcmData->Term();

    m_playPcmDataList.erase(m_playPcmDataList.begin()+id);

    // 連続再生のリンクリストをつなげ直す。
    SetPlayRepeat(m_repeat);
}

void
WWPlayPcmGroup::SetPlayRepeat(bool repeat)
{
    dprintf("D: %s(%d)\n", __FUNCTION__, (int)repeat);
    m_repeat = repeat;

    if (m_playPcmDataList.size() < 1) {
        dprintf("D: %s(%d) pcmDataList.size() == %d nothing to do\n",
            __FUNCTION__, (int)repeat, m_playPcmDataList.size());
        return;
    }

    // 最初のpcmDataから、最後のpcmDataまでnextでつなげる。
    // リピートフラグが立っていたら最後のpcmDataのnextを最初のpcmDataにする。
    for (size_t i=0; i<m_playPcmDataList.size(); ++i) {
        if (i == m_playPcmDataList.size()-1) {
            // 最後→最初に接続。
            if (repeat) {
                m_playPcmDataList[i].SetNext(&m_playPcmDataList[0]);
            } else {
                // 最後→nullptr
                m_playPcmDataList[i].SetNext(nullptr);
            }
        } else {
            // 最後のあたりの項目以外は、連続にnextをつなげる。
            m_playPcmDataList[i].SetNext(&m_playPcmDataList[i+1]);
        }
    }
}

WWPcmData *
WWPlayPcmGroup::FindPcmDataById(int id)
{
    for (size_t i=0; i<m_playPcmDataList.size(); ++i) {
        if (m_playPcmDataList[i].Id() == id) {
            return &m_playPcmDataList[i];
        }
    }

    return nullptr;
}

WWPcmData *
WWPlayPcmGroup::FirstPcmData(void)
{
    if (0 == m_playPcmDataList.size()) {
        return nullptr;
    }

    return &m_playPcmDataList[0];
}

WWPcmData *
WWPlayPcmGroup::LastPcmData(void)
{
    if (0 == m_playPcmDataList.size()) {
        return nullptr;
    }

    return &m_playPcmDataList[m_playPcmDataList.size()-1];
}

WWPcmData *
WWPlayPcmGroup::NthPcmData(int n)
{
    if (n < 0 || m_playPcmDataList.size() <= (size_t)n) {
        return nullptr;
    }

    return &m_playPcmDataList[n];
}

void
WWPlayPcmGroup::PlayPcmDataListDebug(void)
{
#ifdef _DEBUG
    dprintf("D: %s() count=%u\n", __FUNCTION__, m_playPcmDataList.size());
    for (size_t i=0; i<m_playPcmDataList.size(); ++i) {
        WWPcmData *p = &m_playPcmDataList[i];

        dprintf("  %p next=%p i=%d id=%d nFrames=%lld posFrame=%lld contentType=%s stream=%p\n",
            p, p->Next(), i, p->Id(), p->Frames(), p->PosFrame(),
            WWPcmDataContentTypeToStr(p->ContentType()), p->Stream());
    }
#endif
}

bool
WWPlayPcmGroup::SdmToPcm(void)
{
    const size_t N = m_playPcmDataList.size();
    assert(1 <= N);

    if (m_pcmFormat.streamType != WWStreamDop) {
        // 変換の必要なし。
        return false;
    }

    // 変換の必要あり。
    for (size_t i=0; i<N; ++i) {
        WWPcmData *pd = &m_playPcmDataList[i];
        pd->DopToPcmFast();
    }

    // 4分の1のサンプルレートのPCM形式に変換された。
    m_pcmFormat.streamType = WWStreamPcm;
    m_pcmFormat.sampleRate /= 4;

    return true;
}

HRESULT
WWPlayPcmGroup::DoResample(WWPcmFormat &targetFmt, int conversionQuality)
{
    HRESULT hr = S_OK;
    WWMFResampler resampler;
    size_t n = m_playPcmDataList.size();
    const int PROCESS_FRAMES = 128 * 1024;
    BYTE *buff = new BYTE[PROCESS_FRAMES * m_pcmFormat.BytesPerFrame()];
    std::list<size_t> toPcmDataIdxList;
    size_t numConvertedPcmData = 0;
    assert(1 <= conversionQuality && conversionQuality <= 60);

    if (nullptr == buff) {
        hr = E_OUTOFMEMORY;
        goto end;
    }

    if (SdmToPcm()) {
        // SdmをPCMに変換した。
    }

    // 共有モードのサンプルレート変更。
    HRG(resampler.Initialize(
        WWMFPcmFormat(
            (WWMFBitFormatType)WWPcmDataSampleFormatTypeIsFloat(m_pcmFormat.sampleFormat),
            (WORD)m_pcmFormat.numChannels,
            (WORD)WWPcmDataSampleFormatTypeToBitsPerSample(m_pcmFormat.sampleFormat),
            m_pcmFormat.sampleRate,
            0, //< TODO: target dwChannelMask
            (WORD)WWPcmDataSampleFormatTypeToValidBitsPerSample(m_pcmFormat.sampleFormat)),
        WWMFPcmFormat(
            WWMFBitFormatFloat,
            (WORD)targetFmt.numChannels,
            32,
            targetFmt.sampleRate,
            0, //< TODO: target dwChannelMask
            32),
        conversionQuality));

    for (size_t i=0; i<n; ++i) {
        WWPcmData *pFrom = &m_playPcmDataList[i];
        WWPcmData pcmDataTo;

        if (!pcmDataTo.Init(pFrom->Id(), targetFmt.sampleFormat, targetFmt.numChannels,
                (int64_t)(((double)targetFmt.sampleRate / m_pcmFormat.sampleRate) * pFrom->Frames()),
                targetFmt.numChannels * WWPcmDataSampleFormatTypeToBitsPerSample(targetFmt.sampleFormat)/8,
                WWPcmDataContentMusicData, m_pcmFormat.streamType)) {
            dprintf("E: %s malloc failed. pcm id=%d\n", __FUNCTION__, pFrom->Id());
            hr = E_OUTOFMEMORY;
            goto end;
        }
        m_playPcmDataList.push_back(pcmDataTo);
        pFrom = &m_playPcmDataList[i];

        toPcmDataIdxList.push_back(n+i);

        dprintf("D: pFrom stream=%p nFrames=%lld\n", pFrom->Stream(), pFrom->Frames());

        for (size_t posFrames=0; ; posFrames += PROCESS_FRAMES) {
            WWMFSampleData mfSampleData;
            DWORD consumedBytes = 0;

            int buffBytes = pFrom->GetBufferData(posFrames * m_pcmFormat.BytesPerFrame(), PROCESS_FRAMES * m_pcmFormat.BytesPerFrame(), buff);
            dprintf("D: pFrom->GetBufferData posBytes=%Iu bytes=%d rv=%d\n",
                    posFrames * m_pcmFormat.BytesPerFrame(), PROCESS_FRAMES * m_pcmFormat.BytesPerFrame(), buffBytes);
            if (0 == buffBytes) {
                break;
            }

            HRG(resampler.Resample(buff, buffBytes, &mfSampleData));
            dprintf("D: resampler.Resample mfSampleData.bytes=%u\n",
                    mfSampleData.bytes);
            consumedBytes = 0;
            while (0 < toPcmDataIdxList.size() && consumedBytes < mfSampleData.bytes) {
                size_t toIdx = toPcmDataIdxList.front();
                WWPcmData *pTo = &m_playPcmDataList[toIdx];
                assert(pTo);
                int rv = pTo->FillBufferAddData(&mfSampleData.data[consumedBytes], mfSampleData.bytes - consumedBytes);
                dprintf("D: consumedBytes=%d/%d FillBufferAddData() pTo->stream=%p pTo->nFrames=%lld rv=%d\n",
                        consumedBytes, mfSampleData.bytes, pTo->Stream(), pTo->Frames(), rv);
                consumedBytes += rv;
                if (0 == rv) {
                    pTo->FillBufferEnd();
                    ++numConvertedPcmData;
                    toPcmDataIdxList.pop_front();
                }
            }
            mfSampleData.Release();
        }
        pFrom->Term();
    }

    {
        WWMFSampleData mfSampleData;
        DWORD consumedBytes = 0;

        HRG(resampler.Drain(PROCESS_FRAMES * m_pcmFormat.BytesPerFrame(), &mfSampleData));
        consumedBytes = 0;
        while (0 < toPcmDataIdxList.size() && consumedBytes < mfSampleData.bytes) {
            size_t toIdx = toPcmDataIdxList.front();
            WWPcmData *pTo = &m_playPcmDataList[toIdx];
            assert(pTo);
            int rv = pTo->FillBufferAddData(&mfSampleData.data[consumedBytes], mfSampleData.bytes - consumedBytes);
            consumedBytes += rv;
            if (0 == rv) {
                pTo->FillBufferEnd();
                ++numConvertedPcmData;
                toPcmDataIdxList.pop_front();
            }
        }
        mfSampleData.Release();
    }

    while (0 < toPcmDataIdxList.size()) {
        size_t toIdx = toPcmDataIdxList.front();
        WWPcmData *pTo = &m_playPcmDataList[toIdx];
        assert(pTo);

        pTo->FillBufferEnd();
        if (0 == pTo->Frames()) {
            hr = E_FAIL;
            goto end;
        }
        ++numConvertedPcmData;
        toPcmDataIdxList.pop_front();
    }

    assert(n == numConvertedPcmData);

    for (size_t i=0; i<n; ++i) {
        m_playPcmDataList[i] = m_playPcmDataList[n+i];
        m_playPcmDataList[n+i].Forget();
    }

    m_playPcmDataList.resize(numConvertedPcmData);

    // update pcm format info
    m_pcmFormat.sampleFormat  = targetFmt.sampleFormat;
    m_pcmFormat.sampleRate    = targetFmt.sampleRate;
    m_pcmFormat.numChannels   = targetFmt.numChannels;
    m_pcmFormat.dwChannelMask = targetFmt.dwChannelMask;

    // reduce volume level when out of range sample value is found
    {
        float maxV = 0.0f;
        float minV = 0.0f;
        const float  SAMPLE_VALUE_MAX_FLOAT  =  1.0f;
        const float  SAMPLE_VALUE_MIN_FLOAT  = -1.0f;

        for (size_t i=0; i<n; ++i) {
            float currentMax = 0.0f;
            float currentMin = 0.0f;
            m_playPcmDataList[i].FindSampleValueMinMax(&currentMin, &currentMax);
            if (currentMin < minV) {
                minV = currentMin;
            }
            if (maxV < currentMax) {
                maxV = currentMax;
            }
        }

        float scale = 1.0f;
        if (SAMPLE_VALUE_MAX_FLOAT < maxV) {
            scale = SAMPLE_VALUE_MAX_FLOAT / maxV;
        }
        if (minV < SAMPLE_VALUE_MIN_FLOAT && SAMPLE_VALUE_MIN_FLOAT / minV < scale) {
            scale = SAMPLE_VALUE_MIN_FLOAT / minV;
        }
        if (scale < 1.0f) {
            for (size_t i=0; i<n; ++i) {
                m_playPcmDataList[i].ScaleSampleValue(scale);
            }
        }
    }

end:
    resampler.Finalize();
    delete [] buff;
    buff = nullptr;
    return hr;
}
