using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WWAudioFilter {
    class WWAudioFilterCore {
        private const int FILE_READ_COMPLETE_PERCENTAGE = 5;
        private const int FILE_PROCESS_COMPLETE_PERCENTAGE = 95;

        private const int FILTER_FILE_VERSION = 1;

        private long mProgressSamples = 0;

        public static List<FilterBase> LoadFiltersFromFile(string path) {
            try {
                var filters = new List<FilterBase>();

                using (StreamReader r = new StreamReader(path)) {
                    int filterNum = 0;

                    {
                        // ヘッダ部分。バージョン番号とフィルタの個数が入っている。
                        var s = r.ReadLine();
                        s = s.Trim();
                        var tokens = s.Split(null);
                        if (tokens.Length != 2) {
                            MessageBox.Show("Read failed: " + path);
                            return null;
                        }
                        int version;
                        if (!Int32.TryParse(tokens[0], out version) || version != FILTER_FILE_VERSION) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorFilterFileVersionMismatch,
                                    FILTER_FILE_VERSION, tokens[0]));
                            return null;
                        }

                        if (!Int32.TryParse(tokens[1], out filterNum) || filterNum < 0) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. bad filter count {0}",
                                    tokens[1]));
                            return null;
                        }
                    }

                    for (int i = 0; i < filterNum; ++i) {
                        var s = r.ReadLine();
                        s = s.Trim();
                        var f = FilterFactory.Create(s);
                        if (null == f) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. line={0}, {1}",
                                    i + 2, s));
                        }
                        filters.Add(f);
                    }
                }

                return filters;
            } catch (IOException ex) {
                MessageBox.Show(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show(ex.Message);
            }

            return null;
        }

        public static bool SaveFilteresToFile(List<FilterBase> filters, string path) {
            try {
                using (StreamWriter w = new StreamWriter(path)) {
                    w.WriteLine("{0} {1}", FILTER_FILE_VERSION, filters.Count());
                    foreach (var f in filters) {
                        w.WriteLine("{0} {1}", f.FilterType, f.ToSaveText());
                    }
                }

                return true;
            } catch (IOException ex) {
                MessageBox.Show(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        private int SetupResultPcm(AudioData from, List<FilterBase> filters, out AudioData to, FileFormatType toFileFormat) {
            to = new AudioData();
            to.preferredSaveFormat = toFileFormat;

            var fmt = FilterSetup(from, 0, filters);

            to.meta = new WWFlacRWCS.Metadata(from.meta);
            to.meta.sampleRate = fmt.SampleRate;
            to.meta.totalSamples = fmt.NumSamples;
            to.meta.channels = fmt.NumChannels;

            switch (toFileFormat) {
            case FileFormatType.FLAC:
            case FileFormatType.WAVE:
                to.meta.bitsPerSample = 24;
                break;
            case FileFormatType.DSF:
                to.meta.bitsPerSample = 1;
                break;
            }

            if (from.picture != null) {
                to.picture = new byte[from.picture.Length];
                System.Array.Copy(from.picture, to.picture, to.picture.Length);
            }

            // allocate "to" pcm data
            to.pcm = new List<AudioDataPerChannel>();
            for (int ch = 0; ch < to.meta.channels; ++ch) {
                WWUtil.LargeArray<byte> data;

                // set silent sample values to output buffer
                switch (toFileFormat) {
                case FileFormatType.DSF:
                    data = new WWUtil.LargeArray<byte>((to.meta.totalSamples + 7) / 8);
                    for (long i = 0; i < data.LongLength; ++i) {
                        data.Set(i, 0x69);
                    }
                    break;
                case FileFormatType.FLAC:
                    if (655350 < to.meta.sampleRate) {
                        return (int)WWFlacRWCS.FlacErrorCode.InvalidSampleRate;
                    }
                    data = new WWUtil.LargeArray<byte>(to.meta.totalSamples * (to.meta.bitsPerSample / 8));
                    break;
                case FileFormatType.WAVE:
                    data = new WWUtil.LargeArray<byte>(to.meta.totalSamples * (to.meta.bitsPerSample / 8));
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    data = null;
                    break;
                }

                var adp = new AudioDataPerChannel();
                adp.mDataFormat = AudioDataPerChannel.DataFormat.Pcm;
                adp.mData = data;
                adp.mBitsPerSample = to.meta.bitsPerSample;
                adp.mTotalSamples = to.meta.totalSamples;
                to.pcm.Add(adp);
            }
            return 0;
        }

        private static PcmFormat FilterSetup(AudioData from, int ch, List<FilterBase> filters) {
            var fmt = new PcmFormat(from.meta.channels, ch, from.meta.sampleRate, from.meta.totalSamples);
            foreach (var f in filters) {
                fmt = f.Setup(fmt);
            }
            return fmt;
        }

        private static int ReadWavFile(string path, out AudioData ad) {
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

        private static int ReadDsfFile(string path, out AudioData ad) {
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

        private static int ReadFlacFile(string path, out AudioData ad) {
            ad = new AudioData();

            var flac = new WWFlacRWCS.FlacRW();
            int rv = flac.DecodeAll(path);
            if (rv < 0) {
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

        private static int WriteFlacFile(ref AudioData ad, string path) {
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

        private static int WriteWavFile(ref AudioData ad, string path) {
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

        private static int WriteDsfFile(ref AudioData ad, string path) {
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

        private static long CountTotalSamples(List<WWUtil.LargeArray<double>> data) {
            long count = 0;
            foreach (var k in data) {
                count += k.LongLength;
            }
            return count;
        }

        private static void AssembleSample(List<WWUtil.LargeArray<double> > dataList, long count,
                out WWUtil.LargeArray<double> gathered, out WWUtil.LargeArray<double> remainings) {
            gathered = new WWUtil.LargeArray<double>(count);

            // この関数が呼び出されるときにはdataListにはcountバイト丁度か、
            // countバイトよりも少し多いバイト数を含んでいて配列の個数は丁度必要な数入っている。
            // もしも余剰バイトがあるときはそれは最後の配列に含まれる。

            long offs = 0;
            long remainLength = 0;
            foreach (var d in dataList) {
                long length = d.LongLength;
                remainLength = 0;
                if (count < offs + length) {
                    length = count - offs;
                    remainLength = d.LongLength - length;
                }

                gathered.CopyFrom(d, 0, offs, length);
                offs += length;
            }

            remainings = new WWUtil.LargeArray<double>(remainLength);
            if (0 < remainLength) {
                long lastDataLength = dataList[dataList.Count - 1].LongLength;
                remainings.CopyFrom(dataList[dataList.Count - 1], lastDataLength - remainLength, 0, remainLength);
            }
        }

        private Barrier mBarrierReady;
        private Barrier mBarrierSet;
        private Barrier mBarrierClearInPcm;

        private WWUtil.LargeArray<double>[] mInPcmArray;

        // この処理はチャンネルの数だけ並列実行される。
        private WWUtil.LargeArray<double> FilterNth(List<FilterBase> filters, int nth, int channelId,
                ref AudioDataPerChannel from) {
            if (nth == -1) {
                return from.GetPcmInDouble(filters[0].NumOfSamplesNeeded());
            } else {
                // サンプル数が貯まるまでn-1番目のフィルターを実行する。

                var inPcmList = new List<WWUtil.LargeArray<double>>();
                {
                    // 前回フィルタ処理で余った入力データ
                    var prevRemainings = filters[nth].GetPreviousProcessRemains();
                    if (prevRemainings != null && 0 < prevRemainings.LongLength) {
                        inPcmList.Add(prevRemainings);
                    }
                }

                while (CountTotalSamples(inPcmList) < filters[nth].NumOfSamplesNeeded()) {
                    inPcmList.Add(FilterNth(filters, nth - 1, channelId, ref from));
                }

                // inPcmList → inPcmとremainings
                WWUtil.LargeArray<double> inPcm;
                WWUtil.LargeArray<double> remainings;
                AssembleSample(inPcmList, filters[nth].NumOfSamplesNeeded(), out inPcm, out remainings);

                inPcmList = null;

                if (filters[nth].WaitUntilAllChannelDataAvailable()) {
                    mInPcmArray[channelId] = inPcm;

                    // mInPcmArrayに全てのチャンネルのPCMが集まるまで待つ。
                    mBarrierReady.SignalAndWait();

                    for (int ch = 0; ch < mInPcmArray.Length; ++ch) {
                        filters[nth].SetChannelPcm(ch, mInPcmArray[ch]);
                    }

                    // 全てのスレッドのfiltersのSetChannelPcm()が終わるまで待つ。
                    mBarrierSet.SignalAndWait();

                    if (channelId == 0) {
                        // 0番のスレッドが代表して実行。mInPcmArray配列をクリア。
                        for (int ch = 0; ch < mInPcmArray.Length; ++ch) {
                            mInPcmArray[ch] = null;
                        }
                    }

                    // 0番目のスレッドによってmInPcmArray配列がクリアされるまで全てのスレッドが待つ。
                    mBarrierClearInPcm.SignalAndWait();
                }

                // n番目のフィルター実行準備が整った。
                // n番目のフィルターを実行する。

                var outPcm = filters[nth].FilterDo(inPcm);

                // length-1番目のフィルター後に余った入力データremainingsをn番目のフィルターにセットする
                filters[nth].SetPreviousProcessRemains(remainings);

                return outPcm;
            }
        }

        public class ProgressArgs {
            public string Message { get; set; }
            public int Result { get; set; }

            public ProgressArgs(string message, int result) {
                Message = message;
                Result = result;
            }
        }
        public delegate void ProgressReportCallback(int percentage, ProgressArgs args);

        private int ProcessAudioFile(List<FilterBase> filters, int nChannels, int channelId,
                ref AudioDataPerChannel from, ref AudioDataPerChannel to, ProgressReportCallback Callback) {
            foreach (var f in filters) {
                f.FilterStart();
            }

            to.ResetStatistics();
            long pos = 0;
            while (pos < to.mTotalSamples) {
                var pcm = FilterNth(filters, filters.Count - 1, channelId, ref from);

                to.SetPcmInDouble(pcm, pos);

                pos += pcm.LongLength;

                long currentSamples = System.Threading.Interlocked.Add(ref mProgressSamples, pcm.LongLength);
                double percent = (double)FILE_READ_COMPLETE_PERCENTAGE
                    + ((double)FILE_PROCESS_COMPLETE_PERCENTAGE - FILE_READ_COMPLETE_PERCENTAGE) * currentSamples / to.mTotalSamples / nChannels;
                Callback((int)percent, new ProgressArgs("", 0));
            }

            foreach (var f in filters) {
                f.FilterEnd();
            }
            return 0;
        }

        public int Run(string fromPath, List<FilterBase> aFilters,
                string toPath, ProgressReportCallback Callback) {
            AudioData audioDataFrom;
            AudioData audioDataTo;
            int rv;

            if (0 == Path.GetExtension(fromPath).CompareTo(".wav")) {
                rv = ReadWavFile(fromPath, out audioDataFrom);
            } else if (0 == Path.GetExtension(fromPath).CompareTo(".flac")) {
                rv = ReadFlacFile(fromPath, out audioDataFrom);
            } else {
                rv = ReadDsfFile(fromPath, out audioDataFrom);
            }
            if (rv < 0) {
                return rv;
            }

            mBarrierReady = new Barrier(audioDataFrom.meta.channels);
            mBarrierSet = new Barrier(audioDataFrom.meta.channels);
            mBarrierClearInPcm = new Barrier(audioDataFrom.meta.channels);
            mInPcmArray = new WWUtil.LargeArray<double>[audioDataFrom.meta.channels];

            Callback(FILE_READ_COMPLETE_PERCENTAGE, new ProgressArgs(Properties.Resources.LogFileReadCompleted, 0));

            var fileFormat = FileFormatType.FLAC;
            if (0 == string.CompareOrdinal(Path.GetExtension(toPath).ToUpperInvariant(), ".DSF")) {
                fileFormat = FileFormatType.DSF;
            } else if (0 == string.CompareOrdinal(Path.GetExtension(toPath).ToUpperInvariant(), ".WAV")) {
                fileFormat = FileFormatType.WAVE;
            }

            rv = SetupResultPcm(audioDataFrom, aFilters, out audioDataTo, fileFormat);
            if (rv < 0) {
                return rv;
            }

            {
                // タグの編集
                var tagData = new TagData();
                tagData.Meta = new WWFlacRWCS.Metadata(audioDataTo.meta);
                tagData.Picture = audioDataTo.picture;

                foreach (var f in aFilters) {
                    tagData = f.TagEdit(tagData);
                }

                audioDataTo.meta = tagData.Meta;
                audioDataTo.picture = tagData.Picture;
            }

            Parallel.For(0, audioDataFrom.meta.channels, ch => {
                var filters = new List<FilterBase>();
                foreach (var f in aFilters) {
                    filters.Add(f.CreateCopy());
                }

                FilterSetup(audioDataFrom, ch, filters);

                var from = audioDataFrom.pcm[ch];
                var to = audioDataTo.pcm[ch];
                rv = ProcessAudioFile(filters, audioDataFrom.meta.channels, ch, ref from, ref to, Callback);
                if (rv < 0) {
                    return;
                }
                audioDataTo.pcm[ch] = to;

                if (audioDataTo.pcm[ch].mOverflow) {
                    var s = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorSampleValueClipped,
                            ch, audioDataTo.pcm[ch].mMaxMagnitude);
                    Callback(-1, new ProgressArgs(s, 0));
                }

                filters = null;
            });

            if (rv < 0) {
                return rv;
            }

            Callback(FILE_PROCESS_COMPLETE_PERCENTAGE, new ProgressArgs(string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogfileWriteStarted, toPath), 0));

            switch (audioDataTo.preferredSaveFormat) {
            case FileFormatType.FLAC:
                rv = WriteFlacFile(ref audioDataTo, toPath);
                break;
            case FileFormatType.WAVE:
                rv = WriteWavFile(ref audioDataTo, toPath);
                break;
            case FileFormatType.DSF:
                try {
                    rv = WriteDsfFile(ref audioDataTo, toPath);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
                break;
            }

            return rv;
        }
    }
}
