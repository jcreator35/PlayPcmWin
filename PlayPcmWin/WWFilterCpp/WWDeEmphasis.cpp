// 日本語

/* Linear-phase FIR De-emphasis filter for old Pre-emphasis CDs
 *
 * References:
 * [1] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
 */

#include "WWDeEmphasis.h"
#include <Windows.h> //< ARRAYSIZE

static const double sCoeffs[] = {
    0.00031102739649091732,
    0.00036885685453166665,
    0.00057659816229764602,
    0.00082952248115418167,
    0.001449970925925961,
    0.0022387507393955598,
    0.0039420948394406248,
    0.0063363475292175873,
    0.011215231698621361,
    0.018685854350970088,
    0.033951734838472802,
    0.059076192885731141,
    0.11592376177923121,
    0.49018811103703697,
    0.11592376177923121,
    0.059076192885731141,
    0.033951734838472802,
    0.018685854350970088,
    0.011215231698621361,
    0.0063363475292175873,
    0.0039420948394406248,
    0.0022387507393955598,
    0.001449970925925961,
    0.00082952248115418167,
    0.00057659816229764602,
    0.00036885685453166665,
    0.00031102739649091732,
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
