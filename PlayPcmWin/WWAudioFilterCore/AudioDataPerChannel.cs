using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcmDataLib;

namespace WWAudioFilterCore {
    public struct AudioDataPerChannel {
        public WWUtil.LargeArray<byte> mData;
        public long mOffsBytes;

        /// <summary>
        /// これは、サンプル数。1bitデータのとき、mData.LongLength / 8 = mTotalSamples
        /// </summary>
        public long mTotalSamples;

        /// <summary>
        /// 量子化ビット数。1, 16,24,32 (signed int or float)
        /// </summary>
        public int mBitsPerSample;

        public PcmData.ValueRepresentationType mValueRepresentationType;

        public bool mOverflow;
        public double mMaxMagnitude;

        public enum DataFormat {
            Pcm,
            Sdm1bit,
        };

        public DataFormat mDataFormat;

        public void ResetStatistics() {
            mOverflow = false;
            mMaxMagnitude = 0.0;
        }

        public WWUtil.LargeArray<double> GetPcmInDouble(long longCount) {
            switch (mDataFormat) {
            case DataFormat.Pcm:
                return GetPcmInDoublePcm(longCount);
            case DataFormat.Sdm1bit:
                return GetPcmInDoubleSdm1bit(longCount);
            default:
                System.Diagnostics.Debug.Assert(false);
                return null;
            }
        }

        public double[] GetPcmInDoubleSdm1bit(int count) {
            // サンプル数 / 8 == バイト数。

            System.Diagnostics.Debug.Assert(count % 8 == 0);
            System.Diagnostics.Debug.Assert(count <= WWUtil.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM);

            var result = new double[count];

            int writePos = 0;

            if (mTotalSamples / 8 <= mOffsBytes || count == 0) {
                // 完全に範囲外の時。
            } else {
                int copySamples = count;
                if (mTotalSamples < mOffsBytes * 8 + copySamples) {
                    copySamples = (int)(mTotalSamples - mOffsBytes * 8);
                }

                var bitBuff = new byte[copySamples / 8];
                mData.CopyTo(mOffsBytes, ref bitBuff, 0, copySamples / 8);

                for (long i = 0; i < copySamples / 8; ++i) {
                    byte b = bitBuff[i];

                    // 1バイト内のビットの並びはMSBから古い順にデータが詰まっている。
                    for (int bit = 7; 0<=bit; --bit) {
                        result[writePos++] = ((b >> bit) & 1) == 1 ? 1.0 : -1.0;
                    }
                }

                mOffsBytes += copySamples / 8;

            }

            while (writePos < count) {
                byte b = 0x69; // DSD silence

                // 1バイト内のビットの並びはMSBから古い順にデータが詰まっている。
                for (int bit = 7; 0 <= bit; --bit) {
                    result[writePos++] = ((b >> bit) & 1) == 1 ? 1.0 : -1.0;
                }
            }

            return result;

        }

        public WWUtil.LargeArray<double> GetPcmInDoubleSdm1bit(long longCount) {
            // サンプル数 / 8 == バイト数。
            var result = new WWUtil.LargeArray<double>(longCount);

            if (mTotalSamples / 8 <= mOffsBytes || longCount == 0) {
                // 完全に範囲外の時。
                int writePos = 0;
                for (long i = 0; i < (longCount+7)/8; ++i) {
                    byte b = 0x69; // DSD silence

                    // 1バイト内のビットの並びはMSBから古い順にデータが詰まっている。
                    for (int bit = 7; 0 <= bit; --bit) {
                        if (longCount <= writePos) {
                            break;
                        }
                        result.Set(writePos++, ((b >> bit) & 1) == 1 ? 1.0 : -1.0);
                    }
                }

                return result;
            } else {
                long copySamples = longCount;
                if (mTotalSamples < mOffsBytes * 8 + copySamples) {
                    copySamples = mTotalSamples - mOffsBytes * 8;
                }

                long toPos = 0;
                for (long remain = copySamples; 0 < remain;) {
                    int fragmentCount = WWUtil.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM;
                    if (remain < fragmentCount) {
                        fragmentCount = (int)remain;
                    }

                    var fragment = GetPcmInDoubleSdm1bit(fragmentCount);
                    result.CopyFrom(fragment, 0, toPos, fragmentCount);
                    fragment = null;

                    toPos  += fragmentCount;
                    remain -= fragmentCount;
                }

                return result;
            }
        }
        
        /// <summary>
        /// double[]を戻すバージョン。
        /// </summary>
        /// <param name="count">取得する要素数。範囲外の領域は0が入る。</param>
        /// <returns></returns>
        public double[] GetPcmInDoublePcm(int count) {
            // 確保するサイズはcount個。
            if (mTotalSamples <= mOffsBytes / (mBitsPerSample / 8) || count == 0) {
                // 完全に範囲外。
                return new double[count];
            }

            var result = new double[count];

            // コピーするデータの個数はcount個よりも少ないことがある。
            int copyCount = result.Length;
            if (mTotalSamples < mOffsBytes / (mBitsPerSample / 8) + copyCount) {
                copyCount = (int)(mTotalSamples - mOffsBytes / (mBitsPerSample / 8));
            }

            switch (mBitsPerSample) {
            case 16:
                for (int i = 0; i < copyCount; ++i) {
                    short v = (short)((mData.At(mOffsBytes)) + (mData.At(mOffsBytes + 1) << 8));
                    result[i] = v * (1.0 / 32768.0);
                    mOffsBytes += 2;
                }
                break;
            case 24:
                for (int i = 0; i < copyCount; ++i) {
                    int v = (int)((mData.At(mOffsBytes) << 8) + (mData.At(mOffsBytes + 1) << 16)
                        + (mData.At(mOffsBytes + 2) << 24));
                    result[i] = v * (1.0 / 2147483648.0);
                    mOffsBytes += 3;
                }
                break;
            case 32:
                switch (mValueRepresentationType) {
                case PcmData.ValueRepresentationType.SInt:
                    for (int i = 0; i < copyCount; ++i) {
                        int v = (int)(
                              (mData.At(mOffsBytes + 0) << 0)
                            + (mData.At(mOffsBytes + 1) << 8)
                            + (mData.At(mOffsBytes + 2) << 16)
                            + (mData.At(mOffsBytes + 3) << 24));
                        result[i] = v * (1.0 / 2147483648.0);
                        mOffsBytes += 4;
                    }
                    break;
                case PcmData.ValueRepresentationType.SFloat:
                    break;
                    for (int i = 0; i < copyCount; ++i) {
                        byte [] buff = new byte[4];
                        buff[0] = mData.At(mOffsBytes + 0);
                        buff[1] = mData.At(mOffsBytes + 1);
                        buff[2] = mData.At(mOffsBytes + 2);
                        buff[3] = mData.At(mOffsBytes + 3);
                        float v = BitConverter.ToSingle(buff, 0);
                        result[i] = v;
                        mOffsBytes += 4;
                    }
                    break;
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            return result;
        }

        /// <summary>
        /// double型のLargeArrayを戻すバージョン。
        /// </summary>
        /// <param name="longCount">取得する要素数。範囲外の領域は0が入る。</param>
        /// <returns></returns>
        public WWUtil.LargeArray<double> GetPcmInDoublePcm(long longCount) {
            // 確保するサイズはlongCount個。
            var result = new WWUtil.LargeArray<double>(longCount);

            long fromPos = mOffsBytes / (mBitsPerSample / 8);
            long toPos = 0;
            
            // コピーするデータの個数はcount個よりも少ないことがある。
            long copyCount = result.LongLength;
            if (mTotalSamples < fromPos + copyCount) {
                copyCount = (int)(mTotalSamples - fromPos);
            }

            for (long remain = copyCount; 0 < remain; ) {
                int fragmentCount = WWUtil.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = (int)remain;
                }

                var fragment = GetPcmInDoublePcm(fragmentCount);
                result.CopyFrom(fragment, 0, toPos, fragmentCount);
                fragment = null;

                toPos   += fragmentCount;
                remain  -= fragmentCount;
            }

            return result;
        }

        /// <param name="pcm">コピー元データ。先頭から全要素コピーする。</param>
        /// <param name="writePos">コピー先(この配列)先頭要素数。</param>
        public void SetPcmInDouble(double[] pcm, long writePos) {
            if (mBitsPerSample == 1 && 0 != (writePos & 7)) {
                throw new ArgumentException("writeOffs must be multiple of 8");
            }

            int copyCount = pcm.Length;
            if (mTotalSamples < writePos + copyCount) {
                copyCount = (int)(mTotalSamples - writePos);
            }

            long writePosBytes;
            switch (mBitsPerSample) {
            case 1: {
                    long readPos = 0;

                    // set 1bit data (from LSB to MSB) into 8bit buffer
                    writePosBytes = writePos / 8;
                    for (long i = 0; i < copyCount / 8; ++i) {
                        byte sampleValue = 0;
                        for (int subPos = 0; subPos < 8; ++subPos) {
                            byte bit = (0 <= pcm[readPos]) ? (byte)(1 << subPos) : (byte)0;
                            sampleValue |= bit;

                            ++readPos;
                        }
                        mData.Set(writePosBytes, sampleValue);
                        ++writePosBytes;
                    }
                }
                break;
            case 16:
                writePosBytes = writePos * 2;
                for (long i = 0; i < copyCount; ++i) {
                    short vS = 0;
                    double vD = pcm[i];
                    if (vD < -1.0f) {
                        vS = -32768;

                        mOverflow = true;
                        if (mMaxMagnitude < Math.Abs(vD)) {
                            mMaxMagnitude = Math.Abs(vD);
                        }
                    } else if (1.0f <= vD) {
                        vS = 32767;

                        mOverflow = true;
                        if (mMaxMagnitude < Math.Abs(vD)) {
                            mMaxMagnitude = Math.Abs(vD);
                        }
                    } else {
                        vS = (short)(32768.0 * vD);
                    }

                    mData.Set(writePosBytes + 0, (byte)((vS) & 0xff));
                    mData.Set(writePosBytes + 1, (byte)((vS >> 8) & 0xff));

                    writePosBytes += 2;
                }
                break;
            case 24:
                writePosBytes = writePos * 3;
                for (long i = 0; i < copyCount; ++i) {
                    int vI = 0;
                    double vD = pcm[i];
                    if (vD < -1.0f) {
                        vI = Int32.MinValue;

                        mOverflow = true;
                        if (mMaxMagnitude < Math.Abs(vD)) {
                            mMaxMagnitude = Math.Abs(vD);
                        }
                    } else if (1.0f <= vD) {
                        vI = 0x7fffff00;

                        mOverflow = true;
                        if (mMaxMagnitude < Math.Abs(vD)) {
                            mMaxMagnitude = Math.Abs(vD);
                        }
                    } else {
                        vI = (int)(2147483648.0 * vD);
                    }

                    mData.Set(writePosBytes + 0, (byte)((vI >> 8) & 0xff));
                    mData.Set(writePosBytes + 1, (byte)((vI >> 16) & 0xff));
                    mData.Set(writePosBytes + 2, (byte)((vI >> 24) & 0xff));

                    writePosBytes += 3;
                }
                break;
            case 32:
                switch (mValueRepresentationType) {
                case PcmData.ValueRepresentationType.SInt:
                    writePosBytes = writePos * 4;
                    for (long i = 0; i < copyCount; ++i) {
                        int vI = 0;
                        double vD = pcm[i];
                        if (vD < -1.0f) {
                            vI = Int32.MinValue;

                            mOverflow = true;
                            if (mMaxMagnitude < Math.Abs(vD)) {
                                mMaxMagnitude = Math.Abs(vD);
                            }
                        } else if (1.0f <= vD) {
                            vI = 0x7fffffff;

                            mOverflow = true;
                            if (mMaxMagnitude < Math.Abs(vD)) {
                                mMaxMagnitude = Math.Abs(vD);
                            }
                        } else {
                            vI = (int)(2147483648.0 * vD);
                        }

                        mData.Set(writePosBytes + 0, (byte)((vI >> 0) & 0xff));
                        mData.Set(writePosBytes + 1, (byte)((vI >> 8) & 0xff));
                        mData.Set(writePosBytes + 2, (byte)((vI >> 16) & 0xff));
                        mData.Set(writePosBytes + 3, (byte)((vI >> 24) & 0xff));

                        writePosBytes += 4;
                    }
                    break;
                case PcmData.ValueRepresentationType.SFloat:
                    break;
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        /// <summary>
        /// double型のLargeArrayを入力するバージョン。
        /// </summary>
        public void SetPcmInDouble(WWUtil.LargeArray<double> pcm, long toPos) {
            long fromPos = 0;

            long copyCount = pcm.LongLength;
            if (mTotalSamples < toPos + pcm.LongLength) {
                copyCount = mTotalSamples - toPos;
            }

            for (long remain = copyCount; 0 < remain; ) {
                int fragmentCount = WWUtil.LargeArray<byte>.ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = (int)(remain);
                }

                var fragment = new double[fragmentCount];
                pcm.CopyTo(fromPos, ref fragment, 0, fragmentCount);
                SetPcmInDouble(fragment, toPos);
                fragment = null;

                fromPos += fragmentCount;
                toPos   += fragmentCount;
                remain  -= fragmentCount;
            }
        }
    };
}
