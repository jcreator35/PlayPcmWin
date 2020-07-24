// 日本語。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WWUtil;

namespace PcmDataLib {

    /// <summary>
    /// ユーティリティー関数置き場。
    /// </summary>
    public class PcmDataUtil {
        /// <summary>
        /// readerのデータをcountバイトだけスキップする。
        /// ファイルの終わりを超えたときEndOfStreamExceptionを出す
        /// </summary>
        public static long BinaryReaderSkip(BinaryReader reader, long count) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count == 0) {
                return 0;
            }
            if (reader.BaseStream.Length < reader.BaseStream.Position + count) {
                throw new EndOfStreamException();
            }

            if (reader.BaseStream.CanSeek) {
                reader.BaseStream.Seek(count, SeekOrigin.Current);
            } else {
                for (long i = 0; i < count; ++i) {
                    reader.ReadByte();
                }
            }
            return count;
        }

        public static bool BinaryReaderSeekFromBegin(BinaryReader reader, long offset) {
            if (!reader.BaseStream.CanSeek) {
                return false;
            }
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return true;
        }

        /// <summary>
        /// FourCC形式のバイト列をsと比較する
        /// </summary>
        /// <param name="b">FourCC形式のバイト列を含むバッファ</param>
        /// <param name="bPos">バイト列先頭から注目位置までのオフセット</param>
        /// <param name="s">比較対象文字列 最大4文字</param>
        /// <returns></returns>
        public static bool FourCCHeaderIs(byte[] b, int bPos, string s) {
            System.Diagnostics.Debug.Assert(s.Length == 4);
            if (b.Length - bPos < 4) {
                return false;
            }

            /*
            System.Console.WriteLine("D: b={0}{1}{2}{3} s={4}",
                (char)b[0], (char)b[1], (char)b[2], (char)b[3], s);
            */

            return s[0] == b[bPos]
                && s[1] == b[bPos + 1]
                && s[2] == b[bPos + 2]
                && s[3] == b[bPos + 3];
        }

        /// <summary>
        /// チャンクサイズが奇数の場合、近い偶数に繰上げ。
        /// </summary>
        public static long ChunkSizeWithPad(long ckSize) {
            return ((~(1L)) & (ckSize + 1));
        }

        /// <summary>
        /// float → signed int 32bit little endian byte array
        /// </summary>
        public static byte[] ConvertTo24bitLE(float v) {
            int v32 = 0;
            if (1.0f <= v) {
                v32 = Int32.MaxValue;
            } else if (v < -1.0f) {
                v32 = Int32.MinValue;
            } else {
                long vMax = -((long)Int32.MinValue);

                long v64 = (long)(v * vMax);
                if (Int32.MaxValue < v64) {
                    v64 = Int32.MaxValue;
                }
                v32 = (int)v64;
            }

            var r = new byte[3];
            r[0] = (byte)((v32 & 0x0000ff00) >> 8);
            r[1] = (byte)((v32 & 0x00ff0000) >> 16);
            r[2] = (byte)((v32 & 0xff000000) >> 24);
            return r;
        }

        public static LargeArray<byte> ConvertTo24bit(int bitsPerSample, long numSamples,
                PcmData.ValueRepresentationType valueType, LargeArray<byte> data) {
            if (bitsPerSample == 24 && valueType == PcmData.ValueRepresentationType.SInt) {
                // すでに所望の形式。
                return data;
            }

            var data24 = new LargeArray<byte>(numSamples * 3);

            switch (valueType) {
            case PcmData.ValueRepresentationType.SInt:
                switch (bitsPerSample) {
                case 16:
                    for (long i = 0; i < numSamples; ++i) {
                        short v16 = (short)((uint)data.At(i * 2) + ((uint)data.At(i * 2 + 1) << 8));
                        data24.Set(i * 3 + 0, 0);
                        data24.Set(i * 3 + 1, (byte)(0xff & (v16 >> 0)));
                        data24.Set(i * 3 + 2, (byte)(0xff & (v16 >> 8)));
                    }
                    return data24;
                case 32:
                    for (long i = 0; i < numSamples; ++i) {
                        int v32 = (int)(
                              ((uint)data.At(i * 4 + 1) << 8)
                            + ((uint)data.At(i * 4 + 2) << 16)
                            + ((uint)data.At(i * 4 + 3) << 24));
                        data24.Set(i * 3 + 0, (byte)((v32 & 0x0000ff00) >> 8));
                        data24.Set(i * 3 + 1, (byte)((v32 & 0x00ff0000) >> 16));
                        data24.Set(i * 3 + 2, (byte)((v32 & 0xff000000) >> 24));
                    }
                    return data24;
                case 64:
                    for (long i = 0; i < numSamples; ++i) {
                        // 16.48 fixed point を想定している。
                        int v32 = (int)(
                              ((uint)data.At(i * 8 + 3) << 8)
                            + ((uint)data.At(i * 8 + 4) << 16)
                            + ((uint)data.At(i * 8 + 5) << 24));
                        data24.Set(i * 3 + 0, (byte)((v32 & 0x0000ff00) >> 8));
                        data24.Set(i * 3 + 1, (byte)((v32 & 0x00ff0000) >> 16));
                        data24.Set(i * 3 + 2, (byte)((v32 & 0xff000000) >> 24));
                    }
                    return data24;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return null;
                }
            case PcmData.ValueRepresentationType.SFloat:
                for (long i = 0; i < numSamples; ++i) {
                    double v;
                    switch (bitsPerSample) {
                    case 32: {
                            var b4 = new byte[4];
                            data.CopyTo(i * 4, ref b4, 0, 4);
                            v = BitConverter.ToSingle(b4, 0);
                        }
                        break;
                    case 64: {
                            var b8 = new byte[8];
                            data.CopyTo(i * 8, ref b8, 0, 8);
                            v = BitConverter.ToDouble(b8, 0);
                        }
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        return null;
                    }

                    int v32 = 0;
                    if (1.0f <= v) {
                        v32 = Int32.MaxValue;
                    } else if (v < -1.0f) {
                        v32 = Int32.MinValue;
                    } else {
                        long vMax = -((long)Int32.MinValue);

                        long v64 = (long)(v * vMax);
                        if (Int32.MaxValue < v64) {
                            v64 = Int32.MaxValue;
                        }
                        v32 = (int)v64;
                    }

                    data24.Set(i * 3 + 0, (byte)((v32 & 0x0000ff00) >> 8));
                    data24.Set(i * 3 + 1, (byte)((v32 & 0x00ff0000) >> 16));
                    data24.Set(i * 3 + 2, (byte)((v32 & 0xff000000) >> 24));
                }
                return data24;
            default:
                System.Diagnostics.Debug.Assert(false);
                return null;
            }
        }

        public static LargeArray<byte> ConvertTo32bitInt(
                int bitsPerSample, long numSamples,
                PcmData.ValueRepresentationType valueType, LargeArray<byte> data) {
            if (bitsPerSample == 32 && valueType == PcmData.ValueRepresentationType.SInt) {
                // すでに所望の形式。
                return data;
            }

            var data32 = new LargeArray<byte>(numSamples * 4);

            switch (valueType) {
            case PcmData.ValueRepresentationType.SInt:
                switch (bitsPerSample) {
                case 16:
                    for (long i = 0; i < numSamples; ++i) {
                        short v16 = (short)(
                               (uint)data.At(i * 2)
                            + ((uint)data.At(i * 2 + 1) << 8));
                        int v32 = v16;
                        v32 *= 65536;
                        data32.Set(i * 4 + 0, 0);
                        data32.Set(i * 4 + 1, 0);
                        data32.Set(i * 4 + 2, (byte)((v32 & 0x00ff0000) >> 16));
                        data32.Set(i * 4 + 3, (byte)((v32 & 0xff000000) >> 24));
                    }
                    return data32;
                case 24:
                    for (long i = 0; i < numSamples; ++i) {
                        int v32 = (int)(
                              ((uint)data.At(i * 3 + 0) << 8)
                            + ((uint)data.At(i * 3 + 1) << 16)
                            + ((uint)data.At(i * 3 + 2) << 24));
                        data32.Set(i * 4 + 0, 0);
                        data32.Set(i * 4 + 1, (byte)((v32 & 0x0000ff00) >> 8));
                        data32.Set(i * 4 + 2, (byte)((v32 & 0x00ff0000) >> 16));
                        data32.Set(i * 4 + 3, (byte)((v32 & 0xff000000) >> 24));
                    }
                    return data32;
                case 32:
                    // 所望の形式。
                    System.Diagnostics.Debug.Assert(false);
                    return null;
                case 64:
                    for (long i = 0; i < numSamples; ++i) {
                        // 16.48 fixed point
                        int v32 = (int)(
                              ((uint)data.At(i * 8 + 2) << 0)
                            + ((uint)data.At(i * 8 + 3) << 8)
                            + ((uint)data.At(i * 8 + 4) << 16)
                            + ((uint)data.At(i * 8 + 5) << 24));
                        data32.Set(i * 4 + 0, (byte)((v32 & 0x000000ff) >> 0));
                        data32.Set(i * 4 + 1, (byte)((v32 & 0x0000ff00) >> 8));
                        data32.Set(i * 4 + 2, (byte)((v32 & 0x00ff0000) >> 16));
                        data32.Set(i * 4 + 3, (byte)((v32 & 0xff000000) >> 24));
                    }
                    return data32;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return null;
                }
            case PcmData.ValueRepresentationType.SFloat:
                for (long i = 0; i < numSamples; ++i) {
                    double v;
                    switch (bitsPerSample) {
                    case 32: {
                            var b4 = new byte[4];
                            data.CopyTo(i * 4, ref b4, 0, 4);
                            v = BitConverter.ToSingle(b4, 0);
                        }
                        break;
                    case 64: {
                            var b8 = new byte[8];
                            data.CopyTo(i * 8, ref b8, 0, 8);
                            v = BitConverter.ToDouble(b8, 0);
                        }
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        return null;
                    }

                    int v32 = 0;
                    if (1.0f <= v) {
                        v32 = Int32.MaxValue;
                    } else if (v < -1.0f) {
                        v32 = Int32.MinValue;
                    } else {
                        long vMax = -((long)Int32.MinValue);

                        long v64 = (long)(v * vMax);
                        if (Int32.MaxValue < v64) {
                            v64 = Int32.MaxValue;
                        }
                        v32 = (int)v64;
                    }

                    data32.Set(i * 4 + 0, (byte)((v32 & 0x000000ff) >> 0));
                    data32.Set(i * 4 + 1, (byte)((v32 & 0x0000ff00) >> 8));
                    data32.Set(i * 4 + 2, (byte)((v32 & 0x00ff0000) >> 16));
                    data32.Set(i * 4 + 3, (byte)((v32 & 0xff000000) >> 24));
                }
                return data32;
            default:
                System.Diagnostics.Debug.Assert(false);
                return null;
            }
        }
    }
}
