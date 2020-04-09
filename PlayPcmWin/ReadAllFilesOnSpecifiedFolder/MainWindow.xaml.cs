using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace ReadAllFilesOnSpecifiedFolder {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private bool mWindowClosing = false;
        BackgroundWorker mBW = new BackgroundWorker();
        private Stopwatch mStopWatch = new Stopwatch();
        private Stopwatch mStopWatchLogUpdate = new Stopwatch();

        ReadAllFilesOnFolder mReader = null;

        StringBuilder mSbLog = new StringBuilder();
        long mLogCounter = 0;
        long mLogShowCounter = 0;

        class BWArgs {
            public string root;
            public bool isParallel;
            public BWArgs(string aRoot, bool aIsParallel) {
                root = aRoot;
                isParallel = aIsParallel;
            }
        };


        private void mButtonStart_Click(object sender, RoutedEventArgs e) {
            // ログを更新。
            mSbLog.Clear();
            mLogCounter = 0;
            mLogShowCounter = 0;
            mSbLog.AppendFormat("Read Started. IsParallel={0}, Folder={1}\n",
                    mCheckBoxParallelRead.IsChecked== true,
                    mTextBoxFolder.Text);
            mTextBoxLog.Text = mSbLog.ToString();

            mStopWatch.Restart();
            mStopWatchLogUpdate.Restart();

            System.Diagnostics.Debug.Assert(mReader == null);
            mReader = new ReadAllFilesOnFolder(ReadProgressCallback_WindowMode);

            mBW.RunWorkerAsync(new BWArgs(mTextBoxFolder.Text, mCheckBoxParallelRead.IsChecked== true));

            mButtonStart.IsEnabled = false;
            mButtonStop.IsEnabled = true;
        }

        private void ReadProgressCallback_WindowMode(ReadAllFilesOnFolder.EventType ev, string path, string errMsg, int finished, int total) {
            if (mWindowClosing) {
                return;
            }
            
            int progressPercentage = (int)((long)finished * 100 / total);

            switch (ev) {
            case ReadAllFilesOnFolder.EventType.CollectionFinished:
                lock (mSbLog) {
                    mSbLog.AppendFormat("Found {0} files.\n", total);
                }
                Interlocked.Increment(ref mLogCounter);
                mBW.ReportProgress(0);
                break;
            case ReadAllFilesOnFolder.EventType.ReadError:
                lock (mSbLog) {
                    mSbLog.AppendFormat("{0}\n", errMsg);
                }
                Interlocked.Increment(ref mLogCounter);
                break;
            case ReadAllFilesOnFolder.EventType.ReadProgressed:
                if (1000 < mStopWatchLogUpdate.ElapsedMilliseconds) {
                    // 頻度が高いと破綻するので。
                    // 1秒に1回ログ表示を更新する。
                    mBW.ReportProgress(progressPercentage);
                    mStopWatchLogUpdate.Restart();
                }
                break;
            default:
                break;
            }
        }


        void mBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (mWindowClosing || mBW.CancellationPending) {
                return;
            }

            mProgressBar.Value = e.ProgressPercentage;

            long latestCount = Interlocked.Read(ref mLogCounter);
            if (mLogShowCounter < latestCount) {
                lock (mSbLog) {
                    mTextBoxLog.Text = mSbLog.ToString();
                }
                mTextBoxLog.ScrollToEnd();
                mLogShowCounter = latestCount;
            }
        }

        void mBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (mWindowClosing) {
                return;
            }

            if (e.Cancelled) {
                mSbLog.AppendFormat("Task stopped.\n");
            } else {
                mSbLog.AppendFormat("Finished. Elapsed time = {0}\n", mStopWatch.Elapsed);
            }

            mTextBoxLog.Text = mSbLog.ToString();
            mTextBoxLog.ScrollToEnd();

            mProgressBar.Value = 0;
            mButtonStart.IsEnabled = true;
            mButtonStop.IsEnabled = false;

            if (mReader != null) {
                mReader.Dispose();
                mReader = null;
            }
        }

        void mBW_DoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as BWArgs;

            int opt = 0;
            if (args.isParallel) {
                opt |= (int)ReadAllFilesOnFolder.Option.Parallel;
            }

            mReader.Run(args.root, opt);

            if (mBW.CancellationPending) {
                e.Cancel = true;
            }
        }

        private void mButtonStop_Click(object sender, RoutedEventArgs e) {
            mBW.CancelAsync();
            if (mReader != null) {
                mReader.Cancel();
            }
            mButtonStop.IsEnabled = false;
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private void PrintUsage(string programName) {
            Console.WriteLine("Commandline Usage: {0} [-Parallel] \"folderName\"",
                programName);
        }

        private void ReadProgressCallback_Console(ReadAllFilesOnFolder.EventType ev, string path, string errMsg, int finished, int total) {
            double progressPercentage = (double)finished * 100 / total;

            switch (ev) {
            case ReadAllFilesOnFolder.EventType.CollectionFinished:
                Console.WriteLine("Found {0} files.", total);
                break;
            case ReadAllFilesOnFolder.EventType.ReadError:
                Console.WriteLine("{0}", errMsg);
                break;
            case ReadAllFilesOnFolder.EventType.ReadProgressed:
                if (1000 < mStopWatchLogUpdate.ElapsedMilliseconds) {
                    Console.WriteLine("  {0:0.00} %", progressPercentage);
                    mStopWatchLogUpdate.Restart();
                }
                break;
            default:
                break;
            }
        }

        private bool ParseCommandline() {
            var args = System.Environment.GetCommandLineArgs();
            if (2 != args.Length && 3 != args.Length) {
                PrintUsage(args[0]);
                return false;
            }

            string folder = args[args.Length - 1];

            int opt = 0;
            if (args.Length == 3) {
                if (0 != "-Parallel".CompareTo(args[1])) {
                    PrintUsage(args[0]);
                    return false;
                }
                opt = (int)ReadAllFilesOnFolder.Option.Parallel;
            }

            // コマンドライン実行する。
            mStopWatch.Restart();
            mStopWatchLogUpdate.Restart(); 
            Console.WriteLine("IsParallel={0}, Folder={1}", opt, folder);
            var r = new ReadAllFilesOnFolder(ReadProgressCallback_Console);
            r.Run(folder, opt);
            r.Dispose();
            r = null;

            Console.WriteLine("Done. Elapsed time = {0}", mStopWatch.Elapsed);
            return true;

        }

        private void ProcessCommandline() {
            if (ParseCommandline()) {
                Application.Current.Shutdown();
                return;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Title = string.Format("ReadAllFilesOnSpecifiedFolder version {0}", AssemblyVersion);

            mTextBoxFolder.Text = Properties.Settings.Default.FoloderRoot;
            mCheckBoxParallelRead.IsChecked = Properties.Settings.Default.ParallelRead;

            mBW.WorkerReportsProgress = true;
            mBW.WorkerSupportsCancellation = true;
            mBW.DoWork += new DoWorkEventHandler(mBW_DoWork);
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBW_RunWorkerCompleted);
            mBW.ProgressChanged += new ProgressChangedEventHandler(mBW_ProgressChanged);

            ProcessCommandline();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            mWindowClosing = true;

            if (mReader != null) {
                mReader.Cancel();
            }
            mBW.CancelAsync();

            mButtonStart.IsEnabled = false;
            mButtonStop.IsEnabled = false;

            Properties.Settings.Default.ParallelRead = mCheckBoxParallelRead.IsChecked == true;
            Properties.Settings.Default.FoloderRoot = mTextBoxFolder.Text;
            Properties.Settings.Default.Save();
        }

        private void mButtonBrowse_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = mTextBoxFolder.Text;
            if (System.Windows.Forms.DialogResult.OK != dialog.ShowDialog()) {
                return;
            }

            mTextBoxFolder.Text = dialog.SelectedPath;
        }

    }
}
