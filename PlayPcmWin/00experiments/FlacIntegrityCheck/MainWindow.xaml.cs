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
        
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public MainWindow() {
            InitializeComponent();
            mSwLog.Start();

            string programName = string.Format(CultureInfo.CurrentCulture, "FlacIntegrityCheck version {0}", AssemblyVersion);
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

            if (1000 < mSwLog.ElapsedMilliseconds) {
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
        }

        private BackgroundWorker mBw;

        struct BackgroundParams {
            public string path;
            public bool parallelScan;
        };

        struct BackgroundResult {
            public int corrupted;
            public int ok;
        }

        private BackgroundResult mBackgroundResult;

        private void Background_DoWork(object sender, DoWorkEventArgs e) {
            var args = (BackgroundParams)e.Argument;

            mBackgroundResult.corrupted = 0;
            mBackgroundResult.ok = 0;

            mBw.ReportProgress(0, string.Format(Properties.Resources.LogCountingFiles,
                args.path));

            var flacList = CollectFlacFilesOnFolder(args.path);
            mBw.ReportProgress(0, string.Format(Properties.Resources.LogCount + "\n{1}\n",
                flacList.Count,
                Properties.Resources.LogIntegrityChecking));

            int finished = 0;

            if (args.parallelScan) {
                Parallel.For(0, flacList.Count, i => {
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
                            finished, flacList.Count,
                            WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd), path);
                        mBw.ReportProgress((int)(1000000L * finished / flacList.Count), text);
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
                        finished, flacList.Count,
                        WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd), path);
                    mBw.ReportProgress((int)(1000000L * finished / flacList.Count), text);
                }
            }
        }

        private void Background_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            string text = (string)e.UserState;
            AddLog(text);
            progressBar.Value = e.ProgressPercentage;
        }

        private void Background_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            buttonStart.IsEnabled = true;
            groupBoxSettings.IsEnabled = true;
            progressBar.Value = 1000000;
            AddLogLine(string.Format(Properties.Resources.LogFinished, mBackgroundResult.corrupted));
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
            mBw.RunWorkerAsync(args);
        }

        // this code is from https://msdn.microsoft.com/en-us/library/bb513869.aspx
        private List<string> CollectFlacFilesOnFolder(string root) {
            var result = new List<string>();

            var dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root)) {
                throw new ArgumentException("root");
            }
            dirs.Push(root);

            while (dirs.Count > 0) {
                var currentDir = dirs.Pop();
                string[] subDirs;
                try {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                } catch (UnauthorizedAccessException e) {
                    Console.WriteLine(e.Message);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try {
                    files = System.IO.Directory.GetFiles(currentDir);
                } catch (UnauthorizedAccessException e) {
                    Console.WriteLine(e.Message);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e.Message);
                    continue;
                }

                foreach (string file in files) {
                    try {
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        if (String.Equals(".FLAC", fi.Extension.ToUpper(), StringComparison.Ordinal)) {
                            result.Add(fi.FullName);
                            //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                        }
                    } catch (System.IO.FileNotFoundException e) {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }

                foreach (string str in subDirs) {
                    dirs.Push(str);
                }
            }

            return result;
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
