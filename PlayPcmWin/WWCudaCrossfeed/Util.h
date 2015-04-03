#ifndef H_Util
#define H_Util

#include <stdio.h>
#include <stdint.h>
#include <cufft.h>

#define WW_NUM_THREADS_PER_BLOCK (256)
#define WW_BLOCK_X               (32768)

#define CHECKED(x) if (!(x)) { goto END; }
#define CROSSFEED_COEF_NUM (8)

enum PcmChannelType {
    PCT_LeftLow,
    PCT_LeftHigh,
    PCT_RightLow,
    PCT_RightHigh,
    PCT_NUM
};

size_t
NextPowerOf2(size_t v);

bool
ReadOneLine(FILE *fp, char *line_return, size_t lineBytes);

void
GetBestBlockThreadSize(int count, dim3 &threads_return, dim3 &blocks_return);

const char *
CudaFftGetErrorString(cufftResult error);

extern int64_t gCudaAllocatedBytes;
extern int64_t gCudaMaxBytes;

#define CHK_CUDAMALLOC(pp, sz)                                                             \
    ercd = cudaMalloc(pp, sz);                                                             \
    if (cudaSuccess != ercd) {                                                             \
        printf("cudaMalloc(%dMBytes) failed. errorcode=%d (%s). allocated CUDA memory=%lld Mbytes\n", (int)(sz/1024/1024), ercd, cudaGetErrorString(ercd), gCudaAllocatedBytes/1024/1024); \
        return NULL;                                                                       \
    }                                                                                      \
    gCudaAllocatedBytes += sz;                                                             \
    if (gCudaMaxBytes < gCudaAllocatedBytes) {                                             \
        gCudaMaxBytes = gCudaAllocatedBytes;                                               \
    }

#define CHK_CUDAFREE(p, sz)        \
    cudaFree(p);                   \
    if (p != NULL) {               \
        p = NULL;                  \
        gCudaAllocatedBytes -= sz; \
    }

#define CHK_CUDAERROR(x)                                                              \
    ercd = x;                                                                         \
    if (cudaSuccess != ercd) {                                                        \
        printf("%s failed. errorcode=%d (%s)\n", #x, ercd, cudaGetErrorString(ercd)); \
        return NULL;                                                                  \
    }

#define CHK_CUFFT(x)                                                                               \
    fftResult = x;                                                                                 \
    if (cudaSuccess != fftResult) {                                                                \
        printf("%s failed. errorcode=%d (%s)\n", #x, fftResult, CudaFftGetErrorString(fftResult)); \
        return NULL;                                                                               \
    }

#endif /* H_Util */

