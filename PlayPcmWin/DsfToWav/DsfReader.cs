// 日本語UTF-8

using System;
using System.IO;
using System.Threading.Tasks;

namespace PlayPcmWin {

    class DsfReader {
        public enum ResultType {
            Success,
            NotDsf,
            HeaderError,
            NotSupportDsdChunkBytes,
            NotSupportFileTooLarge,
            NotSupportBlockSize,
            NotSupportBitsPerSample,
            NotSupportID3version,
            NotSupportID3Unsynchronization,
            NotSupportDsfVersion,
            NotSupportDsfFormatId,
            NotSupportNumChannels,
            NotSupportSampleFrequency,
            NotFoundFmtHeader,
            NotFoundDataHeader,
            NotFoundId3Header,
            ReadError
        }

        public int NumChannels { get; set; }

        /// <summary>
        /// 2822400(2.8MHz)か、5644800(5.6MHz)か、11289600(11.2MHz)か、22579200(22.5MHz)か、
        /// 3072000(3.0MHz)か、6144000(6.1MHz)か、12288000(12.2MHz)か、24576000(24.5MHz)か、…
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// 1フレーム=16ビット(2バイト) x チャンネル数とする。
        /// 実際にファイルから読み込めるデータのフレーム数DataFrames。
        /// 奇数個のことがある。
        /// </summary>
        private long mDataFrames;

        /// <summary>
        /// 1フレーム=16ビット(2バイト) x チャンネル数とする。
        /// 出力するPCMデータのフレーム数OutputFramesは必ず偶数個にする。
        /// </summary>
        public long OutputFrames { get; set; }

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

        /// <summary>
        /// stream data offset from the start of the file
        /// </summary>
        private const int STREAM_DATA_OFFSET = 92;

        private ulong mMetadataOffset;

        /// <summary>
        /// サンプル数＝データサイズ(bit) ÷ チャンネル数
        /// </summary>
        private long mSampleCount;

        /// <summary>
        /// サンプル数＝データサイズ(bit) ÷ チャンネル数
        /// </summary>
        public long SampleCount { get { return mSampleCount; } }
        
        private int  mBlockSizePerChannel;

        /// <summary>
        /// DSF DATAチャンクのバイト数。
        /// </summary>
        private long mDataBytes;

        private PcmDataLib.ID3Reader mId3Reader = new PcmDataLib.ID3Reader();

        private ResultType ReadDsfChunk(BinaryReader br) {
            byte[] ckID = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(ckID, 0, "DSD ")) {
                return ResultType.NotDsf;
            }

            ulong chunkBytes = br.ReadUInt64();
            if (28 != chunkBytes) {
                return ResultType.NotSupportDsdChunkBytes;
            }

            ulong totalFileBytes = br.ReadUInt64();

            mMetadataOffset = br.ReadUInt64();

            return ResultType.Success;
        }

        private ResultType ReadFmtChunk(BinaryReader br) {
            byte[] ckID = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(ckID, 0, "fmt ")) {
                return ResultType.NotFoundFmtHeader;
            }

            ulong chunkBytes   = br.ReadUInt64();
            uint formatVersion = br.ReadUInt32();
            uint formatId      = br.ReadUInt32();
            uint channelType   = br.ReadUInt32();
            NumChannels        = (int)br.ReadUInt32();

            SampleRate           = (int)br.ReadUInt32();
            uint bitsPerSample   = br.ReadUInt32();
            mSampleCount         = br.ReadInt64();
            mBlockSizePerChannel = br.ReadInt32();
            uint reserved        = br.ReadUInt32();

            if (52 != chunkBytes) {
                return ResultType.NotSupportDsfVersion;
            }

            if (1 != formatVersion) {
                return ResultType.NotSupportDsfVersion;
            }

            if (0 != formatId) {
                return ResultType.NotSupportDsfFormatId;
            }

            if (NumChannels < 1) {
                return ResultType.NotSupportNumChannels;
            }

            if (0 == SampleRate ||
                    (0 != (SampleRate % 2822400) &&
                     0 != (SampleRate % 3072000))) {
                return ResultType.NotSupportSampleFrequency;
            }

            if (1 != bitsPerSample) {
                return ResultType.NotSupportBitsPerSample;
            }

            if (mSampleCount <= 0) {
                return ResultType.NotSupportFileTooLarge;
            }

            if (4096 != mBlockSizePerChannel) {
                return ResultType.NotSupportBlockSize;
            }

            mDataFrames = mSampleCount / 16;

            return ResultType.Success;
        }

        private ResultType ReadDataChunkHeader(BinaryReader br) {
            byte[] ckID = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(ckID, 0, "data")) {
                return ResultType.NotDsf;
            }

            mDataBytes = br.ReadInt64() - 12;
            if (mDataBytes <= 0) {
                return ResultType.NotSupportFileTooLarge;
            }

            return ResultType.Success;
        }

        private ResultType ReadID3Chunk(BinaryReader br) {
            var id3r = mId3Reader.Read(br);

            ResultType result = ResultType.Success;
            switch (id3r) {
            case PcmDataLib.ID3Reader.ID3Result.ReadError:
                result = ResultType.ReadError;
                break;
            case PcmDataLib.ID3Reader.ID3Result.NotSupportedID3version:
                // ID3が読めなくても再生はできるようにする。
                result = ResultType.Success;
                break;
            case PcmDataLib.ID3Reader.ID3Result.Success:
                result = ResultType.Success;
                break;
            default:
                // 追加忘れ
                System.Diagnostics.Debug.Assert(false);
                result = ResultType.ReadError;
                break;
            }
            return result;
        }

        enum ReadHeaderMode {
            AllHeadersWithID3,
            ReadStopBeforeSoundData,
        };

        private ResultType ReadHeader1(BinaryReader br, out PcmDataLib.PcmData pcmData, ReadHeaderMode mode) {
            pcmData = new PcmDataLib.PcmData();

            ResultType result = ReadDsfChunk(br);
            if (result != ResultType.Success) {
                return result;
            }
            
            result = ReadFmtChunk(br);
            if (ResultType.Success != result) {
                return result;
            }

            result = ReadDataChunkHeader(br);
            if (ResultType.Success != result) {
                return result;
            }

            // 読み込めるデータのフレーム数DataFramesと出力するデータのフレーム数OutputFrames。
            // PCMデータのフレーム数OutputFramesは偶数個にする。
            OutputFrames = mDataFrames;
            if (0 != (1 & OutputFrames)) {
                // OutputFrames must be even number
                ++OutputFrames;
            }

            pcmData.SampleDataType = PcmDataLib.PcmData.DataType.DoP;
            pcmData.SetFormat(
                NumChannels,
                24,
                24,
                SampleRate/16,
                PcmDataLib.PcmData.ValueRepresentationType.SInt,
                OutputFrames);

            if (mode == ReadHeaderMode.AllHeadersWithID3 &&
                mMetadataOffset != 0) {
                PcmDataLib.Util.BinaryReaderSkip(br, (long)mMetadataOffset - STREAM_DATA_OFFSET);

                result = ReadID3Chunk(br);
                if (ResultType.Success == result) {
                    // ID3読み込み成功
                    pcmData.DisplayName = TitleName;
                    pcmData.AlbumTitle = AlbumName;
                    pcmData.ArtistName = ArtistName;
                    if (0 < PictureBytes) {
                        pcmData.SetPicture(PictureBytes, PictureData);
                    }
                }
            }

            return 0;
        }

        public ResultType ReadHeader(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            return ReadHeader1(br, out pcmData, ReadHeaderMode.AllHeadersWithID3);
        }

        // 1フレームは
        // DSFファイルを読み込む時 16ビット x チャンネル数
        // DoPデータとしては 24ビット x チャンネル数
        private long mPosFrame;

        public ResultType ReadStreamBegin(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            ResultType rt = ResultType.Success;
            rt = ReadHeader1(br, out pcmData, ReadHeaderMode.ReadStopBeforeSoundData);
            mPosFrame = 0;

            return rt;
        }

        /// <summary>
        /// フレーム指定でスキップする。
        /// </summary>
        /// <param name="skipFrames">スキップするフレーム数。負の値は指定できない。</param>
        /// <returns>実際にスキップできたフレーム数。</returns>
        public long ReadStreamSkip(BinaryReader br, long skipFrames) {
            if (skipFrames < 0) {
                System.Diagnostics.Debug.Assert(false);
            }
            if (0 != (skipFrames & (mBlockSizePerChannel * NumChannels - 1))) {
                // 4096 x NumChannelsの倍数でなければならぬ。
                System.Diagnostics.Debug.Assert(false);
            }

            if (mDataFrames < mPosFrame + skipFrames) {
                // 最後に移動。
                skipFrames = mDataFrames - mPosFrame;
            }
            if (skipFrames == 0) {
                return 0;
            }

            // DSFの1フレーム=16ビット(2バイト) x チャンネル数
            PcmDataLib.Util.BinaryReaderSkip(br, skipFrames * 2 * NumChannels);
            mPosFrame += skipFrames;
            return skipFrames;
        }

        private static readonly byte[] mBitReverseTable = {
            0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0,
            0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
            0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8,
            0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
            0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4,

            0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
            0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec,
            0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
            0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
            0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,

            0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea,
            0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
            0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6,
            0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
            0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee,

            0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
            0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1,
            0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
            0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9,
            0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,

            0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5,
            0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
            0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed,
            0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
            0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3,

            0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
            0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
            0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
            0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7,
            0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,

            0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef,
            0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff
        };

        /// <summary>
        /// preferredFramesフレームぐらい読み出す。
        /// 1Mフレームぐらいにすると効率が良い。
        /// </summary>
        /// <returns>読みだしたフレーム</returns>
        public byte[] ReadStreamReadOne(BinaryReader br, int preferredFrames) {
            bool appendLastFrame = false;
            int readFrames = preferredFrames;
            if (mDataFrames < mPosFrame + readFrames) {
                readFrames = (int)(mDataFrames - mPosFrame);
                if (mDataFrames != OutputFrames) {
                    // ファイルの最後まで読み込む場合で、フレーム数が奇数の時
                    // フレーム数が偶数になるように1フレーム水増しする。
                    appendLastFrame = true;
                }
            }

            if (readFrames <= 0) {
                // 1バイトも読めない。
                // N.B. ReadStreamReadOne()が、DataFrames番目==(OutputFrames-1)番目のフレーム「だけ」を取得しようとすることはない。
                return new byte[0];
            }

            // DoPの1フレーム == 24bit * NumChannels
            int streamBytes         = 3 * NumChannels *  readFrames;
            byte [] stream = new byte[3 * NumChannels * (readFrames + (appendLastFrame ? 1:0))];

            int blockBytes = mBlockSizePerChannel * NumChannels;
            int blockNum = (int)((streamBytes + blockBytes - 1) / blockBytes);

            int writePos = 0;
            for (int block = 0; block < blockNum; ++block) {
                // data is stored in following order:
                // L channel 4096bytes consecutive data, R channel 4096bytes consecutive data, L channel 4096bytes consecutive data, ...
                //
                // read 4096 x numChannels bytes.
                byte [] blockData = br.ReadBytes(blockBytes);
                mPosFrame += blockData.Length / 2 / NumChannels;

                for (int i=0; i < mBlockSizePerChannel / 2; ++i) {
                    for (int ch=0; ch < NumChannels; ++ch) {
                        stream[writePos + 0] = mBitReverseTable[blockData[i * 2 + 1 + ch * mBlockSizePerChannel]];
                        stream[writePos + 1] = mBitReverseTable[blockData[i * 2 + 0 + ch * mBlockSizePerChannel]];
                        stream[writePos + 2] = (byte)(0 != (i & 1) ? 0xfa : 0x05);
                        writePos += 3;
                        if (streamBytes <= writePos) {
                            // recorded sample is ended on part of the way of the block
                            break;
                        }
                    }
                    if (streamBytes <= writePos) {
                        // recorded sample is ended on part of the way of the block
                        break;
                    }
                }
                if (streamBytes <= writePos) {
                    // recorded sample is ended on part of the way of the block
                    break;
                }
            }

            if (appendLastFrame) {
                for (int ch=0; ch < NumChannels; ++ch) {
                    stream[writePos + 0] = stream[writePos - NumChannels * 3 + 0];
                    stream[writePos + 1] = stream[writePos - NumChannels * 3 + 1];
                    stream[writePos + 2] = 0xfa;
                    writePos += 3;
                }
            }

            return stream;
        }

        public void ReadStreamEnd() {
            mPosFrame = 0;
        }
    }
}
