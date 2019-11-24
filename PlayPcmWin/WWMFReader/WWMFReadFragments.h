// 日本語。

#pragma once

#include "../WasapiIODLL/WWUtil.h"
#include "WWMFUtil.h"
#include <stdexcept>

class WWMFReadFragments {
public:
    IMFSourceReader *mReader = nullptr;

    ~WWMFReadFragments(void) {
        SafeRelease(&mReader);
    }

    HRESULT Start(const wchar_t *wszSourceFile) {
        HRESULT hr = S_OK;
        HRG(MFStartup(MF_VERSION));
        HRG(MFCreateSourceReaderFromURL(wszSourceFile, nullptr, &mReader));
        HRG(WWMFUtilConfigureAudioTypeToUncompressedPcm(mReader));

    end:
        return hr;
    }

    HRESULT ReadFragment(unsigned char *data_return, int64_t *dataBytes_inout) {
        HRESULT hr = S_OK;
        assert(data_return);
        const int64_t cbMaxAudioData = *dataBytes_inout;
        *dataBytes_inout = 0;

        IMFSample *pSample = nullptr;
        IMFMediaBuffer *pBuffer = nullptr;
        DWORD cbBuffer = 0;

        // pSampleが1個出てくるまで繰り返す。
        while (true) {
            DWORD dwFlags = 0;
            assert(pSample == nullptr);
            HRB_Quiet(mReader->ReadSample(
                (DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM,
                0,
                NULL,
                &dwFlags,
                NULL,
                &pSample));

            if (dwFlags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) {
                dprintf("Type change - not supported by WAVE file format.\n");
                goto end;
            }
            if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM) {
                //dprintf("End of input file.\n");
                goto end;
            }

            if (pSample == nullptr) {
                dprintf("No sample\n");
                continue;
            } else {
                // pSampleが出てきた。
                break;
            }
        }

        do {
            BYTE *pAudioData = nullptr;

            assert(pSample);
            assert(pBuffer == nullptr);
            HRB_Quiet(pSample->ConvertToContiguousBuffer(&pBuffer));

            cbBuffer = 0;
            HRB_Quiet(pBuffer->Lock(&pAudioData, NULL, &cbBuffer));

            if (cbMaxAudioData < cbBuffer) {
                // 十分に大きいサイズを指定して呼んで下さい。
                throw std::length_error("dataBytes_inout");
            }

            memcpy(&data_return[0], pAudioData, cbBuffer);

            hr = pBuffer->Unlock();
            pAudioData = nullptr;
        } while (false);

    end:
        *dataBytes_inout = cbBuffer;

        SafeRelease(&pSample);
        SafeRelease(&pBuffer);

        return hr;
    };

    void End(void) {
        SafeRelease(&mReader);
        MFShutdown();
    }
};
