#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"
#include <Windows.h>

enum WWPcmDataUsageType {
    WWPDUNowPlaying,
    WWPDUPauseResumeToPlay,
    WWPDUSpliceNext,
    WWPDUCapture,
    WWPDUSplice,
};

class WWPcmStream {
public:
    WWPcmStream(void);

    void PrepareSilenceBuffers(DWORD latencyMillisec, WWPcmDataSampleFormatType deviceSampleFormat,
            int deviceSampleRate, int deviceNumChannels, int deviceBytesPerFrame);

    void ReleaseBuffers(void);

    void Paused(WWPcmData *pauseResume);

    void SetStreamType(WWStreamType t);
    WWStreamType StreamType(void) const { return m_streamType; }

    void SetZeroFlushMillisec(int zeroFlushMillisec);

    void SetPauseResumePcmData(WWPcmData *pcm) { m_pauseResumePcmData = pcm; }

    void UpdateNowPlaying(WWPcmData *nowPlaying) {
        m_nowPlayingPcmData = nowPlaying;
    }
    void UpdatePauseResume(WWPcmData *pauseResume) {
        m_pauseResumePcmData = pauseResume;
    }

    /// 再生開始直後はStart無音を再生、その後startPcmDataを再生するようなリンクリストを作る。
    void UpdateStartPcm(WWPcmData *startPcm);

    /// リピートなしのときはendPcmDataの次にEnd無音を再生し再生停止するようなリンクリストを作る。
    /// リピート再生の場合はendPcmDataの次の曲がstartPcmDataになる。
    void UpdatePlayRepeat(bool repeat, WWPcmData *startPcmData, WWPcmData *endPcmData);

    bool IsSilenceBuffer(WWPcmData *p) const;

    WWPcmData *UnpausePrepare(void);
    void       UnpauseDone(void);

    WWPcmData *GetPcm(WWPcmDataUsageType t);
    WWPcmData *GetSilenceBuffer(WWPcmDataContentType t);

    /// -1: specified buffer is not used
    int GetPcmDataId(WWPcmDataUsageType t);

    /// @return total frames without pregap frame num
    int64_t TotalFrameNum(WWPcmDataUsageType t);

    /// @return negative number when playing pregap
    int64_t PosFrame(WWPcmDataUsageType t);

private:
    WWPcmData    *m_nowPlayingPcmData;
    WWPcmData    *m_pauseResumePcmData;
    WWPcmData    m_spliceBuffer;
    WWPcmData    m_startSilenceBuffer;
    WWPcmData    m_unpauseSilenceBuffer;
    WWPcmData    m_endSilenceBuffer;
    WWPcmData    m_pauseBuffer;

    WWStreamType m_streamType;

    int          m_zeroFlushMillisec;
};
