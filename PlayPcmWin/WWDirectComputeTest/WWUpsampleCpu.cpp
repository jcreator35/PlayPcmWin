// 日本語。

#include "WWUpsampleCpu.h"
#include "WWUtil.h"
#include <assert.h>
#include <math.h>
#include <stdio.h>



HRESULT
WWUpsampleCpu(
        int convolutionN,
        float * sampleData,
        int sampleTotalFrom,
        int sampleRateFrom,
        int sampleRateTo,
        float * outputTo,
        int sampleTotalTo)
{
    HRESULT hr = S_OK;

    assert(sampleRateFrom <= sampleRateTo);

    unsigned int * resamplePosArray = new unsigned int[sampleTotalTo];
    assert(resamplePosArray);

    float * fractionArray = new float[sampleTotalTo];
    assert(fractionArray);

    double *sinPreComputeArray = new double[sampleTotalTo];
    assert(sinPreComputeArray);

    for (int i=0; i<sampleTotalTo; ++i) {
        double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
#if 1
        /* -0.5 <= fraction<+0.5になるようにresamplePosを選ぶ。
         * 最後のほうで範囲外を指さないようにする。
         */
        int resamplePosI = (int)(resamplePos+0.5);
        if (sampleTotalFrom <= resamplePosI) {
            resamplePosI = sampleTotalFrom -1;
        }
#else
        /* 0<=fraction<1になるにresamplePosIを選ぶ。
         * これは1に近い値が頻出するのでよくない。
         */
        int resamplePosI = (int)(resamplePos+0.5);
        assert(resamplePosI < sampleTotalFrom);
#endif
        double fraction = resamplePos - resamplePosI;

        resamplePosArray[i]   = resamplePosI;
        fractionArray[i]      = (float)fraction;
        sinPreComputeArray[i] = sin(-PI_D * fraction);
    }

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("i=%6d rPos=%6d fraction=%+f\n",
            i, resamplePosArray[i], fractionArray[i]);
    }
    */
    //printf("resamplePos created\n");

    for (int toPos=0; toPos<sampleTotalTo; ++toPos) {
        int    fromPos  = resamplePosArray[toPos];
        double fraction = fractionArray[toPos];
        double sinPreCompute = sinPreComputeArray[toPos];

        double v = 0.0;

        for (int convOffs=-convolutionN; convOffs < convolutionN; ++convOffs) {
            int pos = convOffs + fromPos;
            if (0 <= pos && pos < sampleTotalFrom) {
                double x = PI_D * (convOffs - fraction);
                
                double sinX = sinPreCompute;
                if (convOffs & 1) {
                    sinX *= -1.0;
                }

#if 1
                // 合っていた。
                assert(fabs(sinX - sin(x)) < 0.000001);
#endif

                double sinc =  SincD(sinX, x);

                /*
                if (pos == 0) {
                    printf("toPos=%d pos=%d x=%f sinX=%f",
                        toPos, pos, x, sinX);
                    printf("\n");
                }
                */

                v += sampleData[pos] * sinc;
            }
        }
        outputTo[toPos] = (float)v;
    }

    /*
    for (int i=0; i<sampleTotalTo; ++i) {
        printf("i=%6d rPos=%6d fraction=%+6.2f output=%f\n",
            i, resamplePosArray[i], fractionArray[i], outputTo[i]);
    }
    printf("resampled\n");
    */

//end:
    delete [] sinPreComputeArray;
    sinPreComputeArray = nullptr;

    delete [] fractionArray;
    fractionArray = nullptr;

    delete [] resamplePosArray;
    resamplePosArray = nullptr;

    return hr;
}
