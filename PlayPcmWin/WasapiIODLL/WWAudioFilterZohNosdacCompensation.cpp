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
 * Reference:
 * [1] A. V. Oppenheim, R. W. Schafer, Discrete-Time Signal Processing, 3rd Ed., Prentice Hall, 2009, pp. 600-604
 * [2] J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th ed., 2007, Chapter 10, pp. 671-678
 * [3] Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 702
 */

#include "WWAudioFilterZohNosdacCompensation.h"
#include "WWPcmDelay.h"
#include <assert.h>

static const int sCoeffLength = 33;
static const float sCoeffs33[] = {
    4.44017E-05f, -0.000135225f, 0.000232335f,-0.000340729f,0.0004668f,-0.000619343f,0.000811212f,-0.001062254f,
    0.001404865f,-0.001895252f,0.002638312f,-0.00384863f,0.006022133f,-0.010520534f,0.022220703f,-0.069297152f,
    0.756880242f,-0.069297152f,0.022220703f,-0.010520534f,0.006022133f,-0.00384863f,0.002638312f,-0.001895252f,
    0.001404865f,-0.001062254f,0.000811212f,-0.000619343f,0.0004668f,-0.000340729f,0.000232335f,-0.000135225f,4.44017E-05f};

float
WWAudioFilterZohNosdacCompensation::Convolution(int ch)
{
    float v = 0.0f;
#if 0
    for (int i = 0; i < sCoeffLength; ++i) {
        v += sCoeffs33[i] * mDelay[ch].GetNthDelayedSampleValue(i);
    }
#else
    // フィルター係数がsymmetricなので、[3]の高速化(乗算回数削減)が使える。
    for (int i = 0; i < sCoeffLength/2; ++i) {
        v += sCoeffs33[i] * (
            mDelay[ch].GetNthDelayedSampleValue(i) +
            mDelay[ch].GetNthDelayedSampleValue(sCoeffLength - i -1));
    }

    const int center = sCoeffLength/2+1;
    v += sCoeffs33[center] * mDelay[ch].GetNthDelayedSampleValue(center);
#endif
    return v;
}

WWAudioFilterZohNosdacCompensation::~WWAudioFilterZohNosdacCompensation(void)
{
    delete[] mDelay;
    mDelay = nullptr;
}

void
WWAudioFilterZohNosdacCompensation::UpdateSampleFormat(
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    mManip.UpdateFormat(format, streamType, numChannels);

    // mDelayを作り直す。
    delete[] mDelay;

    mDelay = new WWPcmDelay[numChannels];
    for (int i=0; i<numChannels; ++i) {
        mDelay[i].Init(sCoeffLength);
    }
}

void
WWAudioFilterZohNosdacCompensation::Filter(unsigned char *buff, int bytes)
{
    if (mManip.StreamType() == WWStreamDop) {
        // 対応していない
    } else {
        FilterPcm(buff, bytes);
    }
}

void
WWAudioFilterZohNosdacCompensation::FilterPcm(unsigned char *buff, int bytes)
{
    int nFrames = bytes / (mManip.NumChannels() * mManip.BitsPerSample() / 8);

    for (int i=0; i<nFrames; ++i) {
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            float v = 0.0f;
            mManip.GetFloatSample(buff, bytes, i, ch, v);

            mDelay[ch].Filter(v);

            float result = Convolution(ch);

            mManip.SetFloatSample(buff, bytes, i, ch, result);
        }
    }
}

