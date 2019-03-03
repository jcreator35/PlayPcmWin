#include <stdio.h>
#include "WWSpatialAudioUser.h"

static void
Run(void)
{
    WWSpatialAudioUser sa;
    sa.Init();

    sa.DoDeviceEnumeration();

    for (int i = 0; i < sa.GetDeviceCount(); ++i) {
        wchar_t s[256];
        memset(s, 0, sizeof s);
        sa.GetDeviceName(i, s, sizeof s -2);
        printf("%d: %S\n", i, s);

        sa.PrintDeviceProperties(i);
    }

    sa.Term();
}


int main(void)
{
    Run();
    return 0;
}

