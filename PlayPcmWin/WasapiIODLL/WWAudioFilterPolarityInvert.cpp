// 日本語 UTF-8

#include "WWAudioFilterPolarityInvert.h"
#include <assert.h>
#include "WWWasapiIOUtil.h"

WWAudioFilterPolarityInvert::WWAudioFilterPolarityInvert(PCWSTR args)
{
    // mEnableInvertフラグをargsから作る。
    WWCommaSeparatedIdxToFlagArray(args, mEnableInvert, sizeof mEnableInvert);
}

void
WWAudioFilterPolarityInvert::UpdateSampleFormat(
        int sampleRate,
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    (void)sampleRate;
    mManip.UpdateFormat(format, streamType, numChannels);

    assert(mManip.NumChannels() <= WW_CHANNEL_NUM);
}

void
WWAudioFilterPolarityInvert::Filter(unsigned char *buff, int bytes)
{
    if (mManip.StreamType() == WWStreamDop) {
        FilterDoP(buff, bytes);
    } else {
        FilterPcm(buff, bytes);
    }
}

void
WWAudioFilterPolarityInvert::FilterDoP(unsigned char *buff, int bytes)
{
    // DoP DSD。データビットを反転する。
    switch (mManip.SampleFormat()) {
    case WWPcmDataSampleFormatSint24:
        {
            assert((bytes % (3 * mManip.NumChannels())) == 0);

            int ch = 0;
            for (int pos=0; pos<bytes; pos += 3) {
                if (mEnableInvert[ch]) {
                    // b0, b1, marker (makerは反転しない)
                    unsigned char b0 = buff[pos];
                    buff[pos] = ~b0;
                    unsigned char b1 = buff[pos+1];
                    buff[pos+1] = ~b1;
                }

                ++ch;
                if (mManip.NumChannels() <= ch) {
                    ch = 0;
                }
            }
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        {
            assert((bytes % (4 * mManip.NumChannels())) == 0);

            int ch = 0;
            for (int pos=0; pos<bytes; pos += 4) {
                if (mEnableInvert[ch]) {
                    // 0, b0, b1, marker (makerは反転しない)
                    unsigned char b0 = buff[pos+1];
                    buff[pos+1] = ~b0;
                    unsigned char b1 = buff[pos+2];
                    buff[pos+2] = ~b1;
                }

                ++ch;
                if (mManip.NumChannels() <= ch) {
                    ch = 0;
                }
            }
        }
        break;
    default:
        // ここに来ないように上位層でコントロールする。
        assert(0);
        break;
    }
}

void
WWAudioFilterPolarityInvert::FilterPcm1(unsigned char *buff, const int bytes, const int bytesPerSample)
{
    assert((bytes % (bytesPerSample * mManip.NumChannels())) == 0);

    for (int pos=0; pos < bytes;) {
        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            if (mEnableInvert[ch]) {
                for (int i=0; i<bytesPerSample; ++i) {
                    unsigned char b = buff[pos + i];
                    buff[pos + i] = ~b;
                }
            }

            pos += bytesPerSample;
        }
    }
}

void
WWAudioFilterPolarityInvert::FilterPcm(unsigned char *buff, int bytes)
{
    // PCM。データビットを反転する。
    switch (mManip.SampleFormat()) {
    case WWPcmDataSampleFormatSint16:
        FilterPcm1(buff, bytes, 2);
        break;

    case WWPcmDataSampleFormatSint24:
        FilterPcm1(buff, bytes, 3);
        break;

    case WWPcmDataSampleFormatSint32:
        FilterPcm1(buff, bytes, 4);
        break;

    case WWPcmDataSampleFormatSint32V24:
        FilterPcm1(buff, bytes, 4);
        break;

    case WWPcmDataSampleFormatSfloat:
        {
            assert((bytes % (4 * mManip.NumChannels())) == 0);

            float *p = (float *)buff;

            for (int idx=0; idx<bytes/4;) {
                for (int ch=0; ch<mManip.NumChannels(); ++ch) {
                    if (mEnableInvert[ch]) {
                        // 値のSaturate処理はWWAudioFilterSequencerで行う。
                        p[idx+ch] = -p[idx+ch];
                    }
                }
                idx += mManip.NumChannels();
            }
        }
        break;
    default:
        assert(0);
        break;
    }
}

