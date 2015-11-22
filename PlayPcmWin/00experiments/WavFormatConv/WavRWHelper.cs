using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WavRWLib2;
using PcmDataLib;
using System.IO;

namespace WavFormatConv {
    class WavRWHelper {

        public static PcmData ReadWav(BinaryReader br) {
            var reader = new WavReader();
            if (!reader.ReadHeaderAndSamples(br, 0, -1)) {
                return null;
            }

            var pcm = new PcmData();
            pcm.AlbumTitle = reader.AlbumName;
            pcm.ArtistName = reader.ArtistName;
            pcm.DisplayName = reader.Title;

            pcm.SetFormat(reader.NumChannels, reader.BitsPerSample,
                reader.ValidBitsPerSample, reader.SampleRate,
                reader.SampleValueRepresentationType, reader.NumFrames);

            pcm.SetSampleArray(reader.GetSampleArray());

            return pcm;
        }

        public static bool WriteWav(BinaryWriter bw, PcmData pcm, List<WavChunkParams> wavParamList) {
            var writer = new WavWriterLowLevel();

            bool isDs64 = wavParamList.Find((WavChunkParams p) => { return p.ChunkType == WavChunkType.DS64; }) != null;
            long posDS64 = -1;
            long posRiff = -1;



            foreach (var i in wavParamList) {
                switch (i.ChunkType) {
                case WavChunkType.RIFF: {
                        // 仮。ファイルサイズが決まった時に書き直す。
                        posRiff = bw.BaseStream.Position;
                        writer.RiffChunkWrite(bw, -1);
                    }
                    break;
                case WavChunkType.fmt: {
                        var fmt = i as FmtChunkParams;
                        switch (fmt.StructType) {
                        case FmtChunkParams.WaveFormatStructType.WaveFormat:
                            writer.FmtChunkWrite(bw, (short)pcm.NumChannels, (int)pcm.SampleRate, (short)pcm.BitsPerSample);
                            break;
                        case FmtChunkParams.WaveFormatStructType.WaveFormatEx:
                            writer.FmtChunkWriteEx(bw, (short)pcm.NumChannels, (int)pcm.SampleRate,
                                (short)pcm.BitsPerSample,
                                pcm.SampleValueRepresentationType == PcmData.ValueRepresentationType.SFloat ? WavWriterLowLevel.WAVE_FORMAT_IEEE_FLOAT : WavWriterLowLevel.WAVE_FORMAT_PCM,
                                (short)fmt.CbSize);
                            break;
                        case FmtChunkParams.WaveFormatStructType.WaveFormatExtensible: {
                                int dwChannelMask = 0;
                                if (pcm.NumChannels == 2) {
                                    dwChannelMask = 3;
                                }

                                writer.FmtChunkWriteExtensible(bw, (short)pcm.NumChannels, (int)pcm.SampleRate,
                                    (short)pcm.BitsPerSample, (short)pcm.ValidBitsPerSample,
                                    pcm.SampleValueRepresentationType, (int)dwChannelMask);
                            }
                            break;
                        default:
                            System.Diagnostics.Debug.Assert(false);
                            break;
                        }
                    }
                    break;
                case WavChunkType.DATA: {
                        var data = i as DataChunkParams;
                        long posDataStart = bw.BaseStream.Position;

                        writer.DataChunkWrite(bw, isDs64, pcm.GetSampleArray());
                        if (!isDs64 && 0 < data.ExtraChunkBytes) {
                            // 実際よりも長いチャンクサイズを書き込む。

                            long posDataEnd = bw.BaseStream.Position;

                            bw.BaseStream.Seek(posDataStart + 4, SeekOrigin.Begin);
                            int chunkSize = pcm.GetSampleArray().Length + data.ExtraChunkBytes;
                            bw.Write(chunkSize);

                            bw.BaseStream.Seek(posDataEnd, SeekOrigin.Begin);
                        }
                    }
                    break;
                case WavChunkType.JUNK: {
                        var junk = i as JunkChunkParams;
                        writer.JunkChunkWrite(bw, (ushort)junk.ContentBytes);
                    }
                    break;
                case WavChunkType.bext: {
                        var bext = i as BextChunkParams;
                        writer.BextChunkWrite(bw, bext.Description, bext.Originator, bext.OriginatorReference,
                            bext.OriginationDate, bext.OriginationTime, bext.TimeReference, null, 0,
                            0, 0, 0, 0, null);
                    }
                    break;
                case WavChunkType.DS64: {
                        posDS64 = bw.BaseStream.Position;
                        var ds64 = i as DS64ChunkParams;
                        writer.Ds64ChunkWrite(bw, ds64.RiffSize, ds64.DataSize, ds64.SampleCount);
                    }
                    break;
                case WavChunkType.ID3: {
                        var id3 = i as ID3ChunkParams;
                        writer.ID3ChunkWrite(bw, id3.Title, id3.Album, id3.Artists, id3.AlbumCoverArt, id3.AlbumCoverArtMimeType);
                    }
                    break;
                }
            }

            long posEnd = bw.BaseStream.Position;

            RiffChunkParams riff = wavParamList.Find((WavChunkParams p) => { return p.ChunkType == WavChunkType.RIFF; }) as RiffChunkParams;
            if (!isDs64) {

                // RIFFのサイズが決まったので書き込む
                int riffSize = (int)(posEnd - posRiff - 8);
                riffSize += riff.ExtraChunkBytes;

                bw.BaseStream.Seek(posRiff, SeekOrigin.Begin);
                writer.RiffChunkWrite(bw, riffSize);
                bw.BaseStream.Seek(posEnd, SeekOrigin.Begin);
            } else {
                // DS64のサイズが決まったので書き込む
                DataChunkParams dcp = wavParamList.Find((WavChunkParams p) => { return p.ChunkType == WavChunkType.DATA; }) as DataChunkParams;

                long riffSize = posEnd - posRiff - 8;
                riffSize += riff.ExtraChunkBytes;

                long dataSize = pcm.GetSampleArray().LongLength;
                if (0 < dcp.ExtraChunkBytes) {
                    dataSize += dcp.ExtraChunkBytes;
                }

                long sampleCount = pcm.NumFrames;

                bw.BaseStream.Seek(posDS64, SeekOrigin.Begin);
                writer.Ds64ChunkWrite(bw, riffSize, dataSize, sampleCount);
                bw.BaseStream.Seek(posEnd, SeekOrigin.Begin);
            }

            if (0 < riff.GarbageBytes) {
                // ファイルの最後にごみを書き込む
                var zeroes = new byte[riff.GarbageBytes];
                bw.Write(zeroes);
            }

            return true;
        }
    }
}
