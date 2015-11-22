using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WavRWLib2
{
    public class WavWriter
    {
        private WavWriterLowLevel mLL = new WavWriterLowLevel();

        /// <summary>
        /// writes PCM data onto WAV file
        /// </summary>
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

            int dwChannelMask = 0;
            if (numChannels == 2) {
                dwChannelMask = 3;
            }

            mLL.RiffChunkWrite(bw, (int)(36 + sampleArray.LongLength));
            mLL.FmtChunkWriteExtensible(bw, (short)numChannels, (int)sampleRate,
                    (short)bitsPerSample, (short)validBitsPerSample, sampleValueRepresentation, dwChannelMask);
            mLL.DataChunkWrite(bw, false, sampleArray);
            return true;
        }
    }
}
