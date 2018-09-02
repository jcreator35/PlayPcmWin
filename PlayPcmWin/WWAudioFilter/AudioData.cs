using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace WWAudioFilter {
    /// <summary>
    /// 保存ファイルフォーマットの種類。
    /// </summary>
    enum FileFormatType {
        FLAC,
        WAVE,
        DSF,
    }

    struct AudioData {
        public WWFlacRWCS.Metadata meta;
        public List<AudioDataPerChannel> pcm;
        public byte[] picture;
        public FileFormatType preferredSaveFormat;
    };

    class AudioDataIO {
        public static int Read(string path, out AudioData ad) {
            int rv;

            if (0 == Path.GetExtension(path).CompareTo(".wav")) {
                rv = AudioDataIO.ReadWavFile(path, out ad);
            } else if (0 == Path.GetExtension(path).CompareTo(".flac")) {
                rv = AudioDataIO.ReadFlacFile(path, out ad);
            } else {
                rv = AudioDataIO.ReadDsfFile(path, out ad);
            }
            return rv;
        }

        public static int ReadWavFile(string path, out AudioData ad) {
            ad = new AudioData();

            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                var reader = new WavRWLib2.WavReader();
                if (!reader.ReadHeaderAndSamples(br, 0, -1)) {
                    MessageBox.Show(
                        string.Format("Error: Failed to read WAV file: {0}", path));
                    return -1;
                }

                ad.meta = new WWFlacRWCS.Metadata();
                ad.meta.albumStr = reader.AlbumName;
                ad.meta.artistStr = reader.ArtistName;
                ad.meta.titleStr = reader.Title;
                ad.meta.pictureBytes = reader.PictureBytes;
                ad.picture = reader.PictureData;
                ad.meta.totalSamples = reader.NumFrames;
                ad.meta.channels = reader.NumChannels;
                ad.meta.sampleRate = reader.SampleRate;

                var interleaved = reader.GetSampleLargeArray();
                int bytesPerSample = reader.BitsPerSample / 8;

                ad.pcm = new List<AudioDataPerChannel>();
                for (int ch = 0; ch < reader.NumChannels; ++ch) {
                    var pcmOneChannel = new WWUtil.LargeArray<byte>(reader.NumFrames * bytesPerSample);
                    for (long i = 0; i < reader.NumFrames; ++i) {
                        for (int b = 0; b < reader.BitsPerSample / 8; ++b) {
                            pcmOneChannel.Set(bytesPerSample * i + b,
                                interleaved.At(bytesPerSample * (reader.NumChannels * i + ch) + b));
                        }
                    }

                    var pcm24 = PcmDataLib.Util.ConvertTo24bit(reader.BitsPerSample, reader.NumFrames,
                            reader.SampleValueRepresentationType, pcmOneChannel);

                    var adp = new AudioDataPerChannel();
                    adp.mData = pcm24;
                    adp.mOffsBytes = 0;
                    adp.mBitsPerSample = 24;
                    adp.mTotalSamples = ad.meta.totalSamples;
                    ad.pcm.Add(adp);
                }

                // converted to 24bit
                ad.meta.bitsPerSample = 24;
                ad.preferredSaveFormat = FileFormatType.FLAC;
                return 0;
            }
        }

        public static int ReadDsfFile(string path, out AudioData ad) {
            ad = new AudioData();

            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                var reader = new WWDsfRW.WWDsfReader();

                PcmDataLib.PcmData header;
                var readResult = reader.ReadHeader(br, out header);
                if (readResult != WWDsfRW.WWDsfReader.ResultType.Success) {
                    MessageBox.Show(string.Format("Error: Failed to read DSF file: {0} {1}", path, readResult));
                    return -1;
                }

                // DoP DSDデータとしての形式が出てくる。
                ad.meta = new WWFlacRWCS.Metadata();
                ad.meta.albumStr = reader.AlbumName;
                ad.meta.artistStr = reader.ArtistName;
                ad.meta.titleStr = reader.TitleName;
                ad.meta.pictureBytes = reader.PictureBytes;
                ad.picture = reader.PictureData;
                ad.meta.totalSamples = reader.OutputFrames; // PCMのフレーム数が出る。
                ad.meta.channels = reader.NumChannels;
                ad.meta.sampleRate = reader.SampleRate; // DSDレートが出る。
            }

            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                var reader = new WWDsfRW.WWDsfReader();

                PcmDataLib.PcmData pcm;
                reader.ReadStreamBegin(br, out pcm);

                var sampleData = new WWUtil.LargeArray<byte>[ad.meta.channels];

                for (int ch = 0; ch < ad.meta.channels; ++ch) {
                    // 24bit == 3bytes per channelのDoPから
                    // 16bitを抽出するので、１サンプルあたり２バイト。
                    sampleData[ch] = new WWUtil.LargeArray<byte>(ad.meta.totalSamples *2);
                }

                const int FRAGMENT_SAMPLES = 1048576;

                // 1チャンネル、1サンプルあたり２バイト入っている。
                for (long sample = 0; sample < ad.meta.totalSamples; sample += FRAGMENT_SAMPLES) {
                    // DoPのフレームが出てくる。リトルエンディアン、３バイトのうち下位２バイトがDSDデータ。

                    int fragmentSamples = FRAGMENT_SAMPLES;
                    if (ad.meta.totalSamples < sample + FRAGMENT_SAMPLES) {
                        fragmentSamples = (int)(ad.meta.totalSamples - sample);
                    }

                    var buff = reader.ReadStreamReadOne(br, fragmentSamples);

                    // ここで全チャンネルがインターリーブされた、１サンプル３バイトのデータが出てくる。
                    // 3バイト目はマーカーバイト。
                    for (int i = 0; i < fragmentSamples; ++i) {
                        for (int ch = 0; ch < ad.meta.channels; ++ch) {
                            // DoPデータはデータがビッグエンディアン詰めされている。
                            // ここでバイトオーダーを入れ替えて、1バイトごとに読めば順にデータが出てくるようにする。
                            // 1バイト内のビットの並びはMSBから古い順にデータが詰まっている。
                            sampleData[ch].Set((sample + i) * 2 + 0, buff[(ad.meta.channels * i + ch) * 3 + 1]);
                            sampleData[ch].Set((sample + i) * 2 + 1, buff[(ad.meta.channels * i + ch) * 3 + 0]);
                        }
                    }
                }

                // DSDデータとして書き込む。
                ad.meta.totalSamples *= 16;
                ad.meta.bitsPerSample = 1;
                ad.preferredSaveFormat = FileFormatType.DSF;
                ad.pcm = new List<AudioDataPerChannel>();

                // AudioDataPerChannelには本当のサンプル数、量子化ビット数をセットする。
                for (int ch = 0; ch < ad.meta.channels; ++ch) {
                    var adp = new AudioDataPerChannel();
                    adp.mData = sampleData[ch];
                    adp.mOffsBytes = 0;
                    adp.mBitsPerSample = 1;
                    adp.mTotalSamples = ad.meta.totalSamples;
                    adp.mDataFormat = AudioDataPerChannel.DataFormat.Sdm1bit;
                    ad.pcm.Add(adp);
                }

            }

            return 0;
        }

        public static int ReadFlacFile(string path, out AudioData ad) {
            ad = new AudioData();

            var flac = new WWFlacRWCS.FlacRW();
            int rv = flac.DecodeAll(path);
            if (rv < 0) {
                MessageBox.Show(
                    string.Format("Error: Failed to read FLAC file: {0}", path));
                return rv;
            }

            rv = flac.GetDecodedMetadata(out ad.meta);
            if (rv < 0) {
                return rv;
            }

            rv = flac.GetDecodedPicture(out ad.picture, ad.meta.pictureBytes);
            if (rv < 0) {
                return rv;
            }

            ad.pcm = new List<AudioDataPerChannel>();
            for (int ch = 0; ch < ad.meta.channels; ++ch) {

                // ■■■ 1チャンネル分のデータを取り出す。■■■
                long totalBytes = ad.meta.totalSamples * ad.meta.bitsPerSample / 8;

                var pcm = new WWUtil.LargeArray<byte>(totalBytes);
                int fragmentBytes = 4096 * ad.meta.bitsPerSample / 8;
                for (long pos = 0; pos < totalBytes; ) {
                    int copyBytes = fragmentBytes;
                    if (pos + fragmentBytes < totalBytes) {
                        copyBytes = (int)(totalBytes - pos);
                    }

                    var fragment = new byte[copyBytes];
                    int lrv = flac.GetDecodedPcmBytes(ch, pos, out fragment, copyBytes);
                    if (lrv < 0) {
                        return lrv;
                    }

                    // fragmentに入っているPCMデータのサイズはlrvバイト。
                    pcm.CopyFrom(fragment, 0, pos, lrv);
                    pos += copyBytes;
                }

                var pcm24 = PcmDataLib.Util.ConvertTo24bit(ad.meta.bitsPerSample,
                    ad.meta.totalSamples, PcmDataLib.PcmData.ValueRepresentationType.SInt, pcm);

                var adp = new AudioDataPerChannel();
                adp.mDataFormat = AudioDataPerChannel.DataFormat.Pcm;
                adp.mData = pcm24;
                adp.mOffsBytes = 0;
                adp.mBitsPerSample = 24;
                adp.mTotalSamples = ad.meta.totalSamples;
                ad.pcm.Add(adp);
            }

            // converted to 24bit
            ad.meta.bitsPerSample = 24;
            ad.preferredSaveFormat = FileFormatType.FLAC;

            flac.DecodeEnd();

            return 0;
        }

        public static int WriteFlacFile(ref AudioData ad, string path) {
            int rv;
            var flac = new WWFlacRWCS.FlacRW();
            rv = flac.EncodeInit(ad.meta);
            if (rv < 0) {
                return rv;
            }

            rv = flac.EncodeSetPicture(ad.picture);
            if (rv < 0) {
                flac.EncodeEnd();
                return rv;
            }

            for (int ch = 0; ch < ad.meta.channels; ++ch) {
                long lrv = flac.EncodeAddPcm(ch, ad.pcm[ch].mData);
                if (lrv < 0) {
                    flac.EncodeEnd();
                    return (int)lrv;
                }
            }

            rv = flac.EncodeRun(path);
            if (rv < 0) {
                flac.EncodeEnd();
                return rv;
            }

            flac.EncodeEnd();
            return 0;
        }

        public static int WriteWavFile(ref AudioData ad, string path) {
            int rv = 0;

            using (var bw = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                bool bRf64 = (2000 * 1000 * 1000 < ad.meta.PcmBytes);
                if (bRf64) {
                    // RF64形式で保存する。
                    WavRWLib2.WavWriter.WriteRF64Header(bw, ad.meta.channels, ad.meta.bitsPerSample, ad.meta.sampleRate, ad.meta.totalSamples);
                    int padBytes = ((ad.meta.PcmBytes & 1) == 1) ? 1 : 0;

                    int bytesPerSample = ad.meta.bitsPerSample / 8;
                    var buff = new byte[bytesPerSample];

                    for (long i = 0; i < ad.meta.totalSamples; ++i) {
                        for (int ch = 0; ch < ad.meta.channels; ++ch) {
                            var from = ad.pcm[ch].mData;
                            for (int b = 0; b < bytesPerSample; ++b) {
                                buff[b] = from.At(i * bytesPerSample + b);
                            }
                            bw.Write(buff);
                        }
                    }

                    if (1 == padBytes) {
                        // チャンクの終わりが偶数になるようにパッドを入れる。
                        byte zero = 0;
                        bw.Write(zero);
                    }
                } else {
                    var sampleArray = new byte[ad.meta.PcmBytes];

                    int bytesPerSample = ad.meta.bitsPerSample/8;
                    int toPos = 0;
                    for (int i=0; i<ad.meta.totalSamples; ++i) {
                        for (int ch = 0; ch < ad.meta.channels; ++ch) {
                            var from = ad.pcm[ch].mData;
                            for (int b = 0; b < bytesPerSample; ++b) {
                                sampleArray[toPos++] = from.At(i * bytesPerSample + b);
                            }
                        }
                    }

                    WavRWLib2.WavWriter.Write(bw, ad.meta.channels, ad.meta.bitsPerSample,
                        ad.meta.sampleRate, ad.meta.totalSamples, sampleArray);
                }
            }

            return rv;
        }

        public static int WriteDsfFile(ref AudioData ad, string path) {
            int rv;
            var dsf = new WWDsfRW.WWDsfWriter();

            rv = dsf.EncodeInit(ad.meta);
            if (rv < 0) {
                return rv;
            }

            rv = dsf.EncodeSetPicture(ad.picture);
            if (rv < 0) {
                dsf.EncodeEnd();
                return rv;
            }

            for (int ch = 0; ch < ad.meta.channels; ++ch) {
                dsf.EncodeAddPcm(ch, ad.pcm[ch].mData);
            }

            rv = dsf.EncodeRun(path);
            if (rv < 0) {
                dsf.EncodeEnd();
                return rv;
            }

            dsf.EncodeEnd();
            return 0;
        }
    };
}
