#include "WWPcmData.h"
#include <Windows.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <stdio.h>

void
WWPcmData::Init(WWPcmDataStreamAllocType t)
{
    stream = nullptr;
    allocType = t;
}

void
WWPcmData::Term(void)
{
    switch (allocType) {
    case WWPDSA_Normal:
        delete [] stream;
        stream = nullptr;
        break;
    case WWPDSA_LargeMemory:
        if (stream != nullptr) {
            if (!VirtualFree(stream, 0, MEM_RELEASE)) {
                printf("Error: VirtualFree %p\n", stream);
                assert(false);
            }
            stream = nullptr;
        }
        break;
    default:
        assert(0);
        break;
    }
}

WWPcmData::~WWPcmData(void)
{
    assert(!stream);
}

static unsigned char *
AllocStreamMemory(WWPcmDataStreamAllocType t, int64_t bytes)
{
    unsigned char *result = nullptr;
    switch (t) {
    case WWPDSA_Normal:
        result = new unsigned char[bytes];
        break;
    case WWPDSA_LargeMemory:
        {
            int64_t pageSize = GetLargePageMinimum();
            int64_t pageCount = ((bytes + pageSize-1)/pageSize);
            int64_t allocBytes = pageSize * pageCount;

            result = (unsigned char *)VirtualAlloc(nullptr, allocBytes, MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);
            if (nullptr == result) {
                printf("VirtualAlloc(%x) failed %x\n", allocBytes, GetLastError());
            }
        }
        break;
    default:
        assert(0);
        break;
    }

    assert(result);
    return result;
}

bool
WWPcmData::StoreStream(const unsigned char *aStream, int64_t bytes)
{
    stream = AllocStreamMemory(allocType, bytes);
    if (nullptr == stream) {
        return false;
    }
    memcpy(stream, aStream, bytes);
    return true;
}
