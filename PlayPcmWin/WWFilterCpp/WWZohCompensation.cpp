// 日本語

/* NOSDAC high frequency roll-off compensation filter
 * 
 * NOSDAC frequency response (continuous-time): Hr_zoh(jΩ)=sinc(jΩ/2)
 * Compensation filter frequency response (discretE-time): Hr_comp(ω) = 1 / sinc(ω/2)
 * 
 * FIR filter coefficients calculated by Frequency-Sampling design method:
 * filter taps=M, M is odd
 * G(k) = Hr_comp(2πk/M) * (-1)^k, G(k)=-G(M-k)
 * U=(M-1)/2
 * h(n) = (1/M)*{G(0)+2*Σ_{k=1}^{U}{G(k)*cos{2πk*(n+0.5)/M}}
 * 
 * References:
 * [1] A. V. Oppenheim, R. W. Schafer, DiscretE-Time Signal Processing, 3rd Ed, Prentice Hall, 2009, pp. 600-604
 * [2] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
 */

#include "WWZohCompensation.h"
#include <Windows.h> //< ARRAYSIZE

#define K  1.3909672824792

static const double gCoeffs[] = {
        4.46886145464570880E-05  * K,
        -9.20001740228207980E-05 * K,
        1.86758611740503280E-04  * K,
        -2.49328146244324370E-04 * K,
        3.63396389226319080E-04  * K,
        -4.63710851526088670E-04 * K,
        6.22168807820932200E-04  * K,
        -8.06524194476629980E-04 * K,
        1.06714704041711480E-03  * K,
        -1.44810865693034960E-03 * K,
        1.99002976828642050E-03  * K,
        -2.96307235513420830E-03 * K,
        4.53530224066369790E-03  * K,
        -8.17731066323979830E-03 * K,
        1.68891595012301450E-02  * K,
        -5.61516276812759900E-02 * K,
        8.07899332606445640E-01  * K,
        -5.61516276812739910E-02 * K,
        1.68891595012289790E-02  * K,
        -8.17731066323960570E-03 * K,
        4.53530224066378550E-03  * K,
        -2.96307235513304990E-03 * K,
        1.99002976828551240E-03  * K,
        -1.44810865692879460E-03 * K,
        1.06714704041607680E-03  * K,
        -8.06524194476219720E-04 * K,
        6.22168807821357100E-04  * K,
        -4.63710851526716370E-04 * K,
        3.63396389225778280E-04  * K,
        -2.49328146243508350E-04 * K,
        1.86758611740244940E-04  * K,
        -9.20001740224864170E-05 * K,
        4.46886145460032070E-05  * K
};

WWZohCompensation::WWZohCompensation(void)
    : mFIRFilter(ARRAYSIZE(gCoeffs), gCoeffs, WWFIRFilter::WWFIRFF_SYMMETRY | WWFIRFilter::WWFIRFF_NOCOPY_COEFFS)
{
}

WWZohCompensation::~WWZohCompensation(void)
{
}

void
WWZohCompensation::Filter(int count, const double * inPcm, double *outPcm)
{
    mFIRFilter.Filter(count, inPcm, outPcm);
}

