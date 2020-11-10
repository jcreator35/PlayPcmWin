// 日本語

/* Linear-phase FIR De-emphasis filter for old Pre-emphasis CDs
 *
 * References:
 * [1] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
 */

#include "WWDeEmphasis.h"
#include <Windows.h> //< ARRAYSIZE

static const double sCoeffs[] = {
    0.000878299535988309,
    0.000733540734613226,
    0.00130595055284722,
    0.000891583668843791,
    0.00227437129629634,
    0.00175097216120622,
    0.00468567690109955,
    0.00494183233570261,
    0.0116219963372452,
    0.0178251532752356,
    0.0348059183741284,
    0.0579463495762199,
    0.116528858324647,
    0.487618993851852,
    0.116528858324647,
    0.0579463495762199,
    0.0348059183741284,
    0.0178251532752356,
    0.0116219963372452,
    0.00494183233570261,
    0.00468567690109955,
    0.00175097216120622,
    0.00227437129629634,
    0.000891583668843791,
    0.00130595055284722,
    0.000733540734613226,
    0.000878299535988309,
};

WWDeEmphasis::WWDeEmphasis(void)
    : mFIRFilter(ARRAYSIZE(sCoeffs), sCoeffs, WWFIRFilter::WWFIRFF_SYMMETRY | WWFIRFilter::WWFIRFF_NOCOPY_COEFFS)
{
}

WWDeEmphasis::~WWDeEmphasis(void)
{
}

void
WWDeEmphasis::Filter(int count, const double * inPcm, double *outPcm)
{
    mFIRFilter.Filter(count, inPcm, outPcm);
}
