// 日本語

/*
 * References:
 * [1] Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 702
 */

#include "WWFIRFilter.h"


double
WWFIRFilter::Convolution(void)
{
    double v = 0.0;

    if (WWFIRFF_SYMMETRY & mFlags) {
        // FIRフィルター係数が左右対称なので参考文献[1]の方法で乗算回数を半分に削減できる。
        int center = mTaps / 2;
        for (int i = 0; i < center; ++i) {
            v += mCoeffs[i] * (
                mDelay.GetNth(i) +
                mDelay.GetNth(mTaps - i - 1));
        }
        v += mCoeffs[center] * mDelay.GetNth(center);
    } else {
        // mDelay.GetNthは現在から過去の方向順に取り出されることに注意。
        for (int i=0; i<mTaps; ++i) {
            v += mCoeffs[i] * mDelay.GetNth(i);
        }
    }

    return v;
}

void
WWFIRFilter::Filter(int count, const double * inPcm, double *outPcm)
{
    for (long i = 0; i < count; ++i) {
        mDelay.Filter(inPcm[i]);
        outPcm[i] = Convolution();
    }
}

