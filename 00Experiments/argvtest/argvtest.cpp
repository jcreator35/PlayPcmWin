#include <stdio.h>   //< printf
#include <Windows.h> //< WideCharToMultiByte

// link Winmm.lib
#pragma comment(lib,"Winmm.lib")

static HRESULT
ReadWavFile(LPWSTR path)
{
    MMIOINFO mi = {0};
    HMMIO hFile = mmioOpen(path, &mi, MMIO_READ);

    {
        char s[256];
        WideCharToMultiByte(CP_ACP, 0, path, -1, s, sizeof s-1, NULL, NULL);
        printf("ReadWavFile(%s) mmioOpen() %x\n", s, hFile);
    }

    if (NULL == hFile) {
        return E_FAIL;
    }

    mmioClose(hFile, 0);
    return S_OK;
}

int
wmain(int argc, wchar_t* argv[])
{
    HRESULT hr = S_OK;

    printf("argc=%d\n", argc);
    for (int i=0; i<argc; ++i) {
        char s[256];
        WideCharToMultiByte(CP_ACP, 0, argv[i], -1, s, sizeof s-1, NULL, NULL);
        printf("argv[%d]=%s\n", i, s);
    }

    if (argc < 2) {
        printf("Usage: %S inputFileName\n", argv[0]);
        return 1;
    }

    hr = ReadWavFile(argv[1]);

    printf("ReadWavFile() result=%x\n", hr);

    return 0;
}

