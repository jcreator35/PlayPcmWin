// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWMFResampler.h"

class WWAudioReadThread 
{
public:
    /// スレッドを起動。最初の方を読み出す。
    /// @param path 読みだすファイル。
    /// @return S_OK 成功。負の値: エラー。
    HRESULT Init(const wchar_t *path, const WWMFPcmFormat &wantFmt);

    void Term(void);

	HRESULT Seek(int64_t pos);

	HRESULT GetPcm(int64_t pos, unsigned char *data_return, int64_t *dataBytes_inout);

private:
	WWMFPcmFormat mTargetFmt;
	WWMFPcmFormat mFileFmt;
    HANDLE mMutex = nullptr;
    HANDLE mShutdownEvent = nullptr;
    HANDLE mBufferEvent = nullptr;
    HANDLE mReadThread = nullptr;
    int mPlayStreamCount = 0;

    static DWORD ReadThreadEntry(LPVOID lpThreadParameter);
    HRESULT ReadThreadMain(void);
    HRESULT Read1(void);
};

