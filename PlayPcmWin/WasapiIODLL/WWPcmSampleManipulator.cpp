// 日本語 UTF-8

#include "WWPcmSampleManipulator.h"
#include <stdio.h>
#include <assert.h>
#include <stdint.h>

void
WWPcmSampleManipulator::UpdateFormat(
        WWPcmDataSampleFormatType format, WWStreamType streamType, int numChannels)
{
    mFormat        = format;
    mStreamType    = streamType;
    mNumChannels   = numChannels;
    mBitsPerSample = WWPcmDataSampleFormatTypeToBitsPerSample(format);
}

bool
WWPcmSampleManipulator::GetFloatSample(
        const unsigned char *buff, int64_t buffBytes, int64_t frameIdx, int ch, float &value_return)
{
    int64_t pos = (frameIdx * mNumChannels + ch) * (mBitsPerSample / 8);
    if (pos < 0 || buffBytes < pos + (mBitsPerSample / 8)) {
        printf("GetFloatSample() frameIdx is out of range\n");
        return false;
    }

    if (WWPcmDataSampleFormatTypeIsFloat(mFormat)) {
        // floating point
        switch (mBitsPerSample) {
        case 32:
            value_return = *((float *)(buff + pos));
            break;
        case 64:
            {
                double v;
                v = *((double *)(buff + pos));
                value_return = (float)v;
            }
            break;
        default:
            assert(0);
            break;
        }
    } else {
        // integer
        switch (mBitsPerSample) {
        case 8:
            {
                unsigned char v;
                v = *(buff+pos);
                value_return = ((float)v - 128.0f) / 128.0f;
            }
            break;
        case 16:
            {
                short v;
                v = *((short *)(buff+pos));
                value_return = ((float)v) / 32768.0f;
            }
            break;
        case 24:
            {
                unsigned int v8  = *(buff+pos);
                unsigned int v16 = *(buff+pos+1);
                unsigned int v24 = *(buff+pos+2);
                int v = (v8 << 8) + (v16 << 16) + (v24 << 24);
                value_return = ((float)v) / 2147483648.0f;
            }
            break;
        case 32:
            {
                unsigned int v0  = *(buff+pos);
                unsigned int v8  = *(buff+pos+1);
                unsigned int v16 = *(buff+pos+2);
                unsigned int v24 = *(buff+pos+3);
                int v = v0 + (v8 << 8) + (v16 << 16) + (v24 << 24);
                value_return = ((float)v) / 2147483648.0f;
            }
            break;
        default:
            assert(0);
            break;
        }
    }

    return true;
}

bool
WWPcmSampleManipulator::SetFloatSample(
        unsigned char *buff, int64_t buffBytes, int64_t frameIdx, int ch, float value)
{
    int64_t pos = (frameIdx * mNumChannels + ch) * (mBitsPerSample / 8);
    if (pos < 0 || buffBytes < pos + (mBitsPerSample / 8)) {
        printf("SetFloatSample() frameIdx is out of range\n");
        return false;
    }

    if (WWPcmDataSampleFormatTypeIsFloat(mFormat)) {
        // floating point
        switch (mBitsPerSample) {
        case 32:
            {
                if (value < -1.0f) {
                    value = -1.0f;
                }
                if (((float)0x7fffff / 0x800000) < value) {
                    value = (float)0x7fffff / 0x800000;
                }

                float *p = (float *)(buff + pos);
                *p = value;
            }
            break;
        case 64:
            {
                if (value < -1.0f) {
                    value = -1.0f;
                }
                if (1.0f < value) {
                    value = 1.0f;
                }

                double *p = (double *)(buff + pos);
                *p = (double)value;
            }
            break;
        default:
            assert(0);
            break;
        }
    } else {
        // integer
        switch (mBitsPerSample) {
        case 8:
            {
                int v = (int)((value + 1.0f) * 256.0f);
                if (v < 0) {
                    v = 0;
                }
                if (255 < v) {
                    v = 255;
                }

                unsigned char *p = buff+pos;
                *p = (unsigned char)(v);
            }
            break;
        case 16:
            {
                int v = (int)(value * 32768.0f);
                if (v < -32768) {
                    v = -32768;
                }
                if (32767 < v) {
                    v = 32767;
                }

                short *p = (short *)(buff+pos);
                *p = (short)v;
            }
            break;
        case 24:
            {
                int64_t v = (int64_t)(value * 2147483648.0f);
                if (v < -2147483648LL) {
                    v = -2147483648LL;
                }
                if (2147483647LL < v) {
                    v = 2147483647LL;
                }

                unsigned char v8  = (v>>8)&0xff;
                unsigned char v16 = (v>>16)&0xff;
                unsigned char v24 = (v>>24)&0xff;
                *(buff+pos)   = v8;
                *(buff+pos+1) = v16;
                *(buff+pos+2) = v24;
            }
            break;
        case 32:
            {
                int64_t v = (int64_t)(value * 2147483648.0f);
                if (v < -2147483648LL) {
                    v = -2147483648LL;
                }
                if (2147483647LL < v) {
                    v = 2147483647LL;
                }

                int *p = (int *)(buff+pos);
                *p = (int)v;
            }
            break;
        default:
            assert(0);
            break;
        }
    }

    return true;
}
