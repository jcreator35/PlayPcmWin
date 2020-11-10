// 日本語 UTF-8

/* NOSDAC high frequency roll-off compensation filter
 * 
 * [1] NOSDAC frequency response (continuous-time): Hr_zoh(jΩ)=sinc(jΩ/2)
 * Compensation filter frequency response (discrete-time): Hr_comp(ω) = 1 / sinc(ω/2)
 * 
 * [2] FIR filter coefficients calculated by Frequency-Sampling design method:
 * filter taps=M, M is odd
 * G(k) = Hr_comp(2πk/M) * (-1)^k, G(k)=-G(M-k)
 * U=(M-1)/2
 * h(n) = (1/M)*{G(0)+2*Σ_{k=1}^{U}{G(k)*cos{2πk*(n+0.5)/M}}
 * 
 * References:
 * [1] A. V. Oppenheim, R. W. Schafer, Discrete-Time Signal Processing, 3rd Ed., Prentice Hall, 2009, pp. 600-604
 * [2] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th ed., 2007, Chapter 10, pp. 671-678
 */

#include "WWAudioFilterZohNosdacCompensation.h"

static const float sCoeffs[] = {
    4.44017E-05f, -0.000135225f, 0.000232335f,-0.000340729f,0.0004668f,-0.000619343f,0.000811212f,-0.001062254f,
    0.001404865f,-0.001895252f,0.002638312f,-0.00384863f,0.006022133f,-0.010520534f,0.022220703f,-0.069297152f,
    0.756880242f,-0.069297152f,0.022220703f,-0.010520534f,0.006022133f,-0.00384863f,0.002638312f,-0.001895252f,
    0.001404865f,-0.001062254f,0.000811212f,-0.000619343f,0.0004668f,-0.000340729f,0.000232335f,-0.000135225f,4.44017E-05f};

WWAudioFilterZohNosdacCompensation::WWAudioFilterZohNosdacCompensation(void)
    : WWAudioFilterFIR(ARRAYSIZE(sCoeffs), sCoeffs,
        WWAudioFilterFIR::WWAFFC_SYMMETRY | WWAudioFilterFIR::WWAFFC_NOCOPY_COEFFS)
{
}

