// 日本語

#pragma once

#include "WWDelay.h"



class WWFIRFilter {
public:
    enum WWFIRFilterFlags {
        // FIR係数が対称である。この場合若干の高速化が可能。
        WWFIRFF_SYMMETRY      = 1,

        // coeffsのコピーを作る必要無し。
        WWFIRFF_NOCOPY_COEFFS = 2,
    };

    /// @param flags WWFIRFF_???の組み合わせ。
    WWFIRFilter(int taps, const double * coeffs, int flags) : mDelay(taps), mCoeffs(nullptr), mTaps(taps), mFlags(flags) {
        if (WWFIRFF_NOCOPY_COEFFS & mFlags) {
            // コピーを作る必要が無い。
            mCoeffs = coeffs;
        } else {
            // コピーを作成し使用する。
            double *p = new double[taps];
            memcpy(p, coeffs, sizeof(double)*taps);
            mCoeffs = p;
        }
    }

    ~WWFIRFilter(void) {
        if (WWFIRFF_NOCOPY_COEFFS & mFlags) {
            // コピーを作っていない。
            mCoeffs = nullptr;
        } else {
            // コピーを作成した。
            delete [] mCoeffs;
            mCoeffs = nullptr;
        }
    }

    void Filter(int count, const double * inPcm, double *outPcm);

private:
    WWDelay<double> mDelay;
    const double *mCoeffs;
    const int mTaps;
    int mFlags;

    double Convolution(void);
};

