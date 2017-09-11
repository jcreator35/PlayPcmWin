#include "WWMFReader.h"
#include "WWUtil.h"
#include <stdio.h>

int wmain(int argc, WCHAR *argv[])
{
    WWMFReader reader;
    WWMFMetadata metadata;
    
    HRESULT hr;

    if (argc != 2) {
        return 1;
    }
    LPCWCHAR path = argv[1];

    HRG(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE));
    HRG(MFStartup(MF_VERSION));

    HRG(reader.Init(path));
    HRG(reader.RetrieveMFMetadata(metadata));

    printf("title : %S\n"
           "album : %S\n"
           "artist: %S\n"
           "coverart: %d bytes"
           "numChannels: %d\n"
           "sampleRate: %d\n"
           "bitsPerSample: %d\n"
           "validBitsPerSample: %d\n",
           metadata.title,
           metadata.albumName,
           metadata.artistName,
           metadata.albumCoverArtBytes,
           metadata.numChannels,
           metadata.samplesPerSec,
           metadata.bitsPerSample,
           metadata.validBitsPerSample);

end:
    reader.Term();

    MFShutdown();
    CoUninitialize();

    if (FAILED(hr)) {
        return 1;
    }
    return 0;
}
