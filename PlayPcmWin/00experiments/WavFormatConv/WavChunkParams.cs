using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WavFormatConv {
    public enum WavChunkType {
        RIFF,
        fmt,
        DATA,
        JUNK,
        DS64,
        ID3,
        bext
    }

    public class WavChunkParams {
        public WavChunkType ChunkType { get; set; }
        public string Text { get; set; }
        public virtual bool IsSingleInstanceChunk() {
            return true;
        }
        public virtual bool IsMandatoryChunk() {
            return true;
        }

        public virtual void UpdateText() {
        }

        public WavChunkParams(WavChunkType t, string text) {
            ChunkType = t;
            Text = text;
        }
    }

    public class RiffChunkParams : WavChunkParams {
        public int ExtraChunkBytes { get; set; }
        public int GarbageBytes { get; set; }

        public RiffChunkParams(int extraChunkBytes, int garbageBytes)
                : base(WavChunkType.RIFF, "RIFF") {
            ExtraChunkBytes = extraChunkBytes;
            GarbageBytes = garbageBytes;
        }

        public override void UpdateText() {
            Text = string.Format("RIFF extra={0}, garbage={1}", ExtraChunkBytes, GarbageBytes);
        }
    }

    public class FmtChunkParams : WavChunkParams {
        public enum WaveFormatStructType {
            WaveFormat,
            WaveFormatEx,
            WaveFormatExtensible
        };

        public WaveFormatStructType StructType { get; set; }
        public int CbSize { get; set; }

        public FmtChunkParams(WaveFormatStructType t)
            : base(WavChunkType.fmt, "fmt ") {
            StructType = t;
        }

        public override void UpdateText() {
            Text = string.Format("fmt  {0}, CbSize={1}", StructType, CbSize);
        }
    }

    public class DataChunkParams : WavChunkParams {
        public int ExtraChunkBytes { get; set; }

        public DataChunkParams(int extraChunkBytes)
                : base(WavChunkType.DATA, "DATA") {
            ExtraChunkBytes = extraChunkBytes;
        }

        public override void UpdateText() {
            Text = string.Format("DATA extra={0}", ExtraChunkBytes);
        }

    }


    public class JunkChunkParams : WavChunkParams {
        public int ContentBytes { get; set; }

        public JunkChunkParams(int contentBytes)
                : base(WavChunkType.JUNK, "JUNK") {
            ContentBytes = contentBytes;
        }

        public override bool IsSingleInstanceChunk() {
            return false;
        }

        public override bool IsMandatoryChunk() {
            return false;
        }

        public override void UpdateText() {
            Text = string.Format("JUNK bytes={0}", ContentBytes);
        }
    }

    public class BextChunkParams : WavChunkParams {
        public string Description { get; set; }
        public string Originator { get; set; }
        public string OriginatorReference { get; set; }
        public string OriginationDate { get; set; }
        public string OriginationTime { get; set; }
        public int TimeReference { get; set; }

        public BextChunkParams()
            : base(WavChunkType.bext, "bext") {
        }

        public override bool IsMandatoryChunk() {
            return false;
        }

        public override void UpdateText() {
            Text = string.Format("bext {0} {1}, Description=\"{2}\", Originator=\"{3}\", OriginatorReference=\"{4}\", TimeReference={5}",
                OriginationDate, OriginationTime, Description, Originator, OriginatorReference, TimeReference);
        }
    }

    public class DS64ChunkParams : WavChunkParams {
        public long RiffSize { get; set; }
        public long DataSize { get; set; }
        public long SampleCount { get; set; }

        public DS64ChunkParams(long riffSize, long dataSize, long sampleCount)
                : base(WavChunkType.DS64, "ds64") {
            RiffSize = riffSize;
            DataSize = dataSize;
            SampleCount = sampleCount;
        }

        public override bool IsMandatoryChunk() {
            return false;
        }
    }

    public class ID3ChunkParams : WavChunkParams {
        public string Title { get; set; }
        public string Album { get; set; }
        public string Artists { get; set; }
        public string AlbumCoverArtMimeType { get; set; }
        public byte[] AlbumCoverArt { get; set; }

        public ID3ChunkParams()
                : base(WavChunkType.ID3, "ID3") {
            Title = string.Empty;
            Album = string.Empty;
            Artists = string.Empty;
            AlbumCoverArtMimeType = string.Empty;
            AlbumCoverArt = new byte[0];
        }

        public override void UpdateText() {
            Text = string.Format("id3 Title=\"{0}\", Album=\"{1}\", Artists=\"{2}\", AlbumCoverArt={3}bytes, MIME=\"{4}\"",
                Title, Album, Artists, AlbumCoverArt.Length, AlbumCoverArtMimeType);
        }
    }

}
