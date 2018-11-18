// 日本語

#include "MainWindow.h"
#include "WWWinUtil.h"

int
main(int argc, char * argv[])
{
    MainWindow mw;
    HRESULT hr = S_OK;

    HRG(mw.Setup());
    HRG(mw.Loop());
end:
    dprintf("mw.Unsetup\n");
    mw.Unsetup();
    return 0;
}
