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
    class RiffChunkDescriptor
    {
        private byte[] m_chunkId;

        /// <summary>
        /// チャンクサイズはあてにならない。
        /// </summary>
        public uint ChunkSize { get; set; }
        private byte[] m_format;

        public void Create(uint chunkSize)
        {
            m_chunkId = new byte[4];
            m_chunkId[0] = (byte)'R';
            m_chunkId[1] = (byte)'I';
            m_chunkId[2] = (byte)'F';
            m_chunkId[3] = (byte)'F';

            ChunkSize = chunkSize;

            m_format = new byte[4];
            m_format[0] = (byte)'W';
            m_format[1] = (byte)'A';
            m_format[2] = (byte)'V';
            m_format[3] = (byte)'E';
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(m_chunkId);
            bw.Write(ChunkSize);
            bw.Write(m_format);
        }
    }

    class FmtSubChunk
    {
        private byte[] m_subChunk1Id;
        private uint m_subChunk1Size;
        private ushort m_audioFormat;
        public ushort NumChannels { get; set; }
        public uint SampleRate { get; set; }

        public PcmDataLib.PcmData.ValueRepresentationType SampleValueRepresentationType { get; set; }

        private uint   m_byteRate;
        private ushort m_blockAlign;
        public ushort BitsPerSample { get; set; }
        public ushort ValidBitsPerSample { get; set; }
        public uint ChannelMask { get; set; }

        public bool Create(
                int numChannels, int sampleRate, int bitsPerSample, int validBitsPerSample,
                PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation)
        {
            m_subChunk1Id = new byte[4];
            m_subChunk1Id[0] = (byte)'f';
            m_subChunk1Id[1] = (byte)'m';
            m_subChunk1Id[2] = (byte)'t';
            m_subChunk1Id[3] = (byte)' ';

            m_subChunk1Size = 16;

            m_audioFormat = 1;

            System.Diagnostics.Debug.Assert(0 < numChannels);
            NumChannels = (ushort)numChannels;

            SampleRate = (uint)sampleRate;
            m_byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            m_blockAlign = (ushort)(numChannels * bitsPerSample / 8);

            BitsPerSample = (ushort)bitsPerSample;
            ValidBitsPerSample = (ushort)validBitsPerSample;
            ChannelMask = 0;

            SampleValueRepresentationType = sampleValueRepresentation;
            if (sampleValueRepresentation == PcmDataLib.PcmData.ValueRepresentationType.SInt) {
                m_audioFormat = 1;
            } else if (sampleValueRepresentation == PcmDataLib.PcmData.ValueRepresentationType.SFloat) {
                m_audioFormat = 3;
            } else {
                System.Diagnostics.Debug.Assert(false);
            }

            return true;
        }
        
        public void Write(BinaryWriter bw)
        {
            bw.Write(m_subChunk1Id);
            bw.Write(m_subChunk1Size);
            bw.Write(m_audioFormat);
            bw.Write(NumChannels);
            bw.Write(SampleRate);

            bw.Write(m_byteRate);
            bw.Write(m_blockAlign);
            bw.Write(BitsPerSample);
        }
    }
    
    class WavDataSubChunk
    {
        private byte[] m_chunkId;
        public uint ChunkSize { get; set; }

        private byte[] m_rawData;

        /// <summary>
        /// ファイル先頭から、このデータチャンクのPCMデータ先頭までのオフセット
        /// </summary>
        public long Offset { get; set; }
        public long NumFrames { get; set; }

        public byte[] GetSampleArray() {
            return m_rawData;
        }

        public void Clear() {
            m_chunkId = null;
            ChunkSize = 0;
            m_rawData = null;
            NumFrames = 0;
        }

        public void SetRawData(long numSamples, byte[] rawData) {
            NumFrames = numSamples;
            m_rawData = rawData;
        }

        public void Create(long numSamples, byte[] rawData) {
            SetRawData(numSamples, rawData);
            m_chunkId = new byte[4];
            m_chunkId[0] = (byte)'d';
            m_chunkId[1] = (byte)'a';
            m_chunkId[2] = (byte)'t';
            m_chunkId[3] = (byte)'a';
            if (UInt32.MaxValue < rawData.LongLength) {
                // RF64形式。別途ds64チャンクを用意して、そこにdata chunkのバイト数を入れる。
                ChunkSize = UInt32.MaxValue;
            } else {
                ChunkSize = (uint)rawData.LongLength;
            }
        }

        public void TrimRawData(long newNumSamples, long startBytes, long endBytes) {
            System.Diagnostics.Debug.Assert(0 <= startBytes);
            System.Diagnostics.Debug.Assert(0 <= endBytes);
            System.Diagnostics.Debug.Assert(startBytes <= endBytes);

            NumFrames = newNumSamples;
            if (newNumSamples == 0 ||
                m_rawData.Length <= startBytes) {
                m_rawData = null;
                NumFrames = 0;
            } else {
                byte[] newArray = new byte[endBytes - startBytes];
                Array.Copy(m_rawData, startBytes, newArray, 0, endBytes - startBytes);
                m_rawData = null;
                m_rawData = newArray;
            }
        }

        public void Write(BinaryWriter bw) {
            bw.Write(m_chunkId);
            bw.Write(ChunkSize);
            bw.Write(m_rawData);
        }
    }

    public class WavWriter
    {
        private RiffChunkDescriptor mRcd;
        private FmtSubChunk         mFsc;
        private List<WavDataSubChunk>  mDscList = new List<WavDataSubChunk>();

        public PcmDataLib.PcmData.ValueRepresentationType SampleValueRepresentationType {
            get { return mFsc.SampleValueRepresentationType; }
            set { mFsc.SampleValueRepresentationType = value; }
        }

        public void Write(BinaryWriter bw)
        {
            mRcd.Write(bw);
            mFsc.Write(bw);
            foreach (var dsc in mDscList) {
                dsc.Write(bw);
            }
        }

        public int NumChannels
        {
            get { return mFsc.NumChannels; }
        }

        public int BitsPerSample
        {
            get { return mFsc.BitsPerSample; }
        }

        public int ValidBitsPerSample {
            get { return mFsc.ValidBitsPerSample; }
        }

        public long NumFrames
        {
            get {
                long result = 0;
                foreach (var dsc in mDscList) {
                    result += dsc.NumFrames;
                }
                return result;
            }
        }

        public int SampleRate
        {
            get { return (int)mFsc.SampleRate; }
        }

        public byte[] GetSampleArray() {
            if (mDscList.Count != 1) {
                Console.WriteLine("multi data chunk wav. not supported");
                return null;
            }
            return mDscList[0].GetSampleArray();
        }

        public bool Set(
                int numChannels,
                int bitsPerSample,
                int validBitsPerSample,
                int sampleRate,
                PcmDataLib.PcmData.ValueRepresentationType sampleValueRepresentation,
                long numFrames,
                byte[] sampleArray) {
            mRcd = new RiffChunkDescriptor();

            if (0xffffffffL < sampleArray.LongLength + 36) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }
            mRcd.Create((uint)(36 + sampleArray.LongLength));

            mFsc = new FmtSubChunk();
            mFsc.Create(numChannels, sampleRate, bitsPerSample, validBitsPerSample, sampleValueRepresentation);

            var dsc = new WavDataSubChunk();
            dsc.Create(numFrames, sampleArray);
            mDscList.Clear();
            mDscList.Add(dsc);
            return true;
        }
    }
}
