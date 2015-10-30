// このコードは作りが悪い。
// サブチャンクごとにクラス分けしたのがいけなかった。
// またWAVの読み込みと書き込みを別のクラスにするべきだった。
// このコードは書き込み専用とし、読み込みコードを独立させたWavReaderクラスを作った。

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WavRWLib2
{
    public class WavWriter
    {
        public bool Write(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int validBitsPerSample,
                int sampleRate,
                PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation,
                long numFrames,
                byte[] sampleArray) {
            
            if (0xffffffffL < sampleArray.LongLength + 36) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            RiffChunkWrite(bw, (uint)(36 + sampleArray.LongLength));
            FmtChunkWriteExtensible(bw, numChannels, sampleRate, bitsPerSample, validBitsPerSample, sampleValueRepresentation);
            DataChunkWrite(bw, sampleArray);

            return true;
        }

        private void RiffChunkWrite(BinaryWriter bw, uint chunkSize) {
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

        private void FmtChunkWrite(BinaryWriter bw,
                int numChannels, int sampleRate, int bitsPerSample, int validBitsPerSample,
                PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation) {
            byte[] subChunk1Id = new byte[4];
            subChunk1Id[0] = (byte)'f';
            subChunk1Id[1] = (byte)'m';
            subChunk1Id[2] = (byte)'t';
            subChunk1Id[3] = (byte)' ';
            bw.Write(subChunk1Id);

            uint subChunk1Size = 16;
            bw.Write(subChunk1Size);

            ushort audioFormat = 1;
            bw.Write(audioFormat);

            System.Diagnostics.Debug.Assert(0 < numChannels);
            ushort usNumChannels = (ushort)numChannels;
            bw.Write(usNumChannels);

            uint uiSampleRate = (uint)sampleRate;
            bw.Write(uiSampleRate);

            uint byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            bw.Write(byteRate);

            ushort blockAlign = (ushort)(numChannels * bitsPerSample / 8);
            bw.Write(blockAlign);

            ushort usBitsPerSample = (ushort)bitsPerSample;
            bw.Write(usBitsPerSample);
        }

        private void FmtChunkWriteExtensible(BinaryWriter bw,
               int numChannels, int sampleRate, int bitsPerSample, int validBitsPerSample,
               PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation) {
            byte[] subChunk1Id = new byte[4];
            subChunk1Id[0] = (byte)'f';
            subChunk1Id[1] = (byte)'m';
            subChunk1Id[2] = (byte)'t';
            subChunk1Id[3] = (byte)' ';
            bw.Write(subChunk1Id);

            uint subChunk1Size = 40;
            bw.Write(subChunk1Size);

            ushort audioFormat = 0xfffe;
            bw.Write(audioFormat);

            System.Diagnostics.Debug.Assert(0 < numChannels);
            ushort usNumChannels = (ushort)numChannels;
            bw.Write(usNumChannels);

            uint uiSampleRate = (uint)sampleRate;
            bw.Write(uiSampleRate);

            uint byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            bw.Write(byteRate);

            ushort blockAlign = (ushort)(numChannels * bitsPerSample / 8);
            bw.Write(blockAlign);

            ushort usBitsPerSample = (ushort)bitsPerSample;
            bw.Write(usBitsPerSample);

            ushort extensibleSize = 22;
            bw.Write(extensibleSize);

            ushort usValidBitsPerSample = (ushort)validBitsPerSample;
            bw.Write(usValidBitsPerSample);

            uint channelMask = 0;
            if (numChannels == 2) {
                channelMask = 3;
            }
            bw.Write(channelMask);

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

        private void DataChunkWrite(BinaryWriter bw, byte[] rawData) {
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
            } else {
                chunkSize = (uint)rawData.LongLength;
            }

            bw.Write(chunkSize);
            bw.Write(rawData);

        }

    }
}
