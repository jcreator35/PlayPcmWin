using System.IO;

namespace WavRWLib2
{
    public class WavWriter
    {
        /// <summary>
        /// writes PCM data onto WAV file
        /// </summary>
        /// <param name="audioFormat">WavWriterLowLevel.WAVE_FORMAT_PCM or WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT</param>
        public static bool Write(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int audioFormat,
                int sampleRate,
                long numFrames,
                byte[] sampleArray) {
            if (IsRf64Size(sampleArray.Length)) {
                WriteRF64Header(bw, numChannels, bitsPerSample, audioFormat, sampleRate, numFrames);
            } else {
                WriteRiffHeader(bw, numChannels, bitsPerSample, audioFormat, sampleRate, sampleArray.Length);
            }

            bw.Write(sampleArray);

            if ((sampleArray.Length & 1) == 1) {
                byte pad = 0;
                bw.Write(pad);
            }
            return true;
        }

        /// <param name="audioFormat">WavWriterLowLevel.WAVE_FORMAT_PCM or WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT</param>
        public static bool Write(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int audioFormat,
                int sampleRate,
                long numFrames,
                WWUtil.LargeArray<byte> sampleArray) {
            if (IsRf64Size(sampleArray.LongLength)) {
                WriteRF64Header(bw, numChannels, bitsPerSample, audioFormat, sampleRate, numFrames);
            } else {
                WriteRiffHeader(bw, numChannels, bitsPerSample, audioFormat, sampleRate, sampleArray.LongLength);
            }

            for (int i = 0; i < sampleArray.ArrayNum(); ++i) {
                var pcmFragment = sampleArray.ArrayNth(i);
                bw.Write(pcmFragment);
            }

            if ((sampleArray.LongLength & 1) == 1) {
                byte pad = 0;
                bw.Write(pad);
            }
            return true;
        }

        public static bool IsRf64Size(long sampleBytes) {
            int riffChunkBytes = 12;
            int fmtChunkBytes = 26;
            int dataChunkHeaderBytes = 8;
            int padBytes = ((sampleBytes & 1) == 1) ? 1 : 0;
            long fileBytes = riffChunkBytes + fmtChunkBytes + dataChunkHeaderBytes + sampleBytes + padBytes;

            return 0xffffffffL < fileBytes;
        }

        /// <param name="audioFormat">WavWriterLowLevel.WAVE_FORMAT_PCM or WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT</param>
        public static bool WriteRiffHeader(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int audioFormat,
                int sampleRate,
                long sampleBytes) {
            int riffChunkBytes = 12;
            int fmtChunkBytes = 26;
            int dataChunkHeaderBytes = 8;
            int padBytes = ((sampleBytes & 1) == 1) ? 1 : 0;
            long fileBytes = riffChunkBytes + fmtChunkBytes + dataChunkHeaderBytes + sampleBytes + padBytes;

            if (0xffffffffL < fileBytes) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            var wav = new WavWriterLowLevel();
            wav.RiffChunkWrite(bw, (int)(fileBytes - 8));
            wav.FmtChunkWriteEx(bw, (short)numChannels, sampleRate, (short)bitsPerSample, (short)audioFormat, 0);
            wav.DataChunkHeaderWrite(bw, (int)sampleBytes);
            return true;
        }

        /// <summary>
        /// RF64形式のヘッダを書き込む。DATAチャンクヘッダまで書き込む。
        /// この後PCMデータを書き込み、PCMサイズが奇数バイトだったら1バイトのパッドを書き込んでファイルを閉じるとRF64 WAVファイルができる。
        /// 
        /// audioFormat WavWriterLowLevel.WAVE_FORMAT_PCM or WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT
        /// </summary>
        /// <param name="audioFormat">WavWriterLowLevel.WAVE_FORMAT_PCM or WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT</param>
        public static bool WriteRF64Header(BinaryWriter bw,
                int numChannels,
                int bitsPerSample,
                int audioFormat,
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
            wav.FmtChunkWriteEx(bw, (short)numChannels, sampleRate, (short)bitsPerSample, (short)audioFormat, 0);
            wav.DataChunkHeaderWrite(bw, -1);

            int bytesPerSample = (bitsPerSample+7) / 8;
            var buff = new byte[bytesPerSample];

            return true;
        }
    }
}
