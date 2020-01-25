// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWMFResampler.h"
#include "WWMFReadFragments.h"

class WWAudioReadThread 
{
public:
    ~WWAudioReadThread(void);

    /// スレッドを起動。最初の方を読み出す。
    /// @param path 読みだすファイル。
    /// @return S_OK 成功。負の値: エラー。
    HRESULT Init(const wchar_t *path, const WWMFPcmFormat &wantFmt);

    void Term(void);

	HRESULT Seek(int64_t pos);

	HRESULT GetPcm(int64_t pos, unsigned char *data_return, int64_t *dataBytes_inout);

    HRESULT GetThreadErcd(void) const { return mThreadErcd; }

private:
	WWMFPcmFormat mTargetFmt;
	WWMFPcmFormat mFileFmt;
    WWMFReadFragments mReader;
    HANDLE mMutex = nullptr;
    HANDLE mShutdownEvent = nullptr;
    HANDLE mBufferEvent = nullptr;
    HANDLE mReadThread = nullptr;
    int mPlayStreamCount = 0;
    HRESULT mThreadErcd = 0;

    /// フレーム備蓄上限値。→読み出しスレッドを止める閾値。
    int mQueueFullFrames = 1024 * 128;

    /// フレーム備蓄が不足→読み出しスレッドを起動する閾値。
    int mQueueLowFrames  = 1024 * 64;

    static DWORD ReadThreadEntry(LPVOID lpThreadParameter);
    HRESULT ReadThreadMain(void);
    HRESULT Read1(void);
};

