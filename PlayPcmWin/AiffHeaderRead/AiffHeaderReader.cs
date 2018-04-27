// 日本語UTF-8

using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace AiffHeaderRead {

    class AiffHeaderReader {
        public enum ResultType {
            Success,
            NotAiff,
            HeaderError,
            NotSupportBlockSizeNonzero,
            NotSupportBitsPerSample,
            NotSupportID3version,
            NotSupportID3Unsynchronization,
            NotSupportAifcVersion,
            NotSupportAifcCompression,
            NotFoundFverHeader,
            NotFoundCommHeader,
            NotFoundSsndHeader,
            NotFoundID3Header,
            ReadError,
        }

        public int NumChannels { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public long NumFrames { get; set; }

        /// <summary>
        /// returns BitsPerSample * NumChannels
        /// </summary>
        public int BitsPerFrame {
            get { return BitsPerSample * NumChannels; }
        }

        /// <summary>
        /// returns BitsPerSample * NumChannels / 8
        /// </summary>
        public int BytesPerFrame {
            get { return BitsPerSample * NumChannels / 8; }
        }

        private long mCkSize;
        private bool mIsAIFC = false;

        public enum CompressionType {
            Unknown = -1,
            None,
            Sowt,
        }

        public CompressionType Compression { get; set; }

        private PcmDataLib.ID3Reader mId3Reader = new PcmDataLib.ID3Reader();

        private const uint AIFC_TIMESTAMP   = 0xa2805140;
        private const uint COMPRESSION_SOWT = 0x736f7774;

        private StringBuilder mSB = new StringBuilder();

        private ResultType ReadFormChunkHeader(BinaryReader br) {
            mSB.Append("Form chunk.\r\n");

            byte[] ckID = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(ckID, 0, "FORM")) {
                return ResultType.NotAiff;
            }


            mCkSize = Util.ReadBigU32(br);
            
            mSB.Append(string.Format("    FORM chunk size = {0} bytes\r\n", mCkSize));

            if (0 != (mCkSize & 0x80000000)) {
                return ResultType.HeaderError;
            }

            byte[] formType = br.ReadBytes(4);
            
            mSB.Append(string.Format("    FORM type = {0}{1}{2}{3}\r\n", (char)formType[0], (char)formType[1], (char)formType[2], (char)formType[3] ));

            if (!PcmDataLib.Util.FourCCHeaderIs(formType, 0, "AIFF")) {
                if (PcmDataLib.Util.FourCCHeaderIs(formType, 0, "AIFC")) {
                    mIsAIFC = true;
                } else {
                    return ResultType.NotAiff;
                }
            }

            return ResultType.Success;
        }

        /// <summary>
        /// 目的のチャンクが見つかるまでスキップする。
        /// </summary>
        /// <param name="ckId">チャンクID</param>
        private bool SkipToChunk(BinaryReader br, string findCkId) {
            System.Diagnostics.Debug.Assert(findCkId.Length == 4);

            try {
                while (true) {
                    byte[] ckID = br.ReadBytes(4);
                    if (PcmDataLib.Util.FourCCHeaderIs(ckID, 0, findCkId)) {
                        return true;
                    }

                    long ckSize = Util.ReadBigU32(br);
                    mSB.Append(string.Format("  Skipped {0}{1}{2}{3} chunk. size = {4} bytes\r\n",
                        (char)ckID[0], (char)ckID[1], (char)ckID[2], (char)ckID[3], ckSize));

                    long skipBytes = PcmDataLib.Util.ChunkSizeWithPad(ckSize);
                    if (skipBytes < 0) {
                        Console.WriteLine("D: SkipToChunk {0}", skipBytes);
                        return false;
                    }
                    PcmDataLib.Util.BinaryReaderSkip(br, skipBytes);
                }
            } catch (System.IO.EndOfStreamException ex) {
                Console.WriteLine(ex);
                return false;
            }
        }

        private ResultType ReadFverChunk(BinaryReader br) {
            if (!SkipToChunk(br, "FVER")) {
                return ResultType.NotFoundFverHeader;
            }
            uint ckSize = Util.ReadBigU32(br);

            mSB.Append(string.Format("FVER chunk.\r\n    FVER chunk size = {0} bytes\r\n", ckSize));

            if (4 != ckSize) {
                mSB.Append(string.Format("FVER chunk sizeが4ではありません。\r\n"));
                return ResultType.HeaderError;
            }

            uint timestamp = Util.ReadBigU32(br);

            mSB.Append(string.Format("    timestamp = {0:X}\r\n", timestamp));
            if (AIFC_TIMESTAMP != timestamp) {
                mSB.Append(string.Format("FVER chunk timestampが{0:X}ではなく{1:X}が入っています。\r\n", AIFC_TIMESTAMP, timestamp));
                return ResultType.NotSupportAifcVersion;
            }

            return ResultType.Success;
        }

        private ResultType ReadCommonChunk(BinaryReader br) {
            mSB.Append("Common chunk.\r\n");
            if (!SkipToChunk(br, "COMM")) {
                return ResultType.NotFoundCommHeader;
            }
            uint ckSize = Util.ReadBigU32(br);
            NumChannels = Util.ReadBigU16(br);
            NumFrames = Util.ReadBigU32(br);
            BitsPerSample = Util.ReadBigU16(br);

            byte[] sampleRate80 = br.ReadBytes(10);

            uint readSize = 2 + 4 + 2 + 10;

            Compression = CompressionType.None;
            if (4 <= ckSize-readSize) {
                uint compressionId = Util.ReadBigU32(br); 
                readSize += 4;

                switch (compressionId) {
                case COMPRESSION_SOWT:
                    Compression = CompressionType.Sowt;
                    break;
                default:
                    // sowt以外は未対応
                    Compression = CompressionType.Unknown;
                    return ResultType.NotSupportAifcCompression;
                }
            }

            SampleRate = (int)IEEE754ExtendedDoubleBigEndianToDouble(sampleRate80);

            mSB.Append(string.Format("    COMM chunk size = {0} bytes\r\n    NumChannels = {1} ch\r\n    NumFrames = {2}\r\n    BitsPerSample = {3} bit\r\n    SampleRate = {4} Hz\r\n    Compression = {5}\r\n    ckSize-readSize = {6}\r\n",
                ckSize, NumChannels, NumFrames, BitsPerSample, SampleRate, Compression, ckSize-readSize));

            if (ckSize - readSize < 0) {
                mSB.Append(string.Format("Error: ckSize - readSize < 0"));
                return ResultType.HeaderError;
            }
            PcmDataLib.Util.BinaryReaderSkip(br, ckSize - readSize);
            
            return ResultType.Success;
        }

        /// <summary>
        /// サウンドデータチャンクのヘッダ情報を読む
        /// </summary>
        private ResultType ReadSoundDataChunk(BinaryReader br) {
            mSB.Append("SSND chunk.\r\n");
            if (!SkipToChunk(br, "SSND")) {
                return ResultType.NotFoundSsndHeader;
            }
            long ckSize = Util.ReadBigU32(br);
            long offset = Util.ReadBigU32(br);
            long blockSize = Util.ReadBigU32(br);

            mSB.Append(string.Format("    SSND chunk size = {0} bytes\r\n    offset = {1}\r\n    blockSize = {2}\r\n", ckSize, offset, blockSize));

            if (blockSize != 0) {
                return ResultType.NotSupportBlockSizeNonzero;
            }

            // SoundDataチャンクの最後まで移動。
            // sizeof offset + blockSize == 8
            PcmDataLib.Util.BinaryReaderSkip(br, PcmDataLib.Util.ChunkSizeWithPad(ckSize) - 8);

            return ResultType.Success;
        }

        private ResultType ReadID3Chunk(BinaryReader br) {
            long ckSize = 0;
            PcmDataLib.ID3Reader.ID3Result id3r = PcmDataLib.ID3Reader.ID3Result.ReadError;

            try {
                if (!SkipToChunk(br, "ID3 ")) {
                    // ID3チャンクは無い。
                    return ResultType.NotFoundID3Header;
                }

                ckSize = Util.ReadBigU32(br);
                mSB.Append(string.Format("ID3 chunk.\r\n    ID3 ckSize = {0} bytes\r\n", ckSize));
                if (ckSize < 10) {
                    return ResultType.ReadError;
                }

                id3r = mId3Reader.Read(br);
            } catch (Exception ex) {
                mSB.Append(ex);
                mSB.Append("\r\n");
            }

            ResultType result = ResultType.Success;
            switch (id3r) {
            case PcmDataLib.ID3Reader.ID3Result.ReadError:
                result = ResultType.ReadError;
                break;
            case PcmDataLib.ID3Reader.ID3Result.NotSupportedID3version: // ID3が読めなくても再生はできるようにする。
                result = ResultType.Success;
                mSB.Append(string.Format("    ID3Reader returned {0}\r\n", id3r));
                break;
            case PcmDataLib.ID3Reader.ID3Result.Success:
                mSB.Append(string.Format("    ID3 version 2.{0}\r\n", mId3Reader.MinorVersion));
                /* この処理は必要ない。s
                PcmDataLib.Util.BinaryReaderSkip(br, PcmDataLib.Util.ChunkSizeWithPad(ckSize) - mId3Reader.ReadBytes);
                 * */
                break;
            default:
                // 追加忘れ
                System.Diagnostics.Debug.Assert(false);
                result = ResultType.ReadError;
                break;
            }
            return result;
        }

        public string TitleName { get { return mId3Reader.TitleName; } }
        public string AlbumName { get { return mId3Reader.AlbumName; } }
        public string ArtistName { get { return mId3Reader.ArtistName; } }

        /// <summary>
        /// 画像データバイト数(無いときは0)
        /// </summary>
        public int PictureBytes { get { return mId3Reader.PictureBytes; } }

        /// <summary>
        /// 画像データ
        /// </summary>
        public byte[] PictureData { get { return mId3Reader.PictureData; } }
        
        public string ReadHeader(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            pcmData = new PcmDataLib.PcmData();

            ResultType result = ReadFormChunkHeader(br);
            if (result != ResultType.Success) {
                mSB.Append(string.Format("Error: Form Chunkチャンクの読み込み失敗。{0}\r\n", result));
                return mSB.ToString();
            }
            
            if (mIsAIFC) {
                // AIFCの場合、FVERチャンクが来る(required)
                result = ReadFverChunk(br);
                if (ResultType.Success != result) {
                    mSB.Append(string.Format("Error: AIFC FVERチャンクのエラー。{0}\r\n", result));
                    return mSB.ToString();
                }
            }

            result = ReadCommonChunk(br);
            if (ResultType.Success != result) {
                mSB.Append(string.Format("Error: COMMチャンクのエラー。{0}\r\n", result));
                return mSB.ToString();
            }

            result = ReadSoundDataChunk(br);
            if (ResultType.Success != result) {
                mSB.Append(string.Format("Error: SSNDチャンクのエラー。{0}\r\n", result));
                return mSB.ToString();
            }

            if (16 != BitsPerSample &&
                24 != BitsPerSample) {
                mSB.Append(string.Format("Info: 量子化ビット数がエキゾチックです。{0} bit\r\n", BitsPerSample));
                return mSB.ToString();
            }

            pcmData.SetFormat(
                NumChannels,
                BitsPerSample,
                BitsPerSample,
                SampleRate,
                PcmDataLib.PcmData.ValueRepresentationType.SInt,
                NumFrames);

            result = ReadID3Chunk(br);
            switch (result) {
            case ResultType.NotFoundID3Header:
                // ID3ヘッダーが無い。
                break;
            case ResultType.Success:
                // ID3読み込み成功
                pcmData.DisplayName = TitleName;
                pcmData.AlbumTitle = AlbumName;
                pcmData.ArtistName = ArtistName;
                if (0 < PictureBytes) {
                    pcmData.SetPicture(PictureBytes, PictureData);
                }
                mSB.Append(string.Format("    ID3チャンク読み込み終了。\r\n"));
                break;
            default:
                mSB.Append(string.Format("Error: ID3チャンクのエラー。{0}\r\n",result));
                return mSB.ToString();
            }

            mSB.Append("正常終了。\r\n");

            return mSB.ToString();
        }

        /// <summary>
        /// ビッグエンディアンバイトオーダーの80ビット拡張倍精度浮動小数点数→double
        /// 手抜き実装: subnormal numberとか、NaNとかがどうなるかは確かめてない
        /// </summary>
        private static double IEEE754ExtendedDoubleBigEndianToDouble(byte[] extended) {
            System.Diagnostics.Debug.Assert(extended.Length == 10);

            byte[] resultBytes = new byte[8];

            // 7777777777666666666655555555554444444444333333333322222222221111111111
            // 98765432109876543210987654321098765432109876543210987654321098765432109876543210
            // seeeeeeeeeeeeeeeffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
            // 00000000111111112222222233333333444444445555555566666666777777778888888899999999 (big-endian)

            // 666655555555554444444444333333333322222222221111111111
            // 3210987654321098765432109876543210987654321098765432109876543210
            // seeeeeeeeeeeffffffffffffffffffffffffffffffffffffffffffffffffffff
            // 0000000011111111222222223333333344444444555555556666666677777777 (big-endian)

            int exponent =
                ((extended[0] & 0x7f) << 8) + (extended[1]);
            exponent -= 16383;

            int exponent64 = exponent + 1023;

            // extended double precisionには、fractionのimplicit/hidden bitはない。
            // subnormal numberでなければfractionのMSBは1になる
            // double precisionは、fractionのimplicit/hidden bitが存在する。なので1ビット余計に左シフトする

            long fraction =
                ((long)extended[2] << 57) +
                ((long)extended[3] << 49) +
                ((long)extended[4] << 41) +
                ((long)extended[5] << 33) +
                ((long)extended[6] << 25) +
                ((long)extended[7] << 17) +
                ((long)extended[8] << 9) +
                ((long)extended[9] << 1);

            resultBytes[7] = (byte)((extended[0] & 0x80) + (0x7f & (exponent64 >> 4)));
            resultBytes[6] = (byte)(((exponent64 & 0x0f) << 4) + (0x0f & (fraction >> 60)));
            resultBytes[5] = (byte)(0xff & (fraction >> 52));
            resultBytes[4] = (byte)(0xff & (fraction >> 44));
            resultBytes[3] = (byte)(0xff & (fraction >> 36));
            resultBytes[2] = (byte)(0xff & (fraction >> 28));
            resultBytes[1] = (byte)(0xff & (fraction >> 20));
            resultBytes[0] = (byte)(0xff & (fraction >> 12));

            double result = System.BitConverter.ToDouble(resultBytes, 0);
            return result;
        }

    }
}
