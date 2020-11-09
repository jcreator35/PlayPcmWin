#pragma once

// 日本語 UTF-8

#include "WWAudioFilter.h"
#include "WWPcmSampleManipulator.h"
#include "WWPcmDelay.h"
#include <assert.h>

class WWAudioFilterFIR : public WWAudioFilter {
public:

    enum FilterFlags {
        // FIR係数が対称である。この場合若干の高速化が可能。
        WWAFFC_SYMMETRY      = 1,

        // coeffsのコピーを作る必要無し。
        WWAFFC_NOCOPY_COEFFS = 2,
    };

    WWAudioFilterFIR(int taps, const float *coeffs, int flags)
            : mDelays(nullptr), mCoeffs(nullptr), mTaps(taps), mFlags(flags) {
        assert(coeffs);

        if (mFlags & WWAFFC_NOCOPY_COEFFS) {
            // coeffsのコピーを作らずに参照する。
            mCoeffs = coeffs;
        } else {
            // coeffsのコピーを作ってmCoeffsにセットする。
            float *p = new float[mTaps];
            memcpy(p, coeffs, sizeof(float)*mTaps);
            mCoeffs = p;
        }
    }

    virtual ~WWAudioFilterFIR(void) {
        delete[] mDelays;
        mDelays = nullptr;

        if (mFlags & WWAFFC_NOCOPY_COEFFS) {
            mCoeffs = nullptr;
        } else {
            assert(mCoeffs);
            delete [] mCoeffs;
            mCoeffs = nullptr;
        }
    }

    virtual void UpdateSampleFormat(int sampleRate,
            WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels);
    virtual void Filter(unsigned char *buff, int bytes);

private:
    WWPcmSampleManipulator mManip;
    WWPcmDelay *mDelays;
    const float *mCoeffs;
    int mTaps;

    // FilterFlagsの組み合わせ。
    int mFlags;

    void FilterPcm(unsigned char *buff, int bytes);
    float Convolution(int ch);
};

