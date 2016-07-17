using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WavRWLib2
{
    public class WavWriter
    {
        /// <summary>
        /// writes PCM data onto WAV file
        /// </summary>
        public static bool Write(BinaryWriter bw,
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

            int dwChannelMask = 0;
            if (numChannels == 2) {
                dwChannelMask = 3;
            }

            var wav = new WavWriterLowLevel();
            wav.RiffChunkWrite(bw, (int)(36 + sampleArray.LongLength));
            wav.FmtChunkWriteExtensible(bw, (short)numChannels, (int)sampleRate,
                    (short)bitsPerSample, (short)validBitsPerSample, sampleValueRepresentation, dwChannelMask);
            wav.DataChunkWrite(bw, false, sampleArray);
            return true;
        }

        /// <summary>
        /// RF64形式のヘッダを書き込む。DATAチャンクヘッダまで書き込む。
        /// この後PCMデータを書き込み、PCMサイズが奇数バイトだったら1バイトのパッドを書き込んでファイルを閉じるとRF64 WAVファイルができる。
        /// </summary>
        public static bool WriteRF64Header(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int sampleRate,
                long numFrames) {
            long pcmBytes = numFrames * numChannels * bitsPerSample / 8;

            int rf64ChunkBytes = 12;
            int ds64ChunkBytes = 36;
            int fmtChunkBytes = 26;
            int dataChunkHeaderBytes = 8;
            int padBytes = ((pcmBytes & 1) == 1) ? 1 : 0;

            long fileBytes = rf64ChunkBytes + ds64ChunkBytes + fmtChunkBytes + dataChunkHeaderBytes + pcmBytes + padBytes;

            var wav = new WavRWLib2.WavWriterLowLevel();
            wav.Rf64ChunkWrite(bw);
            wav.Ds64ChunkWrite(bw, fileBytes - 8, pcmBytes, numFrames);
            wav.FmtChunkWrite(bw, (short)numChannels, sampleRate, (short)bitsPerSample);
            wav.DataChunkHeaderWrite(bw, -1);

            int bytesPerSample = (bitsPerSample+7) / 8;
            var buff = new byte[bytesPerSample];

            return true;
        }
    }
}
