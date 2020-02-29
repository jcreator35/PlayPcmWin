// 日本語。

#include <SDKDDKVer.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdint.h>
#include <stdio.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <assert.h>
#include <Propvarutil.h>
#include "WWCommonUtil.h"
#include "WWMFVideoReaderIF.h"

// see output image using Gimp
static bool
SaveBufToFile(const uint8_t *buf, int bytes, const wchar_t *path)
{
    FILE *fp = nullptr;
    errno_t er = 0;
    
    er = _wfopen_s(&fp, path, L"wb");
    if (0 != er) {
        printf("E: SaveBuffToFile failed\n");
        return false;
    }

    fwrite(buf, 1, bytes, fp);

    fclose(fp);
    fp = nullptr;

    return true;
}

int
Run(const wchar_t *wszSourceFile)
{
    HRESULT hr = S_OK;
    int instanceId = -1;
    uint8_t *pImg = nullptr;
    int imgBytes = 0;
    int64_t pos = 0;
    WWMFVideoFormat vf;

    instanceId = WWMFVReaderIFReadStart(wszSourceFile);
    if (instanceId < 0) {
        hr = instanceId;
        goto end;
    }

    HRG(WWMFVReaderIFReadImage(instanceId, pos, &pImg, &imgBytes, &vf));
    SaveBufToFile(pImg, imgBytes, L"out.data");

end:
    delete[] pImg;
    pImg = nullptr;

    if (0 <= instanceId) {
        WWMFVReaderIFReadEnd(instanceId);
    }

    return hr;
}

int
wmain(void)
{
    (void)HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    HRESULT hr = S_OK;
    
    HRG(WWMFVReaderIFStaticInit());

    hr = Run(L"C:/data/Test1.mp4");

    WWMFVReaderIFStaticTerm();

end:
    return SUCCEEDED(hr) ? 0 : 1;
}

