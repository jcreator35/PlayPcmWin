// 日本語

#include "WWHalfbandFilter.h"
#include <stdint.h>
#define _USE_MATH_DEFINES
#include <math.h>
#include "WWDftCpu.h"

void
WWHalfbandFilter::Start(void)
{
    mDelay.FillZeroes();
}

void
WWHalfbandFilter::End(void)
{
}

// Understanding Digital Signal Processing 3rd ed., pp.546
void
WWHalfbandFilter::DesignFilter(void) {
    assert(mCoeffs == nullptr);

#if 1
    auto *coeffs = new double[mFilterLength];

    const int len1 = mFilterLength+1;
    auto *coeffsF = new std::complex<double>[len1];
    for (int i=0; i<len1;++i) {
        int phase = i * 4 / len1;
        switch (phase) {
        case 0:
            coeffsF[i] = std::complex<double>(1.0,0.0);
            break;
        case 1:
            coeffsF[i] = std::complex<double>(0.0,0.0);
            break;
        case 2:
            coeffsF[i] = std::complex<double>(0.0,0.0);
            break;
        case 3:
            coeffsF[i] = std::complex<double>(1.0,0.0);
            break;
        }

        if (i == len1/4) {
            coeffsF[i] = std::complex<double>(0.5,0.0);
        }
        if (i == 3 * len1/4) {
            coeffsF[i] = std::complex<double>(0.5,0.0);
        }
    }

    auto *coeffsT = new std::complex<double>[len1];
    WWDftCpu::Dft1d(coeffsF, len1, coeffsT);

    int pos = len1/2 + 1;
    for (int i=0; i<mFilterLength; ++i) {
        coeffs[i] = coeffsT[pos++].real();
        if (len1 <= pos) {
            pos = 0;
        }
    }



    delete [] coeffsT;
    coeffsT = nullptr;

    delete [] coeffsF;
    coeffsF = nullptr;
#else
    static const double gSine90Table[] = { 0.0, 1.0, 0.0, -1.0 };

    auto *coeffs = new double[mFilterLength];
    memset(coeffs, 0, sizeof(double)*mFilterLength);

    const int filterCenter = (mFilterLength+1)/2;
    for (int i = 0; i < filterCenter; ++i) {
        if (i != 0 && 0 == (i & 1)) {
            // coefficient is 0
            continue;
        }
        double theta = M_PI * (i * 90.0) / 180.0f;
        double v = 1.0;
        if (DBL_EPSILON < abs(theta)) {
            v = gSine90Table[i & 3] / theta;
        }
        coeffs[filterCenter - 1 - i] = v;
        coeffs[filterCenter - 1 + i] = v;
    }

# if 0
    // Kaiser窓(α==9)をかける
    auto * w = new double[mFilterLength];
    WWKaiserWindow(9.0, mFilterLength, w);
    for (int i = 0; i < mFilterLength; ++i) {
        coeffs[i] *= w[i];
    }
    delete [] w;
    w = nullptr;
# endif
#endif

#if 0
    // 0.5倍する
    for (int i = 0; i < mFilterLength; ++i) {
        coeffs[i] *= 0.5;
    }
#endif

    mCoeffs = new float[mFilterLength];
    memset(mCoeffs, 0, sizeof(float)*mFilterLength);

    for (int i=0;i<mFilterLength; ++i) {
        if (0 == (i&1) || i == mFilterLength/2) {
            mCoeffs[i] = (float)coeffs[i];
        }
    }

#if 1
    // 合計が1.0になるようにする。
    float sum = 0.0f;
    for (int i=0;i<mFilterLength; ++i) {
        sum += mCoeffs[i];
    }
    for (int i=0;i<mFilterLength; ++i) {
        mCoeffs[i] *= 1.0f / sum;
    }
#endif

    delete [] coeffs;
    coeffs = nullptr;
}

// Understanding Digital Signal Processing 3rd ed.
// pp.546-547 Figure 10-27 (c)
// pp.703 Figure 13-16 (a)
void
WWHalfbandFilter::Filter(
        const float *inPcm, int numIn, float *outPcm_r)
{
    assert(mCoeffs);
    assert(inPcm);
    assert(outPcm_r);

    int outPos = 0;
    for (int inPos=0; inPos < numIn; ++inPos) {
        float v = inPcm[inPos];

        //v = 1.0f;

        mDelay.Filter(v);

        float r = 0;
        for (int i=0; i<mFilterLength; ++i) {
            r += mCoeffs[i] * mDelay.GetNth(i);
        }

        //printf("%f\n", r);

        outPcm_r[outPos++] = r;
    }
}
