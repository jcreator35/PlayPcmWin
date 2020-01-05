// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWMFResampler.h"

class WWAudioReadThread 
{
public:
    HRESULT Init(const WWMFPcmFormat &wantFmt);

    void Term(void);

    /// @param path 読みだすファイル。
	/// @return S_OK 成功。負の値: エラー。
    HRESULT Start(const wchar_t *path);
	HRESULT Seek(int64_t pos);

	HRESULT End(void);

	HRESULT GetPcm(int64_t pos, unsigned char *data_return, int64_t *dataBytes_inout);

private:
	WWMFPcmFormat mTargetFmt;
	WWMFPcmFormat mFileFmt;

    static DWORD ReadThreadEntry(LPVOID lpThreadParameter);
    HRESULT ReadThreadMain(void);
    HRESULT Read1(void);
};

