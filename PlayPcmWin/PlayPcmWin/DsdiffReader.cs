// 日本語UTF-8

using System;
using System.IO;
using System.Threading.Tasks;

namespace PlayPcmWin {

    class DsdiffReader {
        public enum ResultType {
            Success,
            NotDsf,
            HeaderError,
            NotSupportFormatType,
            FormVersionChunkSizeError,
            PropertyChunkSizeError,
            NotSupportPropertyType,
            SampleRateChunkSizeError,
            NotSupportSampleRate,
            ChannelsChunkSizeError,
            CompressionTypeChunkSizeError,
            NotSupportCompressionType,
            NotSupportFormatVersion,
            NotSupportFileTooLarge,
            NotSupportNumChannels,
            NotSupportSampleFrequency,
            NotSupportID3version,
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

        /// <summary>
        /// stream data offset from the start of the file
        /// </summary>
        private const int STREAM_DATA_OFFSET = 92;

        private PcmDataLib.ID3Reader mId3Reader = new PcmDataLib.ID3Reader();

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

        private static ResultType ReadDsdChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes < 4 || 0x7fffffff < chunkBytes) {
                return ResultType.NotSupportFileTooLarge;
            }

            byte[] formType = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(formType, 0, "DSD ")) {
                return ResultType.NotSupportFormatType;
            }

            return ResultType.Success;
        }

        private static ResultType ReadFormVersionChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes != 4) {
                return ResultType.FormVersionChunkSizeError;
            }

            uint version = Util.ReadBigU32(br);
            if (0x01000000 != (version & 0xff000000)) {
                return ResultType.NotSupportFormatVersion;
            }

            return ResultType.Success;
        }

        private static ResultType ReadPropertyChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes < 4) {
                return ResultType.PropertyChunkSizeError;
            }

            byte[] propType = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(propType, 0, "SND ")) {
                return ResultType.NotSupportPropertyType;
            }

            return ResultType.Success;
        }

        private ResultType ReadSampleRateChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes != 4) {
                return ResultType.SampleRateChunkSizeError;
            }

            SampleRate = (int)Util.ReadBigU32(br);
            if (0 == SampleRate ||
                    (0 != (SampleRate % 2822400) &&
                     0 != (SampleRate % 3072000))) {
                return ResultType.NotSupportSampleRate;
            }

            return ResultType.Success;
        }

        private ResultType ReadChannelsChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes < 6) {
                return ResultType.ChannelsChunkSizeError;
            }

            NumChannels = (int)Util.ReadBigU16(br);

            // skip channel ID's
            PcmDataLib.Util.BinaryReaderSkip(br, (long)(chunkBytes - 2));

            return ResultType.Success;
        }

        private static ResultType ReadCompressionTypeChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes < 5) {
                return ResultType.CompressionTypeChunkSizeError;
            }

            byte[] propType = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(propType, 0, "DSD ")) {
                return ResultType.NotSupportCompressionType;
            }

            // skip compression name
            PcmDataLib.Util.BinaryReaderSkip(br, (int)(chunkBytes - 4+1) & (~1));

            return ResultType.Success;
        }

        private ResultType ReadSoundDataChunkHeader(BinaryReader br, ReadHeaderMode mode) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes == 0 || 0x7fffffff < chunkBytes) {
                return ResultType.NotSupportFileTooLarge;
            }

            mDataFrames = (long)chunkBytes / 2 / NumChannels;

            switch (mode) {
            case ReadHeaderMode.AllHeadersWithID3:
                // skip dsd data
                PcmDataLib.Util.BinaryReaderSkip(br, (long)chunkBytes);
                break;
            case ReadHeaderMode.ReadStopBeforeSoundData:
                break;
            }

            return ResultType.Success;
        }

        private ResultType ReadID3Chunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes == 0 || 0x7fffffff < chunkBytes) {
                return ResultType.NotSupportFileTooLarge;
            }
            
            var id3r = mId3Reader.Read(br);

            ResultType result = ResultType.Success;
            switch (id3r) {
            case PcmDataLib.ID3Reader.ID3Result.ReadError:
                result = ResultType.ReadError;
                break;
            case PcmDataLib.ID3Reader.ID3Result.NotSupportedID3version:
                // ID3が読めなくても再生はできるようにする。
                result = ResultType.Success;
                PcmDataLib.Util.BinaryReaderSkip(br, (long)chunkBytes - mId3Reader.ReadBytes);
                break;
            case PcmDataLib.ID3Reader.ID3Result.Success:
                result = ResultType.Success;
                PcmDataLib.Util.BinaryReaderSkip(br, (long)chunkBytes - mId3Reader.ReadBytes);
                break;
            default:
                // 追加忘れ
                System.Diagnostics.Debug.Assert(false);
                result = ResultType.ReadError;
                break;
            }
            return result;
        }

        private static ResultType SkipUnknownChunk(BinaryReader br) {
            ulong chunkBytes = Util.ReadBigU64(br);
            if (chunkBytes == 0 || 0x7fffffff < chunkBytes) {
                return ResultType.NotSupportFileTooLarge;
            }

            // skip
            PcmDataLib.Util.BinaryReaderSkip(br, (int)(chunkBytes+1) & (~1));

            return ResultType.Success;
        }

        const int FOURCC_FRM8 = 0x384d5246;
        const int FOURCC_FVER = 0x52455646;
        const int FOURCC_PROP = 0x504f5250;
        const int FOURCC_FS   = 0x20205346;
        const int FOURCC_SND  = 0x20444e53;
        const int FOURCC_CHNL = 0x4c4e4843;
        const int FOURCC_CMPR = 0x52504d43;
        const int FOURCC_DSD  = 0x20445344;
        const int FOURCC_ID3  = 0x20334449;

        enum ReadHeaderMode {
            AllHeadersWithID3,
            ReadStopBeforeSoundData,
        };

        private ResultType ReadHeader1(BinaryReader br, ReadHeaderMode mode, out PcmDataLib.PcmData pcmData) {
            pcmData = new PcmDataLib.PcmData();
            bool done = false;

            try {
                while (!done) {
                    ResultType rt = ResultType.Success;
                    uint fourCC = br.ReadUInt32();
                    switch (fourCC) {
                    case FOURCC_FRM8:
                        rt = ReadDsdChunk(br);
                        break;
                    case FOURCC_FVER:
                        rt = ReadFormVersionChunk(br);
                        break;
                    case FOURCC_PROP:
                        rt = ReadPropertyChunk(br);
                        break;
                    case FOURCC_FS:
                        rt = ReadSampleRateChunk(br);
                        break;
                    case FOURCC_CHNL:
                        rt = ReadChannelsChunk(br);
                        break;
                    case FOURCC_CMPR:
                        rt = ReadCompressionTypeChunk(br);
                        break;
                    case FOURCC_DSD:
                        rt = ReadSoundDataChunkHeader(br, mode);
                        switch (mode) {
                        case ReadHeaderMode.ReadStopBeforeSoundData:
                            done = true;
                            break;
                        case ReadHeaderMode.AllHeadersWithID3:
                            break;
                        }
                        break;
                    case FOURCC_ID3:
                        rt = ReadID3Chunk(br);
                        break;
                    default:
                        rt = SkipUnknownChunk(br);
                        break;
                    }
                    if (rt != ResultType.Success) {
                        return rt;
                    }
                }
            } catch (EndOfStreamException ex) {
                // this is only way to exit from the while loop above
                System.Console.WriteLine(ex);
            }

            if (0 == SampleRate ||   // SampleRateChunkが存在しないとき。
                0 == mDataFrames) {
                return ResultType.ReadError;
            }

            if (NumChannels < 1) {
                return ResultType.NotSupportNumChannels;
            }

            // 読み込めるデータのフレーム数DataFramesと出力するデータのフレーム数OutputFrames。
            // PCMデータのフレーム数OutputFramesは偶数個にする。
            OutputFrames = mDataFrames;
            if (0 != (1 & OutputFrames)) {
                // OutputFrames must be even number
                ++OutputFrames;
            }

            pcmData.SetFormat(
                NumChannels,
                24,
                24,
                SampleRate/16,
                PcmDataLib.PcmData.ValueRepresentationType.SInt,
                OutputFrames);
            pcmData.SampleDataType = PcmDataLib.PcmData.DataType.DoP;

            if (null != TitleName) {
                pcmData.DisplayName = TitleName;
            }
            if (null != AlbumName) {
                pcmData.AlbumTitle = AlbumName;
            }
            if (null != ArtistName) {
                pcmData.ArtistName = ArtistName;
            }
            if (0 < PictureBytes) {
                pcmData.SetPicture(PictureBytes, PictureData);
            }

            return 0;
        }

        public ResultType ReadHeader(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            return ReadHeader1(br, ReadHeaderMode.AllHeadersWithID3, out pcmData);
        }

        // 1フレームは
        // DFFファイルを読み込む時 16ビット x チャンネル数
        // DoPデータとしては 24ビット x チャンネル数
        private long mPosFrame;

        public ResultType ReadStreamBegin(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            ResultType rt = ResultType.Success;
            rt = ReadHeader1(br, ReadHeaderMode.ReadStopBeforeSoundData, out pcmData);
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

            if (mDataFrames < mPosFrame + skipFrames) {
                // 最後に移動。
                skipFrames = mDataFrames - mPosFrame;
            }
            if (skipFrames == 0) {
                return 0;
            }

            // DSDIFFの1フレーム=16ビット(2バイト) x チャンネル数
            PcmDataLib.Util.BinaryReaderSkip(br, skipFrames * 2 * NumChannels);
            mPosFrame += skipFrames;
            return skipFrames;
        }

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
                    // フレーム数が偶数になるように水増しする。
                    appendLastFrame = true;
                }
            }

            if (readFrames == 0) {
                // 1バイトも読めない。
                // N.B. ReadStreamReadOne()が、DataFrames番目==(OutputFrames-1)番目のフレーム「だけ」を取得しようとすることはない。
                return new byte[0];
            }

            // DoPの1フレーム == 24bit * NumChannels
            int streamBytes         = 3 * NumChannels  * readFrames;
            byte [] stream = new byte[3 * NumChannels * (readFrames + (appendLastFrame ? 1 : 0))];

            int writePos = 0;
            for (int i=0; i < readFrames; ++i) {
                byte [] dsdData = br.ReadBytes(NumChannels * 2);
                for (int ch=0; ch < NumChannels; ++ch) {
                    stream[writePos + 0] = dsdData[ch + NumChannels];
                    stream[writePos + 1] = dsdData[ch];
                    stream[writePos + 2] = (byte)(0 != (i & 1) ? 0xfa : 0x05);
                    writePos += 3;
                }
            }
            mPosFrame += readFrames;

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
