#include "WWSdmToPcm.h"
#include <crtdbg.h>

int main(void)
{
    WWSdmToPcm sp[2];

    int64_t totalOutputSamples = 10 * 44100;

    for (int ch=0; ch<2; ++ch) {
        sp[ch].Start(totalOutputSamples);
    }

    for (int64_t i=0; i<totalOutputSamples*64/16; ++i) {
        for (int ch=0; ch<2; ++ch) {
            sp[ch].AddInputSamples(0);
        }
    }

    for (int ch=0; ch<2; ++ch) {
        sp[ch].Drain();
        sp[ch].End();
    }

#ifndef NDEBUG
    _CrtDumpMemoryLeaks();
#endif
    return 0;
}
