#include "Util.h"
#include <stdio.h>

int64_t gCudaAllocatedBytes = 0;
int64_t gCudaMaxBytes = 0;

size_t
NextPowerOf2(size_t v)
{
    size_t result = 1;
    if (INT_MAX+1U < v) {
        printf("Error: NextPowerOf2(%d) too large!\n", v);
        return 0;
    }
    while (result < v) {
        result *= 2;
    }
    return result;
}

bool
ReadOneLine(FILE *fp, char *line_return, size_t lineBytes)
{
    line_return[0] = 0;
    int c;
    int pos = 0;

    do {
        c = fgetc(fp);
        if (c == EOF || c == '\n') {
            break;
        }

        if (c != '\r') {
            line_return[pos] = (char)c;
            line_return[pos+1] = 0;
            ++pos;
        }
    } while (c != EOF && pos < (int)lineBytes -1);

    return c != EOF;
}

void
GetBestBlockThreadSize(int count, dim3 &threads_return, dim3 &blocks_return)
{
    if ((count / WW_NUM_THREADS_PER_BLOCK) <= 1) {
        threads_return.x = count;
    } else {
        threads_return.x = WW_NUM_THREADS_PER_BLOCK;
        threads_return.y = 1;
        threads_return.z = 1;
        int countRemain = count / WW_NUM_THREADS_PER_BLOCK;
        if ((countRemain / WW_BLOCK_X) <= 1) {
            blocks_return.x = countRemain;
            blocks_return.y = 1;
            blocks_return.z = 1;
        } else {
            blocks_return.x = WW_BLOCK_X;
            countRemain /= WW_BLOCK_X;
            blocks_return.y = countRemain;
            blocks_return.z = 1;
        }
    }
}




const char *
CudaFftGetErrorString(cufftResult error)
{
    switch (error) {
        case CUFFT_SUCCESS:       return "CUFFT_SUCCESS";
        case CUFFT_INVALID_PLAN:  return "CUFFT_INVALID_PLAN";
        case CUFFT_ALLOC_FAILED:  return "CUFFT_ALLOC_FAILED";
        case CUFFT_INVALID_TYPE:  return "CUFFT_INVALID_TYPE";
        case CUFFT_INVALID_VALUE: return "CUFFT_INVALID_VALUE";

        case CUFFT_INTERNAL_ERROR: return "CUFFT_INTERNAL_ERROR";
        case CUFFT_EXEC_FAILED:    return "CUFFT_EXEC_FAILED";
        case CUFFT_SETUP_FAILED:   return "CUFFT_SETUP_FAILED";
        case CUFFT_INVALID_SIZE:   return "CUFFT_INVALID_SIZE";
        case CUFFT_UNALIGNED_DATA: return "CUFFT_UNALIGNED_DATA";

        case CUFFT_INCOMPLETE_PARAMETER_LIST: return "CUFFT_INCOMPLETE_PARAMETER_LIST";
        case CUFFT_INVALID_DEVICE:            return "CUFFT_INVALID_DEVICE";
        case CUFFT_PARSE_ERROR:               return "CUFFT_PARSE_ERROR";
        case CUFFT_NO_WORKSPACE:              return "CUFFT_NO_WORKSPACE";
        default: return "unknown";
    }
}