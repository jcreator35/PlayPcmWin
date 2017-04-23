using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WWDsfRW {
    public class WWDsfWriter {
        private WWFlacRWCS.Metadata mMeta;
        private byte [] mPictureData;
        private List<WWUtil.LargeArray<byte>> mDsdData;

        public int EncodeInit(WWFlacRWCS.Metadata meta) {
            mMeta = meta;
            mDsdData = new List<WWUtil.LargeArray<byte>>();
            return 0;
        }

        public void EncodeEnd() {
            mDsdData = null;
            mPictureData = null;
            mMeta = null;
        }

        public int EncodeSetPicture(byte[] pictureData) {
            mPictureData = pictureData;
            return 0;
        }

        public int EncodeAddPcm(int channel, WWUtil.LargeArray<byte> pcmData) {
            mDsdData.Add(pcmData);
            return 0;
        }

        // ID3チャンクの数字はビッグエンディアンバイトオーダー
        private void WriteBE2(ushort v, ref List<byte> to) {
            to.Add((byte)(v>>8));
            to.Add((byte)(v & 0xff));
        }

        // ID3チャンクの数字はビッグエンディアンバイトオーダー
        private void WriteBE4(uint v, ref List<byte> to) {
            to.Add((byte)((v >> 24)&0xff));
            to.Add((byte)((v >> 16)&0xff));
            to.Add((byte)((v >> 8)&0xff));
            to.Add((byte)(v & 0xff));
        }

        // DSFチャンクの数字はリトルエンディアンバイトオーダー
        private void BwWriteLE8(BinaryWriter bw, ulong v) {
            bw.Write((byte)(v & 0xff));
            bw.Write((byte)((v >> 8) & 0xff));
            bw.Write((byte)((v >> 16) & 0xff));
            bw.Write((byte)((v >> 24) & 0xff));

            bw.Write((byte)((v >> 32) & 0xff));
            bw.Write((byte)((v >> 40) & 0xff));
            bw.Write((byte)((v >> 48) & 0xff));
            bw.Write((byte)((v >> 56) & 0xff));
        }

        // DSFチャンクの数字はリトルエンディアンバイトオーダー
        private void BwWriteLE4(BinaryWriter bw, uint v) {
            bw.Write((byte)(v & 0xff));
            bw.Write((byte)((v >> 8) & 0xff));
            bw.Write((byte)((v >> 16) & 0xff));
            bw.Write((byte)((v >> 24) & 0xff));
        }

        // ID3のName Frameをtoに書き込む
        private void WriteNameFrame(uint frameId, string s, ref List<byte> to) {
            /* (big endian byte order)
                Frame ID       $xx xx xx xx (four characters)
                Size           $xx xx xx xx
                Flags          $xx xx
             * 
                Encoding       $xx
                text           <text string> 00
            */
            WriteBE4(frameId, ref to);

            var sUnicode = System.Text.Encoding.Unicode.GetBytes(s);

            uint size = 1 + 2 + (uint)sUnicode.Length + 2;
            WriteBE4(size, ref to);

            ushort flags = 0;
            WriteBE2(flags, ref to);

            byte encoding = 1;
            to.Add(encoding);

            to.Add((byte)0xff);
            to.Add((byte)0xfe);
            foreach (var v in sUnicode) {
                to.Add(v);
            }
            to.Add((byte)0);
            to.Add((byte)0);
        }

        // ID3のPicture Frameをtoに書き込む
        private void WritePictureFrame(uint frameId, byte[] picture, ref List<byte> to) {
            /* (big endian byte order)
                Frame ID       $xx xx xx xx (four characters)
                Size           $xx xx xx xx
                Flags          $xx xx

                Text encoding   $xx
                MIME type       <text string> $00
                Picture type    $xx (3 for cover(front))
                Description     <text string according to encoding> $00 (00)
                Picture data    <binary data>
             */

            ushort flags = 0;
            byte encoding = 0;
            var mimeTypeUtf8 = System.Text.Encoding.UTF8.GetBytes("image/jpeg");
            byte pictureType = 3;
            byte description = 0;
            uint size = (uint)(1 + mimeTypeUtf8.Length + 1 + 1 + 1 + picture.Length);

            WriteBE4(frameId, ref to);
            WriteBE4(size, ref to);
            WriteBE2(flags, ref to);

            to.Add(encoding);

            foreach (var v in mimeTypeUtf8) { to.Add(v); }
            to.Add((byte)0);

            to.Add(pictureType);

            to.Add(description);

            foreach (var v in picture) { to.Add(v); }
        }

        // ID3タグサイズ書き込み
        private void UpdateID3TagSize(uint v, ref List<byte> to, int offs) {
            to[offs + 0] = (byte)((v >> 21) & 0x7f);
            to[offs + 1] = (byte)((v >> 14) & 0x7f);
            to[offs + 2] = (byte)((v >> 7) & 0x7f);
            to[offs + 3] = (byte)((v >> 0) & 0x7f);
        }

        private byte[] CreateID3v23Chunk() {
            if (0 == mMeta.titleStr.Length) {
                return new byte[0];
            }

            List<byte> buffer = new List<byte>();

            byte flags = 0;

            // id3 tag header (10 bytes) big endian byte order
            buffer.Add((byte)'I');
            buffer.Add((byte)'D');
            buffer.Add((byte)'3');
            WriteBE2(0x0300, ref buffer);
            buffer.Add(flags);
            WriteBE4(0, ref buffer); //< ID3 tag size in bytes (exclude id3 tag header size==10). write correct value later. 

            // "TIT2" title
            WriteNameFrame(0x54495432, mMeta.titleStr, ref buffer);

            if (0 < mMeta.albumStr.Length) {
                // "TALB" Album
                WriteNameFrame(0x54414c42, mMeta.albumStr, ref buffer);
            } else if (0 < mMeta.albumArtistStr.Length) {
                // "TALB" Album
                WriteNameFrame(0x54414c42, mMeta.albumArtistStr, ref buffer);
            }

            if (0 < mMeta.artistStr.Length) {
                // "TPE1" artist
                WriteNameFrame(0x54504531, mMeta.artistStr, ref buffer);
            }

            if (mPictureData != null && 0 < mPictureData.Length) {
                WritePictureFrame(0x41504943, mPictureData, ref buffer);
            }

            // ここでサイズが決定する。buffer[6]～buffer[9]にid3Sizeを書き込む。
            uint id3Size = (uint)(buffer.Count - 10);
            UpdateID3TagSize(id3Size, ref buffer, 6);

            byte [] rv = new byte[buffer.Count];
            for (int i=0; i < buffer.Count; ++i) {
                rv[i] = buffer[i];
            }
            return rv;
        }

        /// <summary>
        /// stream data offset from the start of the file
        /// </summary>
        private const int STREAM_DATA_OFFSET = 92;

        private int WriteDsdChunk(int id3ChunkLength, BinaryWriter bw) {
            /*
             * DSDチャンク 28bytes
             * 4 "DSD "
             * 8 sizeOfThisChunk=28
             * 8 totalFileSize
             * 8 pointerToMetadataChunk
             * 
             * FMT chunk 52bytes
             * 4 "fmt "
             * 8 sizeOfThisChunk=52
             * 4 formatVersion=1
             * 4 formatID=0
             * 4 channelType 1==mono 2==stereo
             * 4 chunnelNum 1==mono 2==stereo
             * 4 samplingFreq
             * 4 bitsPerSample 1
             * 8 sampleCount per channel
             * 4 blockSizePerChannel=4096
             * 4 reserved=0
             * 
             * DATA chunk headersize==12
             * 4 "data"
             * 8 sizeOfThisChunk == sampleDataBytes + 12
             * n sample data
             * 
             * METADATA chunk size is id3ChunkLength
             * */

            long sampleDataPerChannelBytes = (mDsdData[0].LongLength + 4095) & (~4095L);

            ulong totalFileSize  = (ulong)(92 + sampleDataPerChannelBytes * mMeta.channels + id3ChunkLength);
            ulong metaDataOffset = (ulong)(92 + sampleDataPerChannelBytes * mMeta.channels);

            if (id3ChunkLength == 0) {
                metaDataOffset = 0;
            }

            bw.Write(0x20445344); //< "DSD "
            BwWriteLE8(bw, 28);
            BwWriteLE8(bw, totalFileSize);
            BwWriteLE8(bw, metaDataOffset);

            return 0;
        }

        private int WriteFmtChunk(BinaryWriter bw) {
            /*
             * FMT chunk 52bytes
             * 4 "fmt "
             * 8 sizeOfThisChunk=52
             * 4 formatVersion=1
             * 4 formatID=0
             * 4 channelType 1==mono 2==stereo
             * 4 channelNum 1==mono 2==stereo
             * 4 samplingFreq
             * 4 bitsPerSample 1
             * 8 sampleCount per channel
             * 4 blockSizePerChannel=4096
             * 4 reserved=0
             */
            uint channelType = 1;
            uint channelNum = (uint)mMeta.channels;

            switch (mMeta.channels) {
            case 1:
                channelType = 1;
                break;
            case 2:
                channelType = 2;
                break;
            case 3:
                channelType = 3;
                break;
            case 4:
                channelType = 5;
                break;
            case 5:
                channelType = 6;
                break;
            case 6:
                channelType = 7;
                break;
            default:
                // テキトウな数字を入れとく。
                channelType = 0;
                break;
            }

            uint formatVersion = 1;
            uint formatID = 0;

            bw.Write(0x20746d66); //< "fmt "
            BwWriteLE8(bw, 52);
            BwWriteLE4(bw, formatVersion);
            BwWriteLE4(bw, formatID);

            BwWriteLE4(bw, channelType);
            BwWriteLE4(bw, channelNum);
            BwWriteLE4(bw, (uint)mMeta.sampleRate);
            BwWriteLE4(bw, 1);
            BwWriteLE8(bw, (ulong)(mDsdData[0].LongLength * 8));
            BwWriteLE4(bw, 4096);
            BwWriteLE4(bw, 0);

            return 0;
        }

        private int WriteDataChunk(BinaryWriter bw) {
            /*
             * DATA chunk headersize==12
             * 4 "data"
             * 8 sizeOfThisChunk == sampleDataBytes + 12
             * n sample data
             */

            bw.Write(0x61746164); //< "data"

            long sampleDataPerChannelBytes = (mDsdData[0].LongLength + 4095) & (~4095L);
            BwWriteLE8(bw, (ulong)(sampleDataPerChannelBytes * mMeta.channels));

            long pos = 0;
            for (long i=0; i < sampleDataPerChannelBytes / 4096; ++i) {
                for (int ch=0; ch < mMeta.channels; ++ch) {
                    byte [] data = new byte[4096];

                    int copyBytes = 4096;
                    if (mDsdData[ch].LongLength < pos + 4096) {
                        copyBytes = (int)(mDsdData[ch].LongLength - pos);
                    }
                    
                    for (int j=0; j<copyBytes; ++j) {
                        data[j] = mDsdData[ch].At(pos+j);
                    }
                    for (int j=copyBytes; j < data.Length; ++j) {
                        data[j] = 0x69;
                    }
                    bw.Write(data);
                }
                pos += 4096;
            }

            return 0;
        }

        private int WriteId3Chunk(byte[] id3Chunk, BinaryWriter bw) {
            if (id3Chunk.Length == 0) {
                return 0;
            }

            bw.Write(id3Chunk);
            return 0;
        }

        public int EncodeRun(string path) {

            // ID3タグを作ってサイズを調べる
            var id3Chunk = CreateID3v23Chunk();

            try {
                using (BinaryWriter bw = new BinaryWriter(
                        File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                    if (WriteDsdChunk(id3Chunk.Length, bw) < 0) {
                        return -1;
                    }
                    if (WriteFmtChunk(bw) < 0) {
                        return -1;
                    }
                    if (WriteDataChunk(bw) < 0) {
                        return -1;
                    }
                    if (WriteId3Chunk(id3Chunk, bw) < 0) {
                        return -1;
                    }
                }
            } catch (IOException ex) {
                Console.WriteLine(ex);
                return -1;
            }

            return 0;
        }
    }
}
