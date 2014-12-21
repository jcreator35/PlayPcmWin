// 日本語 UTF-8

#include "WWPcmStream.h"
#include "WWUtil.h"
#include <assert.h>

WWPcmStream::WWPcmStream(void)
        : m_nowPlayingPcmData(nullptr),
          m_pauseResumePcmData(nullptr),
          m_streamType(WWStreamPcm),
          m_zeroFlushMillisec(0)
{
}

void
WWPcmStream::SetStreamType(WWStreamType t)
{
    m_streamType = t;
}

void
WWPcmStream::SetZeroFlushMillisec(int zeroFlushMillisec)
{
    assert(0 <= zeroFlushMillisec);
    m_zeroFlushMillisec = zeroFlushMillisec;
}

void
WWPcmStream::PrepareSilenceBuffers(DWORD latencyMillisec, WWPcmDataSampleFormatType deviceSampleFormat,
        int deviceSampleRate, int deviceNumChannels, int deviceBytesPerFrame)
{
    // 再生前無音0(初回再生用) 再生前無音1(一時停止再開用)、再生後無音の準備。
    // DoPマーカーが2フレームを1周期として繰り返されているため、これらのバッファのフレーム数は2の倍数になるようにする。
    {
        DWORD startZeroFlushMillisec = m_zeroFlushMillisec;
        if (startZeroFlushMillisec < latencyMillisec) {
            startZeroFlushMillisec = latencyMillisec;
        }
        m_startSilenceBuffer.Init(-1, deviceSampleFormat, deviceNumChannels,
                (1 * (int)((int64_t)deviceSampleRate * startZeroFlushMillisec / 1000) + 1) & (~1),
                deviceBytesPerFrame, WWPcmDataContentSilenceForTrailing);
    }

    m_unpauseSilenceBuffer.Init(-1, deviceSampleFormat, deviceNumChannels,
            (1 * (int)((int64_t)deviceSampleRate * latencyMillisec / 1000) + 1) & (~1),
            deviceBytesPerFrame, WWPcmDataContentSilenceForPause);

    // endSilenceBufferは最後に再生される無音。
    m_endSilenceBuffer.Init(-1, deviceSampleFormat, deviceNumChannels,
            (4 * (int)((int64_t)deviceSampleRate * latencyMillisec / 1000) + 1) & (~1),
            deviceBytesPerFrame, WWPcmDataContentSilenceForEnding);
    m_endSilenceBuffer.next = nullptr;

    // spliceバッファー。サイズは100分の1秒=10ms 適当に選んだ。
    m_spliceBuffer.Init(-1, deviceSampleFormat, deviceNumChannels,
            (deviceSampleRate / 100 + 1) & (~1),
            deviceBytesPerFrame, WWPcmDataContentSplice);

    // pauseバッファー。ポーズ時の波形つなぎに使われる。spliceバッファーと同様。
    m_pauseBuffer.Init(-1, deviceSampleFormat, deviceNumChannels,
            (deviceSampleRate / 100 + 1) & (~1),
            deviceBytesPerFrame, WWPcmDataContentSplice);

    switch (m_streamType) {
    case WWStreamPcm:
        // Init()で0フィルされているので処理不要。
        break;
    case WWStreamDop:
        m_startSilenceBuffer.FillDopSilentData();
        m_unpauseSilenceBuffer.FillDopSilentData();
        m_endSilenceBuffer.FillDopSilentData();
        m_spliceBuffer.FillDopSilentData();
        m_pauseBuffer.FillDopSilentData();
        break;
    default:
        assert(0);
        break;
    }
}

void
WWPcmStream::ReleaseBuffers(void)
{
    m_spliceBuffer.Term();
    m_pauseBuffer.Term();
    m_startSilenceBuffer.Term();
    m_unpauseSilenceBuffer.Term();
    m_endSilenceBuffer.Term();

    m_nowPlayingPcmData = nullptr;

}

void
WWPcmStream::Paused(WWPcmData *pauseResume)
{
    m_pauseResumePcmData = pauseResume;

    m_pauseBuffer.posFrame = 0;
    m_pauseBuffer.next = &m_endSilenceBuffer;

    m_endSilenceBuffer.posFrame = 0;
    m_endSilenceBuffer.next = nullptr;

    m_pauseBuffer.UpdateSpliceDataWithStraightLine(
        *m_nowPlayingPcmData, m_nowPlayingPcmData->posFrame,
        m_endSilenceBuffer,   m_endSilenceBuffer.posFrame);

    m_nowPlayingPcmData = &m_pauseBuffer;
}

bool
WWPcmStream::IsSilenceBuffer(WWPcmData *p) const
{
    return p == &m_spliceBuffer ||
           p == &m_startSilenceBuffer ||
           p == &m_unpauseSilenceBuffer ||
           p == &m_endSilenceBuffer ||
           p == &m_pauseBuffer;
}

WWPcmData *
WWPcmStream::UnpausePrepare(void)
{
    // 再生するPCMデータへフェードインするPCMデータをpauseBufferにセットして
    // 再生開始する。
    assert(m_pauseResumePcmData);

    dprintf("%s resume=%p posFrame=%d\n", __FUNCTION__, m_pauseResumePcmData, m_pauseResumePcmData->posFrame);

    m_unpauseSilenceBuffer.posFrame = 0;
    m_unpauseSilenceBuffer.next = &m_pauseBuffer;

    m_pauseBuffer.posFrame = 0;
    m_pauseBuffer.next = m_pauseResumePcmData;

    m_pauseBuffer.UpdateSpliceDataWithStraightLine(
            m_unpauseSilenceBuffer, m_unpauseSilenceBuffer.posFrame,
            *m_pauseResumePcmData, m_pauseResumePcmData->posFrame);

    return &m_unpauseSilenceBuffer;
}

void
WWPcmStream::UnpauseDone(void)
{
    m_pauseResumePcmData = nullptr;
}

void
WWPcmStream::UpdateStartPcm(WWPcmData *startPcm)
{
    m_nowPlayingPcmData = &m_startSilenceBuffer;
    m_nowPlayingPcmData->next = startPcm;
}

void
WWPcmStream::UpdatePlayRepeat(bool repeat, WWPcmData *startPcmData, WWPcmData *endPcmData)
{
    assert(!IsSilenceBuffer(startPcmData));
    assert(!IsSilenceBuffer(endPcmData));

    if (!repeat) {
        // リピートなし。endPcmData→endSilence→nullptr
        endPcmData->next = &m_endSilenceBuffer;
    } else {
        // リピートあり。endPcmData→startPcmData
        endPcmData->next = startPcmData;
    }
}

WWPcmData *
WWPcmStream::GetPcm(WWPcmDataUsageType t)
{
    WWPcmData *pcm = nullptr;

    switch (t) {
    case WWPDUNowPlaying:
        pcm = m_nowPlayingPcmData;
        break;
    case WWPDUPauseResumeToPlay:
        pcm = m_pauseResumePcmData;
        break;
    case WWPDUSplice:
        pcm = &m_spliceBuffer;
        break;
    case WWPDUSpliceNext:
        pcm = m_spliceBuffer.next;
        break;
    case WWPDUCapture:
        assert(0);
        break;
    default:
        assert(0);
        break;
    }

    return pcm;
}

WWPcmData *
WWPcmStream::GetSilenceBuffer(WWPcmDataContentType t)
{
    WWPcmData *pcm = nullptr;

    switch (t) {
    case WWPcmDataContentSilenceForTrailing:
        pcm = &m_startSilenceBuffer;
        break;
    case WWPcmDataContentSilenceForPause:
        pcm = &m_unpauseSilenceBuffer;
        break;
    case WWPcmDataContentSilenceForEnding:
        pcm = &m_endSilenceBuffer;
        break;
    default:
        assert(0);
        break;
    }

    return pcm;
}

int
WWPcmStream::GetPcmDataId(WWPcmDataUsageType t)
{
    WWPcmData *pcm = GetPcm(t);

    if (!pcm) {
        return -1;
    }
    return pcm->id;
}

int64_t
WWPcmStream::TotalFrameNum(WWPcmDataUsageType t)
{
    int64_t result = 0;

    WWPcmData *pcm = GetPcm(t);

    if (pcm) {
        result = pcm->nFrames;
    }

    return result;
}

int64_t
WWPcmStream::PosFrame(WWPcmDataUsageType t)
{
    int64_t result = 0;

    WWPcmData *pcm = GetPcm(t);

    // assert(m_mutex);
    // WaitForSingleObject(m_mutex, INFINITE);
    if (pcm) {
        result = pcm->posFrame;
    }
    //ReleaseMutex(m_mutex);

    return result;
}