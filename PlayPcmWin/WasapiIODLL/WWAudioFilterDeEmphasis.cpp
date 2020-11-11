// 日本語 UTF-8

#include "WWAudioFilterDeEmphasis.h"

static const float sCoeffs[] = {
    0.00031102739649091732f,
    0.00036885685453166665f,
    0.00057659816229764602f,
    0.00082952248115418167f,
    0.001449970925925961f,
    0.0022387507393955598f,
    0.0039420948394406248f,
    0.0063363475292175873f,
    0.011215231698621361f,
    0.018685854350970088f,
    0.033951734838472802f,
    0.059076192885731141f,
    0.11592376177923121f,
    0.49018811103703697f,
    0.11592376177923121f,
    0.059076192885731141f,
    0.033951734838472802f,
    0.018685854350970088f,
    0.011215231698621361f,
    0.0063363475292175873f,
    0.0039420948394406248f,
    0.0022387507393955598f,
    0.001449970925925961f,
    0.00082952248115418167f,
    0.00057659816229764602f,
    0.00036885685453166665f,
    0.00031102739649091732f,
};

WWAudioFilterDeEmphasis::WWAudioFilterDeEmphasis(void)
    : WWAudioFilterFIR(ARRAYSIZE(sCoeffs), sCoeffs,
        WWAudioFilterFIR::WWAFFC_SYMMETRY | WWAudioFilterFIR::WWAFFC_NOCOPY_COEFFS)
{
}

