using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using WWDirectCompute12;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace WWArbitraryResampler {
    public partial class MainWindow : Window {
        private Converter mConv = new Converter();
        public MainWindow() {
            InitializeComponent();
        }

        private StringBuilder mSB = new StringBuilder();
        private void AddLog(string s) {
            mSB.Append(s);
            mTextBoxLog.Text = mSB.ToString();
            mTextBoxLog.ScrollToEnd();
        }

        private void ClearLog() {
            mSB.Clear();
            mTextBoxLog.Text = "";
        }

        private List<int> mAdapterIdxList = new List<int>();

        private void UpdateAdapterList() {
            mConv.UpdateAdapterList();

            mAdapterIdxList.Clear();
            mComboBoxAdapterList.Items.Clear();
            foreach (var item in mConv.AdapterList) {
                mComboBoxAdapterList.Items.Add(
                    string.Format("gpuId={0}: {1}, VideoMem={2}MiB, SharedMem={3}MiB {4} {5}",
                    item.gpuId,
                    item.name,
                    item.videoMemMiB, item.sharedMemMiB,
                    item.remote ? "Remote" : "",
                    item.software ? "Software" : ""));
                mAdapterIdxList.Add(item.gpuId);
            }
            if (0 < mComboBoxAdapterList.Items.Count) {
                mComboBoxAdapterList.SelectedIndex = 0;
                mButtonStart.IsEnabled = true;
            } else {
                MessageBox.Show("Error: No DirectX12 GPU found!",
                    "Error: No DirectX12 GPU Found",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                mButtonStart.IsEnabled = false;
                return;
            }
        }

        private void PrintUsage() {
            string appName = "WWArbitraryResampler";
            Console.WriteLine("Commandline usage: {0} inputPath gpuId pitchScale outputPath", appName);
        }

        /// <summary>
        /// コマンドライン引数が5個の時、設定をコマンドライン引数から得てコンバートします。
        private bool ProcessCommandline() {
            int hr = 0;
            var args = System.Environment.GetCommandLineArgs();
            if (5 != args.Length) {
                PrintUsage();
                return false;
            }

            mConv.Init();
            mConv.UpdateAdapterList();

            string inPath = args[1];

            int gpuId = 0;
            if (!int.TryParse(args[3], out gpuId) || gpuId < 0 || mConv.AdapterList.Count <= gpuId) {
                Console.WriteLine("Error: gpuId should be 0 or larger integer value.");
                PrintUsage();
                return false;
            }

            double pitchScale = 1.0;
            if (!double.TryParse(args[3], out pitchScale) || pitchScale < 0.5 || 2.0 < pitchScale) {
                Console.WriteLine("Error: pitchScale value should be 0.5 <= pitchScale <= 2.0");
                PrintUsage();
                return false;
            }

            string outPath = args[4];

            var ca = new Converter.ConvertArgs(gpuId, inPath, outPath, 1.0 / pitchScale);
            hr = mConv.Convert(ca, null);
            Console.WriteLine("result={0:x}", hr);
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (ProcessCommandline()) {
                Close();
                return;
            }

            mBW = new BackgroundWorker();
            mBW.DoWork += MBW_DoWork;
            mBW.RunWorkerCompleted += MBW_RunWorkerCompleted;
            mBW.WorkerReportsProgress = true;
            mBW.ProgressChanged += MBW_ProgressChanged;

            mConv.Init();
            UpdateAdapterList();
        }

        private void Window_Closed(object sender, System.EventArgs e) {
            mConv.Term();
        }

        private void mButtonInput_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "FLAC files(*.flac)|*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxInput.Text = dlg.FileName;
        }

        private void mButtonOutput_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "FLAC files(*.flac)|*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxOutput.Text = dlg.FileName;
        }

        BackgroundWorker mBW = new BackgroundWorker();

        private void mButtonStart_Click(object sender, RoutedEventArgs e) {
            double pitchScale = 1.0;
            if (!double.TryParse(mTextBoxPitchScale.Text, out pitchScale) || pitchScale < 0.5 || 2.0 < pitchScale) {
                MessageBox.Show("Error: pitch scale value is not valid number. specify value from 0.5 to 2.0", "Error: pitch scale value", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mGroupBoxSettings.IsEnabled = false;
            mButtonStart.IsEnabled = false;
            ClearLog();

            int gpuId = mAdapterIdxList[mComboBoxAdapterList.SelectedIndex];

            var ca = new Converter.ConvertArgs(gpuId, mTextBoxInput.Text, mTextBoxOutput.Text, 1.0/pitchScale);
            mBW.RunWorkerAsync(ca);

        }

        private class WorkerResultArgs {
            public int mHr = 0;
            public WorkerResultArgs(int hr) {
                mHr = hr;
            }
        }

        private Stopwatch mSW = new Stopwatch();

        private void MBW_DoWork(object sender, DoWorkEventArgs e) {
            int hr = 0;
            e.Result = null;
            var ca = e.Argument as Converter.ConvertArgs;

            mSW.Start();

            hr = mConv.Convert(ca, (a) => {
                if (a.mCBT == Converter.EventCallbackTypes.ConvProgress && mSW.ElapsedMilliseconds < 1000) {
                    return;
                }
                mSW.Restart();
                mBW.ReportProgress(a.mProgressPercentage, a);
            });

            mSW.Stop();

            var r = new WorkerResultArgs(hr);
            e.Result = r;
        }
        private void MBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var a = e.UserState as Converter.EventCallbackArgs;

            mProgressBar.Value = e.ProgressPercentage;

            if (a.mCBT == Converter.EventCallbackTypes.ConvProgress) {
                return;
            }

            AddLog(string.Format("{0}.\n", a.mCBT));
        }


        private void MBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as WorkerResultArgs;
            System.Diagnostics.Debug.Assert(r != null);

            mButtonStart.IsEnabled = true;
            mGroupBoxSettings.IsEnabled = true;
            mProgressBar.Value = 0;

            AddLog(string.Format("End. Result = {0:X}\n", r.mHr));
        }

        private void mButtonUpdateAdapterList_Click(object sender, RoutedEventArgs e) {
            UpdateAdapterList();
        }
    }
}
