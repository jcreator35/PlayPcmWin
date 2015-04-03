#include "AsioIOIF.h"
#include "asiodrivers.h"
#include <assert.h>

static AsioDrivers* g_pAsioDrivers = 0;

void
AsioDrvInit(void)
{
    if(!g_pAsioDrivers) {
        g_pAsioDrivers = new AsioDrivers();
    }
}

void
AsioDrvTerm(void)
{
    if (g_pAsioDrivers) {
        delete g_pAsioDrivers;
        g_pAsioDrivers = NULL;
    }
}

int
AsioDrvGetNumDev(void)
{
    assert(g_pAsioDrivers);
    return g_pAsioDrivers->asioGetNumDev();
}

int
AsioDrvGetDriverName(int id, char *name_return, unsigned int size)
{
    assert(g_pAsioDrivers);
    return g_pAsioDrivers->asioGetDriverName(id, name_return, size);
}

/* this is from asiodrivers.h */
bool loadAsioDriver(char *name);

bool
AsioDrvLoadDriver(char *name)
{
#if 1
    return loadAsioDriver(name);
#else
    assert(g_pAsioDrivers);
    return g_pAsioDrivers->loadDriver(name);
#endif
}

void
AsioDrvRemoveCurrentDriver(void)
{
    assert(g_pAsioDrivers);
    g_pAsioDrivers->removeCurrentDriver();
}
