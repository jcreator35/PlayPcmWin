// 日本語

#pragma once

#include "WWDelay.h"
#include <assert.h>

/// ハーフバンドフィルターでローパスフィルターする。
class WWHalfbandFilter {
public:
    WWHalfbandFilter(int filterLength)
            : mCoeffs(nullptr),
              mDelay(filterLength) {
        // filterLength +1 must be multiply of 4
        assert(3 == (filterLength & 0x3));
        mFilterLength = filterLength;
        DesignFilter();
    }

    ~WWHalfbandFilter(void) {
        delete [] mCoeffs;
        mCoeffs = nullptr;
    }

    int FilterDelay(void) const { return (mFilterLength + 1)/2; }

    void Start(void);

    /// @param outPcm_r [out] フィルター出力。numIn個出る。
    void Filter(const float *inPcm, int numIn, float *outPcm_r);

    void End(void);

private:
    int mFilterLength;
    float *mCoeffs;
    WWDelay<float> mDelay;

    void DesignFilter(void);
};
