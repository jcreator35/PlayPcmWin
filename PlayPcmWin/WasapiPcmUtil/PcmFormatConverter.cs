using System;
using PcmDataLib;
using Wasapi;
using System.Threading;
using WWUtil;

namespace WasapiPcmUtil {
    public enum NoiseShapingType {
        None,
        AddDither,
        NoiseShaping1stOrder,
        DitheredNoiseShaping1stOrder,
    };

    public class PcmFormatConverter {

        public class BitsPerSampleConvArgs {
            /// <summary>
            /// perform noise shaping if applicable
            /// </summary>
            public NoiseShapingType noiseShaping;

            /// <summary>
            /// [out] true if noise shaping is performed
            /// </summary>
            public bool noiseShapingOrDitherPerformed;

            public BitsPerSampleConvArgs(NoiseShapingType noiseShaping) {
                this.noiseShaping = noiseShaping;
                noiseShapingOrDitherPerformed = false;
            }
        };

        private int [] mErr;
        private Random mRand;

        private PcmFormatConverter() {
        }

        public PcmFormatConverter(int numChannels) {
            mRand = new Random();
            mErr = new int[numChannels];

            var convertFromI16 = new ConvertDelegate[] {
                new ConvertDelegate(ConvClone),
                new ConvertDelegate(ConvI16toI24),
                new ConvertDelegate(ConvI16toI32),
                new ConvertDelegate(ConvI16toI32),
                new ConvertDelegate(ConvI16toF32),
                new ConvertDelegate(ConvI16toF64)};

            var convertFromI24 = new ConvertDelegate[] {
                new ConvertDelegate(ConvI24or32toI16),
                new ConvertDelegate(ConvClone),
                new ConvertDelegate(ConvI24toI32),
                new ConvertDelegate(ConvI24toI32),
                new ConvertDelegate(ConvI24toF32),
                new ConvertDelegate(ConvI24toF64)};

            var convertFromI32V24 = new ConvertDelegate[] {
                new ConvertDelegate(ConvI24or32toI16),
                new ConvertDelegate(ConvI32V24toI24),
                new ConvertDelegate(ConvClone),
                new ConvertDelegate(ConvI32V24toI32),
                new ConvertDelegate(ConvI32toF32),
                new ConvertDelegate(ConvI32toF64)};

            var convertFromI32 = new ConvertDelegate[] {
                new ConvertDelegate(ConvI24or32toI16),
                new ConvertDelegate(ConvI32toI24orI32V24),
                new ConvertDelegate(ConvI32toI24orI32V24),
                new ConvertDelegate(ConvClone),
                new ConvertDelegate(ConvI32toF32),
                new ConvertDelegate(ConvI32toF64)};

            var convertFromF32 = new ConvertDelegate[] {
                new ConvertDelegate(ConvF32toI16),
                new ConvertDelegate(ConvF32toI24orI32V24),
                new ConvertDelegate(ConvF32toI24orI32V24),
                new ConvertDelegate(ConvF32toI32),
                new ConvertDelegate(ConvClone),
                new ConvertDelegate(ConvF32toF64)};

            var convertFromF64 = new ConvertDelegate[] {
                new ConvertDelegate(ConvF64toI16),
                new ConvertDelegate(ConvF64toI24orI32V24),
                new ConvertDelegate(ConvF64toI24orI32V24),
                new ConvertDelegate(ConvF64toI32),
                new ConvertDelegate(ConvF64toF32),
                new ConvertDelegate(ConvClone)};

            mConvert = new ConvertDelegate[][] {
                    convertFromI16,
                    convertFromI24,
                    convertFromI32V24,
                    convertFromI32,
                    convertFromF32,
                    convertFromF64};
        }

        private bool [][] mNoiseShapingOrDitherCapabilityTable = new bool[][] {
            //        to I16    I24    I32v24 I32    F32    F64
            new bool [] {false, false, false, false, false, false}, // fromI16
            new bool [] {true,  false, false, false, false, false}, // fromI24
            new bool [] {true,  false, false, false, false, false}, // fromI32V24
            new bool [] {true,  true,  true,  false, false, false}, // fromI32
            new bool [] {true,  false, false, false, false, false}, // fromF32
            new bool [] {false, false, false, false, false, false}, // fromF64
        };

        public bool IsConversionNoiseshapingOrDitherCapable(WasapiCS.SampleFormatType fromFormat, WasapiCS.SampleFormatType toFormat) {
            System.Diagnostics.Debug.Assert(0 <= (int)fromFormat && (int)fromFormat <= (int)WasapiCS.SampleFormatType.Sdouble);
            System.Diagnostics.Debug.Assert(0 <= (int)toFormat && (int)toFormat <= (int)WasapiCS.SampleFormatType.Sdouble);
            if (fromFormat == WasapiCS.SampleFormatType.Unknown ||
                    toFormat == WasapiCS.SampleFormatType.Unknown) {
                return false;
            }

            return mNoiseShapingOrDitherCapabilityTable[(int)fromFormat][(int)toFormat];
        }

        /// <summary>
        /// Converts sample format to toFormat and returns new instance of PcmData.
        /// pcmFrom is not changed.
        /// </summary>
        /// <param name="toFormat">sample format to convert</param>
        /// <returns>Newly instanciated PcmData</returns>
        public PcmData Convert(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            if (args == null) {
                args = new BitsPerSampleConvArgs(NoiseShapingType.None);
            }

            var fromFormat = WasapiCS.BitAndFormatToSampleFormatType(pcmFrom.BitsPerSample, pcmFrom.ValidBitsPerSample,
                    SampleFormatInfo.VrtToBft(pcmFrom.SampleValueRepresentationType));

            if (fromFormat == WasapiCS.SampleFormatType.Unknown ||
                    toFormat == WasapiCS.SampleFormatType.Unknown) {
                return null;
            }

            long toBytes = pcmFrom.NumFrames * pcmFrom.NumChannels * WasapiCS.SampleFormatTypeToUseBitsPerSample(toFormat) / 8;
            var newSampleArray = new LargeArray<byte>(toBytes);

            {
                var dataFragment = new byte[0x1000000];

                var pcmTemp = new PcmData();
                pcmTemp.CopyFrom(pcmFrom);

                long writePos = 0;
                for (long readPos = 0; readPos < pcmFrom.GetSampleLargeArray().LongLength; readPos += dataFragment.Length) {
                    long count = dataFragment.Length;
                    if (pcmFrom.GetSampleLargeArray().LongLength < readPos + count) {
                        count = pcmFrom.GetSampleLargeArray().LongLength - readPos;
                    }
                    pcmTemp.SetSampleLargeArray(count, new LargeArray<byte>(dataFragment));

                    var toFragment = mConvert[(int)fromFormat][(int)toFormat](pcmTemp, toFormat, args);
                    newSampleArray.CopyFrom(toFragment, 0, writePos, (int)count);
                    writePos += toFragment.Length;
                }
            }

            PcmData newPcmData = new PcmData();
            newPcmData.CopyHeaderInfoFrom(pcmFrom);
            newPcmData.SetFormat(pcmFrom.NumChannels,
                    WasapiCS.SampleFormatTypeToUseBitsPerSample(toFormat),
                    WasapiCS.SampleFormatTypeToValidBitsPerSample(toFormat), pcmFrom.SampleRate,
                    SampleFormatInfo.BftToVrt(WasapiCS.SampleFormatTypeToBitFormatType(toFormat)), pcmFrom.NumFrames);
            newPcmData.SetSampleLargeArray(newSampleArray);

            return newPcmData;
        }

        private delegate byte[] ConvertDelegate(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args);

        private ConvertDelegate[][] mConvert;

        ////////////////////////////////////////////////////////////////////
        // Clipped sample counter used for float to int conversion

        private static long mClippedCount;

        private static void IncrementClippedCounter() {
            Interlocked.Increment(ref mClippedCount);
        }

        /// <summary>
        /// clear clipped sample counter. call before conv start
        /// </summary>
        public static void ClearClippedCounter() {
            mClippedCount = 0;
        }

        /// <summary>
        /// read latest clipped sample counter value
        /// </summary>
        /// <returns>clipped counter</returns>
        public static long ReadClippedCounter() {
            return Thread.VolatileRead(ref mClippedCount);
        }

        private byte[] ConvError(PcmData from, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        private byte[] ConvClone(PcmData from, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return from.GetSampleLargeArray().ToArray();
        }

        private delegate void ConversionLoop(byte[] from, byte[] to, long nSample, NoiseShapingType noiseShaping);

        private byte[] ConvCommon(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args, ConversionLoop convLoop) {
            var from = pcmFrom.GetSampleLargeArray().ToArray();
            long nSample = from.LongLength * 8 / pcmFrom.BitsPerSample;
            var to = new byte[nSample * WasapiCS.SampleFormatTypeToUseBitsPerSample(toFormat) / 8];
            convLoop(from, to, nSample, args.noiseShaping);
            return to;
        }

        private byte[] ConvI16toI24(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    // Lower 8-bit: fill 0s
                    to[toPos++] = 0;

                    // Higher 16-bit: copy PCM
                    to[toPos++] = from[fromPos++];
                    to[toPos++] = from[fromPos++];
                }
            });
        }

        private byte[] ConvI16toI32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    // Lower 16-bit: fill 0s
                    to[toPos++] = 0;
                    to[toPos++] = 0;

                    // Higher 16-bit: copy PCM
                    to[toPos++] = from[fromPos++];
                    to[toPos++] = from[fromPos++];
                }
            });
        }

        private byte[] ConvI24toI32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    // Lower 8-bit: fill 0s
                    to[toPos++] = 0;

                    // Higher 24-bit: copy PCM
                    to[toPos++] = from[fromPos++];
                    to[toPos++] = from[fromPos++];
                    to[toPos++] = from[fromPos++];
                }
            });
        }

        private byte[] ConvI32V24toI24(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    // truncate lower 8-bit of 32-bit PCM to create 24-bit PCM
                    to[toPos++] = from[fromPos + 1];
                    to[toPos++] = from[fromPos + 2];
                    to[toPos++] = from[fromPos + 3];

                    fromPos += 4;
                }
            });
        }

        private byte[] ConvI32V24toI32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    // discard lower 8-bit because it is garbage
                    to[toPos++] = 0;
                    to[toPos++] = from[fromPos + 1];
                    to[toPos++] = from[fromPos + 2];
                    to[toPos++] = from[fromPos + 3];

                    fromPos += 4;
                }
            });
        }

        private byte[] ConvI32toI24orI32V24(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;
                bool writePad = toFormat == WasapiCS.SampleFormatType.Sint32V24;

                switch (noiseShaping) {
                case NoiseShapingType.None: {
                        for (int i = 0; i < nSample; ++i) {
                            // discard lower 8-bit
                            if (writePad) {
                                to[toPos++] = 0;
                            }
                            to[toPos++] = from[fromPos + 1];
                            to[toPos++] = from[fromPos + 2];
                            to[toPos++] = from[fromPos + 3];

                            fromPos += 4;
                        }
                    }
                    break;
                case NoiseShapingType.AddDither: {
                        for (int i = 0; i < nSample; ++i) {
                            long v = (int)((from[fromPos + 0] << 0) + (from[fromPos + 1] << 8) + (from[fromPos + 2] << 16) + (from[fromPos + 3] << 24));

                            // TPDF dither (width 512, center 128, left edge -128, right edge 384)
                            int dither = (int)((mRand.NextDouble() + mRand.NextDouble() - 0.5) * 256);

                            v += dither;

                            int vOut;
                            if (Int32.MaxValue < v) {
                                vOut = Int32.MaxValue;
                            } else if (v < Int32.MinValue) {
                                vOut = Int32.MinValue;
                            } else {
                                vOut = (int)v;
                            }

                            if (writePad) {
                                to[toPos++] = 0;
                            }
                            to[toPos++] = (byte)((vOut >> 8) & 0xff);
                            to[toPos++] = (byte)((vOut >> 16) & 0xff);
                            to[toPos++] = (byte)((vOut >> 24) & 0xff);

                            fromPos += 4;
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.NoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                long v = (int)((from[fromPos + 0] << 0) + (from[fromPos + 1] << 8) + (from[fromPos + 2] << 16) + (from[fromPos + 3] << 24));

                                int vOut;
                                if (Int32.MaxValue < v) {
                                    vOut = Int32.MaxValue;
                                } else if (v < Int32.MinValue) {
                                    vOut = Int32.MinValue;
                                } else {
                                    vOut = (int)v;
                                }
                                vOut = (int)(vOut & 0xffffff00);

                                mErr[ch] = (int)(v - vOut);

                                if (writePad) {
                                    to[toPos++] = 0;
                                }
                                to[toPos++] = (byte)((vOut >> 8) & 0xff);
                                to[toPos++] = (byte)((vOut >> 16) & 0xff);
                                to[toPos++] = (byte)((vOut >> 24) & 0xff);

                                fromPos += 4;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.DitheredNoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                long v = (int)((from[fromPos + 0] << 0) + (from[fromPos + 1] << 8) + (from[fromPos + 2] << 16) + (from[fromPos + 3] << 24));

                                // RPDF
                                int dither = (int)((mRand.NextDouble() - 0.5) * 256 / 4);

                                v += mErr[ch] + dither;

                                int vOut;
                                if (Int32.MaxValue < v) {
                                    vOut = Int32.MaxValue;
                                } else if (v < Int32.MinValue) {
                                    vOut = Int32.MinValue;
                                } else {
                                    vOut = (int)v;
                                }
                                vOut = (int)(vOut & 0xffffff00);

                                mErr[ch] = (int)(v - vOut);

                                if (writePad) {
                                    to[toPos++] = 0;
                                }
                                to[toPos++] = (byte)((vOut >> 8) & 0xff);
                                to[toPos++] = (byte)((vOut >> 16) & 0xff);
                                to[toPos++] = (byte)((vOut >> 24) & 0xff);

                                fromPos += 4;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                }
            });
        }

        private byte[] ConvI24or32toI16(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;
                int fromBytesPerSample = pcmFrom.BitsPerSample / 8;

                switch (noiseShaping) {
                case NoiseShapingType.None: {
                        int fromSkipCount = 1;
                        if (pcmFrom.BitsPerSample == 32) {
                            fromSkipCount = 2;
                        }

                        for (int i = 0; i < nSample; ++i) {
                            // discard lower bits
                            fromPos += fromSkipCount;

                            to[toPos++] = from[fromPos++];
                            to[toPos++] = from[fromPos++];
                        }
                    }
                    break;
                case NoiseShapingType.AddDither: {
                        if (pcmFrom.BitsPerSample == 32) {
                            // discard lower 8-bit
                            ++fromPos;
                        }

                        for (int i = 0; i < nSample; ++i) {
                            long v = (int)((from[fromPos] << 8) + (from[fromPos + 1] << 16) + (from[fromPos + 2] << 24));

                            // TPDF dither (width 131072, center 32768, left edge -32768, right edge 98304)
                            int dither = (int)((mRand.NextDouble() + mRand.NextDouble() - 0.5) * 65536);

                            v += dither;

                            int vOut;
                            if (Int32.MaxValue < v) {
                                vOut = Int32.MaxValue;
                            } else if (v < Int32.MinValue) {
                                vOut = Int32.MinValue;
                            } else {
                                vOut = (int)v;
                            }

                            to[toPos++] = (byte)((vOut >> 16) & 0xff);
                            to[toPos++] = (byte)((vOut >> 24) & 0xff);

                            fromPos += fromBytesPerSample;
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.NoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        if (pcmFrom.BitsPerSample == 32) {
                            // discard lower 8-bit
                            ++fromPos;
                        }

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                int v = (short)(from[fromPos + 1] + (from[fromPos + 2] << 8));

                                v += mErr[ch] >> 8;
                                if (32767 < v) {
                                    v = 32767;
                                }

                                mErr[ch] -= ((mErr[ch]) >> 8) << 8;
                                mErr[ch] += from[fromPos];

                                to[toPos++] = (byte)(v & 0xff);
                                to[toPos++] = (byte)((v >> 8) & 0xff);

                                fromPos += fromBytesPerSample;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.DitheredNoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        if (pcmFrom.BitsPerSample == 32) {
                            // discard lower 8-bit
                            ++fromPos;
                        }

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                long v = (int)((from[fromPos] << 8) + (from[fromPos + 1] << 16) + (from[fromPos + 2] << 24));

                                // RPDF
                                int dither = (int)((mRand.NextDouble() - 0.5) * 65536 / 4);

                                v += mErr[ch] + dither;

                                int vOut;
                                if (Int32.MaxValue < v) {
                                    vOut = Int32.MaxValue;
                                } else if (v < Int32.MinValue) {
                                    vOut = Int32.MinValue;
                                } else {
                                    vOut = (int)v;
                                }
                                vOut = (int)(vOut & 0xffff0000);

                                mErr[ch] = (int)(v - vOut);

                                to[toPos++] = (byte)((vOut >> 16) & 0xff);
                                to[toPos++] = (byte)((vOut >> 24) & 0xff);

                                fromPos += fromBytesPerSample;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                }
            });
        }

        ////////////////////////////////////////////////////////////////////
        // F32

        private static readonly double SAMPLE_VALUE_MIN_DOUBLE = -1.0;
        private static readonly float  SAMPLE_VALUE_MIN_FLOAT  = -1.0f;

        // これらの定数は “ぴったり正確に” 右辺のリテラルの値を保持する。
        private static readonly float SAMPLE_VALUE_MAX_FLOAT_TO_I16 = 32767.0f / 32768.0f;
        private static readonly float SAMPLE_VALUE_MAX_FLOAT_TO_I24 = 8388607.0f / 8388608.0f;  //< 0x3f7ffffe
        private static readonly float SAMPLE_VALUE_MAX_FLOAT_TO_I32 = 16777215.0f / 8388608.0f; //< 0x3f7fffff
        private static readonly float FLOAT_TO_INT16_SCALE = 32768.0f;
        private static readonly float INT16_TO_FLOAT_SCALE = 1.0f / 32768.0f;
        private static readonly float FLOAT_TO_INT24_SCALE = 8388608.0f;
        private static readonly float FLOAT_TO_INT32_SCALE = 2147483648.0f;
        private static readonly float INT32_TO_FLOAT_SCALE = 1.0f / 2147483648.0f;
        private static readonly int INT32_TO_FLOAT_MAX_INT = 2147483520;

        private byte[] ConvF32toI16(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                switch (noiseShaping) {
                case NoiseShapingType.None:
                    for (int i = 0; i < nSample; ++i) {
                        float fv = System.BitConverter.ToSingle(from, fromPos);
                        if (SAMPLE_VALUE_MAX_FLOAT_TO_I16 < fv) {
                            fv = SAMPLE_VALUE_MAX_FLOAT_TO_I16;
                            IncrementClippedCounter();
                        }
                        if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                            fv = SAMPLE_VALUE_MIN_FLOAT;
                            IncrementClippedCounter();
                        }

                        int iv = (int)(fv * FLOAT_TO_INT16_SCALE);

                        to[toPos++] = (byte)(iv & 0xff);
                        to[toPos++] = (byte)((iv >> 8) & 0xff);
                        fromPos += 4;
                    }
                    break;
                case NoiseShapingType.AddDither: {
                        for (int i = 0; i < nSample; ++i) {
                            float fv = System.BitConverter.ToSingle(from, fromPos);
                            if (SAMPLE_VALUE_MAX_FLOAT_TO_I16 < fv) {
                                fv = SAMPLE_VALUE_MAX_FLOAT_TO_I16;
                                IncrementClippedCounter();
                            }
                            if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                                fv = SAMPLE_VALUE_MIN_FLOAT;
                                IncrementClippedCounter();
                            }

                            long iv = (long)(fv * Int32.MaxValue);

                            // TPDF
                            int dither = (int)((mRand.NextDouble() + mRand.NextDouble() - 0.5) * 65536);

                            iv += dither;

                            int vOut;
                            if (Int32.MaxValue < iv) {
                                vOut = Int32.MaxValue;
                            } else if (iv < Int32.MinValue) {
                                vOut = Int32.MinValue;
                            } else {
                                vOut = (int)iv;
                            }

                            to[toPos++] = (byte)((vOut >> 16) & 0xff);
                            to[toPos++] = (byte)((vOut >> 24) & 0xff);
                            fromPos += 4;
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.NoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                float fv = System.BitConverter.ToSingle(from, fromPos);
                                if (SAMPLE_VALUE_MAX_FLOAT_TO_I16 < fv) {
                                    fv = SAMPLE_VALUE_MAX_FLOAT_TO_I16;
                                    IncrementClippedCounter();
                                }
                                if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                                    fv = SAMPLE_VALUE_MIN_FLOAT;
                                    IncrementClippedCounter();
                                }
                                long lv = (long)((double)fv * ((long)Int32.MaxValue + 1L) + mErr[ch]);

                                int iv;
                                if (Int32.MaxValue < lv) {
                                    iv = Int32.MaxValue;
                                } else if (lv < Int32.MinValue) {
                                    iv = Int32.MinValue;
                                } else {
                                    iv = (int)lv;
                                }

                                iv = (int)(iv & 0xffff0000);
                                mErr[ch] = (int)(lv - iv);

                                to[toPos++] = (byte)((iv >> 16) & 0xff);
                                to[toPos++] = (byte)((iv >> 24) & 0xff);
                                fromPos += 4;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                case NoiseShapingType.DitheredNoiseShaping1stOrder: {
                        long nFrame = nSample / pcmFrom.NumChannels;
                        System.Diagnostics.Debug.Assert(mErr != null && mErr.Length == pcmFrom.NumChannels);

                        for (int i = 0; i < nFrame; ++i) {
                            for (int ch=0; ch < pcmFrom.NumChannels; ++ch) {
                                float fv = System.BitConverter.ToSingle(from, fromPos);
                                if (SAMPLE_VALUE_MAX_FLOAT_TO_I16 < fv) {
                                    fv = SAMPLE_VALUE_MAX_FLOAT_TO_I16;
                                    IncrementClippedCounter();
                                }
                                if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                                    fv = SAMPLE_VALUE_MIN_FLOAT;
                                    IncrementClippedCounter();
                                }

                                // RPDF
                                double dither = (mRand.NextDouble() - 0.5) * 65536 / 4;

                                long lv = (long)((double)fv * ((long)Int32.MaxValue + 1L) + mErr[ch] + dither);

                                int iv;
                                if (Int32.MaxValue < lv) {
                                    iv = Int32.MaxValue;
                                } else if (lv < Int32.MinValue) {
                                    iv = Int32.MinValue;
                                } else {
                                    iv = (int)lv;
                                }

                                iv = (int)(iv & 0xffff0000);
                                mErr[ch] = (int)(lv - iv);

                                to[toPos++] = (byte)((iv >> 16) & 0xff);
                                to[toPos++] = (byte)((iv >> 24) & 0xff);

                                fromPos += 4;
                            }
                        }
                        args.noiseShapingOrDitherPerformed = true;
                    }
                    break;
                }
            });
        }

        private byte[] ConvF32toI24orI32V24(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;
                bool writePad = toFormat == WasapiCS.SampleFormatType.Sint32V24;

                for (int i = 0; i < nSample; ++i) {
                    float fv = System.BitConverter.ToSingle(from, fromPos);
                    if (SAMPLE_VALUE_MAX_FLOAT_TO_I24 < fv) {
                        fv = SAMPLE_VALUE_MAX_FLOAT_TO_I24;
                        IncrementClippedCounter();
                    }
                    if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                        fv = SAMPLE_VALUE_MIN_FLOAT;
                        IncrementClippedCounter();
                    }
                    int iv = (int)(fv * FLOAT_TO_INT24_SCALE);

                    if (writePad) {
                        to[toPos++] = 0;
                    }
                    to[toPos++] = (byte)(iv & 0xff);
                    to[toPos++] = (byte)((iv >> 8) & 0xff);
                    to[toPos++] = (byte)((iv >> 16) & 0xff);
                    fromPos += 4;
                }
            });
        }

        private byte[] ConvF32toI32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    float fv = System.BitConverter.ToSingle(from, fromPos);
                    if (SAMPLE_VALUE_MAX_FLOAT_TO_I32 < fv) {
                        fv = SAMPLE_VALUE_MAX_FLOAT_TO_I32;
                        IncrementClippedCounter();
                    }
                    if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                        fv = SAMPLE_VALUE_MIN_FLOAT;
                        IncrementClippedCounter();
                    }
                    int iv = (int)(fv * FLOAT_TO_INT32_SCALE);

                    to[toPos++] = (byte)(iv & 0xff);
                    to[toPos++] = (byte)((iv >> 8) & 0xff);
                    to[toPos++] = (byte)((iv >> 16) & 0xff);
                    to[toPos++] = (byte)((iv >> 24) & 0xff);
                    fromPos += 4;
                }
            });
        }

        private byte[] ConvI16toF32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    short iv = (short)(from[fromPos]
                        + (from[fromPos + 1] << 8));
                    float fv = ((float)iv) * INT16_TO_FLOAT_SCALE;

                    byte [] b = System.BitConverter.GetBytes(fv);

                    to[toPos++] = b[0];
                    to[toPos++] = b[1];
                    to[toPos++] = b[2];
                    to[toPos++] = b[3];
                    fromPos += 2;
                }
            });
        }

        private byte[] ConvI24toF32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    int iv = ((int)from[fromPos] << 8)
                           + ((int)from[fromPos + 1] << 16)
                           + ((int)from[fromPos + 2] << 24);
                    float fv = ((float)iv) * INT32_TO_FLOAT_SCALE;

                    byte [] b = System.BitConverter.GetBytes(fv);

                    to[toPos++] = b[0];
                    to[toPos++] = b[1];
                    to[toPos++] = b[2];
                    to[toPos++] = b[3];
                    fromPos += 3;
                }
            });
        }

        private byte[] ConvI32toF32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    int iv = (int)from[fromPos + 0]
                           + ((int)from[fromPos + 1] << 8)
                           + ((int)from[fromPos + 2] << 16)
                           + ((int)from[fromPos + 3] << 24);

                    // float値 0x3f7fffffは整数値2147483520に対応する。
                    // 範囲外のfloat値 3f800000 (== +1.0f) が
                    // 出てこないようにクランプする。
                    if (INT32_TO_FLOAT_MAX_INT < iv) {
                        iv = INT32_TO_FLOAT_MAX_INT;
                    }

                    float fv = ((float)iv) * INT32_TO_FLOAT_SCALE;

                    byte [] b = System.BitConverter.GetBytes(fv);

                    to[toPos++] = b[0];
                    to[toPos++] = b[1];
                    to[toPos++] = b[2];
                    to[toPos++] = b[3];
                    fromPos += 4;
                }
            });
        }

        ////////////////////////////////////////////////////////////////
        // F64

        private static readonly double SAMPLE_VALUE_MAX_DOUBLE_TO_I16 = 32767.0 / 32768.0;
        private static readonly double SAMPLE_VALUE_MAX_DOUBLE_TO_I24 = 8388607.0 / 8388608.0;
        private static readonly double SAMPLE_VALUE_MAX_DOUBLE_TO_I32 = 2147483647.0 / 2147483648.0;

        private static readonly double DOUBLE_TO_I16_SCALE = 32768.0;
        private static readonly double DOUBLE_TO_I24_SCALE = 8388608.0;
        private static readonly double DOUBLE_TO_I32_SCALE = 2147483648.0;
        private static readonly double INT16_TO_DOUBLE_SCALE = 1.0 / 32768.0;
        private static readonly double INT32_TO_DOUBLE_SCALE = 1.0 / 2147483648.0;

        private byte[] ConvF64toI16(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    double dv = System.BitConverter.ToDouble(from, fromPos);
                    if (SAMPLE_VALUE_MAX_DOUBLE_TO_I16 < dv) {
                        dv = SAMPLE_VALUE_MAX_DOUBLE_TO_I16;
                        IncrementClippedCounter();
                    }
                    if (dv < SAMPLE_VALUE_MIN_DOUBLE) {
                        dv = SAMPLE_VALUE_MIN_DOUBLE;
                        IncrementClippedCounter();
                    }
                    int iv = (int)(dv * DOUBLE_TO_I16_SCALE);

                    to[toPos++] = (byte)(iv & 0xff);
                    to[toPos++] = (byte)((iv >> 8) & 0xff);
                    fromPos += 8;
                }
            });
        }

        private byte[] ConvF64toI24orI32V24(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;
                bool writePad = toFormat == WasapiCS.SampleFormatType.Sint32V24;

                for (int i = 0; i < nSample; ++i) {
                    double dv = System.BitConverter.ToDouble(from, fromPos);
                    if (SAMPLE_VALUE_MAX_DOUBLE_TO_I24 < dv) {
                        dv = SAMPLE_VALUE_MAX_DOUBLE_TO_I24;
                        IncrementClippedCounter();
                    }
                    if (dv < SAMPLE_VALUE_MIN_DOUBLE) {
                        dv = SAMPLE_VALUE_MIN_DOUBLE;
                        IncrementClippedCounter();
                    }
                    int iv = (int)(dv * DOUBLE_TO_I24_SCALE);

                    if (writePad) {
                        to[toPos++] = 0;
                    }
                    to[toPos++] = (byte)(iv & 0xff);
                    to[toPos++] = (byte)((iv >> 8) & 0xff);
                    to[toPos++] = (byte)((iv >> 16) & 0xff);
                    fromPos += 8;
                }
            });
        }

        private byte[] ConvF64toI32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    double dv = System.BitConverter.ToDouble(from, fromPos);
                    if (SAMPLE_VALUE_MAX_DOUBLE_TO_I32 < dv) {
                        dv = SAMPLE_VALUE_MAX_DOUBLE_TO_I32;
                        IncrementClippedCounter();
                    }
                    if (dv < SAMPLE_VALUE_MIN_DOUBLE) {
                        dv = SAMPLE_VALUE_MIN_DOUBLE;
                        IncrementClippedCounter();
                    }

                    int iv = (int)(dv * DOUBLE_TO_I32_SCALE);

                    to[toPos++] = (byte)((iv >> 0) & 0xff);
                    to[toPos++] = (byte)((iv >> 8) & 0xff);
                    to[toPos++] = (byte)((iv >> 16) & 0xff);
                    to[toPos++] = (byte)((iv >> 24) & 0xff);
                    fromPos += 8;
                }
            });
        }

        private byte[] ConvF64toF32(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    double dv = System.BitConverter.ToDouble(from, fromPos);
                    float fv = (float)dv;
                    if (SAMPLE_VALUE_MAX_FLOAT_TO_I32 < fv) {
                        fv = SAMPLE_VALUE_MAX_FLOAT_TO_I32;
                        IncrementClippedCounter();
                    }
                    if (fv < SAMPLE_VALUE_MIN_FLOAT) {
                        fv = SAMPLE_VALUE_MIN_FLOAT;
                        IncrementClippedCounter();
                    }
                    byte [] b = System.BitConverter.GetBytes(fv);
                    to[toPos++] = b[0];
                    to[toPos++] = b[1];
                    to[toPos++] = b[2];
                    to[toPos++] = b[3];
                    fromPos += 8;
                }
            });
        }

        private byte[] ConvI16toF64(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    short iv = (short)(from[fromPos]
                        + (from[fromPos + 1] << 8));
                    double dv = ((double)iv) * INT16_TO_DOUBLE_SCALE;

                    byte [] b = System.BitConverter.GetBytes(dv);

                    for (int j=0; j < 8; ++j) {
                        to[toPos++] = b[j];
                    }
                    fromPos += 2;
                }
            });
        }

        private byte[] ConvI24toF64(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    int iv = ((int)from[fromPos] << 8)
                           + ((int)from[fromPos + 1] << 16)
                           + ((int)from[fromPos + 2] << 24);
                    double dv = ((double)iv) * INT32_TO_DOUBLE_SCALE;

                    byte [] b = System.BitConverter.GetBytes(dv);

                    for (int j=0; j < 8; ++j) {
                        to[toPos++] = b[j];
                    }
                    fromPos += 3;
                }
            });
        }

        private byte[] ConvI32toF64(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    int iv = ((int)from[fromPos + 1] << 8)
                           + ((int)from[fromPos + 2] << 16)
                           + ((int)from[fromPos + 3] << 24);
                    double dv = ((double)iv) * INT32_TO_DOUBLE_SCALE;

                    byte [] b = System.BitConverter.GetBytes(dv);

                    for (int j=0; j < 8; ++j) {
                        to[toPos++] = b[j];
                    }
                    fromPos += 4;
                }
            });
        }

        private byte[] ConvF32toF64(PcmData pcmFrom, WasapiCS.SampleFormatType toFormat, BitsPerSampleConvArgs args) {
            return ConvCommon(pcmFrom, toFormat, args, (from, to, nSample, noiseShaping) => {
                int fromPos = 0;
                int toPos   = 0;

                for (int i = 0; i < nSample; ++i) {
                    float fv = System.BitConverter.ToSingle(from, fromPos);
                    double dv = (double)fv;

                    byte [] b = System.BitConverter.GetBytes(dv);
                    for (int j=0; j < 8; ++j) {
                        to[toPos++] = b[j];
                    }
                    fromPos += 4;
                }
            });
        }
    }
}
