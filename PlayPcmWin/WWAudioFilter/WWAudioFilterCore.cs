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



        private int SetupResultPcm(AudioData from,  List<FilterBase> filters, out AudioData to, FileFormatType toFileFormat) {
            to = new AudioData();
            to.fileFormat = toFileFormat;

            var fmt = FilterSetup(from, 0, filters);

            to.meta = new WWFlacRWCS.Metadata(from.meta);
            to.meta.sampleRate = fmt.SampleRate;
            to.meta.totalSamples = fmt.NumSamples;
            to.meta.channels = fmt.NumChannels;

            switch (toFileFormat) {
            case FileFormatType.FLAC:
#if true
            to.meta.bitsPerSample = 24;
#endif
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
                byte[] data;

                // set silent sample values to output buffer
                switch (toFileFormat) {
                case FileFormatType.DSF:
                if (0x7FFFFFC7 < (to.meta.totalSamples + 7) / 8) {
                    return (int)WWFlacRWCS.FlacErrorCode.OutputFileTooLarge;
                }
                data = new byte[(to.meta.totalSamples + 7) / 8];
                for (long i = 0; i < data.LongLength; ++i) {
                    data[i] = 0x69;
                }
                break;
                case FileFormatType.FLAC:
                if (0x7FFFFFC7 < to.meta.totalSamples * (to.meta.bitsPerSample / 8)) {
                    return (int)WWFlacRWCS.FlacErrorCode.OutputFileTooLarge;
                }
                data = new byte[to.meta.totalSamples * (to.meta.bitsPerSample / 8)];
                break;
                default:
                System.Diagnostics.Debug.Assert(false);
                data = null;
                break;
                }

                var adp = new AudioDataPerChannel();
                adp.data = data;
                adp.bitsPerSample = to.meta.bitsPerSample;
                adp.totalSamples = to.meta.totalSamples;
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

        private static int ReadFlacFile(string path, out AudioData ad) {
            ad = new AudioData();

            ad.fileFormat = FileFormatType.FLAC;

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
                byte[] data;
                long lrv = flac.GetDecodedPcmBytes(ch, 0, out data, ad.meta.totalSamples * (ad.meta.bitsPerSample / 8));
                if (lrv < 0) {
                    return (int)lrv;
                }

                var adp = new AudioDataPerChannel();
                adp.data = data;
                adp.offsBytes = 0;
                adp.bitsPerSample = ad.meta.bitsPerSample;
                adp.totalSamples = ad.meta.totalSamples;
                ad.pcm.Add(adp);
            }

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
                long lrv = flac.EncodeAddPcm(ch, ad.pcm[ch].data);
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

        private static int WriteDsfFile(ref AudioData ad, string path) {
            int rv;
            var dsf = new WWDsfWriter();

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
                long lrv = dsf.EncodeAddPcm(ch, ad.pcm[ch].data);
                if (lrv < 0) {
                    dsf.EncodeEnd();
                    return (int)lrv;
                }
            }

            rv = dsf.EncodeRun(path);
            if (rv < 0) {
                dsf.EncodeEnd();
                return rv;
            }

            dsf.EncodeEnd();
            return 0;
        }

        private static long CountTotalSamples(List<double[]> data) {
            long count = 0;
            foreach (var k in data) {
                count += k.LongLength;
            }
            return count;
        }

        private static void AssembleSample(List<double[]> dataList, long count, out double[] gathered, out double[] remainings) {
            gathered = new double[count];
            long offs = 0;
            long remainLength = 0;
            foreach (var d in dataList) {
                long length = d.LongLength;
                remainLength = 0;
                if (count < offs + length) {
                    length = count - offs;
                    remainLength = d.LongLength - length;
                }

                Array.Copy(d, 0, gathered, offs, length);
                offs += length;
            }

            remainings = new double[remainLength];
            if (0 < remainLength) {
                long lastDataLength = dataList[dataList.Count - 1].LongLength;
                Array.Copy(dataList[dataList.Count - 1], lastDataLength - remainLength, remainings, 0, remainLength);
            }
        }

        private Barrier mBarrierReady;
        private Barrier mBarrierSet;

        private double[][] mInPcmArray;

        private double[] FilterNth(List<FilterBase> filters, int nth, int channelId,
                ref AudioDataPerChannel from) {
            if (nth == -1) {
                return from.GetPcmInDouble(filters[0].NumOfSamplesNeeded());
            } else {
                // サンプル数が貯まるまでn-1番目のフィルターを実行する。

                List<double[]> inPcmList = new List<double[]>();
                {
                    // 前回フィルタ処理で余った入力データ
                    double[] prevRemainings = filters[nth].GetPreviousProcessRemains();
                    if (prevRemainings != null && 0 < prevRemainings.LongLength) {
                        inPcmList.Add(prevRemainings);
                    }
                }

                while (CountTotalSamples(inPcmList) < filters[nth].NumOfSamplesNeeded()) {
                    inPcmList.Add(FilterNth(filters, nth - 1, channelId, ref from));
                }


                double[] inPcm;
                double[] remainings;
                AssembleSample(inPcmList, filters[nth].NumOfSamplesNeeded(), out inPcm, out remainings);

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
                }

                // n番目のフィルター実行準備が整った。
                // n番目のフィルターを実行する。

                double[] outPcm = filters[nth].FilterDo(inPcm);

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
            while (pos < to.totalSamples) {
                var pcm = FilterNth(filters, filters.Count - 1, channelId, ref from);

                to.SetPcmInDouble(pcm, pos);

                pos += pcm.LongLength;

                long currentSamples = System.Threading.Interlocked.Add(ref mProgressSamples, pcm.LongLength);
                double percent = (double)FILE_READ_COMPLETE_PERCENTAGE
                    + ((double)FILE_PROCESS_COMPLETE_PERCENTAGE - FILE_READ_COMPLETE_PERCENTAGE) * currentSamples / to.totalSamples / nChannels;
                Callback((int)percent, new ProgressArgs("", 0));
            }

            foreach (var f in filters) {
                f.FilterEnd();
            }
            return 0;
        }

        public int Run(string fromPath, List<FilterBase> aFilters, string toPath, ProgressReportCallback Callback) {
            AudioData audioDataFrom;
            AudioData audioDataTo;

            int rv = ReadFlacFile(fromPath, out audioDataFrom);
            if (rv < 0) {
                return rv;
            }

            mBarrierReady = new Barrier(audioDataFrom.meta.channels);
            mBarrierSet = new Barrier(audioDataFrom.meta.channels);
            mInPcmArray = new double[audioDataFrom.meta.channels][];

            Callback(FILE_READ_COMPLETE_PERCENTAGE, new ProgressArgs(Properties.Resources.LogFileReadCompleted, 0));

            var fileFormat = FileFormatType.FLAC;
            if (0 == string.CompareOrdinal(Path.GetExtension(toPath).ToUpperInvariant(), ".DSF")) {
                fileFormat = FileFormatType.DSF;
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

                if (audioDataTo.pcm[ch].overflow) {
                    var s = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorSampleValueClipped,
                            ch, audioDataTo.pcm[ch].maxMagnitude);
                    Callback(-1, new ProgressArgs(s, 0));
                }

                filters = null;
            });

            if (rv < 0) {
                return rv;
            }

            Callback(FILE_PROCESS_COMPLETE_PERCENTAGE, new ProgressArgs(string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogfileWriteStarted, toPath), 0));

            switch (audioDataTo.fileFormat) {
            case FileFormatType.FLAC:
                rv = WriteFlacFile(ref audioDataTo, toPath);
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
