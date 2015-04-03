using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    struct AudioDataPerChannel {
        public byte[] data;
        public long offsBytes;
        public long totalSamples;
        public int bitsPerSample;
        public bool overflow;
        public double maxMagnitude;

        public void ResetStatistics() {
            overflow = false;
            maxMagnitude = 0.0;
        }

        public double[] GetPcmInDouble(long count) {
            if (totalSamples <= offsBytes / (bitsPerSample / 8) || count <= 0) {
                return new double[count];
            }

            var result = new double[count];
            var copyCount = result.LongLength;
            if (totalSamples < offsBytes / (bitsPerSample / 8) + copyCount) {
                copyCount = totalSamples - offsBytes / (bitsPerSample / 8);
            }

            switch (bitsPerSample) {
            case 16:
            for (long i = 0; i < copyCount; ++i) {
                short v = (short)((data[offsBytes]) + (data[offsBytes + 1] << 8));
                result[i] = v * (1.0 / 32768.0);
                offsBytes += 2;
            }
            break;

            case 24:
            for (long i = 0; i < copyCount; ++i) {
                int v = (int)((data[offsBytes] << 8) + (data[offsBytes + 1] << 16) + (data[offsBytes + 2] << 24));
                result[i] = v * (1.0 / 2147483648.0);
                offsBytes += 3;
            }
            break;
            default:
            System.Diagnostics.Debug.Assert(false);
            break;
            }
            return result;
        }

        public void SetPcmInDouble(double[] pcm, long writeOffs) {
            if (bitsPerSample == 1 && 0 != (writeOffs & 7)) {
                throw new ArgumentException("writeOffs must be multiple of 8");
            }

            var copyCount = pcm.LongLength;
            if (totalSamples < writeOffs + copyCount) {
                copyCount = totalSamples - writeOffs;
            }

            long writePosBytes;
            switch (bitsPerSample) {
            case 1: {
                long readPos = 0;

                // set 1bit data (from LSB to MSB) into 8bit buffer
                writePosBytes = writeOffs / 8;
                for (long i = 0; i < copyCount / 8; ++i) {
                    byte sampleValue = 0;
                    for (int subPos = 0; subPos < 8; ++subPos) {
                        byte bit = (0 <= pcm[readPos]) ? (byte)(1 << subPos) : (byte)0;
                        sampleValue |= bit;

                        ++readPos;
                    }
                    data[writePosBytes] = sampleValue;
                    ++writePosBytes;
                }
            }
            break;
            case 16:
            writePosBytes = writeOffs * 2;
            for (long i = 0; i < copyCount; ++i) {
                short vS = 0;
                double vD = pcm[i];
                if (vD < -1.0f) {
                    vS = -32768;

                    overflow = true;
                    if (maxMagnitude < Math.Abs(vD)) {
                        maxMagnitude = Math.Abs(vD);
                    }
                } else if (1.0f <= vD) {
                    vS = 32767;

                    overflow = true;
                    if (maxMagnitude < Math.Abs(vD)) {
                        maxMagnitude = Math.Abs(vD);
                    }
                } else {
                    vS = (short)(32768.0 * vD);
                }

                data[writePosBytes + 0] = (byte)((vS) & 0xff);
                data[writePosBytes + 1] = (byte)((vS >> 8) & 0xff);

                writePosBytes += 2;
            }
            break;

            case 24:
            writePosBytes = writeOffs * 3;
            for (long i = 0; i < copyCount; ++i) {
                int vI = 0;
                double vD = pcm[i];
                if (vD < -1.0f) {
                    vI = Int32.MinValue;

                    overflow = true;
                    if (maxMagnitude < Math.Abs(vD)) {
                        maxMagnitude = Math.Abs(vD);
                    }
                } else if (1.0f <= vD) {
                    vI = 0x7fffff00;

                    overflow = true;
                    if (maxMagnitude < Math.Abs(vD)) {
                        maxMagnitude = Math.Abs(vD);
                    }
                } else {
                    vI = (int)(2147483648.0 * vD);
                }

                data[writePosBytes + 0] = (byte)((vI >> 8) & 0xff);
                data[writePosBytes + 1] = (byte)((vI >> 16) & 0xff);
                data[writePosBytes + 2] = (byte)((vI >> 24) & 0xff);

                writePosBytes += 3;
            }
            break;
            default:
            System.Diagnostics.Debug.Assert(false);
            break;
            }
        }
    };

    enum FileFormatType {
        FLAC,
        DSF,
    }

    struct AudioData {
        public WWFlacRWCS.Metadata meta;
        public List<AudioDataPerChannel> pcm;
        public byte[] picture;
        public FileFormatType fileFormat;
    };
}
