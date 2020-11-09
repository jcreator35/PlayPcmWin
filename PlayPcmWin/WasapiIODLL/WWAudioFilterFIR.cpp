// 日本語 UTF-8

/* References:
 * [1] Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 702
 */

#include "WWAudioFilterFIR.h"
#include "WWPcmDelay.h"
#include <assert.h>


float
WWAudioFilterFIR::Convolution(int ch)
{
    if (mFlags & WWAFFC_SYMMETRY) {
        // フィルター係数がsymmetricなので、[1]の高速化(乗算回数削減)が使える。
        float v = 0.0f;

        const int center = mTaps/2;
        for (int i = 0; i < center; ++i) {
            v += mCoeffs[i] * (
                mDelays[ch].GetNthDelayedSampleValue(i) +
                mDelays[ch].GetNthDelayedSampleValue(mTaps - i -1));
        }
        v += mCoeffs[center] * mDelays[ch].GetNthDelayedSampleValue(center);
        return v;
    } else {
        // 順番に掛けて足す。
        float v = 0.0f;

        for (int i = 0; i < mTaps; ++i) {
            v += mCoeffs[i] * mDelays[ch].GetNthDelayedSampleValue(i);
        }

        return v;
    }
}

void
WWAudioFilterFIR::UpdateSampleFormat(
        int sampleRate,
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    (void)sampleRate;
    mManip.UpdateFormat(format, streamType, numChannels);

    // mDelayを作り直す。
    delete[] mDelays;

    mDelays = new WWPcmDelay[numChannels];
    for (int i=0; i<numChannels; ++i) {
        mDelays[i].Init(mTaps);
    }
}

void
WWAudioFilterFIR::Filter(unsigned char *buff, int bytes)
{
    if (mManip.StreamType() == WWStreamDop) {
        // 対応していない
    } else {
        FilterPcm(buff, bytes);
    }
}

void
WWAudioFilterFIR::FilterPcm(unsigned char *buff, int bytes)
{
    int nFrames = bytes / (mManip.NumChannels() * mManip.BitsPerSample() / 8);

    for (int i=0; i<nFrames; ++i) {
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            float v = 0.0f;
            mManip.GetFloatSample(buff, bytes, i, ch, v);

            mDelays[ch].Filter(v);

            float result = Convolution(ch);

            mManip.SetFloatSample(buff, bytes, i, ch, result);
        }
    }
}

