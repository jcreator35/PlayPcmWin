using System;
using System.IO;
using System.Text;

namespace WavRWLib2 {
    public class WavWriterLowLevel {
        public const short WAVE_FORMAT_PCM        = 1;
        public const short WAVE_FORMAT_IEEE_FLOAT = 3;
        public const short WAVE_FORMAT_EXTENSIBLE = -2;

        /// <summary>
        /// chunk must be placed on 2 bytes aligned
        /// </summary>
        private static bool NeedPad(int chunkSize) {
            return (chunkSize & 1) == 1;
        }

        private bool AddPadIfNecessary(BinaryWriter bw, int chunkSize) {
            if (!NeedPad(chunkSize)) {
                return false;
            }

            byte zero = 0;
            bw.Write(zero);
            return true;
        }

        public void RiffChunkWrite(BinaryWriter bw, int chunkSize)
        {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'R';
            chunkId[1] = (byte)'I';
            chunkId[2] = (byte)'F';
            chunkId[3] = (byte)'F';
            bw.Write(chunkId);

            bw.Write(chunkSize);

            var format = new byte[4];
            format[0] = (byte)'W';
            format[1] = (byte)'A';
            format[2] = (byte)'V';
            format[3] = (byte)'E';
            bw.Write(format);
        }
        
        public void Rf64ChunkWrite(BinaryWriter bw) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'R';
            chunkId[1] = (byte)'F';
            chunkId[2] = (byte)'6';
            chunkId[3] = (byte)'4';
            bw.Write(chunkId);

            int chunkSize = -1;
            bw.Write(chunkSize);

            var format = new byte[4];
            format[0] = (byte)'W';
            format[1] = (byte)'A';
            format[2] = (byte)'V';
            format[3] = (byte)'E';
            bw.Write(format);
        }

        private void FmtChunkWriteInternal(BinaryWriter bw,
                short numChannels, int sampleRate, short bitsPerSample, int subChunk1Size, short audioFormat)
        {
            byte[] subChunk1Id = new byte[4];
            subChunk1Id[0] = (byte)'f';
            subChunk1Id[1] = (byte)'m';
            subChunk1Id[2] = (byte)'t';
            subChunk1Id[3] = (byte)' ';
            bw.Write(subChunk1Id);

            bw.Write(subChunk1Size);
            bw.Write(audioFormat);
            bw.Write(numChannels);
            bw.Write(sampleRate);

            uint byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            bw.Write(byteRate);

            ushort blockAlign = (ushort)(numChannels * bitsPerSample / 8);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);
        }

        public void FmtChunkWrite(BinaryWriter bw,
                short numChannels, int sampleRate, short bitsPerSample)
        {
            FmtChunkWriteInternal(bw, numChannels, sampleRate, bitsPerSample, 16, 1);
        }

        /// <param name="audioFormat">WAVE_FORMAT_PCM or WAVE_FORMAT_IEEE_FLOAT</param>
        /// <param name="cbSize">0</param>
        public void FmtChunkWriteEx(BinaryWriter bw,
                short numChannels, int sampleRate, short bitsPerSample, short audioFormat, short cbSize)
        {
            FmtChunkWriteInternal(bw, numChannels, sampleRate, bitsPerSample, 18, audioFormat);
            bw.Write(cbSize);
        }

        public void FmtChunkWriteExtensible(BinaryWriter bw,
               short numChannels, int sampleRate, short bitsPerSample, short validBitsPerSample,
               PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation, int dwChannelMask)
        {
            FmtChunkWriteInternal(bw, numChannels, sampleRate, bitsPerSample, 40, WAVE_FORMAT_EXTENSIBLE);

            ushort cbSize = 22;
            bw.Write(cbSize);
            bw.Write(validBitsPerSample);
            bw.Write(dwChannelMask);

            byte[] guidByteArray = null;
            if (sampleValueRepresentation == PcmDataLib.PcmData.ValueRepresentationType.SInt) {
                var pcmGuid = Guid.Parse("00000001-0000-0010-8000-00aa00389b71");
                guidByteArray = pcmGuid.ToByteArray();
            } else if (sampleValueRepresentation == PcmDataLib.PcmData.ValueRepresentationType.SFloat) {
                var floatGuid = Guid.Parse("00000003-0000-0010-8000-00aa00389b71");
                guidByteArray = floatGuid.ToByteArray();
            } else {
                throw new ArgumentException("sampleValueRepresentation");
            }
            bw.Write(guidByteArray);
        }

        public void DataChunkWrite(BinaryWriter bw, bool isDs64, byte[] rawData) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'d';
            chunkId[1] = (byte)'a';
            chunkId[2] = (byte)'t';
            chunkId[3] = (byte)'a';
            bw.Write(chunkId);

            uint chunkSize;
            if (UInt32.MaxValue < rawData.LongLength) {
                throw new ArgumentException("numSamples");
                // RF64形式。別途ds64チャンクを用意して、そこにdata chunkのバイト数を入れる。
                // ChunkSize = UInt32.MaxValue;
            }

            if (isDs64) {
                chunkSize = UInt32.MaxValue;
            } else {
                chunkSize = (uint)rawData.LongLength;
            }

            bw.Write(chunkSize);
            bw.Write(rawData);

            AddPadIfNecessary(bw, rawData.Length);
        }

        public void DataChunkHeaderWrite(BinaryWriter bw, int chunkSize) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'d';
            chunkId[1] = (byte)'a';
            chunkId[2] = (byte)'t';
            chunkId[3] = (byte)'a';
            bw.Write(chunkId);

            bw.Write(chunkSize);
        }

        public void JunkChunkWrite(BinaryWriter bw, int chunkSize) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'J';
            chunkId[1] = (byte)'U';
            chunkId[2] = (byte)'N';
            chunkId[3] = (byte)'K';
            bw.Write(chunkId);

            bw.Write(chunkSize);

            var a = new byte[chunkSize];
            for (int i = 0; i < a.Length; ++i) {
                a[i] = 0;
            }
            bw.Write(a);

            AddPadIfNecessary(bw, chunkSize);
        }

        private int WriteSpecifiedUTF7s(BinaryWriter bw, string data, int bytes) {
            int count = 0;
            var writeData = new byte[bytes];

            var ascii = UTF7Encoding.ASCII.GetBytes(data);

            for (int i = 0; i < bytes; ++i) {
                if (data == null || ascii.Length <= i) {
                    break;
                }
                writeData[i] = ascii[i];
                ++count;
            }

            bw.Write(writeData);
            return count;
        }

        private void WriteFixedSizeByteArray(BinaryWriter bw, byte[] data, int bytes) {
            for (int i = 0; i < bytes; ++i) {
                if (data == null || data.Length <= i) {
                    byte zero = 0;
                    bw.Write(zero);
                } else {
                    bw.Write(data[i]);
                }
            }
        }

        /// <summary>
        /// See https://tech.ebu.ch/docs/tech/tech3285.pdf
        /// </summary>
        public void BextChunkWrite(BinaryWriter bw, string description, string originator, string originatorReference,
                string originationDate, string originationTime, long timeReference,
                byte[] umid, short loudnessValue, short loudnessRange, short maxTruePeakLevel, short maxMomentaryLoudness,
                short maxShortTermLoudness, byte[] codingHistory) {
            if (codingHistory == null) {
                codingHistory = new byte[0];
            }
            uint chunkSize = 602U + (uint)codingHistory.Length;

            var chunkId = new byte[4];
            chunkId[0] = (byte)'b';
            chunkId[1] = (byte)'e';
            chunkId[2] = (byte)'x';
            chunkId[3] = (byte)'t';
            bw.Write(chunkId);

            bw.Write(chunkSize);

            WriteSpecifiedUTF7s(bw, description, 256);
            WriteSpecifiedUTF7s(bw, originator, 32);
            WriteSpecifiedUTF7s(bw, originatorReference, 32);
            WriteSpecifiedUTF7s(bw, originationDate, 10);
            WriteSpecifiedUTF7s(bw, originationTime, 8);

            bw.Write(timeReference);
            
            short version = 0;
            bw.Write(version);

            WriteFixedSizeByteArray(bw, umid, 64);
            bw.Write(loudnessValue);
            bw.Write(loudnessRange);
            bw.Write(maxTruePeakLevel);
            bw.Write(maxMomentaryLoudness);
            bw.Write(maxShortTermLoudness);
            WriteFixedSizeByteArray(bw, null, 180);

            if (0 < codingHistory.Length) {
                bw.Write(codingHistory);
            }

            AddPadIfNecessary(bw, (int)chunkSize);
        }

        /// <summary>
        /// See https://tech.ebu.ch/docs/tech/tech3306-2009.pdf
        /// </summary>
        public void Ds64ChunkWrite(BinaryWriter bw, long riffSize, long dataSize, long sampleCount) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'d';
            chunkId[1] = (byte)'s';
            chunkId[2] = (byte)'6';
            chunkId[3] = (byte)'4';
            bw.Write(chunkId);

            int chunkSize = 28;
            bw.Write(chunkSize);

            bw.Write(riffSize);
            bw.Write(dataSize);
            bw.Write(sampleCount);

            int tableLength = 0;
            bw.Write(tableLength);
        }

        public void ID3ChunkWrite(BinaryWriter bw, string title, string album, string artists, byte[] albumCoverArt, string mimeType) {
            var chunkId = new byte[4];
            chunkId[0] = (byte)'i';
            chunkId[1] = (byte)'d';
            chunkId[2] = (byte)'3';
            chunkId[3] = (byte)' ';
            bw.Write(chunkId);

            long posStart = bw.BaseStream.Position;

            int chunkSize = 0;
            bw.Write(chunkSize);

            var id3w = new PcmDataLib.ID3Writer();

            id3w.Write(bw, title, album, artists, albumCoverArt, mimeType);

            long posEnd = bw.BaseStream.Position;

            chunkSize = (int)(posEnd - posStart - 4);


            bw.BaseStream.Seek(posStart, SeekOrigin.Begin);
            bw.Write(chunkSize);
            bw.BaseStream.Seek(posEnd, SeekOrigin.Begin);

            AddPadIfNecessary(bw, chunkSize);
        }
    }
}
