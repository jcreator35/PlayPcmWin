/* NOSDAC high frequency roll-off compensation filter
 * 
 * NOSDAC frequency response (continuous-time): Hr_zoh(jΩ)=sinc(jΩ/2)
 * Compensation filter frequency response (discrete-time): Hr_comp(ω) = 1 / sinc(ω/2)
 * 
 * FIR filter coefficients calculated by Frequency-Sampling design method:
 * filter taps=M, M is odd
 * G(k) = Hr_comp(2πk/M) * (-1)^k, G(k)=-G(M-k)
 * U=(M-1)/2
 * h(n) = (1/M)*{G(0)+2*Σ_{k=1}^{U}{G(k)*cos{2πk*(n+0.5)/M}}
 * 
 * References:
 * [1] A. V. Oppenheim, R. W. Schafer, Discrete-Time Signal Processing, 3rd Ed, Prentice Hall, 2009, pp. 600-604
 * [2] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
 * [3] Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 702
 */

#include "stdafx.h"
#include "WWZohCompensation.h"

static const int COEFF_LENGTH = 33;

static const double gCoeffs[] = {
        4.44017E-05* 1.5405388308838,-0.000135225* 1.5405388308838,0.000232335* 1.5405388308838,-0.000340729* 1.5405388308838,0.0004668* 1.5405388308838,-0.000619343* 1.5405388308838,0.000811212* 1.5405388308838,-0.001062254* 1.5405388308838,
        0.001404865* 1.5405388308838,-0.001895252* 1.5405388308838,0.002638312* 1.5405388308838,-0.00384863* 1.5405388308838,0.006022133* 1.5405388308838,-0.010520534* 1.5405388308838,0.022220703* 1.5405388308838,-0.069297152* 1.5405388308838,
        0.756880242* 1.5405388308838,-0.069297152* 1.5405388308838,0.022220703* 1.5405388308838,-0.010520534* 1.5405388308838,0.006022133* 1.5405388308838,-0.00384863* 1.5405388308838,0.002638312* 1.5405388308838,-0.001895252* 1.5405388308838,
        0.001404865* 1.5405388308838,-0.001062254* 1.5405388308838,0.000811212* 1.5405388308838,-0.000619343* 1.5405388308838,0.0004668* 1.5405388308838,-0.000340729* 1.5405388308838,0.000232335* 1.5405388308838,-0.000135225* 1.5405388308838,
        4.44017E-05* 1.5405388308838,
};

WWZohCompensation::WWZohCompensation(void)
    : mDelay(COEFF_LENGTH)
{
}

WWZohCompensation::~WWZohCompensation(void)
{
}

double
WWZohCompensation::Convolution(void)
{
    double v = 0.0;
    // FIRフィルター係数が左右対称なので参考文献[3]の方法で乗算回数を半分に削減できる。
    int center = COEFF_LENGTH / 2;
    for (int i = 0; i < center; ++i) {
        v += gCoeffs[i] * (
            mDelay.GetNth(i) +
            mDelay.GetNth(COEFF_LENGTH - i - 1));
    }
    v += gCoeffs[center] * mDelay.GetNth(center);
    return v;
}

void
WWZohCompensation::Filter(int count, const double * inPcm, double *outPcm)
{
    for (long i = 0; i < count; ++i) {
        mDelay.Filter(inPcm[i]);
        outPcm[i] = Convolution();
    }
}

