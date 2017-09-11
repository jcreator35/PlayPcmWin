using System;
using System.IO;

namespace QuantizationDemo
{
    public class WavWriter
    {
        private static void WriteRiffChunk(uint chunkSize, BinaryWriter bw) {
            byte[] chunkId = new byte[4];
            chunkId[0] = (byte)'R';
            chunkId[1] = (byte)'I';
            chunkId[2] = (byte)'F';
            chunkId[3] = (byte)'F';

            byte[] format = new byte[4];
            format[0] = (byte)'W';
            format[1] = (byte)'A';
            format[2] = (byte)'V';
            format[3] = (byte)'E';

            bw.Write(chunkId);
            bw.Write(chunkSize);
            bw.Write(format);
        }

        /// <param name="audioFormat">1: integer, 3: float</param>
        private static void WriteFmtSubChunk(
                ushort numChannels, uint sampleRate, ushort bitsPerSample,
                ushort validBitsPerSample, ushort audioFormat,
                BinaryWriter bw) {
            byte[] subChunk1Id = new byte[4];
            subChunk1Id[0] = (byte)'f';
            subChunk1Id[1] = (byte)'m';
            subChunk1Id[2] = (byte)'t';
            subChunk1Id[3] = (byte)' ';

            uint subChunk1Size = 16;

            System.Diagnostics.Debug.Assert(0 < numChannels);

            uint byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            ushort blockAlign = (ushort)(numChannels * bitsPerSample / 8);

            bw.Write(subChunk1Id);
            bw.Write(subChunk1Size);
            bw.Write(audioFormat);
            bw.Write(numChannels);
            bw.Write(sampleRate);

            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);
        }

        private static void WriteWavDataSubChunk(long numFrames, byte[] rawData,
                BinaryWriter bw) {
            byte[] chunkId = new byte[4];
            chunkId[0] = (byte)'d';
            chunkId[1] = (byte)'a';
            chunkId[2] = (byte)'t';
            chunkId[3] = (byte)'a';

            System.Diagnostics.Debug.Assert(rawData.LongLength < UInt32.MaxValue);

            uint chunkSize = (uint)rawData.LongLength;

            bw.Write(chunkId);
            bw.Write(chunkSize);
            bw.Write(rawData);
        }

        /// <param name="audioFormat">1: integer, 3: float</param>
        /// <returns></returns>
        public static bool Write(
                int numChannels,
                int bitsPerSample,
                int validBitsPerSample,
                int sampleRate,
                long numFrames,
                int audioFormat,
                byte[] sampleArray,
                BinaryWriter bw) {
            if (0xffffffffL < sampleArray.LongLength + 36) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            WriteRiffChunk((uint)(36 + sampleArray.LongLength), bw);
            WriteFmtSubChunk((ushort)numChannels, (uint)sampleRate, (ushort)bitsPerSample,
                (ushort)validBitsPerSample, (ushort)audioFormat, bw);
            WriteWavDataSubChunk(numFrames, sampleArray, bw);
            return true;
        }
    }
}
