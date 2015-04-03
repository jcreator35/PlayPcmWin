using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq;

namespace WWDistortionNoise {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        BackgroundWorker mBackgroundWorker;

        private bool mInitialized = false;

        private enum State {
            NotReady,
            Ready,
            ReadFile,
            Converting,
            WriteFile
        }

        private State mState = State.NotReady;

        private List<FilterBase> mFilters = new List<FilterBase>();

        private const int FILTER_FILE_VERSION = 1;

        private const int FILE_READ_COMPLETE_PERCENTAGE    = 5;
        private const int FILE_PROCESS_COMPLETE_PERCENTAGE = 95;
        private long mProgressSamples = 0;
        
        public MainWindow() {
            InitializeComponent();

            SetLocalizedTextToUI();
            Title = string.Format(CultureInfo.CurrentCulture, "WWDistortionNoise version {0}", AssemblyVersion);

            mBackgroundWorker = new BackgroundWorker();
            mBackgroundWorker.WorkerReportsProgress = true;
            mBackgroundWorker.DoWork += new DoWorkEventHandler(Background_DoWork);
            mBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(Background_ProgressChanged);
            mBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_RunWorkerCompleted);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // dispose managed resources
            }
            // free native resources
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            Update();
        }
        
        private void SetLocalizedTextToUI() {
            buttonFilterAdd.Content = Properties.Resources.ButtonAddNewFilter;
            buttonBrowseInputFile.Content = Properties.Resources.ButtonBrowseB;
            buttonBrowseOutputFile.Content = Properties.Resources.ButtonBrowseR;
            buttonFilterEdit.Content = Properties.Resources.ButtonEditSelected;
            buttonFilterDelete.Content = Properties.Resources.ButtonDeleteSelected;
            buttonFilterLoad.Content = Properties.Resources.ButtonLoadSettings;
            buttonFilterDown.Content = Properties.Resources.ButtonMoveDownSelected;
            buttonFilterUp.Content = Properties.Resources.ButtonMoveUpSelected;
            buttonFilterSaveAs.Content = Properties.Resources.ButtonSaveSettingsAs;
            buttonStartConversion.Content = Properties.Resources.ButtonStartConversion;
            groupBoxFilterSettings.Header = Properties.Resources.GroupFilterSettings;
            groupBoxLog.Header = Properties.Resources.GroupLog;
            groupBoxOutputFile.Header = Properties.Resources.GroupOutputFile;
            groupBoxInputFile.Header = Properties.Resources.GroupInputFile;
        }

        private void listBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateFilterButtons();
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            UpdateFilterSettings();

            switch (mState) {
            case State.NotReady:
                buttonStartConversion.IsEnabled = false;
                break;
            case State.Ready:
                buttonStartConversion.IsEnabled = true;
                break;
            case State.ReadFile:
            case State.Converting:
            case State.WriteFile:
                buttonStartConversion.IsEnabled = false;
                break;
            }
        }

        private void UpdateFilterButtons() {
            switch (mState) {
            case State.NotReady:
            case State.Ready:
                groupBoxFilterSettings.IsEnabled = true;
                if (listBoxFilters.SelectedIndex < 0) {
                    buttonFilterAdd.IsEnabled = true;
                    buttonFilterDelete.IsEnabled = false;
                    buttonFilterEdit.IsEnabled = false;
                    buttonFilterLoad.IsEnabled = true;
                    buttonFilterSaveAs.IsEnabled = false;

                    buttonFilterDown.IsEnabled = false;
                    buttonFilterUp.IsEnabled = false;
                } else {
                    buttonFilterAdd.IsEnabled = true;
                    buttonFilterDelete.IsEnabled = true;
                    buttonFilterEdit.IsEnabled = true;
                    buttonFilterLoad.IsEnabled = true;
                    buttonFilterSaveAs.IsEnabled = true;

                    buttonFilterDown.IsEnabled = listBoxFilters.SelectedIndex != listBoxFilters.Items.Count - 1;
                    buttonFilterUp.IsEnabled = listBoxFilters.SelectedIndex != 0;
                }
                break;
            case State.ReadFile:
            case State.Converting:
            case State.WriteFile:
                groupBoxFilterSettings.IsEnabled = false;
                break;
            }
        }

        private void UpdateFilterSettings() {
            int selectedIdx = listBoxFilters.SelectedIndex;

            listBoxFilters.Items.Clear();
            foreach (var f in mFilters) {
                listBoxFilters.Items.Add(f.ToDescriptionText());
            }

            if (listBoxFilters.Items.Count == 1) {
                // 最初に項目が追加された
                selectedIdx = 0;
            }
            if (0 <= selectedIdx && listBoxFilters.Items.Count <= selectedIdx) {
                // 選択されていた最後の項目が削除された。
                selectedIdx = listBoxFilters.Items.Count - 1;
            }
            listBoxFilters.SelectedIndex = selectedIdx;

            UpdateFilterButtons();
        }

        ////////////////////////////////////////////////////////////////////////////////////////

        struct AudioDataPerChannel {
            public byte [] data;
            public long offsBytes;
            public long totalSamples;
            public int bitsPerSample;
            public bool overflow;
            public double maxMagnitude;

            public void ResetStatistics() {
                overflow = false;
                maxMagnitude = 0.0;
            }

            public double[] GetPcmInDouble(long count) {
                if (totalSamples <= offsBytes / (bitsPerSample / 8) || count <= 0) {
                    return new double[count];
                }

                var result = new double[count];
                var copyCount = result.LongLength;
                if (totalSamples < offsBytes / (bitsPerSample / 8) + copyCount) {
                    copyCount = totalSamples - offsBytes / (bitsPerSample / 8);
                }

                switch (bitsPerSample) {
                case 16:
                    for (long i=0; i < copyCount; ++i) {
                        short v = (short)((data[offsBytes]) + (data[offsBytes + 1] << 8));
                        result[i] = v * (1.0 / 32768.0);
                        offsBytes += 2;
                    }
                    break;

                case 24:
                    for (long i=0; i < copyCount; ++i) {
                        int v = (int)((data[offsBytes] << 8) + (data[offsBytes + 1] << 16) + (data[offsBytes + 2] << 24));
                        result[i] = v * (1.0 / 2147483648.0);
                        offsBytes += 3;
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
                return result;
            }

            public void SetPcmInDouble(double[] pcm, long writeOffs) {
                if (0 != (writeOffs & 7)) {
                    throw new ArgumentException("writeOffs must be multiple of 8");
                }

                var copyCount = pcm.LongLength;
                if (totalSamples < writeOffs + copyCount) {
                    copyCount = totalSamples - writeOffs;
                }

                long writePosBytes;
                switch (bitsPerSample) {
                case 1: {
                        long readPos = 0;

                        // set 1bit data (from LSB to MSB) into 8bit buffer
                        writePosBytes = writeOffs / 8;
                        for (long i=0; i < copyCount / 8; ++i) {
                            byte sampleValue = 0;
                            for (int subPos = 0; subPos < 8; ++subPos) {
                                byte bit = (0 <= pcm[readPos]) ? (byte)(1 << subPos) : (byte)0;
                                sampleValue |= bit;

                                ++readPos;
                            }
                            data[writePosBytes] = sampleValue;
                            ++writePosBytes;
                        }
                    }
                    break;
                case 16:
                    writePosBytes = writeOffs * 2;
                    for (long i=0; i < copyCount; ++i) {
                        short vS = 0;
                        double vD = pcm[i];
                        if (vD < -1.0f) {
                            vS = -32768;

                            overflow = true;
                            if (maxMagnitude < Math.Abs(vD)) {
                                maxMagnitude = Math.Abs(vD);
                            }
                        } else if (1.0f <= vD) {
                            vS = 32767;

                            overflow = true;
                            if (maxMagnitude < Math.Abs(vD)) {
                                maxMagnitude = Math.Abs(vD);
                            }
                        } else {
                            vS = (short)(32768.0 * vD);
                        }

                        data[writePosBytes + 0] = (byte)((vS) & 0xff);
                        data[writePosBytes + 1] = (byte)((vS >> 8) & 0xff);

                        writePosBytes += 2;
                    }
                    break;

                case 24:
                    writePosBytes = writeOffs * 3;
                    for (long i=0; i < copyCount; ++i) {
                        int vI = 0;
                        double vD = pcm[i];
                        if (vD < -1.0f) {
                            vI = Int32.MinValue;

                            overflow = true;
                            if (maxMagnitude < Math.Abs(vD)) {
                                maxMagnitude = Math.Abs(vD);
                            }
                        } else if (1.0f <= vD) {
                            vI = 0x7fffff00;

                            overflow = true;
                            if (maxMagnitude < Math.Abs(vD)) {
                                maxMagnitude = Math.Abs(vD);
                            }
                        } else {
                            vI = (int)(2147483648.0 * vD);
                        }

                        data[writePosBytes + 0] = (byte)((vI >> 8) & 0xff);
                        data[writePosBytes + 1] = (byte)((vI >> 16) & 0xff);
                        data[writePosBytes + 2] = (byte)((vI >> 24) & 0xff);

                        writePosBytes += 3;
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
            }
        };

        struct AudioData {
            public WWFlacRWCS.Metadata meta;
            public List<AudioDataPerChannel> pcm;
            public byte [] picture;
            public FileFormatType fileFormat;
        };

        class RunWorkerArgs {
            public string FromPath { get; set; }
            public string ToPath { get; set; }

            public RunWorkerArgs(string fromPath, string toPath) {
                FromPath = fromPath;
                ToPath = toPath;
            }
        };

        class ProgressArgs {
            public string Message { get; set; }
            public int Result { get; set; }

            public ProgressArgs(string message, int result) {
                Message = message;
                Result = result;
            }
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
            for (int ch=0; ch < ad.meta.channels; ++ch) {
                byte [] data;
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

            for (int ch=0; ch < ad.meta.channels; ++ch) {
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

        private static PcmFormat FilterSetup(AudioData from, List<FilterBase> filters) {
            var fmt = new PcmFormat(from.meta.channels, from.meta.sampleRate, from.meta.totalSamples);
            foreach (var f in filters) {
                fmt = f.Setup(fmt);
            }
            return fmt;
        }

        enum FileFormatType {
            FLAC,
            DSF,
        }

        private int SetupResultPcm(AudioData from, out AudioData to, FileFormatType toFileFormat) {
            to = new AudioData();
            to.fileFormat = toFileFormat;

            var fmt = FilterSetup(from, mFilters);

            to.meta = new WWFlacRWCS.Metadata(from.meta);
            to.meta.sampleRate = fmt.SampleRate;
            to.meta.totalSamples = fmt.NumSamples;
            to.meta.channels = fmt.Channels;

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
            for (int ch=0; ch < to.meta.channels; ++ch) {
                byte [] data;

                // set silent sample values to output buffer
                switch (toFileFormat) {
                case FileFormatType.DSF:
                    if (0x7FFFFFC7 < (to.meta.totalSamples + 7) / 8) {
                        return (int)WWFlacRWCS.FlacErrorCode.OutputFileTooLarge;
                    }
                    data = new byte[(to.meta.totalSamples + 7) / 8];
                    for (long i=0; i < data.LongLength; ++i) {
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

        private double[] FilterNth(List<FilterBase> filters, int nth, ref AudioDataPerChannel from) {
            if (nth == -1) {
                return from.GetPcmInDouble(filters[0].NumOfSamplesNeeded());
            } else {
                // サンプル数が貯まるまでn-1番目のフィルターを実行する。
                // n番目のフィルターを実行する

                List<double[]> inPcmList = new List<double[]>();
                {
                    // 前回フィルタ処理で余った入力データ
                    double [] prevRemainings = filters[nth].GetPreviousProcessRemains();
                    if (prevRemainings != null && 0 < prevRemainings.LongLength) {
                        inPcmList.Add(prevRemainings);
                    }
                }

                while (CountTotalSamples(inPcmList) < filters[nth].NumOfSamplesNeeded()) {
                    inPcmList.Add(FilterNth(filters, nth - 1, ref from));
                }
                double [] inPcm;
                double [] remainings;
                AssembleSample(inPcmList, filters[nth].NumOfSamplesNeeded(), out inPcm, out remainings);
                double [] outPcm = filters[nth].FilterDo(inPcm);

                // length-1番目のフィルター後に余った入力データremainingsをn番目のフィルターにセットする
                filters[nth].SetPreviousProcessRemains(remainings);

                return outPcm;
            }
        }

        private int ProcessAudioFile(List<FilterBase> filters, int nChannels, ref AudioDataPerChannel from, ref AudioDataPerChannel to) {
            foreach (var f in filters) {
                f.FilterStart();
            }

            to.ResetStatistics();
            long pos = 0;
            while (pos < to.totalSamples) {
                var pcm = FilterNth(filters, filters.Count - 1, ref from);

                to.SetPcmInDouble(pcm, pos);

                pos += pcm.LongLength;

                long currentSamples = System.Threading.Interlocked.Add(ref mProgressSamples, pcm.LongLength);

                double percent = (double)FILE_READ_COMPLETE_PERCENTAGE
                        + ((double)FILE_PROCESS_COMPLETE_PERCENTAGE - FILE_READ_COMPLETE_PERCENTAGE) * currentSamples / to.totalSamples / nChannels;
                mBackgroundWorker.ReportProgress((int)percent, new ProgressArgs("", 0));
            }

            foreach (var f in filters) {
                f.FilterEnd();
            }
            return 0;
        }

        void Background_DoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as RunWorkerArgs;
            int rv;
            AudioData audioDataFrom;
            AudioData audioDataTo;

            rv = ReadFlacFile(args.FromPath, out audioDataFrom);
            if (rv < 0) {
                e.Result = rv;
                return;
            }

            mBackgroundWorker.ReportProgress(FILE_READ_COMPLETE_PERCENTAGE, new ProgressArgs(Properties.Resources.LogFileReadCompleted, 0));

            var fileFormat = FileFormatType.FLAC;
            if (0 == string.CompareOrdinal(Path.GetExtension(args.ToPath).ToUpperInvariant(), ".DSF")) {
                fileFormat = FileFormatType.DSF;
            }

            rv = SetupResultPcm(audioDataFrom, out audioDataTo, fileFormat);
            if (rv < 0) {
                e.Result = rv;
                return;
            }

            mProgressSamples = 0;

            Parallel.For(0, audioDataFrom.meta.channels, ch => {
                var filters = new List<FilterBase>();
                foreach (var f in mFilters) {
                    filters.Add(f.CreateCopy());
                }
                FilterSetup(audioDataFrom, filters);

                var from = audioDataFrom.pcm[ch];
                var to = audioDataTo.pcm[ch];
                rv = ProcessAudioFile(filters, audioDataFrom.meta.channels, ref from, ref to);
                if (rv < 0) {
                    e.Result = rv;
                    return;
                }
                audioDataTo.pcm[ch] = to;

                if (audioDataTo.pcm[ch].overflow) {
                    var s = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorSampleValueClipped,
                            ch, audioDataTo.pcm[ch].maxMagnitude);
                    mBackgroundWorker.ReportProgress(-1, new ProgressArgs(s, 0));
                }

                filters = null;
            });


            mBackgroundWorker.ReportProgress(FILE_PROCESS_COMPLETE_PERCENTAGE,
                    new ProgressArgs(string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogfileWriteStarted, args.ToPath), 0));

            switch (audioDataTo.fileFormat) {
            case FileFormatType.FLAC:
                rv = WriteFlacFile(ref audioDataTo, args.ToPath);
                break;
            case FileFormatType.DSF:
                throw new NotImplementedException();
            }

            if (rv < 0) {
                e.Result = rv;
                return;
            }

            mBackgroundWorker.ReportProgress(100, new ProgressArgs("", 0));

            e.Result = rv;
        }

        void Background_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var args = e.UserState as ProgressArgs;

            if (0 <= e.ProgressPercentage) {
                progressBar1.Value = e.ProgressPercentage;
            }

            if (0 < args.Message.Length) {
                textBoxLog.Text += args.Message;
                textBoxLog.ScrollToEnd();
            }
        }

        private static string ErrorCodeToStr(int ercd) {
            switch (ercd) {
            case (int)WWFlacRWCS.FlacErrorCode.DataNotReady:
                return Properties.Resources.FlacErrorDataNotReady;
            case (int)WWFlacRWCS.FlacErrorCode.WriteOpenFailed:
                return Properties.Resources.FlacerrorWriteOpenFailed;
            case (int)WWFlacRWCS.FlacErrorCode.StreamDecoderNewFailed:
                return Properties.Resources.FlacErrorStreamDecoderNewFailed;
            case (int)WWFlacRWCS.FlacErrorCode.StreamDecoderInitFailed:
                return Properties.Resources.FlacErrorStreamDecoderInitFailed;
            case (int)WWFlacRWCS.FlacErrorCode.DecoderProcessFailed:
                return Properties.Resources.FlacErrorDecoderProcessFailed;
            case (int)WWFlacRWCS.FlacErrorCode.LostSync:
                return Properties.Resources.FlacErrorLostSync;
            case (int)WWFlacRWCS.FlacErrorCode.BadHeader:
                return Properties.Resources.FlacErrorBadHeader;
            case (int)WWFlacRWCS.FlacErrorCode.FrameCrcMismatch:
                return Properties.Resources.FlacErrorFrameCrcMismatch;
            case (int)WWFlacRWCS.FlacErrorCode.Unparseable:
                return Properties.Resources.FlacErrorUnparseable;
            case (int)WWFlacRWCS.FlacErrorCode.NumFrameIsNotAligned:
                return Properties.Resources.FlacErrorNumFrameIsNotAligned;
            case (int)WWFlacRWCS.FlacErrorCode.RecvBufferSizeInsufficient:
                return Properties.Resources.FlacErrorRecvBufferSizeInsufficient;
            case (int)WWFlacRWCS.FlacErrorCode.Other:
                return Properties.Resources.FlacErrorOther;
            case (int)WWFlacRWCS.FlacErrorCode.FileReadOpen:
                return Properties.Resources.FlacErrorFileReadOpen;
            case (int)WWFlacRWCS.FlacErrorCode.BufferSizeMismatch:
                return Properties.Resources.FlacErrorBufferSizeMismatch;
            case (int)WWFlacRWCS.FlacErrorCode.MemoryExhausted:
                return Properties.Resources.FlacErrorMemoryExhausted;
            case (int)WWFlacRWCS.FlacErrorCode.Encoder:
                return Properties.Resources.FlacErrorEncoder;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidNumberOfChannels:
                return Properties.Resources.FlacErrorInvalidNumberOfChannels;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidBitsPerSample:
                return Properties.Resources.FlacErrorInvalidBitsPerSample;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidSampleRate:
                return Properties.Resources.FlacErrorInvalidSampleRate;
            case (int)WWFlacRWCS.FlacErrorCode.InvalidMetadata:
                return Properties.Resources.FlacErrorInvalidMetadata;
            case (int)WWFlacRWCS.FlacErrorCode.BadParams:
                return Properties.Resources.FlacErrorBadParams;
            case (int)WWFlacRWCS.FlacErrorCode.IdNotFound:
                return Properties.Resources.FlacErrorIdNotFound;
            case (int)WWFlacRWCS.FlacErrorCode.EncoderProcessFailed:
                return Properties.Resources.FlacErrorEncoderProcessFailed;
            case (int)WWFlacRWCS.FlacErrorCode.OutputFileTooLarge:
                return Properties.Resources.FlacErrorOutputFileTooLarge;
            default:
                return Properties.Resources.FlacErrorOther;
            }
        }

        void Background_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            int rv = (int)e.Result;

            progressBar1.IsEnabled = false;
            progressBar1.Value = 0;

            groupBoxInputFile.IsEnabled = true;
            groupBoxFilterSettings.IsEnabled = true;
            groupBoxOutputFile.IsEnabled = true;
            buttonStartConversion.IsEnabled = true;

            if (rv < 0) {
                var s = string.Format(CultureInfo.CurrentCulture, "{0} {1} {2}\r\n", Properties.Resources.Error, rv, ErrorCodeToStr(rv));
                MessageBox.Show(s, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);

                textBoxLog.Text += s;
                textBoxLog.ScrollToEnd();
            } else {
                textBoxLog.Text += Properties.Resources.LogCompleted;
                textBoxLog.ScrollToEnd();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void buttonFilterAdd_Click(object sender, RoutedEventArgs e) {
            var w = new DistortionNoiseFilterConfiguration(null);
            w.ShowDialog();

            if (true == w.DialogResult) {
                mFilters.Add(w.Filter);
                Update();
                listBoxFilters.SelectedIndex = listBoxFilters.Items.Count - 1;
            }
        }

        private void buttonFilterEdit_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 <= listBoxFilters.SelectedIndex);
            System.Diagnostics.Debug.Assert(listBoxFilters.SelectedIndex < mFilters.Count);

            var w = new DistortionNoiseFilterConfiguration(mFilters[listBoxFilters.SelectedIndex]);
            w.ShowDialog();

            if (true == w.DialogResult) {
                int idx = listBoxFilters.SelectedIndex;
                mFilters.RemoveAt(idx);
                mFilters.Insert(idx, w.Filter);
                Update();
            }
        }

        private void buttonFilterUp_Click(object sender, RoutedEventArgs e) {
            int pos = listBoxFilters.SelectedIndex;
            var tmp = mFilters[pos];
            mFilters.RemoveAt(pos);
            mFilters.Insert(pos - 1, tmp);

            --listBoxFilters.SelectedIndex;

            Update();
        }

        private void buttonFilterDown_Click(object sender, RoutedEventArgs e) {
            int pos = listBoxFilters.SelectedIndex;
            var tmp = mFilters[pos];
            mFilters.RemoveAt(pos);
            mFilters.Insert(pos + 1, tmp);

            ++listBoxFilters.SelectedIndex;

            Update();
        }

        private void buttonFilterDelete_Click(object sender, RoutedEventArgs e) {
            mFilters.RemoveAt(listBoxFilters.SelectedIndex);

            Update();
        }

        private void listBoxFilters_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateFilterButtons();
        }

        private void buttonFilterSaveAs_Click(object sender, RoutedEventArgs e) {
            if (mFilters.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < mFilters.Count());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWWAFilterFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            // 保存する
            try {
                using (StreamWriter w = new StreamWriter(dlg.FileName)) {
                    w.WriteLine("{0} {1}", FILTER_FILE_VERSION, mFilters.Count());
                    foreach (var f in mFilters) {
                        w.WriteLine("{0} {1}", f.FilterType, f.ToSaveText());
                    }
                }
            } catch (IOException ex) {
                MessageBox.Show("{0}", ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show("{0}", ex.Message);
            }
        }

        private void buttonFilterLoad_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterWWAFilterFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            // 読み込む
            try {
                var filters = new List<FilterBase>();

                using (StreamReader r = new StreamReader(dlg.FileName)) {
                    int filterNum = 0;

                    {
                        // ヘッダ部分。バージョン番号とフィルタの個数が入っている。
                        var s = r.ReadLine();
                        s = s.Trim();
                        var tokens = s.Split(null);
                        if (tokens.Length != 2) {
                            MessageBox.Show("Read failed: " + dlg.FileName);
                            return;
                        }
                        int version;
                        if (!Int32.TryParse(tokens[0], out version) || version != FILTER_FILE_VERSION) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorFilterFileVersionMismatch,
                                    FILTER_FILE_VERSION, tokens[0]));
                            return;
                        }

                        if (!Int32.TryParse(tokens[1], out filterNum) || filterNum < 0) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. bad filter count {0}",
                                    tokens[1]));
                            return;
                        }
                    }

                    for (int i=0; i < filterNum; ++i) {
                        var s = r.ReadLine();
                        s = s.Trim();
                        var f = FilterFactory.Create(s);
                        if (null == f) {
                            MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture, "Read failed. line={0}, {1}",
                                    i+2, s));
                        }
                        filters.Add(f);
                    }
                }

                mFilters = filters;
            } catch (IOException ex) {
                MessageBox.Show("{0}", ex.Message);
            } catch (UnauthorizedAccessException ex) {
                MessageBox.Show("{0}", ex.Message);
            }

            Update();
        }

        private void InputFormUpdated() {
            if (0 < textBoxInputFile.Text.Length &&
                    0 < textBoxOutputFile.Text.Length) {
                mState = State.Ready;
            } else {
                mState = State.NotReady;
            }

            Update();
        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterFlacFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWriteAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonStartConversion_Click(object sender, RoutedEventArgs e) {
            if (0 == string.Compare(textBoxInputFile.Text, textBoxOutputFile.Text, StringComparison.Ordinal)) {
                MessageBox.Show(Properties.Resources.ErrorWriteToReadFile, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            textBoxLog.Text = string.Empty;
            textBoxLog.Text += string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogFileReadStarted, textBoxInputFile.Text);
            progressBar1.Value = 0;
            progressBar1.IsEnabled = true;

            groupBoxInputFile.IsEnabled = false;
            groupBoxFilterSettings.IsEnabled = false;
            groupBoxOutputFile.IsEnabled = false;
            buttonStartConversion.IsEnabled = false;

            mBackgroundWorker.RunWorkerAsync(new RunWorkerArgs(textBoxInputFile.Text, textBoxOutputFile.Text));
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            textBoxInputFile.Text = paths[0];
            InputFormUpdated();
        }
    }
}
