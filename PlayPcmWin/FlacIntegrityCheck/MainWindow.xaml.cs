using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace FlacIntegrityCheck {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const int LOG_UPDATE_INTERVAL_MS = 3000;

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public MainWindow() {
            InitializeComponent();
            mSwLog.Start();

            string programName = string.Format(CultureInfo.CurrentCulture, "WWFlacIntegrityCheck version {0}", AssemblyVersion);
            Title = programName;
            AddLogLine(programName);
            FlushLog();

            SetLocalizedText();
        }

        private StringBuilder mSbLog = new StringBuilder();

        private void AddLogLine(string s) {
            mSbLog.AppendLine(s);
            textBoxLog.Text = mSbLog.ToString();
            textBoxLog.ScrollToEnd();
        }

        private Stopwatch mSwLog = new Stopwatch();

        private void AddLog(string s) {
            mSbLog.Append(s);

            if (LOG_UPDATE_INTERVAL_MS < mSwLog.ElapsedMilliseconds) {
                textBoxLog.Text = mSbLog.ToString();
                textBoxLog.ScrollToEnd();
                mSwLog.Restart();
            }
        }

        private void FlushLog() {
            textBoxLog.Text = mSbLog.ToString();
            textBoxLog.ScrollToEnd();
            mSwLog.Restart();
        }

        private void SetLocalizedText() {
            groupBoxSettings.Header = Properties.Resources.GroupBoxSettings;
            groupBoxDriveType.Header = Properties.Resources.GroupBoxDriveType;
            groupBoxLog.Header = Properties.Resources.GroupBoxLog;
            buttonBrowse.Content = Properties.Resources.ButtonBrowse;
            buttonStart.Content = Properties.Resources.ButtonStart;
            labelFolder.Content = Properties.Resources.LabelFolder;
            radioButtonHdd.Content = Properties.Resources.RadioHdd;
            radioButtonSsd.Content = Properties.Resources.RadioSsd;
            groupBoxLogOutput.Header = Properties.Resources.GroupBoxLogOutput;
            radioButtonOutputConcise.Content = Properties.Resources.RadioLogConcise;
            radioButtonOutputVerbose.Content = Properties.Resources.RadioLogVerbose;
        }

        private BackgroundWorker mBw;

        enum LogOutputMode {
            Concise,
            Verbose,
        };

        struct BackgroundParams {
            public string path;
            public bool parallelScan;
            public LogOutputMode logOutputMode;
        };

        struct BackgroundResult {
            public int corrupted;
            public int ok;
        }

        class ReportProgressArgs {
            public string text;
            public LogOutputMode mode;
            public int ercd;
            public bool flushLog;
            public ReportProgressArgs(string atext, LogOutputMode amode, int aErcd, bool aflushLog) {
                text = atext;
                mode = amode;
                ercd = aErcd;
                flushLog = aflushLog;
            }
        };

        private Stopwatch mStopwatch = new Stopwatch();

        private BackgroundResult mBackgroundResult;

        private void Background_DoWork(object sender, DoWorkEventArgs e) {
            var args = (BackgroundParams)e.Argument;

            mBackgroundResult.corrupted = 0;
            mBackgroundResult.ok = 0;

            mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogCountingFiles,
                args.path), LogOutputMode.Concise, 0, true));

            var flacList = DirectoryUtil.CollectFlacFilesOnFolder(args.path, ".FLAC");
            mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogCount + "\n{1}\n",
                flacList.Length,
                Properties.Resources.LogIntegrityChecking), LogOutputMode.Concise, 0, true));

            int finished = 0;

            if (args.parallelScan) {
                Parallel.For(0, flacList.Length, i => {
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

                    string path = flacList[i];
                    var flacrw = new WWFlacRWCS.FlacRW();
                    int ercd = flacrw.CheckIntegrity(path);
                    lock (mBw) {
                        if (ercd < 0) {
                            ++mBackgroundResult.corrupted;
                        } else {
                            ++mBackgroundResult.ok;
                        }

                        ++finished;

                        string text = string.Format("({0}/{1}) {2} : {3}\n",
                            finished, flacList.Length,
                            WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd), path);

                        mBw.ReportProgress((int)(1000000L * finished / flacList.Length),
                            new ReportProgressArgs(text, LogOutputMode.Verbose, ercd, false));
                    }
                });
            } else {
                foreach (var path in flacList) {
                    var flacrw = new WWFlacRWCS.FlacRW();
                    int ercd = flacrw.CheckIntegrity(path);
                    
                    if (ercd < 0) {
                        ++mBackgroundResult.corrupted;
                    } else {
                        ++mBackgroundResult.ok;
                    } 
                    ++finished;
                    
                    string text = string.Format("({0}/{1}) {2} : {3}\n",
                        finished, flacList.Length,
                        WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd), path);
                    mBw.ReportProgress((int)(1000000L * finished / flacList.Length),
                        new ReportProgressArgs(text, LogOutputMode.Verbose, ercd, false));
                }
            }
        }

        private void Background_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var rpa = (ReportProgressArgs)e.UserState;

            if (radioButtonOutputConcise.IsChecked == true) {
                // 簡潔表示モード。
                if (rpa.mode == LogOutputMode.Concise || rpa.ercd < 0) {
                    // 簡潔表示時に表示するメッセージ、またはエラーメッセージを表示。
                    AddLog(rpa.text);
                }
            } else {
                // 冗長表示モード
                AddLog(rpa.text);
            }

            if (rpa.flushLog) {
                FlushLog();
            }

            progressBar.Value = e.ProgressPercentage;
        }

        private void Background_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            buttonStart.IsEnabled = true;
            groupBoxSettings.IsEnabled = true;
            progressBar.Value = 0;

            mStopwatch.Stop();

            AddLogLine(string.Format(Properties.Resources.LogFinished, mBackgroundResult.corrupted, mStopwatch.Elapsed));

            FlushLog();
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            progressBar.Value = 0;
            buttonStart.IsEnabled = false;
            groupBoxSettings.IsEnabled = false;

            mBackgroundResult = new BackgroundResult();
            mBackgroundResult.corrupted = 0;
            mBackgroundResult.ok = 0;

            mBw = new BackgroundWorker();
            mBw.WorkerReportsProgress = true;
            mBw.DoWork += new DoWorkEventHandler(Background_DoWork);
            mBw.ProgressChanged += new ProgressChangedEventHandler(Background_ProgressChanged);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_RunWorkerCompleted);
            var args = new BackgroundParams();
            args.path = textBoxFolder.Text;
            args.parallelScan = radioButtonSsd.IsChecked == true;
            args.logOutputMode = radioButtonOutputConcise.IsChecked == true ? LogOutputMode.Concise : LogOutputMode.Verbose;
            mBw.RunWorkerAsync(args);

            mStopwatch.Reset();
            mStopwatch.Start();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) {
                return;
            }

            textBoxFolder.Text = dialog.SelectedPath;
        }
    }
}
