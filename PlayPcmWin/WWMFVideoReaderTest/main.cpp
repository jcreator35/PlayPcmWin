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
    WWMFVideoFormat vf;
    wchar_t path[256];
    int64_t seekHNS = -1; // 200LL * 1000 * 1000 * 10;

    instanceId = WWMFVReaderIFReadStart(wszSourceFile);
    if (instanceId < 0) {
        hr = instanceId;
        goto end;
    }

    imgBytes = 1920 * 1080 * 4;
    pImg = new uint8_t[imgBytes];


    do {
        HRG(WWMFVReaderIFReadImage(instanceId, seekHNS, pImg, &imgBytes, &vf));
        seekHNS = -1;

        swprintf_s(path, L"out/%012lld.data", vf.timeStamp);
        SaveBufToFile(pImg, imgBytes, path);
    } while (1);

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

    hr = Run(L"C:/data/test.mp4");

    WWMFVReaderIFStaticTerm();

end:
    return SUCCEEDED(hr) ? 0 : 1;
}

