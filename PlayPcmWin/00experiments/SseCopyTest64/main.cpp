#include <Windows.h> //< QueryPerformanceCounter()
#include <stdio.h>  //< printf()
#include <string.h> //< memset()
#include <malloc.h> //< _aligned_malloc()
#include <assert.h> //< assert()
#include "MyMemcpy64.h"

#define BUFFER_SIZE (8192)

int main(void)
{
    char *from = (char*)_aligned_malloc(BUFFER_SIZE, 16);
    char *to   = (char*)_aligned_malloc(BUFFER_SIZE, 16);

    LARGE_INTEGER freq;
    QueryPerformanceFrequency(&freq);

    LARGE_INTEGER before;
    LARGE_INTEGER after;

    for (int j=0; j<10; ++j) {
        // test memcpy performance
        memset(from, 0x69, BUFFER_SIZE);
        QueryPerformanceCounter(&before);
        for (int i=0; i<100; ++i) {
            memcpy(to, from, BUFFER_SIZE);
        }
        QueryPerformanceCounter(&after);
        printf("memcpy %f\n", (after.QuadPart - before.QuadPart) * 1000.0 * 1000 / freq.QuadPart);

        // test MyMemcpy64 performance
        for (int i=0; i<BUFFER_SIZE; ++i) {
            from[i] = (char)i;
        }
        QueryPerformanceCounter(&before);
        for (int i=0; i<100; ++i) {
            MyMemcpy64(to, from, BUFFER_SIZE);
        }
        QueryPerformanceCounter(&after);
        printf("MyMemcpy64 %f\n", (after.QuadPart - before.QuadPart) * 1000.0 * 1000 / freq.QuadPart);
        for (int i=0; i<BUFFER_SIZE; ++i) {
            if (to[i] != (char)i) {
                printf("to[%d](%x) != %x\n", i, (int)to[i], (char)i);
            }
        }

        // test MyMemcpy64a performance
        for (int i=0; i<BUFFER_SIZE; ++i) {
            from[i] = (char)(i+69);
        }
        QueryPerformanceCounter(&before);
        for (int i=0; i<100; ++i) {
            MyMemcpy64a(to, from, BUFFER_SIZE);
        }
        QueryPerformanceCounter(&after);
        printf("MyMemcpy64a %f\n", (after.QuadPart - before.QuadPart) * 1000.0 * 1000 / freq.QuadPart);
        for (int i=0; i<BUFFER_SIZE; ++i) {
            if (to[i] != (char)(i+69)) {
                printf("to[%d](%x) != %x\n", i, (int)to[i], (char)(i+69));
            }
        }
    }

    _aligned_free(to);
    to = NULL;
    _aligned_free(from);
    from = NULL;

    return 0;
}
