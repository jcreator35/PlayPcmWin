#include "WWPcmData.h"
#include <stdlib.h>
#include <assert.h>

void
WWPcmData::Init(void)
{
    stream = NULL;
}

void
WWPcmData::Term(void)
{
    delete [] stream;
    stream = NULL;
}

WWPcmData::~WWPcmData(void)
{
    assert(!stream);
}
