// 日本語 UTF-8

#include "WWAudioFilterPolarityInvert.h"
#include <assert.h>

void
WWAudioFilterPolarityInvert::UpdateSampleFormat(
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    mManip.UpdateFormat(format, streamType, numChannels);
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
        // b0 b1 marker
        for (int pos=0; pos<bytes; pos += 3) {
            unsigned char b0 = buff[pos];
            buff[pos] = ~b0;
            unsigned char b1 = buff[pos+1];
            buff[pos+1] = ~b1;
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        // 0 b0 b1 marker
        for (int pos=0; pos<bytes; pos += 4) {
            unsigned char b0 = buff[pos+1];
            buff[pos+1] = ~b0;
            unsigned char b1 = buff[pos+2];
            buff[pos+2] = ~b1;
        }
        break;
    default:
        // ここに来ないように上位層でコントロールする。
        assert(0);
        break;
    }
}

void
WWAudioFilterPolarityInvert::FilterPcm(unsigned char *buff, int bytes)
{
    // PCM。データビットを反転する。
    switch (mManip.SampleFormat()) {
    case WWPcmDataSampleFormatSint16:
    case WWPcmDataSampleFormatSint24:
    case WWPcmDataSampleFormatSint32:
        for (int pos=0; pos<bytes; ++pos) {
            unsigned char b = buff[pos];
            buff[pos] = ~b;
        }
        break;
    case WWPcmDataSampleFormatSint32V24:
        for (int pos=0; pos<bytes; pos +=4) {
            unsigned char b0 = buff[pos+1];
            buff[pos+1] = ~b0;
            unsigned char b1 = buff[pos+2];
            buff[pos+2] = ~b1;
            unsigned char b2 = buff[pos+3];
            buff[pos+3] = ~b2;
        }
        break;
    case WWPcmDataSampleFormatSfloat:
        {
            float *p = (float *)buff;
            for (int idx=0; idx<bytes/4; ++idx) {
                // 値のSaturate処理はWWAudioFilterSequencerで行う。
                p[idx] = -p[idx];
            }
        }
        break;
    default:
        assert(0);
        break;
    }
}

