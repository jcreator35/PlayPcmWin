// 日本語

#pragma once

#include "WWDelay.h"
#include <assert.h>

/// ハーフバンドフィルターでローパスフィルターして2倍アップサンプルする。
class WWHalfbandFilterUpsampler {
public:
    WWHalfbandFilterUpsampler(int filterLength)
            : mCoeffsU(nullptr), mCoeffL(0),
              mDelayU((filterLength-1)/2), mDelayL((filterLength-3)/4) {
        // filterLength +1 must be multiply of 4
        assert(3 == (filterLength & 0x3));
        mFilterLength = filterLength;
        DesignFilter();
    }

    ~WWHalfbandFilterUpsampler(void) {
        delete [] mCoeffsU;
        mCoeffsU = nullptr;
    }

    int FilterDelay(void) const { return (mFilterLength - 1)/2; }

    void Start(void);

    /// @param outPcm_r [out] フィルター出力。numIn*2個出る。
    void Filter(const float *inPcm, int numIn, float *outPcm_r);

    void End(void);

private:
    int mFilterLength;
    float *mCoeffsU;
    float mCoeffL;
    WWDelay<float> mDelayU;
    WWDelay<float> mDelayL;

    void DesignFilter(void);
};
