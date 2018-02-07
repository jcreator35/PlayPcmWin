using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using WWUtil;
using System.Diagnostics;

namespace WavRWLib2 {
    public class WavReader {
        class DataSubChunk {
            public uint ChunkSize { get; set; }

            private LargeArray<byte> mRawData;

            /// <summary>
            /// ファイル先頭から、このデータチャンクのPCMデータ先頭までのオフセット
            /// </summary>
            public long Offset { get; set; }
            public long NumFrames { get; set; }

            public LargeArray<byte> GetSampleLargeArray() {
                return mRawData;
            }

            /// <summary>
            /// dataチャンクのヘッダ部分だけを読む。
            /// 4バイトしか進まない。
            /// </summary>
            public long ReadDataChunkHeader(BinaryReader br, long offset, byte[] fourcc, int numChannels, int bitsPerSample) {
                if (!PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "data")) {
                    System.Diagnostics.Debug.Assert(false);
                    return 0;
                }

                ChunkSize = br.ReadUInt32();

                Offset = offset + 4;

                int frameBytes = bitsPerSample / 8 * numChannels;
                NumFrames = ChunkSize / frameBytes;

                mRawData = null;
                return 4;
            }

            /// <summary>
            /// PCMデータを無加工で読み出す。
            /// </summary>
            /// <param name="startFrame">0を指定すると最初から。</param>
            /// <param name="endFrame">負の値を指定するとファイルの最後まで。</param>
            /// <returns>false: ファイルの読み込みエラーなど</returns>
            public bool ReadDataChunkData(BinaryReader br, int numChannels, int bitsPerSample,
                long startFrame, long endFrame) {
                br.BaseStream.Seek(Offset, SeekOrigin.Begin);

                // endBytesがファイルの終わり指定(負の値)の場合の具体的位置を設定する。
                // startBytesとendBytesがファイルの終わり以降を指していたら修正する。
                // ・endBytesがファイルの終わり以降…ファイルの終わりを指す。
                // ・startBytesがファイルの終わり以降…サイズ0バイトのWAVファイルにする。

                int frameBytes = bitsPerSample / 8 * numChannels;
                long startBytes = startFrame * frameBytes;
                long endBytes   = endFrame * frameBytes;

                System.Diagnostics.Debug.Assert(0 <= startBytes);

                if (endBytes < 0 ||
                    (NumFrames * frameBytes) < endBytes) {
                    // 終了位置はファイルの終わり。
                    endBytes = NumFrames * frameBytes;
                }

                long newNumFrames = (endBytes - startBytes) / frameBytes;
                if (newNumFrames <= 0 ||
                    NumFrames * frameBytes <= startBytes ||
                    endBytes <= startBytes) {
                    // サイズが0バイトのWAV。
                    mRawData = null;
                    NumFrames = 0;
                    return true;
                }

                if (0 < startBytes) {
                    PcmDataLib.Util.BinaryReaderSkip(br, startBytes);
                }

                mRawData = new LargeArray<byte>(newNumFrames * frameBytes);
                int fragmentBytes = 1048576;
                for (long pos=0; pos<mRawData.LongLength; pos += fragmentBytes) {
                    int bytes = fragmentBytes;
                    if (mRawData.LongLength - pos < bytes) {
                        bytes = (int)(mRawData.LongLength-pos);
                    }
                    var buff = br.ReadBytes(bytes);
                    mRawData.CopyFrom(buff, 0, pos, bytes);
                }
                NumFrames = newNumFrames;
                return true;
            }
        }

        private List<DataSubChunk>  mDscList = new List<DataSubChunk>();
        private PcmDataLib.ID3Reader mId3Reader = new PcmDataLib.ID3Reader();

        /// <summary>
        /// RIFFチャンクサイズはあてにならない。
        /// </summary>
        public long RiffChunkSize { get; set; }

        public int NumChannels { get; set; }
        public int SampleRate { get; set; }

        public PcmDataLib.PcmData.ValueRepresentationType SampleValueRepresentationType { get; set; }

        public int BitsPerSample { get; set; }
        public int ValidBitsPerSample { get; set; }
        public long ChannelMask { get; set; }

        public string Title { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }

        /// <summary>
        /// 画像データバイト数(無いときは0)
        /// </summary>
        public int PictureBytes { get { return mId3Reader.PictureBytes; } }

        /// <summary>
        /// 画像データ
        /// </summary>
        public byte[] PictureData { get { return mId3Reader.PictureData; } }

        /// <summary>
        /// ReadHeader()またはReadHeaderAndSamples()でエラー発生時セットされる
        /// </summary>
        public string ErrorReason { get; set; }

        public long Ds64RiffSize { get; set; }
        public long Ds64DataSize { get; set; }
        public long Ds64SampleCount { get; set; }
        private List<long> mDs64Table;

        private enum ReadMode {
            HeaderAndPcmData,
            OnlyHeader
        }

        // 大体100テラバイトぐらい
        const long INT64_DATA_SIZE_LIMIT = 0x0000ffffffffffffL;

        public long ReadRiffChunk(BinaryReader br, byte[] chunkId) {
            if (!PcmDataLib.Util.FourCCHeaderIs(chunkId, 0, "RIFF") &&
                    !PcmDataLib.Util.FourCCHeaderIs(chunkId, 0, "RF64")) {
                // 起こらない。
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }

            RiffChunkSize = br.ReadUInt32();
            if (RiffChunkSize < 36) {
                ErrorReason = string.Format("E: RIFF chunkSize is too small {0}.", RiffChunkSize);
                return 0;
            }

            byte[] format = br.ReadBytes(4);
            if (!PcmDataLib.Util.FourCCHeaderIs(format, 0, "WAVE")) {
                ErrorReason = string.Format("E: RiffChunkDescriptor.format mismatch. \"{0}{1}{2}{3}\" should be \"WAVE\"",
                        (char)format[0], (char)format[1], (char)format[2], (char)format[3]);
                return 0;
            }

            return 8;
        }

        public int FmtSubChunkSize { get; set; }

        private long ReadFmtChunk(BinaryReader br, byte[] fourcc) {
            if (!PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "fmt ")) {
                // 起こらない。
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }

            uint subChunk1Size = br.ReadUInt32();
            FmtSubChunkSize = (int)subChunk1Size;

            if (18 <= subChunk1Size) {
                // extensible領域がある。
                // subChunk1Size==18でextensibleSize==0のWAVファイルを見たことがある。
                // subChunk1Size==40でextensibleSize==22のときWAVEFORMATEXTENSIBLE。
            } else if (14 != subChunk1Size && 16 != subChunk1Size) {
                ErrorReason = string.Format("E: FmtSubChunk.ReadRiffChunk() subChunk1Size={0} this file type is not supported", subChunk1Size);
                return 0;
            }

            ushort audioFormat = br.ReadUInt16();
            if (1 == audioFormat) {
                // WAVE_FORMAT_PCM
                SampleValueRepresentationType = PcmDataLib.PcmData.ValueRepresentationType.SInt;
            } else if (3 == audioFormat) {
                SampleValueRepresentationType = PcmDataLib.PcmData.ValueRepresentationType.SFloat;
            } else if (0xfffe == audioFormat) {
                // WAVEFORMATEXTENSIBLE == 0xfffe
            } else {
                ErrorReason = string.Format("E: non PCM format {0}. Cannot read this wav file.", audioFormat);
                return 0;
            }

            NumChannels = br.ReadUInt16();
            uint sampleRate = br.ReadUInt32();
            uint byteRate = br.ReadUInt32();
            ushort blockAlign = br.ReadUInt16();

            if (16 <= subChunk1Size) {
                BitsPerSample = br.ReadUInt16();
            } else {
                BitsPerSample = blockAlign * 8 / NumChannels;
            }

            if (BitsPerSample == 20) {
                // WaveLab creates such WAVE file. (WAVEFORMAT structure with BitsPerSample==20 and apparently 1 sample==3bytes)
                BitsPerSample = 24;
                ValidBitsPerSample = 20;
            } else {
                // WAVEFORMATEXの場合、ValidBitsPerSampleはここで確定する。
                // WAVEFORMATEXTENSIBLEの場合、真のValidBitsPerSampleが20行後に判明する。
                ValidBitsPerSample = BitsPerSample;
            }

            if (Int32.MaxValue <= SampleRate) {
                ErrorReason = string.Format("E: Sample rate is too large {0}", SampleRate);
                return 0;
            }
            SampleRate = (int)sampleRate;

            int extensibleSize = 0;
            if (18 <= subChunk1Size) {
                // cbSize 2bytes
                extensibleSize = br.ReadUInt16();
            }

            if (22 == extensibleSize && 40 <= subChunk1Size) {
                // WAVEFORMATEXTENSIBLE(22 bytes)
                // WAVEFORMATex == 18バイト
                // 計40バイト。

                ValidBitsPerSample = br.ReadUInt16();
                if (ValidBitsPerSample == 0) {
                    // Adobe Audition CS5.5
                    ValidBitsPerSample = BitsPerSample;
                }

                ChannelMask = br.ReadUInt32();
                var formatGuid = br.ReadBytes(16);

                var pcmGuid   = Guid.Parse("00000001-0000-0010-8000-00aa00389b71");
                var pcmGuidByteArray = pcmGuid.ToByteArray();
                var floatGuid = Guid.Parse("00000003-0000-0010-8000-00aa00389b71");
                var floatGuidByteArray = floatGuid.ToByteArray();
                if (pcmGuidByteArray.SequenceEqual(formatGuid)) {
                    SampleValueRepresentationType = PcmDataLib.PcmData.ValueRepresentationType.SInt;
                } else if (floatGuidByteArray.SequenceEqual(formatGuid)) {
                    SampleValueRepresentationType = PcmDataLib.PcmData.ValueRepresentationType.SFloat;
                } else {
                    ErrorReason = string.Format("E: FmtSubChunk.ReadRiffChunk() unknown format guid on WAVEFORMATEX.SubFormat.\r\n{3:X2}{2:X2}{1:X2}{0:X2}-{5:X2}{4:X2}-{7:X2}{6:X2}-{9:X2}{8:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}",
                            formatGuid[0], formatGuid[1], formatGuid[2], formatGuid[3],
                            formatGuid[4], formatGuid[5], formatGuid[6], formatGuid[7],
                            formatGuid[8], formatGuid[9], formatGuid[10], formatGuid[11],
                            formatGuid[12], formatGuid[13], formatGuid[14], formatGuid[15]);
                    return 0;
                }
            } else {
                if (18 < subChunk1Size) {
                    // 知らないextensibleSize。
                    // extensibleSizeを信用せず、subChunk1Sizeを使用してfmtチャンクをスキップする。
                    // この場合fmtチャンクを既に18バイト読み込んでいるので
                    // スキップするバイト数はsubChunk1Size - 18。2の倍数に繰り上げる。
                    int skipBytes = (int)((subChunk1Size - 18 + 1) & (~1L));
                    PcmDataLib.Util.BinaryReaderSkip(br, skipBytes);
                }
            }

            if (byteRate != SampleRate * NumChannels * BitsPerSample / 8) {
                ErrorReason = string.Format("E: byteRate has wrong value {0}. File corrupted. Can not continue read this file\n", byteRate);
                return 0;
            }

            if (blockAlign != NumChannels * BitsPerSample / 8) {
                ErrorReason = string.Format("E: blockAlign has wrong value {0}. File corrupted. Can not continue read this file\n", blockAlign);
                return 0;
            }

            return subChunk1Size + 4;
        }

        private long ReadDS64Chunk(BinaryReader br) {
            // chunkIdは、既に読んでいる。

            uint chunkSize = br.ReadUInt32();
            if (chunkSize < 0x1c) {
                ErrorReason = string.Format("E: ds64 chunk too small. {0}.", chunkSize);
                return 0;
            }
            Ds64RiffSize = br.ReadInt64();
            Ds64DataSize = br.ReadInt64();
            Ds64SampleCount = br.ReadInt64();

            if (Ds64RiffSize < 0 || INT64_DATA_SIZE_LIMIT < Ds64RiffSize ||
                    Ds64DataSize < 0 || INT64_DATA_SIZE_LIMIT < Ds64DataSize ||
                    Ds64SampleCount < 0 || INT64_DATA_SIZE_LIMIT < Ds64SampleCount) {
                ErrorReason = string.Format("E: DS64 content size info too large to handle. RiffSize={0}, DataSize={1}, SampleCount={2}",
                        Ds64RiffSize, Ds64DataSize, Ds64SampleCount);
                return 0;
            }

            mDs64Table = new List<long>();
            uint tableLength = br.ReadUInt32();
            for (uint i=0; i < tableLength; ++i) {
                // 何に使うのかわからないが、取っておく。
                byte[] id = br.ReadBytes(4);
                long v = br.ReadInt64();
                mDs64Table.Add(v);
            }

            // chunkSize情報自体のサイズ4バイトを足す
            return chunkSize + 4;
        }

        /// <summary>
        /// StartTickとEndTickを見て、DSCヘッダ以降の必要な部分だけ読み込む。
        /// </summary>
        private bool ReadPcmDataInternal(BinaryReader br, long startFrame, long endFrame) {
            if (startFrame < 0) {
                // データ壊れ。先頭を読む。
                startFrame = 0;
            }

            if (0 <= endFrame && endFrame < startFrame) {
                // 1サンプルもない。
                startFrame = endFrame;
            }

            if (mDscList.Count != 1) {
                // この読み込み方法は、複数個のデータチャンクが存在する場合には対応しない。
                ErrorReason = string.Format("multi data chunk wav is not supported.");
                return false;
            }

            return mDscList[0].ReadDataChunkData(br, NumChannels, BitsPerSample, startFrame, endFrame);
        }

        private long SkipUnknownChunk(BinaryReader br, byte[] fourcc) {
            // Console.WriteLine("D: SkipUnknownChunk skip \"{0}{1}{2}{3}\"", (char)fourcc[0], (char)fourcc[1], (char)fourcc[2], (char)fourcc[3]);

            long chunkSize = br.ReadUInt32();
            if (chunkSize == 0) {
                ErrorReason = string.Format("E: SkipUnknownChunk chunk \"{0}{1}{2}{3}\" chunkSize={4} File corrupted?",
                    (char)fourcc[0], (char)fourcc[1], (char)fourcc[2], (char)fourcc[3], chunkSize);
                return 0;
            }
            // PADの処理。chunkSizeが奇数の場合、1バイト読み進める。
            long skipBytes = (chunkSize + 1) & (~1L);
            PcmDataLib.Util.BinaryReaderSkip(br, skipBytes);
            return skipBytes + 4;
        }

        private long ReadId3Chunk(BinaryReader br) {
            uint headerBytes = br.ReadUInt32();
            if (headerBytes < 4) {
                ErrorReason = string.Format("ID3 header size is too short {0}", headerBytes);
                return 0;
            }
            if (0x7ffffff0 < headerBytes) {
                ErrorReason = string.Format("ID3 header size is too large {0}", headerBytes);
                return 0;
            }
            int readBytes = (int)((headerBytes + 1) & (~1L));

            byte[] data = br.ReadBytes(readBytes);

            mId3Reader.Read(new BinaryReader(new MemoryStream(data)));
            if (mId3Reader.TitleName != null && 0 < mId3Reader.TitleName.Length) {
                Title = mId3Reader.TitleName;
            }
            if (mId3Reader.AlbumName != null && 0 < mId3Reader.AlbumName.Length) {
                AlbumName = mId3Reader.AlbumName;
            }
            if (mId3Reader.ArtistName != null && 0 < mId3Reader.ArtistName.Length) {
                ArtistName = mId3Reader.ArtistName;
            }

            return 4 + readBytes;
        }

        private long ReadListChunk(BinaryReader br) {
            uint listHeaderBytes = br.ReadUInt32();
            if (listHeaderBytes < 4) {
                ErrorReason = string.Format("E: LIST header size is too short {0}", listHeaderBytes);
                return 0;
            }
            if (0x7ffffff0 < listHeaderBytes) {
                ErrorReason = string.Format("E: LIST header size is too large {0}", listHeaderBytes);
                return 0;
            }

            byte[] data = br.ReadBytes((int)listHeaderBytes);
            long result = 4 + listHeaderBytes;
            if (1 == (listHeaderBytes & 1)) {
                // PADの処理。listHeaderBytesが奇数の場合、1バイト読み進める
                br.ReadBytes(1);
                ++result;
            }

            if (!PcmDataLib.Util.FourCCHeaderIs(data, 0, "INFO")) {
                // これはエラーでない。チャンクをスキップして続行する。
                Console.WriteLine("D: LIST header does not follows INFO. skipped.");
                return result;
            }

            int pos = 4;
            while (pos + 8 < data.Length) {
                if (data[pos + 7] != 0) {
                    // これはエラーでない。チャンクをスキップして続行する。
                    Console.WriteLine("D: LIST header contains very long text. parse aborted.");
                    return result;
                }
                int bytes = data[pos + 4] + 256 * data[pos + 5]+ 65536 * data[pos + 6];
                if (0 < bytes) {
                    if (PcmDataLib.Util.FourCCHeaderIs(data, pos, "INAM")) {
                        Title = JapaneseTextByteArrayToString(data, pos + 8, bytes);
                    }
                    if (PcmDataLib.Util.FourCCHeaderIs(data, pos, "IART")) {
                        ArtistName = JapaneseTextByteArrayToString(data, pos + 8, bytes);
                    }
                    if (PcmDataLib.Util.FourCCHeaderIs(data, pos, "IPRD")) {
                        AlbumName = JapaneseTextByteArrayToString(data, pos + 8, bytes);
                    }
                }
                pos += 8 + ((bytes + 1) & (~1));
            }
            return result;
        }

        private static string JapaneseTextByteArrayToString(byte[] bytes, int index, int count) {
            string result = "不明";

            // 最後の'\0'を削る
            while (0 < count && bytes[index + count - 1] == 0) {
                --count;
            }
            if (0 == count) {
                return result;
            }

            var part = new byte[count];
            Buffer.BlockCopy(bytes, index, part, 0, count);

            var encoding = JCodeInspect.DetectEncoding(part);
            if (encoding == System.Text.Encoding.GetEncoding(932)) {
                // SJIS
                result = System.Text.Encoding.GetEncoding(932).GetString(part);
            } else {
                // UTF-8
                result = System.Text.Encoding.UTF8.GetString(part);
            }

            return result;
        }

        public enum DataType {
            PCM,
            DoP
        };

        /// <summary>
        /// DoP or PCM
        /// </summary>
        public DataType SampleDataType { get; set; }

        /// <summary>
        /// DoPマーカーが出てくるか調べ、SampleDataTypeを確定する。
        /// </summary>
        /// <returns>DATAチャンクの中身をPeekしたバイト数。</returns>
        private int ScanDopMarker(BinaryReader br, long numFrames) {
            SampleDataType = DataType.PCM;

            var pcmData = new PcmDataLib.PcmData();
            pcmData.SetFormat(NumChannels, BitsPerSample, ValidBitsPerSample, SampleRate,
                SampleValueRepresentationType, numFrames);

            int bytesPerFrame = BitsPerSample / 8 * NumChannels;

            int scanFrames = PcmDataLib.PcmData.DOP_SCAN_FRAMES;
            if (numFrames < scanFrames) {
                scanFrames = (int)NumFrames;
            }

            int scanBytes = scanFrames * bytesPerFrame;

            var buff = br.ReadBytes(scanBytes);

            if (pcmData.ScanDopMarker(buff)) {
                SampleDataType = DataType.DoP;
            }

            return buff.Length;
        }

        private bool Read(BinaryReader br, ReadMode mode, long startFrame, long endFrame) {
            ErrorReason = string.Empty;

            bool riffChunkExist = false;
            bool fmtChunkExist = false;
            bool ds64ChunkExist = false;

            long offset = 0;
            try {
                do {
                    var fourcc = br.ReadBytes(4);
                    if (fourcc.Length < 4) {
                        // ファイルの終わりに達した。
                        break;
                    }
                    offset += 4;

                    long advance = 0;

                    if (!riffChunkExist) {
                        if (!PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "RIFF") &&
                            !PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "RF64")) {
                            // ファイルの先頭がRF64でもRIFFもない。WAVではない。
                            ErrorReason = "File does not start with RIFF nor RF64.";
                            return false;
                        }

                        advance = ReadRiffChunk(br, fourcc);
                        riffChunkExist = true;
                    } else if (PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "fmt ")) {
                        advance = ReadFmtChunk(br, fourcc);
                        fmtChunkExist = true;
                    } else if (PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "LIST")) {
                        advance = ReadListChunk(br);
                    } else if (PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "id3 ")) {
                        advance = ReadId3Chunk(br);
                    } else if (PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "ds64")) {
                        advance = ReadDS64Chunk(br);
                        ds64ChunkExist = true;
                    } else if (PcmDataLib.Util.FourCCHeaderIs(fourcc, 0, "data")) {
                        if (!fmtChunkExist) {
                            // fmtチャンクがないと量子化ビット数がわからず処理が継続できない。
                            ErrorReason = "fmt subchunk is missing.";
                            return false;
                        }
                        if (ds64ChunkExist && 0 < mDscList.Count) {
                            ErrorReason = "multiple data chunk in RF64. not supported format.";
                            return false;
                        }

                        int frameBytes = BitsPerSample / 8 * NumChannels;

                        var dsc = new DataSubChunk();

                        advance = dsc.ReadDataChunkHeader(br, offset, fourcc, NumChannels, BitsPerSample);

                        if (ds64ChunkExist) {
                            // ds64チャンクが存在する場合(RF64形式)
                            // dsc.ChunkSizeは正しくないので、そこから算出するdsc.NumFramesも正しくない。
                            // dsc.NumFrameをds64の値で上書きする。
                            dsc.NumFrames = Ds64DataSize / frameBytes;
                        } else {
                            const long MASK = UInt32.MaxValue;
                            if (MASK < (br.BaseStream.Length - 8) && RiffChunkSize != MASK) {
                                // RIFF chunkSizeが0xffffffffではないのにファイルサイズが4GB+8(8==RIFFチャンクのヘッダサイズ)以上ある。
                                // このファイルはdsc.ChunkSize情報の信憑性が薄い。
                                // dsc.ChunkSizeの上位ビットが桁あふれによって消失している可能性があるので、
                                // dsc.ChunkSizeの上位ビットをファイルサイズから類推して付加し、
                                // dsc.NumFrameを更新する。

                                long remainBytes = br.BaseStream.Length - (offset + advance);
                                long maskedRemainBytes =  remainBytes & 0xffffffffL;
                                if (maskedRemainBytes <= dsc.ChunkSize && RiffChunkSize - maskedRemainBytes < 4096) {
                                    long realChunkSize = dsc.ChunkSize;
                                    while (realChunkSize + 0x100000000L <= remainBytes) {
                                        realChunkSize += 0x100000000L;
                                    }
                                    dsc.NumFrames = realChunkSize / frameBytes;
                                }
                            }
                        }

                        int peekFrames = 0;

                        if (mode == ReadMode.OnlyHeader) {
                            peekFrames = ScanDopMarker(br, dsc.NumFrames);
                        }

                        // マルチデータチャンク形式の場合、data chunkの後にさらにdata chunkが続いたりするので、
                        // 読み込みを続行する。
                        long skipBytes = ((dsc.NumFrames - peekFrames) * frameBytes + 1) & (~1L);
                        if (0 < skipBytes) {
                            PcmDataLib.Util.BinaryReaderSkip(br, skipBytes);
                        }

                        if (br.BaseStream.Length < (offset + advance) + skipBytes) {
                            // ファイルが途中で切れている。
                            dsc.NumFrames = (br.BaseStream.Length - (offset + advance)) / frameBytes;
                        }

                        if (0 < dsc.NumFrames) {
                            mDscList.Add(dsc);
                        } else {
                            // ファイルがDSCヘッダ部分を最後に切れていて、サンプルデータが1フレーム分すらも無いとき。
                        }

                        advance += skipBytes;
                    } else {
                        advance = SkipUnknownChunk(br, fourcc);
                    }

                    if (0 == advance) {
                        // 行儀が悪いWAVファイル。ファイルの最後に0がいくつか書き込まれている。
                        return riffChunkExist && fmtChunkExist && mDscList.Count != 0;
                    }
                    offset += advance;
                } while (true);
            } catch (Exception ex) {
                ErrorReason = string.Format("E: WavRWLib2.WavData.ReadRiffChunk() {0}", ex);
            }

            if (mode == ReadMode.HeaderAndPcmData) {
                if (!ReadPcmDataInternal(br, startFrame, endFrame)) {
                    return false;
                }
                return true;
            }

            return riffChunkExist && fmtChunkExist && mDscList.Count != 0;
        }

        /// <summary>
        /// read only header part.
        /// </summary>
        /// <returns>if false, ErrorReason is set.</returns>
        public bool ReadHeader(BinaryReader br) {
            return Read(br, ReadMode.OnlyHeader, 0, -1);
        }

        /// <summary>
        /// read header and all sample data.
        /// </summary>
        /// <returns>if false, ErrorReason is set.</returns>
        public bool ReadHeaderAndSamples(BinaryReader br, long startFrame, long endFrame) {
            return Read(br, ReadMode.HeaderAndPcmData, startFrame, endFrame);
        }

        public long NumFrames {
            get {
                long result = 0;
                foreach (var dsc in mDscList) {
                    result += dsc.NumFrames;
                }
                return result;
            }
        }

        public LargeArray<byte> GetSampleLargeArray() {
            if (mDscList.Count != 1) {
                Console.WriteLine("multi data chunk wav. not supported");
                return null;
            }
            return mDscList[0].GetSampleLargeArray();
        }

        int mCurrentDsc = -1;
        long mDscPosFrame = 0;

        public bool ReadStreamBegin(BinaryReader br, out PcmDataLib.PcmData pcmData) {
            if (!ReadHeader(br)) {
                pcmData = new PcmDataLib.PcmData();
                return false;
            }

            pcmData = new PcmDataLib.PcmData();
            pcmData.SetFormat(NumChannels, BitsPerSample,
                    BitsPerSample, (int)SampleRate,
                    SampleValueRepresentationType, NumFrames);

            mCurrentDsc = -1;
            mDscPosFrame = 0;

            // 最初のDSCまでシークする。
            return ReadStreamSkip(br, 0);
        }


        public bool ReadStreamSkip(BinaryReader br, long skipFrames) {
            int frameBytes = BitsPerSample / 8 * NumChannels;

            for (int i=0; i < mDscList.Count; ++i) {
                var dsc = mDscList[i];

                if (skipFrames < dsc.NumFrames) {
                    // 開始フレームはこのdscにある。
                    mCurrentDsc = i;
                    mDscPosFrame = skipFrames;
                    PcmDataLib.Util.BinaryReaderSeekFromBegin(br, dsc.Offset + frameBytes * skipFrames);
                    return true;
                } else {
                    skipFrames -= dsc.NumFrames;
                }
            }

            // 開始フレームが見つからない
            return false;
        }

        /// <summary>
        /// 読めるデータ量は少ないことがある
        /// </summary>
        public byte[] ReadStreamReadOne(BinaryReader br, long preferredFrames) {
            if (mCurrentDsc < 0 || mDscList.Count <= mCurrentDsc) {
                return null;
            }

            var dsc = mDscList[mCurrentDsc];

            // 現dscの残りデータ量
            long readFrames = dsc.NumFrames - mDscPosFrame;
            if (preferredFrames < readFrames) {
                // 多すぎるので、減らす。
                readFrames = preferredFrames;
            }
            if (readFrames == 0) {
                return null;
            }

            int frameBytes = BitsPerSample / 8 * NumChannels;
            var result = br.ReadBytes((int)(readFrames * frameBytes));

            mDscPosFrame += readFrames;

            if (dsc.NumFrames <= mDscPosFrame && (mCurrentDsc + 1) < mDscList.Count) {
                // 次のdscに移動する
                // 8 == data chunk id + data chunk sizeのバイト数
                PcmDataLib.Util.BinaryReaderSkip(br, 8);
                ++mCurrentDsc;
                mDscPosFrame = 0;
            }
            return result;
        }

        public void ReadStreamEnd() {
            mCurrentDsc = -1;
        }
    }
}
