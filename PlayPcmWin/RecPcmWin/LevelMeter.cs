using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasapi;
using WWAudioFilter;

namespace RecPcmWin {
    class LevelMeter {
        private PeakCalculator [] mPeakCalcArray;
        private WasapiCS.SampleFormatType mSampleFormat;
        private int mCh;

        /// <param name="peakHoldSec">-1のとき∞</param>
        /// <param name="updateIntervalSec">録音バッファー秒数を入れる。</param>
        public LevelMeter(WasapiCS.SampleFormatType sampleFormat, int ch, int peakHoldSec, double updateIntervalSec) {
            mSampleFormat = sampleFormat;
            mCh = ch;

            mPeakCalcArray = new PeakCalculator[ch];
            for (int i=0; i<ch; ++i) {
                mPeakCalcArray[i] = new PeakCalculator((peakHoldSec < 0) ? -1 : (int)(peakHoldSec / updateIntervalSec));
            }
        }

        private double GetSampleValue(byte[] pcm, int pos) {
            double v = 0.0;
            switch (mSampleFormat) {
            case WasapiCS.SampleFormatType.Sdouble:
                v = BitConverter.ToDouble(pcm, pos);
                break;
            case WasapiCS.SampleFormatType.Sfloat:
                v = BitConverter.ToSingle(pcm, pos);
                break;
            case WasapiCS.SampleFormatType.Sint16:
                v = (double)BitConverter.ToInt16(pcm, pos) / 0x8000;
                break;
            case WasapiCS.SampleFormatType.Sint24:
                {
                    int i32 = (int)pcm[pos] + ((int)pcm[pos+1] << 8) + ((int)pcm[pos+2] << 16);
                    i32 <<= 8;
                    v = (double)i32 / 0x80000000L;
                }
                break;
            case WasapiCS.SampleFormatType.Sint32:
            case WasapiCS.SampleFormatType.Sint32V24:
                v = (double)BitConverter.ToInt32(pcm, pos) / 0x80000000L;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            return v;
        }

        public void Update(byte [] pcm) {
            for (int ch=0; ch<mCh; ++ch) {
                mPeakCalcArray[ch].UpdateBegin();
            }

            int sampleBytes = WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / 8;
            int nFrames = pcm.Length / (mCh * sampleBytes);
            int pos = 0;
            for (int n=0; n<nFrames; ++n) {
                for (int ch=0; ch<mCh; ++ch) {
                    double v = GetSampleValue(pcm, pos);
                    mPeakCalcArray[ch].NextSample(v);
                    pos += sampleBytes;
                }
            }

            for (int ch=0; ch<mCh; ++ch) {
                mPeakCalcArray[ch].UpdateEnd();
            }
        }

        public double GetPeakDb(int ch) {
            return mPeakCalcArray[ch].PeakDb;
        }

        public double GetPeakHoldDb(int ch) {
            return mPeakCalcArray[ch].PeakHoldDb;
        }

        public void PeakHoldReset() {
            for (int ch = 0; ch < mCh; ++ch) {
                mPeakCalcArray[ch].PeakHoldReset();
            }
        }
    }
}
